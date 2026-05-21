using RepairKit.Agent;

namespace RepairKit.Agent.Tests;

public sealed class RunIdGeneratorTests
{
    [Fact]
    public void CreatesSortableTimestampRunId()
    {
        var utcNow = new DateTime(2026, 5, 20, 22, 35, 0, DateTimeKind.Utc);

        var runId = RunIdGenerator.Create(utcNow);

        Assert.Equal("20260520-223500", runId);
        Assert.True(RunIdGenerator.IsValid(runId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("2026-05-20T22:35:00")]
    [InlineData("20260520_223500")]
    public void RejectsInvalidRunIds(string runId)
    {
        Assert.False(RunIdGenerator.IsValid(runId));
    }
}

