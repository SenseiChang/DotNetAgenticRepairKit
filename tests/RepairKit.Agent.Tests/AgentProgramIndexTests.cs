using RepairKit.Agent;

namespace RepairKit.Agent.Tests;

[Collection("AgentProgram working directory")]
public sealed class AgentProgramIndexTests : IDisposable
{
    private readonly string _repoRoot = Path.Combine(Path.GetTempPath(), $"repairkit-agent-program-index-tests-{Guid.NewGuid():N}");
    private readonly string _originalDirectory = Directory.GetCurrentDirectory();

    [Fact]
    public async Task IndexOptionBuildsRepoIndexAndDoesNotCreateRunFolder()
    {
        Directory.CreateDirectory(_repoRoot);
        Directory.SetCurrentDirectory(_repoRoot);
        File.WriteAllText(Path.Combine(_repoRoot, RepoRootLocator.SolutionFileName), string.Empty);
        File.WriteAllText(
            Path.Combine(_repoRoot, "repairkit.config.json"),
            """
{
  "solutionPath": "DotNetAgenticRepairKit.sln",
  "repoRoot": ".",
  "agentOutputPath": ".agent",
  "allowedEditPaths": ["src/"],
  "blockedPathSegments": [".git", ".agent", "bin", "obj"],
  "blockedPathTerms": [".env", "secret", "token", "password", "key", "appsettings"]
}
""");
        var sourcePath = Path.Combine(_repoRoot, "src", "Demo", "Example.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(sourcePath)!);
        File.WriteAllText(sourcePath, "namespace Demo; public sealed class Example { }");

        var exitCode = await AgentProgram.RunAsync(["--index"]);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(Path.Combine(_repoRoot, ".agent", "repo-index.json")));
        Assert.False(Directory.Exists(Path.Combine(_repoRoot, ".agent", "runs")));
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_originalDirectory);
        if (Directory.Exists(_repoRoot))
        {
            Directory.Delete(_repoRoot, recursive: true);
        }
    }
}

[CollectionDefinition("AgentProgram working directory", DisableParallelization = true)]
public sealed class AgentProgramWorkingDirectoryCollection;
