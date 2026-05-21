namespace RepairKit.Agent;

public sealed record ContextRetrievalRequest(
    RepairKitConfig Config,
    string FailureText,
    IReadOnlyList<string> DetectedKeywords,
    IReadOnlyList<string> RelatedHistoryTargetFiles,
    int MaxFiles);
