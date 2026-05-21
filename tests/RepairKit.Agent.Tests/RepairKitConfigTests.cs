using RepairKit.Agent;

namespace RepairKit.Agent.Tests;

public sealed class RepairKitConfigTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), $"repairkit-config-tests-{Guid.NewGuid():N}");

    [Fact]
    public void DefaultConfigValuesMatchDemoRepo()
    {
        var repoRoot = CreateRepo("Demo.sln");
        var config = RepairKitConfigResolver.Resolve(
            new RepairKitConfig { SolutionPath = "Demo.sln" },
            repoRoot,
            repoRoot);

        Assert.Equal(Path.Combine(repoRoot, "Demo.sln"), config.ResolvedSolutionPath);
        Assert.Equal(Path.Combine(repoRoot, ".agent"), config.ResolvedAgentOutputPath);
        Assert.Contains("src/RepairKit.Core/", config.AllowedEditPaths);
        Assert.Equal(80000, config.MaxContextCharacters);
        Assert.Equal(20, config.RecentHistoryLimit);
        Assert.Equal(Path.Combine(repoRoot, ".agent", "repo-index.json"), config.ResolvedRepoIndexPath);
        Assert.Contains(".cs", config.IndexedExtensions);
        Assert.Equal(12, config.MaxRetrievedFiles);
    }

    [Fact]
    public void LoadsRepairKitConfigJson()
    {
        var repoRoot = CreateRepo("Demo.sln");
        File.WriteAllText(
            Path.Combine(repoRoot, "repairkit.config.json"),
            """
{
  "solutionPath": "Demo.sln",
  "repoRoot": ".",
  "agentOutputPath": ".custom-agent",
  "allowedEditPaths": ["src/"],
  "maxContextCharacters": 123,
  "recentHistoryLimit": 7,
  "repoIndexPath": ".custom-agent/index.json",
  "indexedExtensions": [".cs"],
  "maxRetrievedFiles": 5
}
""");

        var config = RepairKitConfigResolver.Load(repoRoot, new AgentRunOptions(false, false, false, false, false));

        Assert.Equal(Path.Combine(repoRoot, ".custom-agent"), config.ResolvedAgentOutputPath);
        Assert.Equal(["src/"], config.AllowedEditPaths);
        Assert.Equal(123, config.MaxContextCharacters);
        Assert.Equal(7, config.RecentHistoryLimit);
        Assert.Equal(Path.Combine(repoRoot, ".custom-agent", "index.json"), config.ResolvedRepoIndexPath);
        Assert.Equal([".cs"], config.IndexedExtensions);
        Assert.Equal(5, config.MaxRetrievedFiles);
    }

    [Fact]
    public void MissingConfigFallsBackToDefaults()
    {
        var repoRoot = CreateRepo("DotNetAgenticRepairKit.sln");

        var config = RepairKitConfigResolver.Load(repoRoot, new AgentRunOptions(false, false, false, false, false));

        Assert.Equal(Path.Combine(repoRoot, "DotNetAgenticRepairKit.sln"), config.ResolvedSolutionPath);
        Assert.Equal(Path.Combine(repoRoot, ".agent"), config.ResolvedAgentOutputPath);
    }

    [Fact]
    public void ConfigOptionLoadsSpecifiedConfig()
    {
        var repoRoot = CreateRepo("Base.sln");
        File.WriteAllText(Path.Combine(repoRoot, "Other.sln"), string.Empty);
        var configPath = Path.Combine(repoRoot, "custom.json");
        File.WriteAllText(
            configPath,
            """
{
  "solutionPath": "Other.sln",
  "repoRoot": ".",
  "allowedEditPaths": ["src/"]
}
""");

        var config = RepairKitConfigResolver.Load(repoRoot, new AgentRunOptions(false, false, false, false, false, configPath));

        Assert.Equal(Path.Combine(repoRoot, "Other.sln"), config.ResolvedSolutionPath);
    }

    [Fact]
    public void OverridesWork()
    {
        var repoRoot = CreateRepo("Base.sln");
        var otherRepo = Path.Combine(_tempRoot, "other");
        Directory.CreateDirectory(otherRepo);
        File.WriteAllText(Path.Combine(otherRepo, "Other.sln"), string.Empty);

        var config = RepairKitConfigResolver.Load(
            repoRoot,
            new AgentRunOptions(
                false,
                false,
                false,
                false,
                false,
                SolutionOverride: Path.Combine(otherRepo, "Other.sln"),
                RepoRootOverride: otherRepo,
                AgentOutputOverride: Path.Combine(otherRepo, ".repairkit")));

        Assert.Equal(otherRepo, config.ResolvedRepoRoot);
        Assert.Equal(Path.Combine(otherRepo, "Other.sln"), config.ResolvedSolutionPath);
        Assert.Equal(Path.Combine(otherRepo, ".repairkit"), config.ResolvedAgentOutputPath);
    }

    [Fact]
    public void InvalidSolutionPathFailsValidation()
    {
        var repoRoot = CreateRepo("Base.sln");

        Assert.Throws<FileNotFoundException>(() =>
            RepairKitConfigResolver.Resolve(
                new RepairKitConfig { SolutionPath = "missing.sln" },
                repoRoot,
                repoRoot));
    }

    [Fact]
    public void EmptyAllowedEditPathsFailsValidation()
    {
        var repoRoot = CreateRepo("Base.sln");

        Assert.Throws<InvalidOperationException>(() =>
            RepairKitConfigResolver.Resolve(
                new RepairKitConfig { SolutionPath = "Base.sln", AllowedEditPaths = [] },
                repoRoot,
                repoRoot));
    }

    [Fact]
    public void CommandTemplateExpansionWorks()
    {
        var repoRoot = CreateRepo("Base.sln");
        var config = RepairKitConfigResolver.Resolve(
            new RepairKitConfig { SolutionPath = "Base.sln" },
            repoRoot,
            repoRoot);

        var command = CommandTemplate.Expand(
            "dotnet build \"{solutionPath}\" -p:OutputPath=\"{buildOutputPath}\"",
            config,
            "run",
            "build-bin");

        Assert.Contains(config.ResolvedSolutionPath, command);
        Assert.Contains("build-bin", command);
    }

    [Fact]
    public void PatchPathValidatorUsesConfiguredAllowedEditPaths()
    {
        var repoRoot = CreateRepo("Base.sln");
        var errors = PatchPathValidator.ValidateRelativePath(
            repoRoot,
            "custom/Allowed/File.cs",
            ["custom/Allowed/"],
            [".git"],
            ["secret"]);

        Assert.Empty(errors);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    private string CreateRepo(string solutionName)
    {
        var repoRoot = Path.Combine(_tempRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(repoRoot);
        File.WriteAllText(Path.Combine(repoRoot, solutionName), string.Empty);
        return repoRoot;
    }
}
