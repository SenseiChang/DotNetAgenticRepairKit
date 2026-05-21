namespace RepairKit.Agent;

public sealed class RepairApprovalService
{
    private readonly IUserPrompt _userPrompt;

    public RepairApprovalService(IUserPrompt userPrompt)
    {
        _userPrompt = userPrompt;
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
            Console.WriteLine("Plan-only mode skipped approval.");
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

    private static void PrintApprovalPrompt(RepairPlan plan)
    {
        Console.WriteLine();
        Console.WriteLine("AI repair plan generated.");
        Console.WriteLine();
        Console.WriteLine("Summary:");
        Console.WriteLine(plan.Summary);
        Console.WriteLine();
        Console.WriteLine("Risk Level:");
        Console.WriteLine(plan.RiskLevel);
        Console.WriteLine();
        Console.WriteLine("Target Files:");
        foreach (var targetFile in plan.TargetFiles)
        {
            Console.WriteLine($"- {targetFile}");
        }

        Console.WriteLine();
        Console.WriteLine("Proposed Changes:");
        for (var i = 0; i < plan.Changes.Count; i++)
        {
            var change = plan.Changes[i];
            Console.WriteLine($"{i + 1}. {change.FilePath}");
            Console.WriteLine($"   Reason: {change.Reason}");
        }

        Console.WriteLine();
        Console.WriteLine("Validation Commands:");
        foreach (var command in plan.ValidationCommands)
        {
            Console.WriteLine($"- {command}");
        }

        Console.WriteLine();
        Console.WriteLine("To approve this repair for a future patch step, type:");
        Console.WriteLine("APPLY");
        Console.WriteLine();
        Console.WriteLine("Any other input will reject the repair.");
    }
}
