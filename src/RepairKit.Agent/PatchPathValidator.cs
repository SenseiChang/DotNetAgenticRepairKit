namespace RepairKit.Agent;

public static class PatchPathValidator
{
    private static readonly string[] AllowedPrefixes =
    [
        "src/RepairKit.Core/",
        "src/RepairKit.Web/",
        "src/RepairKit.Infrastructure/",
        "tests/"
    ];

    private static readonly string[] BlockedPrefixes =
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

    private static readonly string[] BlockedTerms =
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

        foreach (var blockedPrefix in BlockedPrefixes)
        {
            if (normalizedPath.StartsWith(blockedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Path '{relativePath}' is under blocked directory '{blockedPrefix}'.");
            }
        }

        foreach (var blockedTerm in BlockedTerms)
        {
            if (normalizedPath.Contains(blockedTerm, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Path '{relativePath}' contains blocked term '{blockedTerm}'.");
            }
        }

        if (!AllowedPrefixes.Any(prefix => normalizedPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
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
}

