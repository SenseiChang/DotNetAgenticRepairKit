namespace RepairKit.Agent;

public sealed record RunSummary(
    string RunId,
    DateTime StartedUtc,
    DateTime EndedUtc,
    long DurationMs,
    string WorkingDirectory,
    string BuildCommand,
    int BuildExitCode,
    bool BuildPassed,
    string TestCommand,
    int? TestExitCode,
    bool TestsPassed,
    bool OverallPassed,
    string BuildOutputFile,
    string TestOutputFile)
{
    public static RunSummary FromResults(
        string runId,
        DateTime startedUtc,
        DateTime endedUtc,
        string workingDirectory,
        CommandResult buildResult,
        CommandResult? testResult,
        string testCommand,
        string buildOutputFile,
        string testOutputFile)
    {
        var buildPassed = buildResult.ExitCode == 0;
        var testsPassed = testResult?.ExitCode == 0;

        return new RunSummary(
            runId,
            startedUtc,
            endedUtc,
            (long)(endedUtc - startedUtc).TotalMilliseconds,
            workingDirectory,
            buildResult.Command,
            buildResult.ExitCode,
            buildPassed,
            testResult?.Command ?? testCommand,
            testResult?.ExitCode,
            testsPassed,
            buildPassed && testsPassed,
            buildOutputFile,
            testOutputFile);
    }
}
