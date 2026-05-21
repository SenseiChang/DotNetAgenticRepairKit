using RepairKit.Agent;

namespace RepairKit.Agent.Tests;

public sealed class ContextBuilderTests : IDisposable
{
    private readonly string _repoRoot = Path.Combine(Path.GetTempPath(), $"repairkit-context-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task WritesContextPacketAndMetadataForMatchedFailure()
    {
        const string runId = "20260520-223500";
        var runFolder = AgentOutputPaths.GetRunFolder(_repoRoot, runId);
        Directory.CreateDirectory(runFolder);
        CreateFile("src/RepairKit.Core/Services/TicketSlaService.cs", "public sealed class TicketSlaService { }");
        CreateFile("tests/RepairKit.Tests/TicketSlaServiceTests.cs", "public sealed class TicketSlaServiceTests { }");
        CreateFile("src/RepairKit.Core/Models/Ticket.cs", "public sealed class Ticket { }");
        CreateFile("src/RepairKit.Core/Models/CustomerTier.cs", "public enum CustomerTier { Standard }");
        CreateFile("src/RepairKit.Core/Models/Severity.cs", "public enum Severity { Critical }");
        CreateFile("src/RepairKit.Core/Models/TicketStatus.cs", "public enum TicketStatus { New }");
        CreateFile("src/RepairKit.Core/Models/AssignedTeam.cs", "public enum AssignedTeam { Support }");
        await File.WriteAllTextAsync(AgentOutputPaths.GetBuildOutputFile(_repoRoot, runId), "Build succeeded.");
        await File.WriteAllTextAsync(AgentOutputPaths.GetTestOutputFile(_repoRoot, runId), "TicketSlaServiceTests failed.");
        var summary = CreateSummary(runId, buildPassed: true, testsPassed: false);

        var metadata = await new ContextBuilder().BuildAsync(_repoRoot, runId, summary);
        var packet = await File.ReadAllTextAsync(AgentOutputPaths.GetContextPacketFile(_repoRoot, runId));
        var metadataJson = await File.ReadAllTextAsync(AgentOutputPaths.GetContextMetadataFile(_repoRoot, runId));

        Assert.True(metadata.DeterministicMatchesFound);
        Assert.Contains("TicketSlaServiceTests", metadata.MatchedKeywords);
        Assert.Contains("### File: src/RepairKit.Core/Services/TicketSlaService.cs", packet);
        Assert.Contains("public sealed class TicketSlaService", packet);
        Assert.Contains("\"deterministicMatchesFound\": true", metadataJson);
    }

    [Fact]
    public async Task ExplainsWhenNoDeterministicMatchesAreFound()
    {
        const string runId = "20260520-223501";
        Directory.CreateDirectory(AgentOutputPaths.GetRunFolder(_repoRoot, runId));
        await File.WriteAllTextAsync(AgentOutputPaths.GetBuildOutputFile(_repoRoot, runId), "Unknown failure.");
        await File.WriteAllTextAsync(AgentOutputPaths.GetTestOutputFile(_repoRoot, runId), string.Empty);
        var summary = CreateSummary(runId, buildPassed: false, testsPassed: false);

        var metadata = await new ContextBuilder().BuildAsync(_repoRoot, runId, summary);
        var packet = await File.ReadAllTextAsync(AgentOutputPaths.GetContextPacketFile(_repoRoot, runId));

        Assert.False(metadata.DeterministicMatchesFound);
        Assert.Empty(metadata.IncludedFiles);
        Assert.Contains("No deterministic source file matches were found.", packet);
    }

    [Fact]
    public async Task FallsBackToKeywordMatchingWhenIndexIsInvalid()
    {
        const string runId = "20260520-223502";
        var runFolder = AgentOutputPaths.GetRunFolder(_repoRoot, runId);
        Directory.CreateDirectory(runFolder);
        CreateFile("Demo.sln", string.Empty);
        CreateFile("src/RepairKit.Core/Services/TicketSlaService.cs", "public sealed class TicketSlaService { }");
        CreateFile("tests/RepairKit.Tests/TicketSlaServiceTests.cs", "public sealed class TicketSlaServiceTests { }");
        CreateFile("src/RepairKit.Core/Models/Ticket.cs", "public sealed class Ticket { }");
        CreateFile("src/RepairKit.Core/Models/CustomerTier.cs", "public enum CustomerTier { Standard }");
        CreateFile("src/RepairKit.Core/Models/Severity.cs", "public enum Severity { Critical }");
        CreateFile("src/RepairKit.Core/Models/TicketStatus.cs", "public enum TicketStatus { New }");
        CreateFile("src/RepairKit.Core/Models/AssignedTeam.cs", "public enum AssignedTeam { Support }");
        await File.WriteAllTextAsync(AgentOutputPaths.GetBuildOutputFile(_repoRoot, runId), "Build succeeded.");
        await File.WriteAllTextAsync(AgentOutputPaths.GetTestOutputFile(_repoRoot, runId), "TicketSlaServiceTests failed.");
        var config = RepairKitConfigResolver.Resolve(new RepairKitConfig { SolutionPath = "Demo.sln" }, _repoRoot, _repoRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(config.ResolvedRepoIndexPath)!);
        await File.WriteAllTextAsync(config.ResolvedRepoIndexPath, "not json");

        var metadata = await new ContextBuilder().BuildAsync(config, runId, CreateSummary(runId, buildPassed: true, testsPassed: false));

        Assert.Equal("fallback-keyword", metadata.RetrievalMode);
        Assert.Contains("src/RepairKit.Core/Services/TicketSlaService.cs", metadata.IncludedFiles);
    }

    public void Dispose()
    {
        if (Directory.Exists(_repoRoot))
        {
            Directory.Delete(_repoRoot, recursive: true);
        }
    }

    private void CreateFile(string relativePath, string contents)
    {
        var path = Path.Combine(_repoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
    }

    private static RunSummary CreateSummary(string runId, bool buildPassed, bool testsPassed)
    {
        return new RunSummary(
            runId,
            new DateTime(2026, 5, 20, 22, 35, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 20, 22, 35, 5, DateTimeKind.Utc),
            5000,
            @"H:\Projects\DotNetAgenticRepairKit",
            "dotnet build",
            buildPassed ? 0 : 1,
            buildPassed,
            "dotnet test",
            testsPassed ? 0 : 1,
            testsPassed,
            buildPassed && testsPassed,
            "build-output.txt",
            "test-output.txt");
    }
}
