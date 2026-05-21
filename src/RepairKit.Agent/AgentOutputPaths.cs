namespace RepairKit.Agent;

public static class AgentOutputPaths
{
    public static string GetRunFolder(string repoRoot, string runId)
    {
        return Path.Combine(repoRoot, ".agent", "runs", runId);
    }

    public static string GetRelativeRunFolder(string runId)
    {
        return Path.Combine(".agent", "runs", runId);
    }

    public static string GetTestOutputFile(string repoRoot, string runId)
    {
        return Path.Combine(GetRunFolder(repoRoot, runId), "test-output.txt");
    }

    public static string GetBuildOutputFile(string repoRoot, string runId)
    {
        return Path.Combine(GetRunFolder(repoRoot, runId), "build-output.txt");
    }

    public static string GetBuildArtifactsFolder(string repoRoot, string runId)
    {
        return Path.Combine(GetRunFolder(repoRoot, runId), "build-bin");
    }

    public static string GetRunSummaryFile(string repoRoot, string runId)
    {
        return Path.Combine(GetRunFolder(repoRoot, runId), "run-summary.json");
    }

    public static string GetContextPacketFile(string repoRoot, string runId)
    {
        return Path.Combine(GetRunFolder(repoRoot, runId), "context-packet.md");
    }

    public static string GetContextMetadataFile(string repoRoot, string runId)
    {
        return Path.Combine(GetRunFolder(repoRoot, runId), "context-metadata.json");
    }

    public static string GetModelRequestFile(string runFolder)
    {
        return Path.Combine(runFolder, "model-request.json");
    }

    public static string GetModelResponseFile(string runFolder)
    {
        return Path.Combine(runFolder, "model-response.raw.txt");
    }

    public static string GetRepairPlanFile(string runFolder)
    {
        return Path.Combine(runFolder, "repair-plan.json");
    }

    public static string GetAiErrorFile(string runFolder)
    {
        return Path.Combine(runFolder, "ai-error.txt");
    }

    public static string GetApprovalDecisionFile(string runFolder)
    {
        return Path.Combine(runFolder, "approval-decision.json");
    }

    public static string GetBackupsFolder(string repoRoot, string runId)
    {
        return Path.Combine(GetRunFolder(repoRoot, runId), "backups");
    }

    public static string GetBackupFile(string repoRoot, string runId, string relativePath)
    {
        return Path.Combine(
            GetBackupsFolder(repoRoot, runId),
            relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    public static string GetValidationArtifactsFolder(string repoRoot, string runId)
    {
        return Path.Combine(GetRunFolder(repoRoot, runId), "validation-bin");
    }

    public static string GetValidationBuildOutputFile(string repoRoot, string runId)
    {
        return Path.Combine(GetRunFolder(repoRoot, runId), "validation-build-output.txt");
    }

    public static string GetValidationTestOutputFile(string repoRoot, string runId)
    {
        return Path.Combine(GetRunFolder(repoRoot, runId), "validation-test-output.txt");
    }

    public static string GetPatchApplicationFile(string runFolder)
    {
        return Path.Combine(runFolder, "patch-application.json");
    }

    public static string GetPatchErrorFile(string runFolder)
    {
        return Path.Combine(runFolder, "patch-error.txt");
    }

    public static string GetGitDiffFile(string repoRoot, string runId)
    {
        return Path.Combine(GetRunFolder(repoRoot, runId), "git-diff.patch");
    }

    public static string GetGitDiffErrorFile(string repoRoot, string runId)
    {
        return Path.Combine(GetRunFolder(repoRoot, runId), "git-diff-error.txt");
    }

    public static string GetRepairReportFile(string repoRoot, string runId)
    {
        return Path.Combine(GetRunFolder(repoRoot, runId), "repair-report.md");
    }
}
