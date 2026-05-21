using System.Text;

namespace RepairKit.Agent;

public sealed class RelatedRunMemory
{
    public IReadOnlyList<AgentRunHistoryEntry> FindRelated(
        ContextMetadata metadata,
        IReadOnlyList<AgentRunHistoryEntry> recentEntries)
    {
        var serviceNames = GetServiceNames(metadata).ToArray();
        if (serviceNames.Length == 0)
        {
            return [];
        }

        return recentEntries
            .Where(entry => IsRelated(entry, serviceNames))
            .TakeLast(5)
            .ToArray();
    }

    public async Task AppendToContextPacketAsync(
        string repoRoot,
        string runId,
        CancellationToken cancellationToken = default)
    {
        var metadata = await ReadContextMetadataAsync(repoRoot, runId, cancellationToken);
        if (metadata is null)
        {
            return;
        }

        var recentEntries = await new AgentRunHistoryReader().ReadRecentAsync(repoRoot, 20, cancellationToken);
        var relatedEntries = FindRelated(metadata, recentEntries);
        if (relatedEntries.Count == 0)
        {
            return;
        }

        var packetPath = AgentOutputPaths.GetContextPacketFile(repoRoot, runId);
        if (!File.Exists(packetPath))
        {
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine();
        builder.AppendLine("## Recent Related Runs");
        builder.AppendLine();
        builder.AppendLine("This lightweight memory section is derived from local run history. It includes only compact run metadata, not prompts, responses, source code, or diffs.");
        builder.AppendLine();

        foreach (var entry in relatedEntries)
        {
            builder.AppendLine($"- Run ID: {entry.RunId}");
            builder.AppendLine($"  - Final outcome: {entry.FinalOutcome}");
            builder.AppendLine($"  - Repair summary: {entry.RepairSummary ?? "not available"}");
            builder.AppendLine($"  - Target files: {FormatList(entry.TargetFiles)}");
            builder.AppendLine($"  - Validation overall passed: {Format(entry.ValidationOverallPassed)}");
        }

        await File.AppendAllTextAsync(packetPath, builder.ToString(), cancellationToken);
    }

    private static IEnumerable<string> GetServiceNames(ContextMetadata metadata)
    {
        foreach (var value in metadata.MatchedKeywords.Concat(metadata.IncludedFiles))
        {
            if (value.Contains("TicketSlaService", StringComparison.OrdinalIgnoreCase))
            {
                yield return "TicketSlaService";
            }

            if (value.Contains("TicketStatusPolicy", StringComparison.OrdinalIgnoreCase))
            {
                yield return "TicketStatusPolicy";
            }

            if (value.Contains("TicketPriorityService", StringComparison.OrdinalIgnoreCase))
            {
                yield return "TicketPriorityService";
            }
        }
    }

    private static bool IsRelated(AgentRunHistoryEntry entry, IReadOnlyList<string> serviceNames)
    {
        foreach (var serviceName in serviceNames.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (entry.TargetFiles.Any(file => file.Contains(serviceName + ".cs", StringComparison.OrdinalIgnoreCase)) ||
                entry.ChangedFiles.Any(file => file.Contains(serviceName + ".cs", StringComparison.OrdinalIgnoreCase)) ||
                (entry.RepairSummary?.Contains(serviceName, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task<ContextMetadata?> ReadContextMetadataAsync(
        string repoRoot,
        string runId,
        CancellationToken cancellationToken)
    {
        var path = AgentOutputPaths.GetContextMetadataFile(repoRoot, runId);
        if (!File.Exists(path))
        {
            return null;
        }

        return System.Text.Json.JsonSerializer.Deserialize<ContextMetadata>(
            await File.ReadAllTextAsync(path, cancellationToken),
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private static string FormatList(IReadOnlyList<string> values)
    {
        return values.Count == 0 ? "not available" : string.Join(", ", values);
    }

    private static string Format(bool? value)
    {
        return value.HasValue ? value.Value.ToString() : "not available";
    }
}

