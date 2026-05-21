namespace RepairKit.Agent;

public sealed class AgentToolRegistry
{
    private readonly Dictionary<string, IAgentTool> _tools = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IAgentTool tool)
    {
        if (!_tools.TryAdd(tool.Name, tool))
        {
            throw new InvalidOperationException($"Tool is already registered: {tool.Name}");
        }
    }

    public IAgentTool Get(string name)
    {
        return _tools.TryGetValue(name, out var tool)
            ? tool
            : throw new KeyNotFoundException($"Tool is not registered: {name}");
    }

    public IReadOnlyList<IAgentTool> List()
    {
        return _tools.Values.OrderBy(tool => tool.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public IReadOnlyList<AgentToolMetadata> ListMetadata()
    {
        return List()
            .Select(tool => new AgentToolMetadata(tool.Name, tool.Description, tool.InputSchemaJson))
            .ToArray();
    }
}
