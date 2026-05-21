using RepairKit.Agent;

namespace RepairKit.Agent.Tests;

public sealed class GitDiffCaptureTests : IDisposable
{
    private readonly string _repoRoot = Path.Combine(Path.GetTempPath(), $"repairkit-gitdiff-tests-{Guid.NewGuid():N}");
    private const string RunId = "20260521-110000";

    [Fact]
    public async Task SavesDiffWhenCommandSucceeds()
    {
        var runner = new FakeCommandRunner(new CommandResult(
            "git diff -- src tests",
            _repoRoot,
            "diff --git a/file.cs b/file.cs",
            string.Empty,
            0,
            DateTime.UtcNow,
            DateTime.UtcNow));

        var result = await new GitDiffCapture(runner).CaptureAsync(_repoRoot, RunId);

        Assert.True(result.Succeeded);
        Assert.True(File.Exists(AgentOutputPaths.GetGitDiffFile(_repoRoot, RunId)));
        Assert.Contains("diff --git", File.ReadAllText(AgentOutputPaths.GetGitDiffFile(_repoRoot, RunId)));
    }

    [Fact]
    public async Task WritesErrorFileWhenCommandFails()
    {
        var runner = new FakeCommandRunner(new CommandResult(
            "git diff -- src tests",
            _repoRoot,
            string.Empty,
            "git not found",
            1,
            DateTime.UtcNow,
            DateTime.UtcNow));

        var result = await new GitDiffCapture(runner).CaptureAsync(_repoRoot, RunId);

        Assert.False(result.Succeeded);
        Assert.True(File.Exists(AgentOutputPaths.GetGitDiffErrorFile(_repoRoot, RunId)));
        Assert.Contains("git not found", File.ReadAllText(AgentOutputPaths.GetGitDiffErrorFile(_repoRoot, RunId)));
    }

    public void Dispose()
    {
        if (Directory.Exists(_repoRoot))
        {
            Directory.Delete(_repoRoot, recursive: true);
        }
    }

    private sealed class FakeCommandRunner : ICommandRunner
    {
        private readonly CommandResult _result;

        public FakeCommandRunner(CommandResult result)
        {
            _result = result;
        }

        public Task<CommandResult> RunAsync(
            string fileName,
            string arguments,
            string workingDirectory,
            CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(AgentOutputPaths.GetRunFolder(workingDirectory, RunId));
            return Task.FromResult(_result);
        }
    }
}

