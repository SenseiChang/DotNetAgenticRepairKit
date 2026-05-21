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
        var buildOutputFile = AgentOutputPaths.GetBuildOutputFile(repoRoot, runId);
        var testOutputFile = AgentOutputPaths.GetTestOutputFile(repoRoot, runId);
        var contextPacketFile = AgentOutputPaths.GetContextPacketFile(repoRoot, runId);
        var contextMetadataFile = AgentOutputPaths.GetContextMetadataFile(repoRoot, runId);

        var buildOutput = await ReadIfExistsAsync(buildOutputFile, cancellationToken);
        var testOutput = await ReadIfExistsAsync(testOutputFile, cancellationToken);
        var matchResult = ContextFileMatcher.Match(buildOutput + Environment.NewLine + testOutput);

        var packet = await CreatePacketAsync(
            repoRoot,
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

