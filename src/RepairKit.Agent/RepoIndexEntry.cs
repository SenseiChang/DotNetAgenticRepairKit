namespace RepairKit.Agent;

public sealed record RepoIndexEntry(
    string FilePath,
    string FileName,
    string Extension,
    long SizeBytes,
    string ContentHash,
    IReadOnlyList<string> DeclaredTypes,
    IReadOnlyList<string> Namespaces,
    IReadOnlyList<string> Keywords,
    string Snippet,
    DateTime IndexedUtc);
