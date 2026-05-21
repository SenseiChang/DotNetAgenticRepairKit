using System.Text.Json;
using RepairKit.Agent;

namespace RepairKit.Agent.Tests;

public sealed class AgentRunHistoryTests : IDisposable
{
    private readonly string _repoRoot = Path.Combine(Path.GetTempPath(), $"repairkit-history-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task HistoryWriterAppendsJsonlEntries()
    {
        var writer = new AgentRunHistoryWriter();

        await writer.AppendAsync(_repoRoot, CreateEntry("run-1"));
        await writer.AppendAsync(_repoRoot, CreateEntry("run-2"));

        var lines = await File.ReadAllLinesAsync(AgentOutputPaths.GetHistoryFile(_repoRoot));
        Assert.Equal(2, lines.Length);
        Assert.Contains("\"runId\":\"run-1\"", lines[0]);
        Assert.Contains("\"runId\":\"run-2\"", lines[1]);
    }

    [Fact]
    public async Task HistoryReaderReadsRecentEntries()
    {
        var writer = new AgentRunHistoryWriter();
        await writer.AppendAsync(_repoRoot, CreateEntry("run-1"));
        await writer.AppendAsync(_repoRoot, CreateEntry("run-2"));

        var entries = await new AgentRunHistoryReader().ReadRecentAsync(_repoRoot, 10);

        Assert.Equal(["run-1", "run-2"], entries.Select(entry => entry.RunId).ToArray());
    }

    [Fact]
    public async Task HistoryReaderSkipsMalformedLines()
    {
        Directory.CreateDirectory(AgentOutputPaths.GetAgentFolder(_repoRoot));
        await File.WriteAllTextAsync(
            AgentOutputPaths.GetHistoryFile(_repoRoot),
            "not json" + Environment.NewLine + JsonSerializer.Serialize(CreateEntry("run-1")));

        var entries = await new AgentRunHistoryReader().ReadRecentAsync(_repoRoot, 10);

        Assert.Single(entries);
        Assert.Equal("run-1", entries[0].RunId);
    }

    [Fact]
    public async Task HistoryReaderHandlesBomOnFirstLine()
    {
        Directory.CreateDirectory(AgentOutputPaths.GetAgentFolder(_repoRoot));
        await File.WriteAllTextAsync(
            AgentOutputPaths.GetHistoryFile(_repoRoot),
            "\uFEFF" + JsonSerializer.Serialize(CreateEntry("run-1")));

        var entries = await new AgentRunHistoryReader().ReadRecentAsync(_repoRoot, 10);

        Assert.Single(entries);
        Assert.Equal("run-1", entries[0].RunId);
    }

    [Fact]
    public async Task HistoryWriterDoesNotEmitBom()
    {
        await new AgentRunHistoryWriter().AppendAsync(_repoRoot, CreateEntry("run-1"));

        var bytes = await File.ReadAllBytesAsync(AgentOutputPaths.GetHistoryFile(_repoRoot));

        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
    }

    [Fact]
    public async Task HistoryReaderLimitsToLastNEntries()
    {
        var writer = new AgentRunHistoryWriter();
        await writer.AppendAsync(_repoRoot, CreateEntry("run-1", minutes: 1));
        await writer.AppendAsync(_repoRoot, CreateEntry("run-2", minutes: 2));
        await writer.AppendAsync(_repoRoot, CreateEntry("run-3", minutes: 3));

        var entries = await new AgentRunHistoryReader().ReadRecentAsync(_repoRoot, 2);

        Assert.Equal(["run-2", "run-3"], entries.Select(entry => entry.RunId).ToArray());
    }

    [Fact]
    public async Task HistoryEntryDoesNotSerializeApiKeys()
    {
        const string apiKey = "sk-or-secret-value";
        var writer = new AgentRunHistoryWriter();

        await writer.AppendAsync(_repoRoot, CreateEntry("run-1", repairSummary: "Fix TicketSlaService"));

        var json = await File.ReadAllTextAsync(AgentOutputPaths.GetHistoryFile(_repoRoot));
        Assert.DoesNotContain(apiKey, json);
        Assert.DoesNotContain("OPENROUTER_API_KEY", json);
    }

    [Fact]
    public void TokenUsageFieldsAreNullableAndSerialize()
    {
        var entry = CreateEntry("run-1") with
        {
            PromptTokens = null,
            CompletionTokens = null,
            TotalTokens = null
        };

        var json = JsonSerializer.Serialize(entry, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        Assert.Contains("\"promptTokens\":null", json);
        Assert.Contains("\"completionTokens\":null", json);
        Assert.Contains("\"totalTokens\":null", json);
    }

    public void Dispose()
    {
        if (Directory.Exists(_repoRoot))
        {
            Directory.Delete(_repoRoot, recursive: true);
        }
    }

    public static AgentRunHistoryEntry CreateEntry(
        string runId,
        int minutes = 0,
        string repairSummary = "Fix TicketSlaService",
        string finalOutcome = AgentRunFinalOutcome.PatchAppliedValidationPassed,
        IReadOnlyList<string>? targetFiles = null,
        bool? validationOverallPassed = true)
    {
        var startedUtc = new DateTime(2026, 5, 21, 12, 0, 0, DateTimeKind.Utc).AddMinutes(minutes);
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
            null,
            null,
            null,
            repairSummary,
            "low",
            targetFiles ?? ["src/RepairKit.Core/Services/TicketSlaService.cs"],
            1,
            true,
            true,
            true,
            true,
            targetFiles ?? ["src/RepairKit.Core/Services/TicketSlaService.cs"],
            true,
            true,
            validationOverallPassed,
            true,
            true,
            finalOutcome,
            null);
    }
}
