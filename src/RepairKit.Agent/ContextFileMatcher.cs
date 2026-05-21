namespace RepairKit.Agent;

public static class ContextFileMatcher
{
    private static readonly IReadOnlyList<string> SupportingModelFiles =
    [
        "src/RepairKit.Core/Models/Ticket.cs",
        "src/RepairKit.Core/Models/CustomerTier.cs",
        "src/RepairKit.Core/Models/Severity.cs",
        "src/RepairKit.Core/Models/TicketStatus.cs",
        "src/RepairKit.Core/Models/AssignedTeam.cs"
    ];

    private static readonly IReadOnlyList<ContextMatchRule> Rules =
    [
        new(
            ["TicketSlaService", "TicketSlaServiceTests"],
            [
                "src/RepairKit.Core/Services/TicketSlaService.cs",
                "tests/RepairKit.Tests/TicketSlaServiceTests.cs"
            ]),
        new(
            ["TicketStatusPolicy", "TicketStatusPolicyTests"],
            [
                "src/RepairKit.Core/Services/TicketStatusPolicy.cs",
                "tests/RepairKit.Tests/TicketStatusPolicyTests.cs"
            ]),
        new(
            ["TicketPriorityService", "TicketPriorityServiceTests"],
            [
                "src/RepairKit.Core/Services/TicketPriorityService.cs",
                "tests/RepairKit.Tests/TicketPriorityServiceTests.cs"
            ])
    ];

    public static ContextMatchResult Match(string output)
    {
        var matchedKeywords = new List<string>();
        var includedFiles = new List<string>();

        foreach (var rule in Rules)
        {
            var ruleMatched = false;

            foreach (var keyword in rule.Keywords)
            {
                if (output.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    matchedKeywords.Add(keyword);
                    ruleMatched = true;
                }
            }

            if (ruleMatched)
            {
                includedFiles.AddRange(rule.Files);
            }
        }

        if (includedFiles.Count > 0)
        {
            includedFiles.AddRange(SupportingModelFiles);
        }

        return new ContextMatchResult(
            matchedKeywords.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value).ToArray(),
            includedFiles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private sealed record ContextMatchRule(
        IReadOnlyList<string> Keywords,
        IReadOnlyList<string> Files);
}

