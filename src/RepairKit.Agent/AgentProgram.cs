namespace RepairKit.Agent;

public static class AgentProgram
{
    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            var runOptions = AgentRunOptions.Parse(args);
            var detectedRepoRoot = RepoRootLocator.FindRepoRoot(Directory.GetCurrentDirectory());
            var config = RepairKitConfigResolver.Load(detectedRepoRoot, runOptions);
            var repoRoot = config.ResolvedRepoRoot;
            var runId = RunIdGenerator.Create();
            var outputFolder = AgentOutputPaths.GetRunFolder(config, runId);

            Directory.CreateDirectory(outputFolder);

            var runner = new CommandRunner();
            var startedUtc = DateTime.UtcNow;
            var buildArtifactsFolder = AgentOutputPaths.GetBuildArtifactsFolder(config, runId);
            Directory.CreateDirectory(buildArtifactsFolder);

            var buildOutputPath = Path.EndsInDirectorySeparator(buildArtifactsFolder)
                ? buildArtifactsFolder
                : buildArtifactsFolder + Path.DirectorySeparatorChar;
            var buildCommand = CommandTemplate.Expand(config.BuildCommand, config, outputFolder, buildOutputPath);
            var testCommand = CommandTemplate.Expand(config.TestCommand, config, outputFolder, buildOutputPath);
            var (buildFileName, buildArguments) = CommandTemplate.Split(buildCommand);
            var (testFileName, testArguments) = CommandTemplate.Split(testCommand);

            var buildResult = await runner.RunAsync(buildFileName, buildArguments, repoRoot);
            CommandResult? testResult = null;

            var buildOutputFile = AgentOutputPaths.GetBuildOutputFile(config, runId);
            var outputFile = AgentOutputPaths.GetTestOutputFile(config, runId);
            var summaryFile = AgentOutputPaths.GetRunSummaryFile(config, runId);

            await File.WriteAllTextAsync(buildOutputFile, TestOutputFormatter.Format(buildResult));

            if (buildResult.ExitCode == 0)
            {
                testResult = await runner.RunAsync(testFileName, testArguments, repoRoot);
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
                testCommand,
                buildOutputFile,
                outputFile);

            await RunSummaryJsonSerializer.WriteAsync(summaryFile, summary);

            RepairPlanResult? repairPlanResult = null;
            RepairApprovalDecision? approvalDecision = null;
            PatchApplicationResult? patchApplicationResult = null;
            GitDiffCaptureResult? gitDiffResult = null;
            string? repairReportPath = null;
            string? aiError = null;
            var contextGenerated = false;

            if (!summary.OverallPassed)
            {
                var contextBuilder = new ContextBuilder();
                await contextBuilder.BuildAsync(config, runId, summary);
                contextGenerated = true;
                await new RelatedRunMemory().AppendToContextPacketAsync(config, runId);

                if (!runOptions.NoAi)
                {
                    try
                    {
                        var contextPacketPath = AgentOutputPaths.GetContextPacketFile(config, runId);
                        var contextPacket = await File.ReadAllTextAsync(contextPacketPath);
                        var openRouterOptions = OpenRouterOptions.FromEnvironment(requireApiKey: true);
                        var planner = new OpenRouterRepairPlanner(new HttpClient(), openRouterOptions);
                        repairPlanResult = await planner.CreateRepairPlanAsync(
                            contextPacket,
                            outputFolder,
                            CancellationToken.None);

                        var approvalService = new RepairApprovalService(new ConsoleUserPrompt());
                        approvalDecision = await approvalService.RequestApprovalAsync(
                            runId,
                            repairPlanResult.Plan,
                            runOptions,
                            outputFolder);

                        if (approvalDecision is not null &&
                            approvalDecision.Approved &&
                            !runOptions.NoApply)
                        {
                            var patchApplier = new PatchApplier();
                            patchApplicationResult = await patchApplier.ApplyAsync(
                                config,
                                runId,
                                CancellationToken.None);

                            var gitDiffCapture = new GitDiffCapture(new CommandRunner());
                            gitDiffResult = await gitDiffCapture.CaptureAsync(config, runId);
                            repairReportPath = await new RepairReportWriter().WriteAsync(config, runId);
                        }
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
                Path.GetRelativePath(repoRoot, outputFolder),
                runOptions,
                repairPlanResult,
                approvalDecision,
                patchApplicationResult,
                gitDiffResult,
                repairReportPath,
                aiError);

            await TryAppendHistoryAsync(
                config,
                summary,
                runOptions,
                contextGenerated,
                repairPlanResult,
                approvalDecision,
                patchApplicationResult,
                gitDiffResult,
                repairReportPath,
                aiError);

            if (patchApplicationResult is not null)
            {
                return patchApplicationResult.ValidationOverallPassed ? 0 : 1;
            }

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
        RepairApprovalDecision? approvalDecision,
        PatchApplicationResult? patchApplicationResult,
        GitDiffCaptureResult? gitDiffResult,
        string? repairReportPath,
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

            if (repairPlanResult is not null)
            {
                Console.WriteLine($"Repair Plan Path: {repairPlanResult.RepairPlanPath}");
            }

            if (approvalDecision is not null)
            {
                var approvalPath = AgentOutputPaths.GetApprovalDecisionFile(
                    Path.Combine(
                        summary.WorkingDirectory,
                        AgentOutputPaths.GetRelativeRunFolder(summary.RunId)));
                Console.WriteLine($"Approval Decision Path: {approvalPath}");
                Console.WriteLine($"Approved: {approvalDecision.Approved}");
            }

            if (runOptions.NoApply && approvalDecision?.Approved == true)
            {
                Console.WriteLine("Patch Applied: false (--no-apply)");
            }

            if (patchApplicationResult is not null)
            {
                var patchApplicationPath = AgentOutputPaths.GetPatchApplicationFile(
                    Path.Combine(
                        summary.WorkingDirectory,
                        AgentOutputPaths.GetRelativeRunFolder(summary.RunId)));

                Console.WriteLine($"Patch Applied: {patchApplicationResult.Applied}");
                Console.WriteLine("Changed files:");
                foreach (var changedFile in patchApplicationResult.ChangedFiles)
                {
                    Console.WriteLine($"- {changedFile}");
                }

                Console.WriteLine("Backup files:");
                foreach (var backupFile in patchApplicationResult.BackupFiles)
                {
                    Console.WriteLine($"- {backupFile}");
                }

                Console.WriteLine($"Validation Build Passed: {patchApplicationResult.ValidationBuildPassed}");
                Console.WriteLine($"Validation Tests Passed: {patchApplicationResult.ValidationTestsPassed}");
                Console.WriteLine($"Validation Overall Passed: {patchApplicationResult.ValidationOverallPassed}");
                Console.WriteLine($"Patch Application Result: {patchApplicationPath}");
            }

            if (gitDiffResult?.DiffFile is not null)
            {
                Console.WriteLine($"Git Diff: {gitDiffResult.DiffFile}");
            }
            else if (gitDiffResult?.ErrorFile is not null)
            {
                Console.WriteLine($"Git Diff Error: {gitDiffResult.ErrorFile}");
            }

            if (repairReportPath is not null)
            {
                Console.WriteLine($"Repair Report: {repairReportPath}");
            }

            Console.WriteLine("Next step: Review generated artifacts before committing changes.");
        }
    }

    private static async Task TryAppendHistoryAsync(
        RepairKitConfig config,
        RunSummary summary,
        AgentRunOptions runOptions,
        bool contextGenerated,
        RepairPlanResult? repairPlanResult,
        RepairApprovalDecision? approvalDecision,
        PatchApplicationResult? patchApplicationResult,
        GitDiffCaptureResult? gitDiffResult,
        string? repairReportPath,
        string? aiError)
    {
        try
        {
            var entry = AgentRunHistoryEntryFactory.Create(
                summary,
                runOptions,
                contextGenerated,
                repairPlanResult,
                approvalDecision,
                patchApplicationResult,
                gitDiffResult,
                repairReportPath,
                aiError);

            await new AgentRunHistoryWriter().AppendAsync(config, entry);
            Console.WriteLine($"History: {Path.Combine(".agent", "history.jsonl")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: failed to append agent history: {ex.Message}");
        }
    }
}
