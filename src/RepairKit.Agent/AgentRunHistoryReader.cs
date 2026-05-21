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
        var historyFile = AgentOutputPaths.GetHistoryFile(repoRoot);
        if (!File.Exists(historyFile) || limit <= 0)
        {
            return [];
        }

        var entries = new List<AgentRunHistoryEntry>();
        foreach (var line in await File.ReadAllLinesAsync(historyFile, cancellationToken))
        {
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

