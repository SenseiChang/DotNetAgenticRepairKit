namespace RepairKit.Agent;

public sealed record AgentToolResult(
    string ToolName,
    bool Succeeded,
    string Summary,
    string OutputJson,
    string? ErrorMessage,
    DateTime StartedUtc,
    DateTime EndedUtc)
{
    public long DurationMs => (long)(EndedUtc - StartedUtc).TotalMilliseconds;
}
