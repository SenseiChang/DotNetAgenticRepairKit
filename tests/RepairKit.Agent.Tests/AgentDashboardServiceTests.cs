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

    [Fact]
    public async Task ReadsToolEventsAndSkipsMalformedLines()
    {
        WriteConfig();
        WriteHistory(CreateEntry("run-1"));
        var runFolder = Path.Combine(_repoRoot, ".agent", "runs", "run-1");
        Directory.CreateDirectory(runFolder);
        File.WriteAllText(
            Path.Combine(runFolder, "tool-events.jsonl"),
            "not json" + Environment.NewLine +
            """
{"toolName":"build_context_packet","startedUtc":"2026-05-21T12:00:00Z","endedUtc":"2026-05-21T12:00:01Z","durationMs":1000,"succeeded":true,"summary":"ok","errorMessage":null}
""");
        var service = new AgentDashboardService(_repoRoot);

        var events = await service.ReadToolEventsAsync("run-1");

        var item = Assert.Single(events);
        Assert.Equal("build_context_packet", item.ToolName);
        Assert.True(item.Succeeded);
    }

    [Fact]
    public async Task ReadsContextMetadataSummary()
    {
        WriteConfig();
        WriteHistory(CreateEntry("run-1"));
        var runFolder = Path.Combine(_repoRoot, ".agent", "runs", "run-1");
        Directory.CreateDirectory(runFolder);
        var packetPath = Path.Combine(runFolder, "context-packet.md");
        File.WriteAllText(packetPath, "context");
        File.WriteAllText(
            Path.Combine(runFolder, "context-metadata.json"),
            $$"""
{
  "retrievalMode": "repo-index",
  "indexFile": "{{Path.Combine(_repoRoot, ".agent", "repo-index.json").Replace("\\", "\\\\")}}",
  "contextPacketFile": "{{packetPath.Replace("\\", "\\\\")}}",
  "truncated": false,
  "matchedKeywords": ["TicketSlaService"],
  "includedFiles": ["src/RepairKit.Core/Services/TicketSlaService.cs"],
  "excludedFiles": ["src/Other.cs"],
  "retrievedFiles": [
    {
      "filePath": "src/RepairKit.Core/Services/TicketSlaService.cs",
      "score": 100,
      "reasons": ["file name match"]
    }
  ]
}
""");
        var service = new AgentDashboardService(_repoRoot);

        var metadata = await service.ReadContextMetadataSummaryAsync("run-1");

        Assert.True(metadata.Exists);
        Assert.Equal("repo-index", metadata.RetrievalMode);
        Assert.Equal(80000, metadata.MaxContextCharacters);
        Assert.Equal(7, metadata.ActualContextCharacters);
        Assert.False(metadata.Truncated);
        Assert.Contains("TicketSlaService", metadata.DetectedKeywords);
        Assert.Contains("src/RepairKit.Core/Services/TicketSlaService.cs", metadata.IncludedFiles);
        Assert.Contains("src/Other.cs", metadata.ExcludedFiles);
        Assert.Equal(100, Assert.Single(metadata.RetrievedFiles).Score);
    }

    [Fact]
    public async Task MissingContextMetadataReturnsSafeSummary()
    {
        WriteConfig();
        var service = new AgentDashboardService(_repoRoot);

        var metadata = await service.ReadContextMetadataSummaryAsync("run-1");

        Assert.False(metadata.Exists);
        Assert.Equal("not available", metadata.Message);
    }

    [Fact]
    public void ReadsRepoIndexStatus()
    {
        WriteConfig();
        var agentFolder = Path.Combine(_repoRoot, ".agent");
        Directory.CreateDirectory(agentFolder);
        File.WriteAllText(
            Path.Combine(agentFolder, "repo-index.json"),
            """
{
  "generatedUtc": "2026-05-21T12:00:00Z",
  "entries": [
    { "filePath": "src/A.cs" },
    { "filePath": "src/B.cs" }
  ]
}
""");
        var service = new AgentDashboardService(_repoRoot);

        var status = service.GetRepoIndexStatus();

        Assert.True(status.Exists);
        Assert.Equal(2, status.IndexedFileCount);
        Assert.NotNull(status.GeneratedUtc);
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
  "agentOutputPath": ".agent",
  "maxContextCharacters": 80000
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
