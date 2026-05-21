namespace RepairKit.Agent;

public sealed record RepairPlan(
    string Summary,
    string RiskLevel,
    IReadOnlyList<string> TargetFiles,
    IReadOnlyList<RepairPlanChange> Changes,
    IReadOnlyList<string> ValidationCommands);

