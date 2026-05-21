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
        ["run-summary"] = "run-summary.json",
        ["context-metadata"] = "context-metadata.json",
        ["tool-events"] = "tool-events.jsonl"
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

        var toolEvents = await ReadToolEventsAsync(runId, cancellationToken);
        var contextMetadata = await ReadContextMetadataSummaryAsync(runId, cancellationToken);

        return new AgentRunDetails(entry, artifacts, toolEvents, contextMetadata);
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

        if (!TryGetRunFilePath(runId, fileName, out var artifactPath) || !File.Exists(artifactPath))
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

    private bool TryGetRunFilePath(string runId, string fileName, out string path)
    {
        path = string.Empty;
        if (!IsValidRunId(runId) ||
            fileName.Contains("..", StringComparison.Ordinal) ||
            fileName.Contains('/', StringComparison.Ordinal) ||
            fileName.Contains('\\', StringComparison.Ordinal))
        {
            return false;
        }

        var runsRoot = Path.Combine(AgentOutputPath, "runs");
        var runFolder = Path.GetFullPath(Path.Combine(runsRoot, runId));
        var fullRunsRoot = EnsureTrailingSeparator(Path.GetFullPath(runsRoot));

        if (!runFolder.StartsWith(fullRunsRoot, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var artifactPath = Path.GetFullPath(Path.Combine(runFolder, fileName));
        if (!artifactPath.StartsWith(EnsureTrailingSeparator(runFolder), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        path = artifactPath;
        return true;
    }

    public async Task<IReadOnlyList<AgentToolEvent>> ReadToolEventsAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetRunFilePath(runId, "tool-events.jsonl", out var path) || !File.Exists(path))
        {
            return [];
        }

        var events = new List<AgentToolEvent>();
        foreach (var line in await File.ReadAllLinesAsync(path, cancellationToken))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var item = JsonSerializer.Deserialize<AgentToolEvent>(line, JsonOptions);
                if (item is not null)
                {
                    events.Add(item);
                }
            }
            catch (JsonException)
            {
                // Tool events are diagnostic; malformed lines should not break the dashboard.
            }
        }

        return events;
    }

    public async Task<AgentContextMetadataSummary> ReadContextMetadataSummaryAsync(
        string runId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetRunFilePath(runId, "context-metadata.json", out var path) || !File.Exists(path))
        {
            return new AgentContextMetadataSummary(false, "not available", null, null, null, null, [], [], [], [], "not available");
        }

        try
        {
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(path, cancellationToken));
            var root = document.RootElement;
            var contextPacketPath = TryGetString(root, "contextPacketFile");
            var actualCharacters = !string.IsNullOrWhiteSpace(contextPacketPath) && File.Exists(contextPacketPath)
                ? (int?)new FileInfo(contextPacketPath).Length
                : null;

            return new AgentContextMetadataSummary(
                true,
                TryGetString(root, "retrievalMode") ?? "not available",
                TryGetString(root, "indexFile"),
                TryReadMaxContextCharacters(),
                actualCharacters,
                TryGetBool(root, "truncated"),
                ReadStringArray(root, "matchedKeywords"),
                ReadStringArray(root, "includedFiles"),
                ReadStringArray(root, "excludedFiles"),
                ReadRetrievedFiles(root),
                null);
        }
        catch (JsonException)
        {
            return new AgentContextMetadataSummary(false, "not available", null, null, null, null, [], [], [], [], "not available");
        }
    }

    public RepoIndexStatus GetRepoIndexStatus()
    {
        var indexPath = ResolveRepoIndexPath();
        if (!File.Exists(indexPath))
        {
            return new RepoIndexStatus(
                false,
                indexPath,
                null,
                null,
                "Repo index not found. Run: dotnet run --project src\\RepairKit.Agent --index");
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(indexPath));
            var root = document.RootElement;
            var count = root.TryGetProperty("entries", out var entries) && entries.ValueKind == JsonValueKind.Array
                ? entries.GetArrayLength()
                : null as int?;

            return new RepoIndexStatus(
                true,
                indexPath,
                count,
                TryGetDateTime(root, "generatedUtc"),
                null);
        }
        catch (JsonException)
        {
            return new RepoIndexStatus(true, indexPath, null, null, "Repo index exists but could not be parsed.");
        }
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

    private string ResolveRepoIndexPath()
    {
        var configPath = FindConfigPath(_startDirectory);
        if (configPath is null)
        {
            return Path.Combine(AgentOutputPath, "repo-index.json");
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(configPath));
            if (document.RootElement.TryGetProperty("repoIndexPath", out var value) &&
                value.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(value.GetString()))
            {
                var configDirectory = Path.GetDirectoryName(configPath)!;
                return Path.GetFullPath(value.GetString()!, configDirectory);
            }
        }
        catch (JsonException)
        {
            return Path.Combine(AgentOutputPath, "repo-index.json");
        }

        return Path.Combine(AgentOutputPath, "repo-index.json");
    }

    private int? TryReadMaxContextCharacters()
    {
        var configPath = FindConfigPath(_startDirectory);
        if (configPath is null)
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(configPath));
            return document.RootElement.TryGetProperty("maxContextCharacters", out var value) &&
                value.ValueKind == JsonValueKind.Number &&
                value.TryGetInt32(out var max)
                    ? max
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? TryGetString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static bool? TryGetBool(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var value) &&
            (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False)
                ? value.GetBoolean()
                : null;
    }

    private static DateTime? TryGetDateTime(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var value) &&
            value.ValueKind == JsonValueKind.String &&
            value.TryGetDateTime(out var dateTime)
                ? dateTime
                : null;
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString()!)
            .ToArray();
    }

    private static IReadOnlyList<AgentRetrievedFile> ReadRetrievedFiles(JsonElement root)
    {
        if (!root.TryGetProperty("retrievedFiles", out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var files = new List<AgentRetrievedFile>();
        foreach (var item in value.EnumerateArray())
        {
            var filePath = TryGetString(item, "filePath");
            if (string.IsNullOrWhiteSpace(filePath))
            {
                continue;
            }

            var score = item.TryGetProperty("score", out var scoreElement) &&
                scoreElement.ValueKind == JsonValueKind.Number &&
                scoreElement.TryGetInt32(out var parsedScore)
                    ? parsedScore
                    : 0;

            files.Add(new AgentRetrievedFile(
                filePath,
                score,
                ReadStringArray(item, "reasons")));
        }

        return files;
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
