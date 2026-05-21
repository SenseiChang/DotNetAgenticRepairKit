using RepairKit.Agent;

namespace RepairKit.Agent.Tests;

public sealed class OpenRouterRepairPlannerTests
{
    [Fact]
    public void SanitizedModelRequestDoesNotIncludeApiKey()
    {
        const string apiKey = "sk-or-secret-test-key";
        var request = OpenRouterRepairPlanner.CreateRequest(
            "openai/gpt-5.2",
            "TicketSlaServiceTests failed.");

        var json = OpenRouterRepairPlanner.CreateSanitizedRequestJson(request);

        Assert.DoesNotContain(apiKey, json);
        Assert.DoesNotContain("OPENROUTER_API_KEY", json);
        Assert.Contains("\"authorization\": \"Bearer [redacted]\"", json);
        Assert.Contains("\"model\": \"openai/gpt-5.2\"", json);
    }
}

