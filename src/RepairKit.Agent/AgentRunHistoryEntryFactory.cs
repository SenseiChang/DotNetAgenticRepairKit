namespace RepairKit.Agent;

public static class AgentRunHistoryEntryFactory
{
    public static AgentRunHistoryEntry Create(
        RunSummary summary,
        AgentRunOptions options,
        bool contextGenerated,
        RepairPlanResult? repairPlanResult,
        RepairApprovalDecision? approvalDecision,
        PatchApplicationResult? patchApplicationResult,
        GitDiffCaptureResult? gitDiffResult,
        string? repairReportPath,
        string? aiError)
    {
        var finalOutcome = AgentRunFinalOutcome.Resolve(
            summary,
            options,
            repairPlanResult,
            approvalDecision,
            patchApplicationResult,
            aiError);

        return new AgentRunHistoryEntry(
            summary.RunId,
            summary.StartedUtc,
            summary.EndedUtc,
            summary.DurationMs,
            summary.BuildPassed,
            summary.TestsPassed,
            summary.OverallPassed,
            contextGenerated,
            !summary.OverallPassed && !options.NoAi,
            repairPlanResult is not null,
            repairPlanResult?.Model,
            repairPlanResult?.PromptTokens,
            repairPlanResult?.CompletionTokens,
            repairPlanResult?.TotalTokens,
            repairPlanResult?.Plan.Summary,
            repairPlanResult?.Plan.RiskLevel,
            repairPlanResult?.Plan.TargetFiles ?? [],
            repairPlanResult?.Plan.Changes.Count ?? 0,
            repairPlanResult is not null && !options.PlanOnly,
            approvalDecision?.Approved,
            patchApplicationResult is not null,
            patchApplicationResult?.Applied ?? false,
            patchApplicationResult?.ChangedFiles ?? [],
            patchApplicationResult?.ValidationBuildPassed,
            patchApplicationResult?.ValidationTestsPassed,
            patchApplicationResult?.ValidationOverallPassed,
            gitDiffResult?.Succeeded == true && File.Exists(gitDiffResult.DiffFile),
            repairReportPath is not null && File.Exists(repairReportPath),
            finalOutcome,
            aiError ?? patchApplicationResult?.SkippedReason);
    }
}

