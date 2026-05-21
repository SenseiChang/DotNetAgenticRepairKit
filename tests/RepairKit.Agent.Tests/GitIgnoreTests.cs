using RepairKit.Agent;

namespace RepairKit.Agent.Tests;

public sealed class GitIgnoreTests
{
    [Fact]
    public void RepoIndexJsonIsGitignored()
    {
        var repoRoot = RepoRootLocator.FindRepoRoot(Directory.GetCurrentDirectory());
        var gitignore = File.ReadAllText(Path.Combine(repoRoot, ".gitignore"));

        Assert.Contains(".agent/repo-index.json", gitignore);
    }
}
