using System.Text;

namespace RepairKit.Agent;

public sealed class ContextBuilder
{
    public async Task<ContextMetadata> BuildAsync(
        string repoRoot,
        string runId,
        RunSummary summary,
        CancellationToken cancellationToken = default)
    {
        return await BuildAsync(RepairKitConfig.CreateUnvalidatedDefault(repoRoot), runId, summary, cancellationToken);
    }

    public async Task<ContextMetadata> BuildAsync(
        RepairKitConfig config,
        string runId,
        RunSummary summary,
        CancellationToken cancellationToken = default)
    {
        var buildOutputFile = AgentOutputPaths.GetBuildOutputFile(config, runId);
        var testOutputFile = AgentOutputPaths.GetTestOutputFile(config, runId);
        var contextPacketFile = AgentOutputPaths.GetContextPacketFile(config, runId);
        var contextMetadataFile = AgentOutputPaths.GetContextMetadataFile(config, runId);

        var buildOutput = Truncate(await ReadIfExistsAsync(buildOutputFile, cancellationToken), config.MaxContextCharacters);
        var testOutput = Truncate(await ReadIfExistsAsync(testOutputFile, cancellationToken), config.MaxContextCharacters);
        var matchResult = ContextFileMatcher.Match(buildOutput + Environment.NewLine + testOutput);

        var packet = await CreatePacketAsync(
            config.ResolvedRepoRoot,
            runId,
            summary,
            buildOutput,
            testOutput,
            matchResult,
            cancellationToken);

        await File.WriteAllTextAsync(contextPacketFile, packet, cancellationToken);

        var metadata = new ContextMetadata(
            runId,
            DateTime.UtcNow,
            matchResult.IncludedFiles.Count > 0,
            matchResult.MatchedKeywords,
            matchResult.IncludedFiles,
            buildOutputFile,
            testOutputFile,
            contextPacketFile);

        await ContextMetadataJsonSerializer.WriteAsync(contextMetadataFile, metadata);

        return metadata;
    }

    private static string Truncate(string value, int maxCharacters)
    {
        if (maxCharacters <= 0 || value.Length <= maxCharacters)
        {
            return value;
        }

        return value[..maxCharacters] + Environment.NewLine + "[truncated]";
    }

    private static async Task<string> CreatePacketAsync(
        string repoRoot,
        string runId,
        RunSummary summary,
        string buildOutput,
        string testOutput,
        ContextMatchResult matchResult,
        CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Agent Context Packet");
        builder.AppendLine();
        builder.AppendLine("## Purpose");
        builder.AppendLine("This packet contains deterministic build/test failure context for a future AI repair planner.");
        builder.AppendLine();
        builder.AppendLine("## Run Information");
        builder.AppendLine($"- Run ID: {runId}");
        builder.AppendLine($"- Started UTC: {summary.StartedUtc:O}");
        builder.AppendLine($"- Build passed: {summary.BuildPassed}");
        builder.AppendLine($"- Tests passed: {summary.TestsPassed}");
        builder.AppendLine($"- Overall passed: {summary.OverallPassed}");
        builder.AppendLine();
        builder.AppendLine("## Build Output");
        builder.AppendLine();
        builder.AppendLine(string.IsNullOrWhiteSpace(buildOutput) ? "No build output was available." : buildOutput);
        builder.AppendLine();
        builder.AppendLine("## Test Output");
        builder.AppendLine();
        builder.AppendLine(string.IsNullOrWhiteSpace(testOutput) ? "No test output was available." : testOutput);
        builder.AppendLine();
        builder.AppendLine("## Included Files");
        builder.AppendLine();

        if (matchResult.IncludedFiles.Count == 0)
        {
            builder.AppendLine("No deterministic source file matches were found.");
            return builder.ToString();
        }

        foreach (var relativePath in matchResult.IncludedFiles)
        {
            builder.AppendLine($"- {relativePath}");
        }

        foreach (var relativePath in matchResult.IncludedFiles)
        {
            var fullPath = Path.Combine(repoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            builder.AppendLine();
            builder.AppendLine($"### File: {relativePath}");
            builder.AppendLine();
            builder.AppendLine("```csharp");

            if (File.Exists(fullPath))
            {
                builder.AppendLine(await File.ReadAllTextAsync(fullPath, cancellationToken));
            }
            else
            {
                builder.AppendLine($"// File not found: {relativePath}");
            }

            builder.AppendLine("```");
        }

        return builder.ToString();
    }

    private static async Task<string> ReadIfExistsAsync(
        string path,
        CancellationToken cancellationToken)
    {
        return File.Exists(path)
            ? await File.ReadAllTextAsync(path, cancellationToken)
            : string.Empty;
    }
}
