namespace RepairKit.Infrastructure.AgentDashboard;

public sealed record AgentToolEvent(
    string ToolName,
    DateTime StartedUtc,
    DateTime EndedUtc,
    long DurationMs,
    bool Succeeded,
    string Summary,
    string? ErrorMessage);
