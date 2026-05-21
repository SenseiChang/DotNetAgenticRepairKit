namespace RepairKit.Agent;

public sealed class RepairKitConfig
{
    public string SolutionPath { get; set; } = RepoRootLocator.SolutionFileName;

    public string RepoRoot { get; set; } = ".";

    public string AgentOutputPath { get; set; } = ".agent";

    public string BuildCommand { get; set; } = "dotnet build \"{solutionPath}\" --no-incremental -p:OutputPath=\"{buildOutputPath}\"";

    public string TestCommand { get; set; } = "dotnet test \"{solutionPath}\" --no-build -p:OutputPath=\"{buildOutputPath}\"";

    public string GitDiffCommand { get; set; } = "git diff -- src tests";

    public List<string> AllowedEditPaths { get; set; } =
    [
        "src/RepairKit.Core/",
        "src/RepairKit.Web/",
        "src/RepairKit.Infrastructure/",
        "tests/"
    ];

    public List<string> BlockedPathSegments { get; set; } =
    [
        ".git",
        ".agent",
        "bin",
        "obj",
        ".vs",
        "node_modules",
        "scripts",
        "docs"
    ];

    public List<string> BlockedPathTerms { get; set; } =
    [
        ".env",
        "secret",
        "token",
        "password",
        "key",
        "appsettings"
    ];

    public int MaxContextCharacters { get; set; } = 80000;

    public int RecentHistoryLimit { get; set; } = 20;

    public string ResolvedRepoRoot { get; set; } = string.Empty;

    public string ResolvedSolutionPath { get; set; } = string.Empty;

    public string ResolvedAgentOutputPath { get; set; } = string.Empty;

    public static RepairKitConfig CreateDefault(string repoRoot)
    {
        var config = new RepairKitConfig();
        return RepairKitConfigResolver.Resolve(config, repoRoot, repoRoot);
    }

    public static RepairKitConfig CreateUnvalidatedDefault(string repoRoot)
    {
        var fullRepoRoot = Path.GetFullPath(repoRoot);
        return new RepairKitConfig
        {
            ResolvedRepoRoot = fullRepoRoot,
            ResolvedSolutionPath = Path.Combine(fullRepoRoot, RepoRootLocator.SolutionFileName),
            ResolvedAgentOutputPath = Path.Combine(fullRepoRoot, ".agent")
        };
    }
}
