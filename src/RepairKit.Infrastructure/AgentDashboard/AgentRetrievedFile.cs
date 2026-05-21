namespace RepairKit.Infrastructure.AgentDashboard;

public sealed record AgentRetrievedFile(
    string FilePath,
    int Score,
    IReadOnlyList<string> Reasons);
