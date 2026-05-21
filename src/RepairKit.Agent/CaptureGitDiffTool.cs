using System.Text.Json;

namespace RepairKit.Agent;

public sealed class CaptureGitDiffTool : AgentToolBase
{
    public override string Name => "capture_git_diff";

    public override string Description => "Captures git diff output for source and test changes.";

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
        var result = await new GitDiffCapture(context.CommandRunner).CaptureAsync(
            context.Config,
            context.RunId,
            cancellationToken);

        return new AgentToolOutput(
            result.Succeeded,
            result.Succeeded ? "Git diff captured." : "Git diff capture failed.",
            JsonSerializer.Serialize(new
            {
                diffFile = result.DiffFile,
                errorFile = result.ErrorFile
            }),
            result.ErrorMessage);
    }
}
