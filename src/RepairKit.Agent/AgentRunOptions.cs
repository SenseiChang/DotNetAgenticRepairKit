namespace RepairKit.Agent;

public sealed record AgentRunOptions(
    bool NoAi,
    bool PlanOnly,
    bool ApprovePlan,
    bool RequireApproval)
{
    public static AgentRunOptions Parse(string[] args)
    {
        var noAi = args.Any(arg =>
            string.Equals(arg, "--no-ai", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(arg, "--plan-only-without-ai", StringComparison.OrdinalIgnoreCase));

        var planOnly = args.Any(arg =>
            string.Equals(arg, "--plan-only", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(arg, "--plan-only-without-ai", StringComparison.OrdinalIgnoreCase));

        var approvePlan = args.Any(arg =>
            string.Equals(arg, "--approve-plan", StringComparison.OrdinalIgnoreCase));

        var requireApproval = args.Any(arg =>
            string.Equals(arg, "--require-approval", StringComparison.OrdinalIgnoreCase));

        return new AgentRunOptions(noAi, planOnly, approvePlan, requireApproval);
    }
}

