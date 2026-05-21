namespace RepairKit.Agent;

public static class AgentRunFinalOutcome
{
    public const string PassedWithoutAi = "passed_without_ai";
    public const string FailedNoAi = "failed_no_ai";
    public const string PlanGenerated = "plan_generated";
    public const string PlanRejected = "plan_rejected";
    public const string ApprovedNotApplied = "approved_not_applied";
    public const string PatchAppliedValidationPassed = "patch_applied_validation_passed";
    public const string PatchAppliedValidationFailed = "patch_applied_validation_failed";
    public const string BuildFailed = "build_failed";
    public const string AiPlanningFailed = "ai_planning_failed";
    public const string PatchFailed = "patch_failed";

    public static string Resolve(
        RunSummary summary,
        AgentRunOptions options,
        RepairPlanResult? repairPlanResult,
        RepairApprovalDecision? approvalDecision,
        PatchApplicationResult? patchApplicationResult,
        string? aiError)
    {
        if (summary.OverallPassed)
        {
            return PassedWithoutAi;
        }

        if (!summary.BuildPassed)
        {
            return BuildFailed;
        }

        if (options.NoAi)
        {
            return FailedNoAi;
        }

        if (!string.IsNullOrWhiteSpace(aiError))
        {
            return AiPlanningFailed;
        }

        if (repairPlanResult is null)
        {
            return FailedNoAi;
        }

        if (approvalDecision is null)
        {
            return PlanGenerated;
        }

        if (!approvalDecision.Approved)
        {
            return PlanRejected;
        }

        if (options.NoApply)
        {
            return ApprovedNotApplied;
        }

        if (patchApplicationResult is null || !patchApplicationResult.Applied)
        {
            return PatchFailed;
        }

        return patchApplicationResult.ValidationOverallPassed
            ? PatchAppliedValidationPassed
            : PatchAppliedValidationFailed;
    }
}

