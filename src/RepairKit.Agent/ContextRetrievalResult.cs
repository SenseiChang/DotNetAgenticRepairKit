namespace RepairKit.Agent;

public sealed record ContextRetrievalResult(
    IReadOnlyList<string> DetectedKeywords,
    IReadOnlyList<string> IncludedFiles,
    IReadOnlyList<RetrievedContextFile> RetrievedFiles,
    IReadOnlyList<string> ExcludedFiles);
