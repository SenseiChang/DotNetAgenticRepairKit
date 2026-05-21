using RepairKit.Agent;

namespace RepairKit.Agent.Tests;

public sealed class PatchApplicationResultJsonSerializerTests
{
    [Fact]
    public void SerializesPatchApplicationResult()
    {
        var result = new PatchApplicationResult(
            "20260521-100312",
            new DateTime(2026, 5, 21, 10, 3, 12, DateTimeKind.Utc),
            true,
            true,
            ["src/RepairKit.Core/Services/TicketSlaService.cs"],
            [@".agent\runs\20260521-100312\backups\src\RepairKit.Core\Services\TicketSlaService.cs"],
            null,
            0,
            0,
            true,
            true,
            true,
            "validation-build-output.txt",
            "validation-test-output.txt");

        var json = PatchApplicationResultJsonSerializer.Serialize(result);

        Assert.Contains("\"applied\": true", json);
        Assert.Contains("\"validationOverallPassed\": true", json);
        Assert.Contains("TicketSlaService.cs", json);
    }
}

