using RepairKit.Agent;

namespace RepairKit.Agent.Tests;

public sealed class RelatedRunMemoryTests
{
    [Fact]
    public void MatchesRelatedRunByTargetFile()
    {
        var metadata = CreateMetadata("TicketSlaService");
        var entries = new[]
        {
            AgentRunHistoryTests.CreateEntry("run-1", targetFiles: ["src/RepairKit.Core/Services/TicketSlaService.cs"]),
            AgentRunHistoryTests.CreateEntry(
                "run-2",
                repairSummary: "Adjusted unrelated scoring.",
                targetFiles: ["src/RepairKit.Core/Services/TicketPriorityService.cs"])
        };

        var related = new RelatedRunMemory().FindRelated(metadata, entries);

        Assert.Single(related);
        Assert.Equal("run-1", related[0].RunId);
    }

    [Fact]
    public void MatchesRelatedRunByRepairSummaryKeyword()
    {
        var metadata = CreateMetadata("TicketPriorityService");
        var entries = new[]
        {
            AgentRunHistoryTests.CreateEntry("run-1", repairSummary: "Adjusted TicketPriorityService scoring.", targetFiles: ["tests/file.cs"])
        };

        var related = new RelatedRunMemory().FindRelated(metadata, entries);

        Assert.Single(related);
    }

    private static ContextMetadata CreateMetadata(string keyword)
    {
        return new ContextMetadata(
            "run-current",
            DateTime.UtcNow,
            true,
            [keyword],
            [$"src/RepairKit.Core/Services/{keyword}.cs"],
            "build-output.txt",
            "test-output.txt",
            "context-packet.md");
    }
}
