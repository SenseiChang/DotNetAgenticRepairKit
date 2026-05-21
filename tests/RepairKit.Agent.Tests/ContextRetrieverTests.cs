using RepairKit.Agent;

namespace RepairKit.Agent.Tests;

public sealed class ContextRetrieverTests
{
    [Theory]
    [InlineData("TicketSlaServiceTests failed Critical tickets are due 24 hours", "src/RepairKit.Core/Services/TicketSlaService.cs", "tests/RepairKit.Tests/TicketSlaServiceTests.cs")]
    [InlineData("TicketStatusPolicyTests failed Closed tickets can move to InProgress", "src/RepairKit.Core/Services/TicketStatusPolicy.cs", "tests/RepairKit.Tests/TicketStatusPolicyTests.cs")]
    [InlineData("TicketPriorityServiceTests failed Enterprise escalated priority", "src/RepairKit.Core/Services/TicketPriorityService.cs", "tests/RepairKit.Tests/TicketPriorityServiceTests.cs")]
    public void RanksTicketServiceAndRelatedTests(
        string failureText,
        string expectedService,
        string expectedTest)
    {
        var result = new ContextRetriever().Retrieve(
            failureText,
            [],
            CreateTicketIndex(),
            12);

        Assert.Contains(expectedService, result.IncludedFiles);
        Assert.Contains(expectedTest, result.IncludedFiles);
        Assert.Contains("src/RepairKit.Core/Models/Ticket.cs", result.IncludedFiles);
    }

    [Fact]
    public void BoostsRelatedHistoryTargetFiles()
    {
        var result = new ContextRetriever().Retrieve(
            "ticket failure with ambiguous output",
            [],
            CreateTicketIndex(),
            3,
            ["src/RepairKit.Core/Services/TicketPriorityService.cs"]);

        Assert.Equal("src/RepairKit.Core/Services/TicketPriorityService.cs", result.IncludedFiles[0]);
    }

    private static RepoIndex CreateTicketIndex()
    {
        var entries = new List<RepoIndexEntry>
        {
            Entry("src/RepairKit.Core/Services/TicketSlaService.cs", ["TicketSlaService"], ["ticket", "sla", "service", "critical"]),
            Entry("tests/RepairKit.Tests/TicketSlaServiceTests.cs", ["TicketSlaServiceTests"], ["ticket", "sla", "service", "tests"]),
            Entry("src/RepairKit.Core/Services/TicketStatusPolicy.cs", ["TicketStatusPolicy"], ["ticket", "status", "policy", "closed"]),
            Entry("tests/RepairKit.Tests/TicketStatusPolicyTests.cs", ["TicketStatusPolicyTests"], ["ticket", "status", "policy", "tests"]),
            Entry("src/RepairKit.Core/Services/TicketPriorityService.cs", ["TicketPriorityService"], ["ticket", "priority", "service", "enterprise"]),
            Entry("tests/RepairKit.Tests/TicketPriorityServiceTests.cs", ["TicketPriorityServiceTests"], ["ticket", "priority", "service", "tests"]),
            Entry("src/RepairKit.Core/Models/Ticket.cs", ["Ticket"], ["ticket"]),
            Entry("src/RepairKit.Core/Models/CustomerTier.cs", ["CustomerTier"], ["customer", "tier"]),
            Entry("src/RepairKit.Core/Models/Severity.cs", ["Severity"], ["severity"]),
            Entry("src/RepairKit.Core/Models/TicketStatus.cs", ["TicketStatus"], ["ticket", "status"]),
            Entry("src/RepairKit.Core/Models/AssignedTeam.cs", ["AssignedTeam"], ["assigned", "team"])
        };

        return new RepoIndex(DateTime.UtcNow, @"H:\repo", entries);
    }

    private static RepoIndexEntry Entry(
        string path,
        IReadOnlyList<string> declaredTypes,
        IReadOnlyList<string> keywords)
    {
        return new RepoIndexEntry(
            path,
            Path.GetFileName(path),
            Path.GetExtension(path),
            10,
            "hash",
            declaredTypes,
            ["RepairKit.Core"],
            keywords,
            "snippet",
            DateTime.UtcNow);
    }
}
