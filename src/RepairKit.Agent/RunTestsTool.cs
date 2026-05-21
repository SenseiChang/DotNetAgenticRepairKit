using System.Text.Json;

namespace RepairKit.Agent;

public sealed class RunTestsTool : AgentToolBase
{
    public override string Name => "run_tests";

    public override string Description => "Runs the configured deterministic test command.";

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
        var buildArtifactsFolder = AgentOutputPaths.GetBuildArtifactsFolder(context.Config, context.RunId);
        var buildOutputPath = Path.EndsInDirectorySeparator(buildArtifactsFolder)
            ? buildArtifactsFolder
            : buildArtifactsFolder + Path.DirectorySeparatorChar;
        var command = CommandTemplate.Expand(context.Config.TestCommand, context.Config, context.RunFolder, buildOutputPath);
        var (fileName, arguments) = CommandTemplate.Split(command);
        var result = await context.CommandRunner.RunAsync(fileName, arguments, context.RepoRoot, cancellationToken);
        var outputFile = AgentOutputPaths.GetTestOutputFile(context.Config, context.RunId);
        await File.WriteAllTextAsync(outputFile, TestOutputFormatter.Format(result), cancellationToken);

        return new AgentToolOutput(
            result.ExitCode == 0,
            result.ExitCode == 0 ? "Tests passed." : "Tests failed.",
            JsonSerializer.Serialize(new
            {
                command,
                exitCode = result.ExitCode,
                outputFile,
                result.DurationMs
            }),
            result.ExitCode == 0 ? null : result.StandardError);
    }
}
