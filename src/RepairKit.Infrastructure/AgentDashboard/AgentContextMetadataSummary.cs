namespace RepairKit.Infrastructure.AgentDashboard;

public sealed record AgentContextMetadataSummary(
    bool Exists,
    string RetrievalMode,
    string? IndexFile,
    int? MaxContextCharacters,
    int? ActualContextCharacters,
    bool? Truncated,
    IReadOnlyList<string> DetectedKeywords,
    IReadOnlyList<string> IncludedFiles,
    IReadOnlyList<string> ExcludedFiles,
    IReadOnlyList<AgentRetrievedFile> RetrievedFiles,
    string? Message);
