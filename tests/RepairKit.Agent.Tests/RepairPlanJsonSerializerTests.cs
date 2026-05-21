using RepairKit.Agent;

namespace RepairKit.Agent.Tests;

public sealed class RepairPlanJsonSerializerTests
{
    [Fact]
    public void ParsesValidRepairPlanJson()
    {
        var plan = RepairPlanJsonSerializer.Parse(ValidPlanJson());

        Assert.Equal("Fix critical SLA calculation.", plan.Summary);
        Assert.Equal("low", plan.RiskLevel);
        Assert.Contains("src/RepairKit.Core/Services/TicketSlaService.cs", plan.TargetFiles);
        Assert.Single(plan.Changes);
    }

    [Fact]
    public void RejectsInvalidRiskLevel()
    {
        var json = ValidPlanJson().Replace("\"riskLevel\": \"low\"", "\"riskLevel\": \"extreme\"");

        Assert.Throws<InvalidOperationException>(() => RepairPlanJsonSerializer.Parse(json));
    }

    [Fact]
    public void RejectsEmptyTargetFiles()
    {
        var json = ValidPlanJson().Replace(
            "\"targetFiles\": [\"src/RepairKit.Core/Services/TicketSlaService.cs\"]",
            "\"targetFiles\": []");

        Assert.Throws<InvalidOperationException>(() => RepairPlanJsonSerializer.Parse(json));
    }

    [Fact]
    public void RejectsEmptyChanges()
    {
        var json = ValidPlanJson().Replace(
            """
  "changes": [
    {
      "filePath": "src/RepairKit.Core/Services/TicketSlaService.cs",
      "reason": "Restore critical SLA to two hours.",
      "fullReplacement": "public sealed class TicketSlaService { }"
    }
  ],
""",
            """
  "changes": [],
""");

        Assert.Throws<InvalidOperationException>(() => RepairPlanJsonSerializer.Parse(json));
    }

    [Theory]
    [InlineData("C:/repo/src/RepairKit.Core/Services/TicketSlaService.cs")]
    [InlineData("/repo/src/RepairKit.Core/Services/TicketSlaService.cs")]
    public void RejectsAbsolutePaths(string path)
    {
        Assert.Throws<InvalidOperationException>(() => RepairPlanJsonSerializer.Parse(ValidPlanJson(path)));
    }

    [Fact]
    public void RejectsPathTraversal()
    {
        Assert.Throws<InvalidOperationException>(() =>
            RepairPlanJsonSerializer.Parse(ValidPlanJson("../src/RepairKit.Core/Services/TicketSlaService.cs")));
    }

    [Theory]
    [InlineData(".env")]
    [InlineData("src/RepairKit.Web/appsettings.json")]
    [InlineData("src/RepairKit.Core/secret-token.cs")]
    [InlineData(".agent/runs/file.cs")]
    [InlineData("src/RepairKit.Core/bin/file.cs")]
    public void RejectsSecretConfigAndBlockedPaths(string path)
    {
        Assert.Throws<InvalidOperationException>(() => RepairPlanJsonSerializer.Parse(ValidPlanJson(path)));
    }

    [Fact]
    public void EveryChangeFileMustBeListedInTargetFiles()
    {
        var json = ValidPlanJson().Replace(
            "\"filePath\": \"src/RepairKit.Core/Services/TicketSlaService.cs\"",
            "\"filePath\": \"src/RepairKit.Core/Services/TicketPriorityService.cs\"");

        Assert.Throws<InvalidOperationException>(() => RepairPlanJsonSerializer.Parse(json));
    }

    private static string ValidPlanJson(string path = "src/RepairKit.Core/Services/TicketSlaService.cs")
    {
        return $$"""
{
  "summary": "Fix critical SLA calculation.",
  "riskLevel": "low",
  "targetFiles": ["{{path}}"],
  "changes": [
    {
      "filePath": "{{path}}",
      "reason": "Restore critical SLA to two hours.",
      "fullReplacement": "public sealed class TicketSlaService { }"
    }
  ],
  "validationCommands": ["dotnet test"]
}
""";
    }
}

