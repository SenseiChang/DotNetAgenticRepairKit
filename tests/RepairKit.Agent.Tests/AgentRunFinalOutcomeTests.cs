using RepairKit.Agent;

namespace RepairKit.Agent.Tests;

public sealed class AgentRunFinalOutcomeTests
{
    [Fact]
    public void MapsPassingRunToPassedWithoutAi()
    {
        var outcome = AgentRunFinalOutcome.Resolve(
            CreateSummary(buildPassed: true, testsPassed: true),
            new AgentRunOptions(false, false, false, false, false),
            null,
            null,
            null,
            null);

        Assert.Equal(AgentRunFinalOutcome.PassedWithoutAi, outcome);
    }

    [Fact]
    public void MapsNoAiFailureToFailedNoAi()
    {
        var outcome = AgentRunFinalOutcome.Resolve(
            CreateSummary(buildPassed: true, testsPassed: false),
            new AgentRunOptions(true, false, false, false, false),
            null,
            null,
            null,
            null);

        Assert.Equal(AgentRunFinalOutcome.FailedNoAi, outcome);
    }

    [Fact]
    public void MapsValidationPassToPatchAppliedValidationPassed()
    {
        var outcome = AgentRunFinalOutcome.Resolve(
            CreateSummary(buildPassed: true, testsPassed: false),
            new AgentRunOptions(false, false, false, false, false),
            CreateRepairPlanResult(),
            CreateApproval(approved: true),
            CreatePatchResult(validationPassed: true),
            null);

        Assert.Equal(AgentRunFinalOutcome.PatchAppliedValidationPassed, outcome);
    }

    [Fact]
    public void MapsRejectedApprovalToPlanRejected()
    {
        var outcome = AgentRunFinalOutcome.Resolve(
            CreateSummary(buildPassed: true, testsPassed: false),
            new AgentRunOptions(false, false, false, false, false),
            CreateRepairPlanResult(),
            CreateApproval(approved: false),
            null,
            null);

        Assert.Equal(AgentRunFinalOutcome.PlanRejected, outcome);
    }

    private static RunSummary CreateSummary(bool buildPassed, bool testsPassed)
    {
        return new RunSummary(
            "run-1",
            DateTime.UtcNow,
            DateTime.UtcNow,
            1,
            @"H:\repo",
            "dotnet build",
            buildPassed ? 0 : 1,
            buildPassed,
            "dotnet test",
            testsPassed ? 0 : 1,
            testsPassed,
            buildPassed && testsPassed,
            "build-output.txt",
            "test-output.txt");
    }

    private static RepairPlanResult CreateRepairPlanResult()
    {
        var plan = new RepairPlan(
            "Fix TicketSlaService",
            "low",
            ["src/RepairKit.Core/Services/TicketSlaService.cs"],
            [new RepairPlanChange("src/RepairKit.Core/Services/TicketSlaService.cs", "reason", "replacement")],
            ["dotnet test"]);

        return new RepairPlanResult(plan, "openai/gpt-5.2", "repair-plan.json", "model-request.json", "model-response.raw.txt", null, null, null);
    }

    private static RepairApprovalDecision CreateApproval(bool approved)
    {
        return new RepairApprovalDecision(
            "run-1",
            DateTime.UtcNow,
            approved,
            approved ? "APPLY" : "no",
            "low",
            ["src/RepairKit.Core/Services/TicketSlaService.cs"],
            1,
            "reason");
    }

    private static PatchApplicationResult CreatePatchResult(bool validationPassed)
    {
        return new PatchApplicationResult(
            "run-1",
            DateTime.UtcNow,
            true,
            true,
            ["src/RepairKit.Core/Services/TicketSlaService.cs"],
            ["backup.cs"],
            null,
            0,
            validationPassed ? 0 : 1,
            true,
            validationPassed,
            validationPassed,
            "validation-build-output.txt",
            "validation-test-output.txt");
    }
}

