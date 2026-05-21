using System.Text.Json;

namespace RepairKit.Agent;

public static class PatchApplicationResultJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static string Serialize(PatchApplicationResult result)
    {
        return JsonSerializer.Serialize(result, Options);
    }

    public static Task WriteAsync(string path, PatchApplicationResult result)
    {
        return File.WriteAllTextAsync(path, Serialize(result));
    }
}

