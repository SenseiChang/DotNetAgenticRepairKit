namespace RepairKit.Infrastructure.AgentDashboard;

public sealed record RepoIndexStatus(
    bool Exists,
    string IndexPath,
    int? IndexedFileCount,
    DateTime? GeneratedUtc,
    string? Message);
