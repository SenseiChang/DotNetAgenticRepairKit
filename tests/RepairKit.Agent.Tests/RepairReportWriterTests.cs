using RepairKit.Agent;

namespace RepairKit.Agent.Tests;

public sealed class RepairReportWriterTests : IDisposable
{
    private readonly string _repoRoot = Path.Combine(Path.GetTempPath(), $"repairkit-report-tests-{Guid.NewGuid():N}");
    private const string RunId = "20260521-110001";

    [Fact]
    public async Task CreatesReportWithRunPlanApprovalAndPatchData()
    {
        WriteStandardArtifacts();
        File.WriteAllText(AgentOutputPaths.GetGitDiffFile(_repoRoot, RunId), "diff --git a/file.cs b/file.cs");

        var reportPath = await new RepairReportWriter().WriteAsync(_repoRoot, RunId);
        var report = await File.ReadAllTextAsync(reportPath);

        Assert.Contains("# Repair Report", report);
        Assert.Contains("Fix critical SLA calculation.", report);
        Assert.Contains("Approved: True", report);
        Assert.Contains("Patch applied: True", report);
        Assert.Contains("```diff", report);
        Assert.Contains("diff --git", report);
    }

    [Fact]
    public async Task ToleratesMissingOptionalFiles()
    {
        Directory.CreateDirectory(AgentOutputPaths.GetRunFolder(_repoRoot, RunId));

        var reportPath = await new RepairReportWriter().WriteAsync(_repoRoot, RunId);
        var report = await File.ReadAllTextAsync(reportPath);

        Assert.Contains("not available", report);
        Assert.Contains("Git diff: not available", report);
    }

    [Fact]
    public async Task EmbedsShortDiff()
    {
        WriteStandardArtifacts();
        File.WriteAllText(AgentOutputPaths.GetGitDiffFile(_repoRoot, RunId), "+ short change");

        var report = await File.ReadAllTextAsync(await new RepairReportWriter().WriteAsync(_repoRoot, RunId));

        Assert.Contains("```diff", report);
        Assert.Contains("+ short change", report);
    }

    [Fact]
    public async Task TruncatesLongDiff()
    {
        WriteStandardArtifacts();
        File.WriteAllText(AgentOutputPaths.GetGitDiffFile(_repoRoot, RunId), new string('+', 5000));

        var report = await File.ReadAllTextAsync(await new RepairReportWriter().WriteAsync(_repoRoot, RunId));

        Assert.Contains("... truncated ...", report);
    }

    public void Dispose()
    {
        if (Directory.Exists(_repoRoot))
        {
            Directory.Delete(_repoRoot, recursive: true);
        }
    }

    private void WriteStandardArtifacts()
    {
        var runFolder = AgentOutputPaths.GetRunFolder(_repoRoot, RunId);
        Directory.CreateDirectory(runFolder);

        var now = new DateTime(2026, 5, 21, 11, 0, 0, DateTimeKind.Utc);
        File.WriteAllText(
            AgentOutputPaths.GetRunSummaryFile(_repoRoot, RunId),
            RunSummaryJsonSerializer.Serialize(new RunSummary(
                RunId,
                now,
                now.AddSeconds(10),
                10000,
                _repoRoot,
                "dotnet build",
                0,
                true,
                "dotnet test",
                1,
                false,
                false,
                "build-output.txt",
                "test-output.txt")));

        File.WriteAllText(
            AgentOutputPaths.GetRepairPlanFile(runFolder),
            RepairPlanJsonSerializer.Serialize(new RepairPlan(
                "Fix critical SLA calculation.",
                "low",
                ["src/RepairKit.Core/Services/TicketSlaService.cs"],
                [
                    new RepairPlanChange(
                        "src/RepairKit.Core/Services/TicketSlaService.cs",
                        "Restore critical SLA.",
                        "replacement")
                ],
                ["dotnet test"])));

        File.WriteAllText(
            AgentOutputPaths.GetApprovalDecisionFile(runFolder),
            RepairApprovalDecisionJsonSerializer.Serialize(new RepairApprovalDecision(
                RunId,
                now,
                true,
                "APPLY",
                "low",
                ["src/RepairKit.Core/Services/TicketSlaService.cs"],
                1,
                "Approved.")));

        File.WriteAllText(
            AgentOutputPaths.GetPatchApplicationFile(runFolder),
            PatchApplicationResultJsonSerializer.Serialize(new PatchApplicationResult(
                RunId,
                now,
                true,
                true,
                ["src/RepairKit.Core/Services/TicketSlaService.cs"],
                ["backup.cs"],
                null,
                0,
                0,
                true,
                true,
                true,
                "validation-build-output.txt",
                "validation-test-output.txt")));
    }
}

