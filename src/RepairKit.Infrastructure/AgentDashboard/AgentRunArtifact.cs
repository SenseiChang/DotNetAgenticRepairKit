namespace RepairKit.Infrastructure.AgentDashboard;

public sealed record AgentRunArtifact(
    string Name,
    string FileName,
    bool Exists,
    string Content);

