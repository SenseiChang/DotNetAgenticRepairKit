using System.Text.Json;

namespace RepairKit.Agent;

public static class RepairKitConfigResolver
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static RepairKitConfig Load(string detectedRepoRoot, AgentRunOptions options)
    {
        var configBaseDirectory = Directory.GetCurrentDirectory();
        RepairKitConfig config;

        if (!string.IsNullOrWhiteSpace(options.ConfigPath))
        {
            var configPath = Path.GetFullPath(options.ConfigPath, Directory.GetCurrentDirectory());
            configBaseDirectory = Path.GetDirectoryName(configPath)!;
            config = ReadConfig(configPath);
        }
        else
        {
            var defaultConfigPath = Path.Combine(detectedRepoRoot, "repairkit.config.json");
            if (File.Exists(defaultConfigPath))
            {
                configBaseDirectory = detectedRepoRoot;
                config = ReadConfig(defaultConfigPath);
            }
            else
            {
                configBaseDirectory = detectedRepoRoot;
                config = new RepairKitConfig();
            }
        }

        if (!string.IsNullOrWhiteSpace(options.SolutionOverride))
        {
            config.SolutionPath = options.SolutionOverride;
        }

        if (!string.IsNullOrWhiteSpace(options.RepoRootOverride))
        {
            config.RepoRoot = options.RepoRootOverride;
        }

        if (!string.IsNullOrWhiteSpace(options.AgentOutputOverride))
        {
            config.AgentOutputPath = options.AgentOutputOverride;
        }

        return Resolve(config, configBaseDirectory, detectedRepoRoot);
    }

    public static RepairKitConfig Resolve(
        RepairKitConfig config,
        string baseDirectory,
        string fallbackRepoRoot)
    {
        ApplyDefaults(config);

        var resolvedRepoRoot = ResolvePath(
            string.IsNullOrWhiteSpace(config.RepoRoot) ? fallbackRepoRoot : config.RepoRoot,
            baseDirectory);

        var resolvedSolutionPath = ResolvePath(config.SolutionPath, resolvedRepoRoot);
        var resolvedAgentOutputPath = ResolvePath(config.AgentOutputPath, resolvedRepoRoot);
        var resolvedRepoIndexPath = ResolvePath(config.RepoIndexPath, resolvedRepoRoot);

        config.ResolvedRepoRoot = resolvedRepoRoot;
        config.ResolvedSolutionPath = resolvedSolutionPath;
        config.ResolvedAgentOutputPath = resolvedAgentOutputPath;
        config.ResolvedRepoIndexPath = resolvedRepoIndexPath;

        Validate(config);
        return config;
    }

    private static RepairKitConfig ReadConfig(string path)
    {
        return JsonSerializer.Deserialize<RepairKitConfig>(File.ReadAllText(path), Options)
            ?? new RepairKitConfig();
    }

    private static void ApplyDefaults(RepairKitConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.SolutionPath))
        {
            config.SolutionPath = RepoRootLocator.SolutionFileName;
        }

        if (string.IsNullOrWhiteSpace(config.RepoRoot))
        {
            config.RepoRoot = ".";
        }

        if (string.IsNullOrWhiteSpace(config.AgentOutputPath))
        {
            config.AgentOutputPath = ".agent";
        }

        if (string.IsNullOrWhiteSpace(config.BuildCommand))
        {
            config.BuildCommand = "dotnet build \"{solutionPath}\" --no-incremental -p:OutputPath=\"{buildOutputPath}\"";
        }

        if (string.IsNullOrWhiteSpace(config.TestCommand))
        {
            config.TestCommand = "dotnet test \"{solutionPath}\" --no-build -p:OutputPath=\"{buildOutputPath}\"";
        }

        if (string.IsNullOrWhiteSpace(config.GitDiffCommand))
        {
            config.GitDiffCommand = "git diff -- src tests";
        }

        if (config.BlockedPathSegments.Count == 0)
        {
            config.BlockedPathSegments.AddRange([".git", ".agent", "bin", "obj", ".vs", "node_modules", "scripts", "docs"]);
        }

        if (config.BlockedPathTerms.Count == 0)
        {
            config.BlockedPathTerms.AddRange([".env", "secret", "token", "password", "key", "appsettings"]);
        }

        if (config.MaxContextCharacters <= 0)
        {
            config.MaxContextCharacters = 80000;
        }

        if (config.RecentHistoryLimit <= 0)
        {
            config.RecentHistoryLimit = 20;
        }

        if (string.IsNullOrWhiteSpace(config.RepoIndexPath))
        {
            config.RepoIndexPath = ".agent/repo-index.json";
        }

        if (config.IndexedExtensions.Count == 0)
        {
            config.IndexedExtensions.AddRange([".cs", ".razor", ".csproj", ".md", ".json"]);
        }

        if (config.MaxRetrievedFiles <= 0)
        {
            config.MaxRetrievedFiles = 12;
        }
    }

    private static void Validate(RepairKitConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.SolutionPath))
        {
            throw new InvalidOperationException("solutionPath is required.");
        }

        if (!Directory.Exists(config.ResolvedRepoRoot))
        {
            throw new DirectoryNotFoundException($"repoRoot does not exist: {config.ResolvedRepoRoot}");
        }

        if (!File.Exists(config.ResolvedSolutionPath))
        {
            throw new FileNotFoundException($"Solution file does not exist: {config.ResolvedSolutionPath}");
        }

        if (config.AllowedEditPaths.Count == 0)
        {
            throw new InvalidOperationException("allowedEditPaths must not be empty.");
        }
    }

    private static string ResolvePath(string path, string baseDirectory)
    {
        return Path.GetFullPath(path, baseDirectory);
    }
}
