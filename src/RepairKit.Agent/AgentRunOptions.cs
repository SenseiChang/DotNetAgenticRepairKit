namespace RepairKit.Agent;

public sealed record AgentRunOptions(
    bool NoAi,
    bool PlanOnly,
    bool ApprovePlan,
    bool RequireApproval,
    bool NoApply,
    string? ConfigPath = null,
    string? SolutionOverride = null,
    string? RepoRootOverride = null,
    string? AgentOutputOverride = null,
    bool Index = false,
    bool Reindex = false)
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

        var noApply = args.Any(arg =>
            string.Equals(arg, "--no-apply", StringComparison.OrdinalIgnoreCase));

        var index = args.Any(arg =>
            string.Equals(arg, "--index", StringComparison.OrdinalIgnoreCase));

        var reindex = args.Any(arg =>
            string.Equals(arg, "--reindex", StringComparison.OrdinalIgnoreCase));

        return new AgentRunOptions(
            noAi,
            planOnly,
            approvePlan,
            requireApproval,
            noApply,
            GetOptionValue(args, "--config"),
            GetOptionValue(args, "--solution"),
            GetOptionValue(args, "--repo-root"),
            GetOptionValue(args, "--agent-output"),
            index,
            reindex);
    }

    private static string? GetOptionValue(string[] args, string name)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length)
                {
                    throw new InvalidOperationException($"{name} requires a value.");
                }

                return args[i + 1];
            }
        }

        return null;
    }
}
