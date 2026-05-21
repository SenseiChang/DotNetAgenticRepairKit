using RepairKit.Agent;

namespace RepairKit.Agent.Tests;

public sealed class RepoRootLocatorTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), $"repairkit-agent-tests-{Guid.NewGuid():N}");

    [Fact]
    public void FindsRepoRootFromNestedDirectory()
    {
        var repoRoot = Path.Combine(_tempRoot, "repo");
        var nestedDirectory = Path.Combine(repoRoot, "src", "RepairKit.Agent");
        Directory.CreateDirectory(nestedDirectory);
        File.WriteAllText(Path.Combine(repoRoot, RepoRootLocator.SolutionFileName), string.Empty);

        var detectedRoot = RepoRootLocator.FindRepoRoot(nestedDirectory);

        Assert.Equal(repoRoot, detectedRoot);
    }

    [Fact]
    public void ThrowsClearErrorWhenSolutionCannotBeFound()
    {
        var startDirectory = Path.Combine(_tempRoot, "not-a-repo", "child");
        Directory.CreateDirectory(startDirectory);

        var exception = Assert.Throws<DirectoryNotFoundException>(
            () => RepoRootLocator.FindRepoRoot(startDirectory));

        Assert.Contains(RepoRootLocator.SolutionFileName, exception.Message);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }
}

