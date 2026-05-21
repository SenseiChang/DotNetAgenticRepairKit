namespace RepairKit.Infrastructure.AgentDashboard;

public sealed record AgentRunDetails(
    AgentRunHistoryEntry? HistoryEntry,
    IReadOnlyList<AgentRunArtifact> Artifacts,
    IReadOnlyList<AgentToolEvent>? ToolEvents = null,
    AgentContextMetadataSummary? ContextMetadata = null);
