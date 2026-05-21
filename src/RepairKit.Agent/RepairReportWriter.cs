using System.Text;
using System.Text.Json;

namespace RepairKit.Agent;

public sealed class RepairReportWriter
{
    private const int EmbeddedDiffLimit = 4000;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<string> WriteAsync(
        string repoRoot,
        string runId,
        CancellationToken cancellationToken = default)
    {
        return await WriteAsync(RepairKitConfig.CreateUnvalidatedDefault(repoRoot), runId, cancellationToken);
    }

    public async Task<string> WriteAsync(
        RepairKitConfig config,
        string runId,
        CancellationToken cancellationToken = default)
    {
        var runFolder = AgentOutputPaths.GetRunFolder(config, runId);
        var reportPath = AgentOutputPaths.GetRepairReportFile(config, runId);
        var summary = await ReadJsonAsync<RunSummary>(AgentOutputPaths.GetRunSummaryFile(config, runId), cancellationToken);
        var plan = await ReadJsonAsync<RepairPlan>(AgentOutputPaths.GetRepairPlanFile(runFolder), cancellationToken);
        var approval = await ReadJsonAsync<RepairApprovalDecision>(AgentOutputPaths.GetApprovalDecisionFile(runFolder), cancellationToken);
        var patch = await ReadJsonAsync<PatchApplicationResult>(AgentOutputPaths.GetPatchApplicationFile(runFolder), cancellationToken);
        var diffPath = AgentOutputPaths.GetGitDiffFile(config, runId);
        var diffErrorPath = AgentOutputPaths.GetGitDiffErrorFile(config, runId);

        var builder = new StringBuilder();
        builder.AppendLine("# Repair Report");
        builder.AppendLine();
        builder.AppendLine("## Run Summary");
        builder.AppendLine($"- Run ID: {summary?.RunId ?? runId}");
        builder.AppendLine($"- Started UTC: {Format(summary?.StartedUtc)}");
        builder.AppendLine($"- Ended UTC: {Format(summary?.EndedUtc)}");
        builder.AppendLine($"- Initial build passed: {Format(summary?.BuildPassed)}");
        builder.AppendLine($"- Initial tests passed: {Format(summary?.TestsPassed)}");
        builder.AppendLine($"- Initial overall passed: {Format(summary?.OverallPassed)}");
        builder.AppendLine($"- Final validation build passed: {Format(patch?.ValidationBuildPassed)}");
        builder.AppendLine($"- Final validation tests passed: {Format(patch?.ValidationTestsPassed)}");
        builder.AppendLine($"- Final validation overall passed: {Format(patch?.ValidationOverallPassed)}");
        builder.AppendLine();

        builder.AppendLine("## AI Repair Plan");
        builder.AppendLine("- Model used: not available");
        builder.AppendLine($"- Summary: {plan?.Summary ?? "not available"}");
        builder.AppendLine($"- Risk level: {plan?.RiskLevel ?? "not available"}");
        builder.AppendLine("- Target files:");
        AppendList(builder, plan?.TargetFiles);
        builder.AppendLine($"- Change count: {plan?.Changes.Count.ToString() ?? "not available"}");
        builder.AppendLine();

        builder.AppendLine("## Approval");
        builder.AppendLine($"- Approved: {Format(approval?.Approved)}");
        builder.AppendLine($"- Decision text: {approval?.DecisionText ?? "not available"}");
        builder.AppendLine($"- Decided UTC: {Format(approval?.DecidedUtc)}");
        builder.AppendLine();

        builder.AppendLine("## Patch Application");
        builder.AppendLine($"- Patch applied: {Format(patch?.Applied)}");
        builder.AppendLine("- Changed files:");
        AppendList(builder, patch?.ChangedFiles);
        builder.AppendLine("- Backup files:");
        AppendList(builder, patch?.BackupFiles);
        if (!string.IsNullOrWhiteSpace(patch?.SkippedReason))
        {
            builder.AppendLine($"- Skipped reason: {patch.SkippedReason}");
        }
        builder.AppendLine();

        builder.AppendLine("## Validation");
        builder.AppendLine($"- Build command: {summary?.BuildCommand ?? "not available"}");
        builder.AppendLine($"- Test command: {summary?.TestCommand ?? "not available"}");
        builder.AppendLine($"- Validation build output file: {patch?.ValidationBuildOutputFile ?? "not available"}");
        builder.AppendLine($"- Validation test output file: {patch?.ValidationTestOutputFile ?? "not available"}");
        builder.AppendLine();

        builder.AppendLine("## Git Diff");
        if (File.Exists(diffPath))
        {
            builder.AppendLine($"- Path: {diffPath}");
            var diff = await File.ReadAllTextAsync(diffPath, cancellationToken);
            if (diff.Length <= EmbeddedDiffLimit)
            {
                builder.AppendLine();
                builder.AppendLine("```diff");
                builder.AppendLine(diff);
                builder.AppendLine("```");
            }
            else
            {
                builder.AppendLine("- Diff is long; showing excerpt.");
                builder.AppendLine();
                builder.AppendLine("```diff");
                builder.AppendLine(diff[..EmbeddedDiffLimit]);
                builder.AppendLine("... truncated ...");
                builder.AppendLine("```");
            }
        }
        else if (File.Exists(diffErrorPath))
        {
            builder.AppendLine($"- Git diff error: {diffErrorPath}");
            builder.AppendLine();
            builder.AppendLine("```text");
            builder.AppendLine(await File.ReadAllTextAsync(diffErrorPath, cancellationToken));
            builder.AppendLine("```");
        }
        else
        {
            builder.AppendLine("- Git diff: not available");
        }
        builder.AppendLine();

        builder.AppendLine("## Next Steps");
        builder.AppendLine(patch?.ValidationOverallPassed == true
            ? "Review the diff, then commit the repaired source changes if desired."
            : "Inspect validation output and repair report before retrying.");

        await File.WriteAllTextAsync(reportPath, builder.ToString(), cancellationToken);
        return reportPath;
    }

    private static async Task<T?> ReadJsonAsync<T>(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(
            await File.ReadAllTextAsync(path, cancellationToken),
            JsonOptions);
    }

    private static string Format(DateTime? value)
    {
        return value.HasValue ? value.Value.ToString("O") : "not available";
    }

    private static string Format(bool? value)
    {
        return value.HasValue ? value.Value.ToString() : "not available";
    }

    private static void AppendList(StringBuilder builder, IReadOnlyList<string>? values)
    {
        if (values is null || values.Count == 0)
        {
            builder.AppendLine("  - not available");
            return;
        }

        foreach (var value in values)
        {
            builder.AppendLine($"  - {value}");
        }
    }
}
