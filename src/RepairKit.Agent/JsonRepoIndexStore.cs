namespace RepairKit.Agent;

public sealed class JsonRepoIndexStore : IRepoIndexStore
{
    public Task<bool> ExistsAsync(
        RepairKitConfig config,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(File.Exists(AgentOutputPaths.GetRepoIndexFile(config)));
    }

    public async Task<RepoIndex> ReadAsync(
        RepairKitConfig config,
        CancellationToken cancellationToken = default)
    {
        return await RepoIndexJsonSerializer.ReadAsync(
            AgentOutputPaths.GetRepoIndexFile(config),
            cancellationToken);
    }

    public async Task WriteAsync(
        RepairKitConfig config,
        RepoIndex index,
        CancellationToken cancellationToken = default)
    {
        await RepoIndexJsonSerializer.WriteAsync(
            AgentOutputPaths.GetRepoIndexFile(config),
            index,
            cancellationToken);
    }
}
