namespace RepairKit.Agent;

public interface IRepoIndexStore
{
    Task<bool> ExistsAsync(
        RepairKitConfig config,
        CancellationToken cancellationToken = default);

    Task<RepoIndex> ReadAsync(
        RepairKitConfig config,
        CancellationToken cancellationToken = default);

    Task WriteAsync(
        RepairKitConfig config,
        RepoIndex index,
        CancellationToken cancellationToken = default);
}
