using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace RepairKit.Agent;

public sealed class RepoIndexer
{
    private static readonly Regex TypeRegex = new(
        @"\b(class|record|struct|enum|interface)\s+([A-Za-z_][A-Za-z0-9_]*)",
        RegexOptions.Compiled);

    private static readonly Regex NamespaceRegex = new(
        @"\bnamespace\s+([A-Za-z_][A-Za-z0-9_.]*)",
        RegexOptions.Compiled);

    private static readonly Regex RazorPageRegex = new(
        @"@page\s+""([^""]+)""",
        RegexOptions.Compiled);

    private static readonly Regex IdentifierRegex = new(
        @"[A-Za-z_][A-Za-z0-9_]*",
        RegexOptions.Compiled);

    private static readonly HashSet<string> CommonKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "public", "private", "protected", "internal", "sealed", "static", "readonly", "using",
        "namespace", "class", "record", "struct", "enum", "interface", "void", "string", "int",
        "bool", "true", "false", "new", "return", "var", "async", "await", "task", "list",
        "get", "set", "if", "else", "foreach", "for", "while", "switch", "case", "break",
        "default", "null", "this", "base", "inheritdoc", "summary"
    };

    public async Task<RepoIndexBuildResult> BuildAsync(
        RepairKitConfig config,
        CancellationToken cancellationToken = default)
    {
        return await BuildAsync(
            new RepoIndexOptions(
                config.ResolvedRepoRoot,
                AgentOutputPaths.GetRepoIndexFile(config),
                config.AllowedEditPaths,
                config.BlockedPathSegments,
                config.BlockedPathTerms,
                config.IndexedExtensions),
            cancellationToken);
    }

    public async Task<RepoIndexBuildResult> BuildAsync(
        RepoIndexOptions options,
        CancellationToken cancellationToken = default)
    {
        var entries = new List<RepoIndexEntry>();
        var indexedFiles = new List<string>();
        var skippedFiles = new List<string>();
        var indexedExtensions = options.IndexedExtensions
            .Select(value => value.StartsWith('.') ? value : "." + value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var file in Directory.EnumerateFiles(options.RepoRoot, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath = NormalizePath(Path.GetRelativePath(options.RepoRoot, file));
            if (!ShouldIndex(relativePath, indexedExtensions, options))
            {
                skippedFiles.Add(relativePath);
                continue;
            }

            var contents = await File.ReadAllTextAsync(file, cancellationToken);
            var info = new FileInfo(file);
            var declaredTypes = GetDeclaredTypes(relativePath, contents);
            var namespaces = Path.GetExtension(relativePath).Equals(".cs", StringComparison.OrdinalIgnoreCase)
                ? NamespaceRegex.Matches(contents).Select(match => match.Groups[1].Value).Distinct().OrderBy(value => value).ToArray()
                : [];
            var keywords = ExtractKeywords(relativePath, contents, declaredTypes, namespaces);

            entries.Add(new RepoIndexEntry(
                relativePath,
                Path.GetFileName(relativePath),
                Path.GetExtension(relativePath),
                info.Length,
                Hash(contents),
                declaredTypes,
                namespaces,
                keywords,
                CreateSnippet(contents),
                DateTime.UtcNow));
            indexedFiles.Add(relativePath);
        }

        var index = new RepoIndex(
            DateTime.UtcNow,
            options.RepoRoot,
            entries.OrderBy(entry => entry.FilePath, StringComparer.OrdinalIgnoreCase).ToArray());

        await RepoIndexJsonSerializer.WriteAsync(options.IndexFile, index, cancellationToken);

        return new RepoIndexBuildResult(
            options.IndexFile,
            entries.Count,
            skippedFiles.Count,
            indexedFiles.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray(),
            skippedFiles.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private static bool ShouldIndex(
        string relativePath,
        HashSet<string> indexedExtensions,
        RepoIndexOptions options)
    {
        if (string.IsNullOrWhiteSpace(relativePath) ||
            relativePath.Contains("..", StringComparison.Ordinal) ||
            relativePath.EndsWith(".local.cmd", StringComparison.OrdinalIgnoreCase) ||
            relativePath.Equals("set-agent-env.local.cmd", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var extension = Path.GetExtension(relativePath);
        if (!indexedExtensions.Contains(extension))
        {
            return false;
        }

        if (extension.Equals(".json", StringComparison.OrdinalIgnoreCase) &&
            !relativePath.Equals("repairkit.config.json", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (IsBlocked(relativePath, options.BlockedPathSegments, options.BlockedPathTerms))
        {
            return false;
        }

        if (relativePath.Equals("repairkit.config.json", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return options.AllowedPaths.Any(allowed =>
            relativePath.StartsWith(NormalizePath(allowed), StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsBlocked(
        string relativePath,
        IEnumerable<string> blockedSegments,
        IEnumerable<string> blockedTerms)
    {
        var parts = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (blockedSegments.Any(segment =>
                parts.Any(part => string.Equals(part, segment, StringComparison.OrdinalIgnoreCase))))
        {
            return true;
        }

        return blockedTerms.Any(term => relativePath.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<string> GetDeclaredTypes(string relativePath, string contents)
    {
        if (Path.GetExtension(relativePath).Equals(".cs", StringComparison.OrdinalIgnoreCase))
        {
            return TypeRegex.Matches(contents)
                .Select(match => match.Groups[2].Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value)
                .ToArray();
        }

        if (Path.GetExtension(relativePath).Equals(".razor", StringComparison.OrdinalIgnoreCase))
        {
            var pageRoutes = RazorPageRegex.Matches(contents)
                .Select(match => match.Groups[1].Value)
                .ToArray();

            return pageRoutes.Length == 0 ? [] : pageRoutes;
        }

        return [];
    }

    private static IReadOnlyList<string> ExtractKeywords(
        string relativePath,
        string contents,
        IReadOnlyList<string> declaredTypes,
        IReadOnlyList<string> namespaces)
    {
        var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddKeywordParts(keywords, Path.GetFileNameWithoutExtension(relativePath));

        foreach (var type in declaredTypes)
        {
            AddKeywordParts(keywords, type);
        }

        foreach (var ns in namespaces)
        {
            foreach (var part in ns.Split('.', StringSplitOptions.RemoveEmptyEntries))
            {
                AddKeywordParts(keywords, part);
            }
        }

        foreach (Match match in IdentifierRegex.Matches(contents))
        {
            AddKeywordParts(keywords, match.Value);
        }

        return keywords
            .Where(value => value.Length > 2 && !CommonKeywords.Contains(value))
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .Take(80)
            .ToArray();
    }

    private static void AddKeywordParts(HashSet<string> keywords, string value)
    {
        foreach (var part in SplitIdentifier(value))
        {
            if (part.Length > 2 && !CommonKeywords.Contains(part))
            {
                keywords.Add(part.ToLowerInvariant());
            }
        }
    }

    private static IEnumerable<string> SplitIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            yield break;
        }

        foreach (var token in Regex.Split(value, @"[^A-Za-z0-9]+"))
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                continue;
            }

            foreach (Match match in Regex.Matches(token, @"[A-Z]?[a-z]+|[A-Z]+(?![a-z])|\d+"))
            {
                yield return match.Value;
            }
        }
    }

    private static string CreateSnippet(string contents)
    {
        var normalized = contents.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
        return normalized.Length <= 500 ? normalized : normalized[..500];
    }

    private static string Hash(string contents)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(contents));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').TrimStart('/');
    }
}
