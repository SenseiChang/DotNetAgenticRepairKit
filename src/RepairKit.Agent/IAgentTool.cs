namespace RepairKit.Agent;

public interface IAgentTool
{
    string Name { get; }

    string Description { get; }

    string InputSchemaJson { get; }

    Task<AgentToolResult> ExecuteAsync(
        AgentToolContext context,
        string inputJson,
        CancellationToken cancellationToken = default);
}
