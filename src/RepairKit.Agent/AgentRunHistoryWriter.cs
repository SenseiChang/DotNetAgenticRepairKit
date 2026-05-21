using System.Text;
using System.Text.Json;

namespace RepairKit.Agent;

public sealed class AgentRunHistoryWriter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task AppendAsync(
        string repoRoot,
        AgentRunHistoryEntry entry,
        CancellationToken cancellationToken = default)
    {
        var agentFolder = AgentOutputPaths.GetAgentFolder(repoRoot);
        Directory.CreateDirectory(agentFolder);

        await File.AppendAllTextAsync(
            AgentOutputPaths.GetHistoryFile(repoRoot),
            JsonSerializer.Serialize(entry, Options) + Environment.NewLine,
            Encoding.UTF8,
            cancellationToken);
    }
}

