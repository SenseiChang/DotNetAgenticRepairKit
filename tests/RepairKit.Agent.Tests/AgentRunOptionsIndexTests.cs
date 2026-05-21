using RepairKit.Agent;

namespace RepairKit.Agent.Tests;

public sealed class AgentRunOptionsIndexTests
{
    [Fact]
    public void ParsesIndexOption()
    {
        var options = AgentRunOptions.Parse(["--index"]);

        Assert.True(options.Index);
        Assert.False(options.Reindex);
    }

    [Fact]
    public void ParsesReindexOption()
    {
        var options = AgentRunOptions.Parse(["--reindex", "--no-ai"]);

        Assert.True(options.Reindex);
        Assert.True(options.NoAi);
    }
}
