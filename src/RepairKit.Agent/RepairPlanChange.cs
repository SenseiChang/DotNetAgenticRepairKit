namespace RepairKit.Agent;

public sealed record RepairPlanChange(
    string FilePath,
    string Reason,
    string FullReplacement);

