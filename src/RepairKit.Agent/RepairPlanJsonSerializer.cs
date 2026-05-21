using System.Text.Json;

namespace RepairKit.Agent;

public static class RepairPlanJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static RepairPlan Parse(string json)
    {
        var plan = JsonSerializer.Deserialize<RepairPlan>(json, Options);
        var validation = RepairPlanValidator.Validate(plan);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(
                "Repair plan validation failed: " + string.Join(" ", validation.Errors));
        }

        return plan!;
    }

    public static string Serialize(RepairPlan plan)
    {
        return JsonSerializer.Serialize(plan, Options);
    }
}

