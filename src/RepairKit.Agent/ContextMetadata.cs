namespace RepairKit.Agent;

public sealed record ContextMetadata(
    string RunId,
    DateTime GeneratedUtc,
    bool DeterministicMatchesFound,
    IReadOnlyList<string> MatchedKeywords,
    IReadOnlyList<string> IncludedFiles,
    string BuildOutputFile,
    string TestOutputFile,
    string ContextPacketFile,
    string RetrievalMode = "fallback-keyword",
    string? IndexFile = null,
    IReadOnlyList<RetrievedContextFile>? RetrievedFiles = null,
    IReadOnlyList<string>? ExcludedFiles = null,
    bool Truncated = false);
