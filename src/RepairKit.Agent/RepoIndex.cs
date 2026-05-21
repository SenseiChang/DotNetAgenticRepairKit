namespace RepairKit.Agent;

public sealed record RepoIndex(
    DateTime GeneratedUtc,
    string RepoRoot,
    IReadOnlyList<RepoIndexEntry> Entries);
