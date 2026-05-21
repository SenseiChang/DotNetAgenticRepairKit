using RepairKit.Agent;

namespace RepairKit.Agent.Tests;

public sealed class AgentToolRegistryTests
{
    [Fact]
    public void RegistersAndListsTools()
    {
        var registry = new AgentToolRegistry();
        var tool = new StubTool("demo_tool");

        registry.Register(tool);

        Assert.Same(tool, registry.Get("demo_tool"));
        Assert.Contains(tool, registry.List());
    }

    [Fact]
    public void RejectsDuplicateToolNames()
    {
        var registry = new AgentToolRegistry();
        registry.Register(new StubTool("demo_tool"));

        Assert.Throws<InvalidOperationException>(() => registry.Register(new StubTool("demo_tool")));
    }

    [Fact]
    public void MetadataIncludesNameDescriptionAndSchema()
    {
        var registry = new AgentToolRegistry();
        registry.Register(new StubTool("demo_tool"));

        var metadata = Assert.Single(registry.ListMetadata());

        Assert.Equal("demo_tool", metadata.Name);
        Assert.Equal("Demo tool.", metadata.Description);
        Assert.Contains("\"type\"", metadata.InputSchemaJson);
    }

    [Fact]
    public void AgentToolResultCalculatesDuration()
    {
        var started = DateTime.UtcNow;
        var ended = started.AddMilliseconds(250);

        var result = new AgentToolResult("demo", true, "ok", "{}", null, started, ended);

        Assert.Equal(250, result.DurationMs);
    }

    private sealed class StubTool(string name) : IAgentTool
    {
        public string Name { get; } = name;

        public string Description => "Demo tool.";

        public string InputSchemaJson => """{ "type": "object" }""";

        public Task<AgentToolResult> ExecuteAsync(
            AgentToolContext context,
            string inputJson,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            return Task.FromResult(new AgentToolResult(Name, true, "ok", "{}", null, now, now));
        }
    }
}
