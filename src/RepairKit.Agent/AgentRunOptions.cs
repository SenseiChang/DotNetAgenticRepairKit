namespace RepairKit.Agent;

public sealed record AgentRunOptions(bool NoAi, bool PlanOnly)
{
    public static AgentRunOptions Parse(string[] args)
    {
        var noAi = args.Any(arg =>
            string.Equals(arg, "--no-ai", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(arg, "--plan-only-without-ai", StringComparison.OrdinalIgnoreCase));

        var planOnly = true;

        return new AgentRunOptions(noAi, planOnly);
    }
}

