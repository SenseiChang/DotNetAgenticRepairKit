using System.Text.Json;

namespace RepairKit.Agent;

public static class ContextMetadataJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static string Serialize(ContextMetadata metadata)
    {
        return JsonSerializer.Serialize(metadata, Options);
    }

    public static Task WriteAsync(string path, ContextMetadata metadata)
    {
        return File.WriteAllTextAsync(path, Serialize(metadata));
    }
}

