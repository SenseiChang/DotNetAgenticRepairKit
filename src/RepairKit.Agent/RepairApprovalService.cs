namespace RepairKit.Agent;

public sealed class RepairApprovalService
{
    private readonly IUserPrompt _userPrompt;
    private readonly IUserOutput _userOutput;

    public RepairApprovalService(IUserPrompt userPrompt, IUserOutput? userOutput = null)
    {
        _userPrompt = userPrompt;
        _userOutput = userOutput ?? new ConsoleUserOutput();
    }

    public async Task<RepairApprovalDecision?> RequestApprovalAsync(
        string runId,
        RepairPlan plan,
        AgentRunOptions options,
        string runFolder,
        CancellationToken cancellationToken = default)
    {
        if (options.PlanOnly)
        {
            _userOutput.WriteLine("Plan-only mode skipped approval.");
            return null;
        }

        RepairApprovalDecision decision;

        if (options.ApprovePlan &&
            !options.RequireApproval &&
            string.Equals(plan.RiskLevel, "low", StringComparison.OrdinalIgnoreCase))
        {
            decision = CreateDecision(
                runId,
                plan,
                approved: true,
                decisionText: "APPLY",
                reason: "--approve-plan auto-approved low risk plan.");
        }
        else
        {
            PrintApprovalPrompt(plan);
            var input = _userPrompt.ReadLine();
            var decisionText = string.IsNullOrWhiteSpace(input) ? "not provided" : input;
            var approved = string.Equals(input, "APPLY", StringComparison.Ordinal);

            decision = CreateDecision(
                runId,
                plan,
                approved,
                decisionText,
                approved ? "User approved by typing APPLY." : "User did not type APPLY.");
        }

        Directory.CreateDirectory(runFolder);
        await RepairApprovalDecisionJsonSerializer.WriteAsync(
            AgentOutputPaths.GetApprovalDecisionFile(runFolder),
            decision);

        return decision;
    }

    public static RepairApprovalDecision CreateDecision(
        string runId,
        RepairPlan plan,
        bool approved,
        string decisionText,
        string reason)
    {
        return new RepairApprovalDecision(
            runId,
            DateTime.UtcNow,
            approved,
            approved ? "APPLY" : decisionText,
            plan.RiskLevel,
            plan.TargetFiles,
            plan.Changes.Count,
            reason);
    }

    private void PrintApprovalPrompt(RepairPlan plan)
    {
        _userOutput.WriteLine();
        _userOutput.WriteLine("AI repair plan generated.");
        _userOutput.WriteLine();
        _userOutput.WriteLine("Summary:");
        _userOutput.WriteLine(plan.Summary);
        _userOutput.WriteLine();
        _userOutput.WriteLine("Risk Level:");
        _userOutput.WriteLine(plan.RiskLevel);
        _userOutput.WriteLine();
        _userOutput.WriteLine("Target Files:");
        foreach (var targetFile in plan.TargetFiles)
        {
            _userOutput.WriteLine($"- {targetFile}");
        }

        _userOutput.WriteLine();
        _userOutput.WriteLine("Proposed Changes:");
        for (var i = 0; i < plan.Changes.Count; i++)
        {
            var change = plan.Changes[i];
            _userOutput.WriteLine($"{i + 1}. {change.FilePath}");
            _userOutput.WriteLine($"   Reason: {change.Reason}");
        }

        _userOutput.WriteLine();
        _userOutput.WriteLine("Validation Commands:");
        foreach (var command in plan.ValidationCommands)
        {
            _userOutput.WriteLine($"- {command}");
        }

        _userOutput.WriteLine();
        _userOutput.WriteLine("To approve this repair for a future patch step, type:");
        _userOutput.WriteLine("APPLY");
        _userOutput.WriteLine();
        _userOutput.WriteLine("Any other input will reject the repair.");
    }
}
