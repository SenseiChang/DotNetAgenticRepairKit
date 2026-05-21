using System.Text.Json;

namespace RepairKit.Agent;

public static class RepairApprovalDecisionJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static string Serialize(RepairApprovalDecision decision)
    {
        return JsonSerializer.Serialize(decision, Options);
    }

    public static Task WriteAsync(string path, RepairApprovalDecision decision)
    {
        return File.WriteAllTextAsync(path, Serialize(decision));
    }
}

