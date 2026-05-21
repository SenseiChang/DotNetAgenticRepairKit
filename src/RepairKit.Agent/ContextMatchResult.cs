namespace RepairKit.Agent;

public sealed record ContextMatchResult(
    IReadOnlyList<string> MatchedKeywords,
    IReadOnlyList<string> IncludedFiles);

