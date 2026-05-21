using System.Text.Json;

namespace RepairKit.Agent;

public sealed class ReadArtifactTool : AgentToolBase
{
    private static readonly HashSet<string> AllowedArtifacts = new(StringComparer.OrdinalIgnoreCase)
    {
        "run-summary.json",
        "context-packet.md",
        "context-metadata.json",
        "repair-plan.json",
        "approval-decision.json",
        "patch-application.json",
        "repair-report.md",
        "git-diff.patch",
        "build-output.txt",
        "test-output.txt",
        "validation-build-output.txt",
        "validation-test-output.txt",
        "tool-events.jsonl"
    };

    public override string Name => "read_artifact";

    public override string Description => "Reads a known safe artifact from the current run folder.";

    public override string InputSchemaJson => """
{
  "type": "object",
  "properties": {
    "fileName": { "type": "string" }
  },
  "required": ["fileName"],
  "additionalProperties": false
}
""";

    protected override async Task<AgentToolOutput> ExecuteCoreAsync(
        AgentToolContext context,
        string inputJson,
        CancellationToken cancellationToken)
    {
        var input = JsonSerializer.Deserialize<ReadArtifactInput>(inputJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Input JSON is required.");

        if (!AllowedArtifacts.Contains(input.FileName) ||
            input.FileName.Contains("..", StringComparison.Ordinal) ||
            input.FileName.Contains('/', StringComparison.Ordinal) ||
            input.FileName.Contains('\\', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Artifact name is not allowed.");
        }

        var path = Path.Combine(context.RunFolder, input.FileName);
        if (!File.Exists(path))
        {
            return new AgentToolOutput(
                false,
                "Artifact was not found.",
                JsonSerializer.Serialize(new { path }),
                "Artifact was not found.");
        }

        var content = await File.ReadAllTextAsync(path, cancellationToken);
        return new AgentToolOutput(
            true,
            "Artifact read.",
            JsonSerializer.Serialize(new { path, content }));
    }

    private sealed record ReadArtifactInput(string FileName);
}
