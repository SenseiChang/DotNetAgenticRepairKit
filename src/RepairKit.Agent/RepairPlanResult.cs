namespace RepairKit.Agent;

public sealed record RepairPlanResult(
    RepairPlan Plan,
    string Model,
    string RepairPlanPath,
    string ModelRequestPath,
    string ModelResponsePath);

