using System.Text.Json;

namespace RepairKit.Agent;

public sealed class WriteRepairReportTool : AgentToolBase
{
    public override string Name => "write_repair_report";

    public override string Description => "Writes the human-readable repair report.";

    public override string InputSchemaJson => """
{
  "type": "object",
  "properties": {},
  "additionalProperties": false
}
""";

    protected override async Task<AgentToolOutput> ExecuteCoreAsync(
        AgentToolContext context,
        string inputJson,
        CancellationToken cancellationToken)
    {
        var reportPath = await new RepairReportWriter().WriteAsync(
            context.Config,
            context.RunId,
            cancellationToken);

        return new AgentToolOutput(
            true,
            "Repair report written.",
            JsonSerializer.Serialize(new { reportPath }));
    }
}
