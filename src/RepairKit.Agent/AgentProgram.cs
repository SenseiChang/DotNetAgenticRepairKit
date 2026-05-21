namespace RepairKit.Agent;

public static class AgentProgram
{
    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            var repoRoot = RepoRootLocator.FindRepoRoot(Directory.GetCurrentDirectory());
            var runId = RunIdGenerator.Create();
            var outputFolder = AgentOutputPaths.GetRunFolder(repoRoot, runId);

            Directory.CreateDirectory(outputFolder);

            var runner = new CommandRunner();
            var startedUtc = DateTime.UtcNow;
            var buildArtifactsFolder = AgentOutputPaths.GetBuildArtifactsFolder(repoRoot, runId);
            Directory.CreateDirectory(buildArtifactsFolder);

            var solutionPath = Path.Combine(repoRoot, RepoRootLocator.SolutionFileName);
            var buildOutputPath = Path.EndsInDirectorySeparator(buildArtifactsFolder)
                ? buildArtifactsFolder
                : buildArtifactsFolder + Path.DirectorySeparatorChar;
            var outputPathArgument = $"-p:OutputPath=\"{buildOutputPath}\"";

            var buildArguments = $"build \"{solutionPath}\" --no-incremental {outputPathArgument}";
            var testArguments = $"test \"{solutionPath}\" --no-build {outputPathArgument}";

            var buildResult = await runner.RunAsync("dotnet", buildArguments, repoRoot);
            CommandResult? testResult = null;

            var buildOutputFile = AgentOutputPaths.GetBuildOutputFile(repoRoot, runId);
            var outputFile = AgentOutputPaths.GetTestOutputFile(repoRoot, runId);
            var summaryFile = AgentOutputPaths.GetRunSummaryFile(repoRoot, runId);

            await File.WriteAllTextAsync(buildOutputFile, TestOutputFormatter.Format(buildResult));

            if (buildResult.ExitCode == 0)
            {
                testResult = await runner.RunAsync("dotnet", testArguments, repoRoot);
                await File.WriteAllTextAsync(outputFile, TestOutputFormatter.Format(testResult));
            }
            else
            {
                await File.WriteAllTextAsync(
                    outputFile,
                    "Tests were not run because the build failed." + Environment.NewLine);
            }

            var endedUtc = DateTime.UtcNow;
            var summary = RunSummary.FromResults(
                runId,
                startedUtc,
                endedUtc,
                repoRoot,
                buildResult,
                testResult,
                $"dotnet {testArguments}",
                buildOutputFile,
                outputFile);

            await RunSummaryJsonSerializer.WriteAsync(summaryFile, summary);

            PrintSummary(summary, AgentOutputPaths.GetRelativeRunFolder(runId));

            if (!summary.BuildPassed)
            {
                return summary.BuildExitCode == 0 ? 1 : summary.BuildExitCode;
            }

            if (!summary.TestsPassed)
            {
                return summary.TestExitCode.GetValueOrDefault(1) == 0
                    ? 1
                    : summary.TestExitCode.GetValueOrDefault(1);
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Agent run failed.");
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static void PrintSummary(RunSummary summary, string outputFolder)
    {
        Console.WriteLine();
        Console.WriteLine("Agent run complete.");
        Console.WriteLine($"Run ID: {summary.RunId}");
        Console.WriteLine($"Build Command: {summary.BuildCommand}");
        Console.WriteLine($"Build Exit Code: {summary.BuildExitCode}");
        Console.WriteLine($"Build Passed: {summary.BuildPassed}");
        Console.WriteLine($"Test Command: {summary.TestCommand}");
        Console.WriteLine($"Test Exit Code: {summary.TestExitCode?.ToString() ?? "Not run"}");
        Console.WriteLine($"Tests Passed: {summary.TestsPassed}");
        Console.WriteLine($"Overall Passed: {summary.OverallPassed}");
        Console.WriteLine($"Output: {outputFolder}");
    }
}
