using RepairKit.Agent;

namespace RepairKit.Agent.Tests;

public sealed class RepairApprovalDecisionJsonSerializerTests
{
    [Fact]
    public void SerializesApprovalDecisionAsIndentedCamelCaseJson()
    {
        var decision = new RepairApprovalDecision(
            "20260521-120000",
            new DateTime(2026, 5, 21, 12, 0, 0, DateTimeKind.Utc),
            true,
            "APPLY",
            "low",
            ["src/RepairKit.Core/Services/TicketSlaService.cs"],
            1,
            "User approved by typing APPLY.");

        var json = RepairApprovalDecisionJsonSerializer.Serialize(decision);

        Assert.Contains(Environment.NewLine, json);
        Assert.Contains("\"runId\": \"20260521-120000\"", json);
        Assert.Contains("\"approved\": true", json);
        Assert.Contains("\"decisionText\": \"APPLY\"", json);
        Assert.Contains("\"changeCount\": 1", json);
        Assert.Contains("src/RepairKit.Core/Services/TicketSlaService.cs", json);
    }
}

