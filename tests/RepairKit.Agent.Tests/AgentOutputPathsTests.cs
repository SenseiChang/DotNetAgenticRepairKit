using RepairKit.Agent;

namespace RepairKit.Agent.Tests;

public sealed class AgentOutputPathsTests
{
    [Fact]
    public void BuildsExpectedRunFolderPath()
    {
        var repoRoot = Path.Combine("C:", "work", "repo");
        var runId = "20260520-223500";

        var path = AgentOutputPaths.GetRunFolder(repoRoot, runId);

        Assert.Equal(Path.Combine(repoRoot, ".agent", "runs", runId), path);
    }

    [Fact]
    public void BuildsExpectedOutputFilePaths()
    {
        var repoRoot = Path.Combine("C:", "work", "repo");
        var runId = "20260520-223500";

        Assert.Equal(
            Path.Combine(repoRoot, ".agent", "runs", runId, "build-output.txt"),
            AgentOutputPaths.GetBuildOutputFile(repoRoot, runId));

        Assert.Equal(
            Path.Combine(repoRoot, ".agent", "runs", runId, "build-bin"),
            AgentOutputPaths.GetBuildArtifactsFolder(repoRoot, runId));

        Assert.Equal(
            Path.Combine(repoRoot, ".agent", "runs", runId, "test-output.txt"),
            AgentOutputPaths.GetTestOutputFile(repoRoot, runId));

        Assert.Equal(
            Path.Combine(repoRoot, ".agent", "runs", runId, "run-summary.json"),
            AgentOutputPaths.GetRunSummaryFile(repoRoot, runId));

        Assert.Equal(
            Path.Combine(repoRoot, ".agent", "runs", runId, "context-packet.md"),
            AgentOutputPaths.GetContextPacketFile(repoRoot, runId));

        Assert.Equal(
            Path.Combine(repoRoot, ".agent", "runs", runId, "context-metadata.json"),
            AgentOutputPaths.GetContextMetadataFile(repoRoot, runId));

        var runFolder = AgentOutputPaths.GetRunFolder(repoRoot, runId);

        Assert.Equal(
            Path.Combine(runFolder, "model-request.json"),
            AgentOutputPaths.GetModelRequestFile(runFolder));

        Assert.Equal(
            Path.Combine(runFolder, "model-response.raw.txt"),
            AgentOutputPaths.GetModelResponseFile(runFolder));

        Assert.Equal(
            Path.Combine(runFolder, "repair-plan.json"),
            AgentOutputPaths.GetRepairPlanFile(runFolder));

        Assert.Equal(
            Path.Combine(runFolder, "ai-error.txt"),
            AgentOutputPaths.GetAiErrorFile(runFolder));

        Assert.Equal(
            Path.Combine(runFolder, "approval-decision.json"),
            AgentOutputPaths.GetApprovalDecisionFile(runFolder));

        Assert.Equal(
            Path.Combine(runFolder, "git-diff.patch"),
            AgentOutputPaths.GetGitDiffFile(repoRoot, runId));

        Assert.Equal(
            Path.Combine(runFolder, "git-diff-error.txt"),
            AgentOutputPaths.GetGitDiffErrorFile(repoRoot, runId));

        Assert.Equal(
            Path.Combine(runFolder, "repair-report.md"),
            AgentOutputPaths.GetRepairReportFile(repoRoot, runId));

        Assert.Equal(
            Path.Combine(repoRoot, ".agent", "history.jsonl"),
            AgentOutputPaths.GetHistoryFile(repoRoot));
    }
}
