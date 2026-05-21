using RepairKit.Agent;

namespace RepairKit.Agent.Tests;

public sealed class RepairApprovalServiceTests : IDisposable
{
    private readonly string _runFolder = Path.Combine(Path.GetTempPath(), $"repairkit-approval-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task ManualApplyProducesApprovedTrue()
    {
        var service = new RepairApprovalService(new FakeUserPrompt("APPLY"));

        var decision = await service.RequestApprovalAsync(
            "20260521-120000",
            CreatePlan("low"),
            new AgentRunOptions(false, false, false, true),
            _runFolder);

        Assert.NotNull(decision);
        Assert.True(decision.Approved);
        Assert.Equal("APPLY", decision.DecisionText);
    }

    [Fact]
    public async Task NonApplyInputProducesApprovedFalse()
    {
        var service = new RepairApprovalService(new FakeUserPrompt("no"));

        var decision = await service.RequestApprovalAsync(
            "20260521-120000",
            CreatePlan("low"),
            new AgentRunOptions(false, false, false, true),
            _runFolder);

        Assert.NotNull(decision);
        Assert.False(decision.Approved);
        Assert.Equal("no", decision.DecisionText);
    }

    [Fact]
    public async Task ApprovePlanAutoApprovesLowRisk()
    {
        var prompt = new FakeUserPrompt("no");
        var service = new RepairApprovalService(prompt);

        var decision = await service.RequestApprovalAsync(
            "20260521-120000",
            CreatePlan("low"),
            new AgentRunOptions(false, false, true, false),
            _runFolder);

        Assert.NotNull(decision);
        Assert.True(decision.Approved);
        Assert.Equal("APPLY", decision.DecisionText);
        Assert.False(prompt.WasRead);
    }

    [Fact]
    public async Task ApprovePlanDoesNotAutoApproveMediumRisk()
    {
        var prompt = new FakeUserPrompt("no");
        var service = new RepairApprovalService(prompt);

        var decision = await service.RequestApprovalAsync(
            "20260521-120000",
            CreatePlan("medium"),
            new AgentRunOptions(false, false, true, false),
            _runFolder);

        Assert.NotNull(decision);
        Assert.False(decision.Approved);
        Assert.True(prompt.WasRead);
    }

    [Fact]
    public async Task RequireApprovalOverridesApprovePlan()
    {
        var prompt = new FakeUserPrompt("no");
        var service = new RepairApprovalService(prompt);

        var decision = await service.RequestApprovalAsync(
            "20260521-120000",
            CreatePlan("low"),
            new AgentRunOptions(false, false, true, true),
            _runFolder);

        Assert.NotNull(decision);
        Assert.False(decision.Approved);
        Assert.True(prompt.WasRead);
    }

    [Fact]
    public async Task PlanOnlySkipsApproval()
    {
        var service = new RepairApprovalService(new FakeUserPrompt("APPLY"));

        var decision = await service.RequestApprovalAsync(
            "20260521-120000",
            CreatePlan("low"),
            new AgentRunOptions(false, true, false, false),
            _runFolder);

        Assert.Null(decision);
        Assert.False(File.Exists(AgentOutputPaths.GetApprovalDecisionFile(_runFolder)));
    }

    [Fact]
    public async Task ApprovalDecisionIncludesTargetFilesAndChangeCount()
    {
        var service = new RepairApprovalService(new FakeUserPrompt("APPLY"));

        var decision = await service.RequestApprovalAsync(
            "20260521-120000",
            CreatePlan("low"),
            new AgentRunOptions(false, false, false, true),
            _runFolder);

        Assert.NotNull(decision);
        Assert.Contains("src/RepairKit.Core/Services/TicketSlaService.cs", decision.TargetFiles);
        Assert.Equal(1, decision.ChangeCount);
    }

    public void Dispose()
    {
        if (Directory.Exists(_runFolder))
        {
            Directory.Delete(_runFolder, recursive: true);
        }
    }

    private static RepairPlan CreatePlan(string riskLevel)
    {
        return new RepairPlan(
            "Fix critical SLA calculation.",
            riskLevel,
            ["src/RepairKit.Core/Services/TicketSlaService.cs"],
            [
                new RepairPlanChange(
                    "src/RepairKit.Core/Services/TicketSlaService.cs",
                    "Restore critical SLA to two hours.",
                    "public sealed class TicketSlaService { }")
            ],
            ["dotnet test"]);
    }

    private sealed class FakeUserPrompt : IUserPrompt
    {
        private readonly string? _input;

        public FakeUserPrompt(string? input)
        {
            _input = input;
        }

        public bool WasRead { get; private set; }

        public string? ReadLine()
        {
            WasRead = true;
            return _input;
        }
    }
}

