namespace RepairKit.Agent;

public abstract class AgentToolBase : IAgentTool
{
    public abstract string Name { get; }

    public abstract string Description { get; }

    public abstract string InputSchemaJson { get; }

    public async Task<AgentToolResult> ExecuteAsync(
        AgentToolContext context,
        string inputJson,
        CancellationToken cancellationToken = default)
    {
        var startedUtc = DateTime.UtcNow;
        AgentToolResult result;

        try
        {
            var output = await ExecuteCoreAsync(context, inputJson, cancellationToken);
            var endedUtc = DateTime.UtcNow;
            result = new AgentToolResult(
                Name,
                output.Succeeded,
                output.Summary,
                output.OutputJson,
                output.ErrorMessage,
                startedUtc,
                endedUtc);
        }
        catch (Exception ex)
        {
            var endedUtc = DateTime.UtcNow;
            result = new AgentToolResult(
                Name,
                false,
                $"{Name} failed.",
                "{}",
                ex.Message,
                startedUtc,
                endedUtc);
        }

        await AgentToolEventWriter.AppendAsync(context, result, cancellationToken);
        return result;
    }

    protected abstract Task<AgentToolOutput> ExecuteCoreAsync(
        AgentToolContext context,
        string inputJson,
        CancellationToken cancellationToken);

    protected sealed record AgentToolOutput(
        bool Succeeded,
        string Summary,
        string OutputJson,
        string? ErrorMessage = null);
}
