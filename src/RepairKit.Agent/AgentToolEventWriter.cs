using System.Text.Json;

namespace RepairKit.Agent;

public static class AgentToolEventWriter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task AppendAsync(
        AgentToolContext context,
        AgentToolResult result,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(context.RunFolder))
        {
            return;
        }

        Directory.CreateDirectory(context.RunFolder);
        var path = Path.Combine(context.RunFolder, "tool-events.jsonl");
        var line = JsonSerializer.Serialize(
            new
            {
                result.ToolName,
                result.StartedUtc,
                result.EndedUtc,
                result.DurationMs,
                result.Succeeded,
                result.Summary,
                result.ErrorMessage
            },
            Options);

        await File.AppendAllTextAsync(path, line + Environment.NewLine, cancellationToken);
    }
}
