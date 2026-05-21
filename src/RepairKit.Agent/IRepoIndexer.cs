namespace RepairKit.Agent;

public interface IRepoIndexer
{
    Task<RepoIndexBuildResult> BuildAsync(
        RepairKitConfig config,
        CancellationToken cancellationToken = default);
}
