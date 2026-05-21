using System.Text.Json;
using System.Text.RegularExpressions;

namespace RepairKit.Infrastructure.AgentDashboard;

public sealed partial class AgentDashboardService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly IReadOnlyDictionary<string, string> KnownArtifacts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["repair-report"] = "repair-report.md",
        ["git-diff"] = "git-diff.patch",
        ["repair-plan"] = "repair-plan.json",
        ["approval-decision"] = "approval-decision.json",
        ["patch-application"] = "patch-application.json",
        ["build-output"] = "build-output.txt",
        ["test-output"] = "test-output.txt",
        ["validation-build-output"] = "validation-build-output.txt",
        ["validation-test-output"] = "validation-test-output.txt",
        ["run-summary"] = "run-summary.json"
    };

    private readonly string _startDirectory;

    public AgentDashboardService()
        : this(Directory.GetCurrentDirectory())
    {
    }

    public AgentDashboardService(string startDirectory)
    {
        _startDirectory = startDirectory;
    }

    public string AgentOutputPath => ResolveAgentOutputPath();

    public async Task<IReadOnlyList<AgentRunHistoryEntry>> GetRecentRunsAsync(
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var historyFile = Path.Combine(AgentOutputPath, "history.jsonl");
        if (!File.Exists(historyFile) || limit <= 0)
        {
            return [];
        }

        var entries = new List<AgentRunHistoryEntry>();
        var firstLine = true;

        foreach (var rawLine in await File.ReadAllLinesAsync(historyFile, cancellationToken))
        {
            var line = firstLine ? rawLine.TrimStart('\uFEFF') : rawLine;
            firstLine = false;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var entry = JsonSerializer.Deserialize<AgentRunHistoryEntry>(line, JsonOptions);
                if (entry is not null)
                {
                    entries.Add(entry);
                }
            }
            catch (JsonException)
            {
                // History is diagnostic; malformed lines should not break the dashboard.
            }
        }

        return entries
            .OrderByDescending(entry => entry.StartedUtc)
            .Take(limit)
            .ToArray();
    }

    public async Task<AgentRunDetails> GetRunDetailsAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidRunId(runId))
        {
            return new AgentRunDetails(null, []);
        }

        var history = await GetRecentRunsAsync(500, cancellationToken);
        var entry = history.FirstOrDefault(item => string.Equals(item.RunId, runId, StringComparison.OrdinalIgnoreCase));
        var artifacts = new List<AgentRunArtifact>();

        foreach (var artifact in KnownArtifacts)
        {
            artifacts.Add(await ReadArtifactAsync(runId, artifact.Key, cancellationToken));
        }

        return new AgentRunDetails(entry, artifacts);
    }

    public async Task<AgentRunArtifact> ReadArtifactAsync(
        string runId,
        string artifactName,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidRunId(runId) || !KnownArtifacts.TryGetValue(artifactName, out var fileName))
        {
            return new AgentRunArtifact(artifactName, artifactName, false, "not available");
        }

        var runsRoot = Path.Combine(AgentOutputPath, "runs");
        var runFolder = Path.GetFullPath(Path.Combine(runsRoot, runId));
        var fullRunsRoot = EnsureTrailingSeparator(Path.GetFullPath(runsRoot));

        if (!runFolder.StartsWith(fullRunsRoot, StringComparison.OrdinalIgnoreCase))
        {
            return new AgentRunArtifact(artifactName, fileName, false, "not available");
        }

        var artifactPath = Path.GetFullPath(Path.Combine(runFolder, fileName));
        if (!artifactPath.StartsWith(EnsureTrailingSeparator(runFolder), StringComparison.OrdinalIgnoreCase) ||
            !File.Exists(artifactPath))
        {
            return new AgentRunArtifact(artifactName, fileName, false, "not available");
        }

        return new AgentRunArtifact(
            artifactName,
            fileName,
            true,
            await File.ReadAllTextAsync(artifactPath, cancellationToken));
    }

    public static bool IsValidRunId(string? runId)
    {
        return !string.IsNullOrWhiteSpace(runId) && SafeRunIdRegex().IsMatch(runId);
    }

    public static bool IsKnownArtifact(string artifactName)
    {
        return KnownArtifacts.ContainsKey(artifactName);
    }

    private string ResolveAgentOutputPath()
    {
        var configPath = FindConfigPath(_startDirectory);
        if (configPath is null)
        {
            return Path.Combine(_startDirectory, ".agent");
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(configPath));
            if (document.RootElement.TryGetProperty("agentOutputPath", out var value) &&
                value.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(value.GetString()))
            {
                var configDirectory = Path.GetDirectoryName(configPath)!;
                return Path.GetFullPath(value.GetString()!, configDirectory);
            }
        }
        catch (JsonException)
        {
            return Path.Combine(Path.GetDirectoryName(configPath)!, ".agent");
        }

        return Path.Combine(Path.GetDirectoryName(configPath)!, ".agent");
    }

    private static string? FindConfigPath(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "repairkit.config.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static string EnsureTrailingSeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
    }

    [GeneratedRegex("^[A-Za-z0-9_-]+$")]
    private static partial Regex SafeRunIdRegex();
}

