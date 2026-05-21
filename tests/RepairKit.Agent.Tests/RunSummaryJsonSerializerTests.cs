using RepairKit.Agent;

namespace RepairKit.Agent.Tests;

public sealed class RunSummaryJsonSerializerTests
{
    [Fact]
    public void SerializesIndentedCamelCaseSummary()
    {
        var summary = new RunSummary(
            "20260520-223500",
            new DateTime(2026, 5, 20, 22, 35, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 20, 22, 35, 5, DateTimeKind.Utc),
            5000,
            @"H:\Projects\DotNetAgenticRepairKit",
            "dotnet build --no-incremental",
            0,
            true,
            "dotnet test --no-build",
            0,
            true,
            true,
            @".agent\runs\20260520-223500\build-output.txt",
            @".agent\runs\20260520-223500\test-output.txt");

        var json = RunSummaryJsonSerializer.Serialize(summary);

        Assert.Contains(Environment.NewLine, json);
        Assert.Contains("\"runId\": \"20260520-223500\"", json);
        Assert.Contains("\"buildCommand\": \"dotnet build --no-incremental\"", json);
        Assert.Contains("\"buildExitCode\": 0", json);
        Assert.Contains("\"buildPassed\": true", json);
        Assert.Contains("\"testCommand\": \"dotnet test --no-build\"", json);
        Assert.Contains("\"testExitCode\": 0", json);
        Assert.Contains("\"testsPassed\": true", json);
        Assert.Contains("\"overallPassed\": true", json);
    }
}
