using System.Text.Json;
using RepairKit.Infrastructure.AgentDashboard;

namespace RepairKit.Agent.Tests;

public sealed class AgentDashboardServiceTests : IDisposable
{
    private readonly string _repoRoot = Path.Combine(Path.GetTempPath(), $"repairkit-dashboard-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task ReadsHistoryEntries()
    {
        WriteConfig();
        WriteHistory(CreateEntry("run-1"), CreateEntry("run-2"));
        var service = new AgentDashboardService(_repoRoot);

        var entries = await service.GetRecentRunsAsync();

        Assert.Equal(2, entries.Count);
        Assert.Equal("run-2", entries[0].RunId);
    }

    [Fact]
    public async Task MissingHistoryReturnsEmptyList()
    {
        WriteConfig();
        var service = new AgentDashboardService(_repoRoot);

        var entries = await service.GetRecentRunsAsync();

        Assert.Empty(entries);
    }

    [Fact]
    public async Task MalformedHistoryLinesAreSkipped()
    {
        WriteConfig();
        Directory.CreateDirectory(Path.Combine(_repoRoot, ".agent"));
        File.WriteAllText(
            Path.Combine(_repoRoot, ".agent", "history.jsonl"),
            "not json" + Environment.NewLine + JsonSerializer.Serialize(CreateEntry("run-1")));
        var service = new AgentDashboardService(_repoRoot);

        var entries = await service.GetRecentRunsAsync();

        Assert.Single(entries);
        Assert.Equal("run-1", entries[0].RunId);
    }

    [Theory]
    [InlineData("../run")]
    [InlineData("run/child")]
    [InlineData("run\\child")]
    [InlineData("C:/temp/run")]
    [InlineData("")]
    public void RunIdValidationRejectsUnsafeValues(string runId)
    {
        Assert.False(AgentDashboardService.IsValidRunId(runId));
    }

    [Fact]
    public void ArtifactReadingOnlyAllowsKnownArtifactNames()
    {
        Assert.True(AgentDashboardService.IsKnownArtifact("repair-report"));
        Assert.False(AgentDashboardService.IsKnownArtifact("../secret"));
    }

    [Fact]
    public async Task MissingArtifactReturnsNotAvailable()
    {
        WriteConfig();
        WriteHistory(CreateEntry("run-1"));
        var service = new AgentDashboardService(_repoRoot);

        var artifact = await service.ReadArtifactAsync("run-1", "repair-report");

        Assert.False(artifact.Exists);
        Assert.Equal("not available", artifact.Content);
    }

    [Fact]
    public async Task ReadsKnownArtifact()
    {
        WriteConfig();
        WriteHistory(CreateEntry("run-1"));
        var runFolder = Path.Combine(_repoRoot, ".agent", "runs", "run-1");
        Directory.CreateDirectory(runFolder);
        File.WriteAllText(Path.Combine(runFolder, "repair-report.md"), "# Report");
        var service = new AgentDashboardService(_repoRoot);

        var artifact = await service.ReadArtifactAsync("run-1", "repair-report");

        Assert.True(artifact.Exists);
        Assert.Equal("# Report", artifact.Content);
    }

    public void Dispose()
    {
        if (Directory.Exists(_repoRoot))
        {
            Directory.Delete(_repoRoot, recursive: true);
        }
    }

    private void WriteConfig()
    {
        Directory.CreateDirectory(_repoRoot);
        File.WriteAllText(
            Path.Combine(_repoRoot, "repairkit.config.json"),
            """
{
  "agentOutputPath": ".agent"
}
""");
    }

    private void WriteHistory(params AgentRunHistoryEntry[] entries)
    {
        var agentFolder = Path.Combine(_repoRoot, ".agent");
        Directory.CreateDirectory(agentFolder);
        File.WriteAllLines(
            Path.Combine(agentFolder, "history.jsonl"),
            entries.Select(entry => JsonSerializer.Serialize(entry, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })));
    }

    private static AgentRunHistoryEntry CreateEntry(string runId)
    {
        var startedUtc = runId == "run-2"
            ? new DateTime(2026, 5, 21, 12, 1, 0, DateTimeKind.Utc)
            : new DateTime(2026, 5, 21, 12, 0, 0, DateTimeKind.Utc);

        return new AgentRunHistoryEntry(
            runId,
            startedUtc,
            startedUtc.AddSeconds(5),
            5000,
            true,
            false,
            false,
            true,
            true,
            true,
            "openai/gpt-5.2",
            10,
            20,
            30,
            "Fix test failure.",
            "low",
            ["src/RepairKit.Core/Services/TicketSlaService.cs"],
            1,
            true,
            true,
            true,
            true,
            ["src/RepairKit.Core/Services/TicketSlaService.cs"],
            true,
            true,
            true,
            true,
            true,
            "patch_applied_validation_passed",
            null);
    }
}

