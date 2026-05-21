namespace RepairKit.Agent;

public sealed record RepoIndexOptions(
    string RepoRoot,
    string IndexFile,
    IReadOnlyList<string> AllowedPaths,
    IReadOnlyList<string> BlockedPathSegments,
    IReadOnlyList<string> BlockedPathTerms,
    IReadOnlyList<string> IndexedExtensions);
