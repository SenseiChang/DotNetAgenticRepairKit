using System.Text.Json;

namespace RepairKit.Agent;

public static class RepoIndexJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static string Serialize(RepoIndex index)
    {
        return JsonSerializer.Serialize(index, Options);
    }

    public static RepoIndex Deserialize(string json)
    {
        return JsonSerializer.Deserialize<RepoIndex>(json, Options)
            ?? new RepoIndex(DateTime.MinValue, string.Empty, []);
    }

    public static async Task WriteAsync(string path, RepoIndex index, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, Serialize(index), cancellationToken);
    }

    public static async Task<RepoIndex> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        return Deserialize(await File.ReadAllTextAsync(path, cancellationToken));
    }
}
