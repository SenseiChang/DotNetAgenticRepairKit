using RepairKit.Agent;

namespace RepairKit.Agent.Tests;

public sealed class ContextFileMatcherTests
{
    [Fact]
    public void MatchesSlaServiceAndSupportingModels()
    {
        var result = ContextFileMatcher.Match("RepairKit.Tests.TicketSlaServiceTests failed.");

        Assert.Contains("TicketSlaServiceTests", result.MatchedKeywords);
        Assert.Contains("src/RepairKit.Core/Services/TicketSlaService.cs", result.IncludedFiles);
        Assert.Contains("tests/RepairKit.Tests/TicketSlaServiceTests.cs", result.IncludedFiles);
        Assert.Contains("src/RepairKit.Core/Models/Ticket.cs", result.IncludedFiles);
        Assert.Contains("src/RepairKit.Core/Models/Severity.cs", result.IncludedFiles);
    }

    [Fact]
    public void MatchesStatusPolicyFiles()
    {
        var result = ContextFileMatcher.Match("TicketStatusPolicy rejected a transition.");

        Assert.Contains("src/RepairKit.Core/Services/TicketStatusPolicy.cs", result.IncludedFiles);
        Assert.Contains("tests/RepairKit.Tests/TicketStatusPolicyTests.cs", result.IncludedFiles);
    }

    [Fact]
    public void MatchesPriorityFiles()
    {
        var result = ContextFileMatcher.Match("TicketPriorityServiceTests reported a failure.");

        Assert.Contains("src/RepairKit.Core/Services/TicketPriorityService.cs", result.IncludedFiles);
        Assert.Contains("tests/RepairKit.Tests/TicketPriorityServiceTests.cs", result.IncludedFiles);
    }

    [Fact]
    public void ReturnsNoFilesWhenNoDeterministicMatchesExist()
    {
        var result = ContextFileMatcher.Match("Some unrelated compiler error.");

        Assert.Empty(result.MatchedKeywords);
        Assert.Empty(result.IncludedFiles);
    }
}

