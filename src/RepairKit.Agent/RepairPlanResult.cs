namespace RepairKit.Agent;

public sealed record RepairPlanResult(
    RepairPlan Plan,
    string Model,
    string RepairPlanPath,
    string ModelRequestPath,
    string ModelResponsePath,
    int? PromptTokens,
    int? CompletionTokens,
    int? TotalTokens);
