using System.Text.Json;

namespace RepairKit.Agent;

public sealed class BuildSolutionTool : AgentToolBase
{
    public override string Name => "build_solution";

    public override string Description => "Runs the configured deterministic solution build command.";

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
        Directory.CreateDirectory(buildArtifactsFolder);
        var buildOutputPath = Path.EndsInDirectorySeparator(buildArtifactsFolder)
            ? buildArtifactsFolder
            : buildArtifactsFolder + Path.DirectorySeparatorChar;
        var command = CommandTemplate.Expand(context.Config.BuildCommand, context.Config, context.RunFolder, buildOutputPath);
        var (fileName, arguments) = CommandTemplate.Split(command);
        var result = await context.CommandRunner.RunAsync(fileName, arguments, context.RepoRoot, cancellationToken);
        var outputFile = AgentOutputPaths.GetBuildOutputFile(context.Config, context.RunId);
        await File.WriteAllTextAsync(outputFile, TestOutputFormatter.Format(result), cancellationToken);

        return new AgentToolOutput(
            result.ExitCode == 0,
            result.ExitCode == 0 ? "Build passed." : "Build failed.",
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
