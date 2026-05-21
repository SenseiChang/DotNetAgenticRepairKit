using System.Text.Json;

namespace RepairKit.Agent;

public sealed class BuildRepoIndexTool : AgentToolBase
{
    private readonly IRepoIndexer _repoIndexer;

    public BuildRepoIndexTool(IRepoIndexer repoIndexer)
    {
        _repoIndexer = repoIndexer;
    }

    public override string Name => "build_repo_index";

    public override string Description => "Builds the deterministic local repository index.";

    public override string InputSchemaJson => """
{
  "type": "object",
  "properties": {},
  "additionalProperties": false
}
""";

    protected override async Task<AgentToolOutput> ExecuteCoreAsync(
        AgentToolContext context,
        string inputJson,
        CancellationToken cancellationToken)
    {
        var result = await _repoIndexer.BuildAsync(context.Config, cancellationToken);
        return new AgentToolOutput(
            true,
            $"Indexed {result.IndexedFileCount} files.",
            JsonSerializer.Serialize(new
            {
                indexFile = result.IndexFile,
                indexedFileCount = result.IndexedFileCount,
                skippedFileCount = result.SkippedFileCount
            }));
    }
}
