namespace RepairKit.Agent;

public sealed class GitDiffCapture
{
    private readonly ICommandRunner _commandRunner;

    public GitDiffCapture(ICommandRunner commandRunner)
    {
        _commandRunner = commandRunner;
    }

    public async Task<GitDiffCaptureResult> CaptureAsync(
        string repoRoot,
        string runId,
        CancellationToken cancellationToken = default)
    {
        var diffFile = AgentOutputPaths.GetGitDiffFile(repoRoot, runId);
        var errorFile = AgentOutputPaths.GetGitDiffErrorFile(repoRoot, runId);

        try
        {
            var result = await _commandRunner.RunAsync(
                "git",
                "diff -- src tests",
                repoRoot,
                cancellationToken);

            if (result.ExitCode == 0)
            {
                await File.WriteAllTextAsync(diffFile, result.StandardOutput, cancellationToken);
                return new GitDiffCaptureResult(true, diffFile, null, null);
            }

            var error = $"git diff failed with exit code {result.ExitCode}.{Environment.NewLine}{result.StandardError}{Environment.NewLine}{result.StandardOutput}";
            await File.WriteAllTextAsync(errorFile, error, cancellationToken);
            return new GitDiffCaptureResult(false, null, errorFile, error);
        }
        catch (Exception ex)
        {
            var error = "git diff failed: " + ex.Message;
            await File.WriteAllTextAsync(errorFile, error + Environment.NewLine, cancellationToken);
            return new GitDiffCaptureResult(false, null, errorFile, error);
        }
    }
}

