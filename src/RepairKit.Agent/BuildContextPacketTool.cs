using System.Text.Json;

namespace RepairKit.Agent;

public sealed class BuildContextPacketTool : AgentToolBase
{
    private readonly ContextBuilder _contextBuilder;

    public BuildContextPacketTool(ContextBuilder contextBuilder)
    {
        _contextBuilder = contextBuilder;
    }

    public override string Name => "build_context_packet";

    public override string Description => "Builds deterministic failure context and metadata.";

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
        var summary = await RunSummaryJsonSerializer.ReadAsync(
            AgentOutputPaths.GetRunSummaryFile(context.Config, context.RunId),
            cancellationToken);
        var metadata = await _contextBuilder.BuildAsync(context.Config, context.RunId, summary, cancellationToken);
        return new AgentToolOutput(
            true,
            $"Context packet generated with {metadata.IncludedFiles.Count} included files.",
            JsonSerializer.Serialize(new
            {
                contextPacketFile = metadata.ContextPacketFile,
                contextMetadataFile = AgentOutputPaths.GetContextMetadataFile(context.Config, context.RunId),
                includedFileCount = metadata.IncludedFiles.Count,
                metadata.RetrievalMode
            }));
    }
}
