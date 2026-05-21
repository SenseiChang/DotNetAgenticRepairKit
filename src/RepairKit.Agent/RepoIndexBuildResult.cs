namespace RepairKit.Agent;

public sealed record RepoIndexBuildResult(
    string IndexFile,
    int IndexedFileCount,
    int SkippedFileCount,
    IReadOnlyList<string> IndexedFiles,
    IReadOnlyList<string> SkippedFiles);
