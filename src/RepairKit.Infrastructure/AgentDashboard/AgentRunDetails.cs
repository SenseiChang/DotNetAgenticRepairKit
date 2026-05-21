namespace RepairKit.Infrastructure.AgentDashboard;

public sealed record AgentRunDetails(
    AgentRunHistoryEntry? HistoryEntry,
    IReadOnlyList<AgentRunArtifact> Artifacts);

