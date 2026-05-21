namespace RepairKit.Agent;

public static class PatchPathValidator
{
    public static readonly string[] DefaultAllowedPrefixes =
    [
        "src/RepairKit.Core/",
        "src/RepairKit.Web/",
        "src/RepairKit.Infrastructure/",
        "tests/"
    ];

    public static readonly string[] DefaultBlockedPrefixes =
    [
        ".git/",
        ".agent/",
        "bin/",
        "obj/",
        ".vs/",
        "node_modules/",
        "scripts/",
        "docs/"
    ];

    public static readonly string[] DefaultBlockedTerms =
    [
        ".env",
        "secret",
        "token",
        "password",
        "key",
        "appsettings"
    ];

    public static IReadOnlyList<string> ValidateRelativePath(string repoRoot, string relativePath)
    {
        return ValidateRelativePath(
            repoRoot,
            relativePath,
            DefaultAllowedPrefixes,
            DefaultBlockedPrefixes,
            DefaultBlockedTerms);
    }

    public static IReadOnlyList<string> ValidateRelativePath(
        string repoRoot,
        string relativePath,
        IReadOnlyList<string> allowedPrefixes,
        IReadOnlyList<string> blockedPrefixes,
        IReadOnlyList<string> blockedTerms)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            errors.Add("Path must not be empty.");
            return errors;
        }

        var normalizedPath = relativePath.Replace('\\', '/');

        if (Path.IsPathRooted(relativePath) || normalizedPath.StartsWith("/", StringComparison.Ordinal))
        {
            errors.Add($"Path '{relativePath}' must not be absolute.");
        }

        if (normalizedPath.Split('/').Any(segment => segment == ".."))
        {
            errors.Add($"Path '{relativePath}' must not traverse upward using '..'.");
        }

        foreach (var blockedPrefix in blockedPrefixes.Select(NormalizeBlockedPrefix))
        {
            if (normalizedPath.StartsWith(blockedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Path '{relativePath}' is under blocked directory '{blockedPrefix}'.");
            }
        }

        foreach (var blockedTerm in blockedTerms)
        {
            if (normalizedPath.Contains(blockedTerm, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Path '{relativePath}' contains blocked term '{blockedTerm}'.");
            }
        }

        if (!allowedPrefixes.Any(prefix => normalizedPath.StartsWith(NormalizeAllowedPrefix(prefix), StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add($"Path '{relativePath}' is not under an allowed source or test directory.");
        }

        var fullPath = Path.GetFullPath(Path.Combine(repoRoot, normalizedPath));
        var fullRepoRoot = Path.GetFullPath(repoRoot);
        if (!fullRepoRoot.EndsWith(Path.DirectorySeparatorChar))
        {
            fullRepoRoot += Path.DirectorySeparatorChar;
        }

        if (!fullPath.StartsWith(fullRepoRoot, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"Path '{relativePath}' resolves outside the repository root.");
        }

        return errors;
    }

    private static string NormalizeAllowedPrefix(string prefix)
    {
        return prefix.Replace('\\', '/').TrimStart('/');
    }

    private static string NormalizeBlockedPrefix(string prefix)
    {
        var normalized = prefix.Replace('\\', '/').TrimStart('/');
        return normalized.EndsWith('/') ? normalized : normalized + "/";
    }
}
