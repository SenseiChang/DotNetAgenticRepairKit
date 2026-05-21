namespace RepairKit.Agent;

public static class AgentProgram
{
    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            var runOptions = AgentRunOptions.Parse(args);
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

            RepairPlanResult? repairPlanResult = null;
            string? aiError = null;

            if (!summary.OverallPassed)
            {
                var contextBuilder = new ContextBuilder();
                await contextBuilder.BuildAsync(repoRoot, runId, summary);

                if (!runOptions.NoAi)
                {
                    try
                    {
                        var contextPacketPath = AgentOutputPaths.GetContextPacketFile(repoRoot, runId);
                        var contextPacket = await File.ReadAllTextAsync(contextPacketPath);
                        var openRouterOptions = OpenRouterOptions.FromEnvironment(requireApiKey: true);
                        var planner = new OpenRouterRepairPlanner(new HttpClient(), openRouterOptions);
                        repairPlanResult = await planner.CreateRepairPlanAsync(
                            contextPacket,
                            outputFolder,
                            CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        aiError = ex.Message;
                        await File.WriteAllTextAsync(
                            AgentOutputPaths.GetAiErrorFile(outputFolder),
                            ex.Message + Environment.NewLine);
                    }
                }
            }

            PrintSummary(
                summary,
                AgentOutputPaths.GetRelativeRunFolder(runId),
                runOptions,
                repairPlanResult,
                aiError);

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

    private static void PrintSummary(
        RunSummary summary,
        string outputFolder,
        AgentRunOptions runOptions,
        RepairPlanResult? repairPlanResult,
        string? aiError)
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

        if (!summary.OverallPassed)
        {
            if (runOptions.NoAi)
            {
                Console.WriteLine("AI Planning: skipped (--no-ai)");
            }
            else if (repairPlanResult is not null)
            {
                Console.WriteLine($"Model Used: {repairPlanResult.Model}");
                Console.WriteLine($"Repair Plan: {repairPlanResult.RepairPlanPath}");
                Console.WriteLine($"Risk Level: {repairPlanResult.Plan.RiskLevel}");
                Console.WriteLine($"Target Files: {string.Join(", ", repairPlanResult.Plan.TargetFiles)}");
                Console.WriteLine($"Change Count: {repairPlanResult.Plan.Changes.Count}");
            }
            else
            {
                Console.WriteLine("AI Planning: failed");
                Console.WriteLine($"AI Error: {aiError}");
            }
        }
    }
}
