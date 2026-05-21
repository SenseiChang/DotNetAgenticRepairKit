using System.Text.Json;

namespace RepairKit.Agent;

public static class RunSummaryJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static string Serialize(RunSummary summary)
    {
        return JsonSerializer.Serialize(summary, Options);
    }

    public static Task WriteAsync(string path, RunSummary summary)
    {
        return File.WriteAllTextAsync(path, Serialize(summary));
    }

    public static async Task<RunSummary> ReadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        return JsonSerializer.Deserialize<RunSummary>(
            await File.ReadAllTextAsync(path, cancellationToken),
            Options) ?? throw new InvalidOperationException($"Run summary could not be read: {path}");
    }
}
