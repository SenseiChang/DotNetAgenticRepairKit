using System.Text.Json;

namespace RepairKit.Agent;

public sealed class AgentRunHistoryReader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<AgentRunHistoryEntry>> ReadRecentAsync(
        string repoRoot,
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await ReadRecentCoreAsync(AgentOutputPaths.GetHistoryFile(repoRoot), limit, cancellationToken);
    }

    public async Task<IReadOnlyList<AgentRunHistoryEntry>> ReadRecentAsync(
        RepairKitConfig config,
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await ReadRecentCoreAsync(AgentOutputPaths.GetHistoryFile(config), limit, cancellationToken);
    }

    private static async Task<IReadOnlyList<AgentRunHistoryEntry>> ReadRecentCoreAsync(
        string historyFile,
        int limit,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(historyFile) || limit <= 0)
        {
            return [];
        }

        var entries = new List<AgentRunHistoryEntry>();
        var firstLine = true;
        foreach (var rawLine in await File.ReadAllLinesAsync(historyFile, cancellationToken))
        {
            var line = firstLine ? rawLine.TrimStart('\uFEFF') : rawLine;
            firstLine = false;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var entry = JsonSerializer.Deserialize<AgentRunHistoryEntry>(line, Options);
                if (entry is not null)
                {
                    entries.Add(entry);
                }
            }
            catch (JsonException)
            {
                // Skip malformed history lines; history is diagnostic only.
            }
        }

        return entries
            .OrderBy(entry => entry.StartedUtc)
            .TakeLast(limit)
            .ToArray();
    }
}
