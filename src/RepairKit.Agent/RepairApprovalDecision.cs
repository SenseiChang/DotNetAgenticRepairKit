namespace RepairKit.Agent;

public sealed record RepairApprovalDecision(
    string RunId,
    DateTime DecidedUtc,
    bool Approved,
    string DecisionText,
    string RiskLevel,
    IReadOnlyList<string> TargetFiles,
    int ChangeCount,
    string Reason);

