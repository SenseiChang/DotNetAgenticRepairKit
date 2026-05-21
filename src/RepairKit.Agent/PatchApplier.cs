using System.Text.Json;

namespace RepairKit.Agent;

public sealed class PatchApplier
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<PatchApplicationResult> ApplyAsync(
        string repoRoot,
        string runId,
        CancellationToken cancellationToken = default)
    {
        var runFolder = AgentOutputPaths.GetRunFolder(repoRoot, runId);
        var resultPath = AgentOutputPaths.GetPatchApplicationFile(runFolder);
        var validationBuildOutputFile = AgentOutputPaths.GetValidationBuildOutputFile(repoRoot, runId);
        var validationTestOutputFile = AgentOutputPaths.GetValidationTestOutputFile(repoRoot, runId);

        try
        {
            var planPath = AgentOutputPaths.GetRepairPlanFile(runFolder);
            var approvalPath = AgentOutputPaths.GetApprovalDecisionFile(runFolder);

            if (!File.Exists(approvalPath))
            {
                return await WriteSkippedAsync("approval-decision.json is missing.", approved: false);
            }

            var approval = JsonSerializer.Deserialize<RepairApprovalDecision>(
                await File.ReadAllTextAsync(approvalPath, cancellationToken),
                JsonOptions);

            if (approval is null)
            {
                return await WriteSkippedAsync("approval-decision.json could not be parsed.", approved: false);
            }

            if (!approval.Approved)
            {
                return await WriteSkippedAsync("Repair was not approved.", approved: false);
            }

            if (!File.Exists(planPath))
            {
                return await WriteSkippedAsync("repair-plan.json is missing.", approved: true);
            }

            var plan = RepairPlanJsonSerializer.Parse(
                await File.ReadAllTextAsync(planPath, cancellationToken));

            var validationErrors = ValidatePlanForPatch(repoRoot, plan).ToArray();
            if (validationErrors.Length > 0)
            {
                return await WriteSkippedAsync(
                    "Patch safety validation failed: " + string.Join(" ", validationErrors),
                    approved: true);
            }

            var changedFiles = plan.Changes.Select(change => Normalize(change.FilePath)).ToArray();
            var backupFiles = changedFiles
                .Select(path => AgentOutputPaths.GetBackupFile(repoRoot, runId, path))
                .ToArray();

            foreach (var changedFile in changedFiles)
            {
                var sourcePath = GetFullRepoPath(repoRoot, changedFile);
                if (!File.Exists(sourcePath))
                {
                    return await WriteSkippedAsync($"Target file does not exist: {changedFile}", approved: true);
                }
            }

            for (var i = 0; i < changedFiles.Length; i++)
            {
                var sourcePath = GetFullRepoPath(repoRoot, changedFiles[i]);
                var backupPath = backupFiles[i];
                Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
                File.Copy(sourcePath, backupPath, overwrite: true);
            }

            try
            {
                foreach (var change in plan.Changes)
                {
                    var sourcePath = GetFullRepoPath(repoRoot, Normalize(change.FilePath));
                    await File.WriteAllTextAsync(sourcePath, change.FullReplacement, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await File.WriteAllTextAsync(
                    AgentOutputPaths.GetPatchErrorFile(runFolder),
                    ex.Message + Environment.NewLine,
                    cancellationToken);

                return await WriteResultAsync(new PatchApplicationResult(
                    runId,
                    DateTime.UtcNow,
                    true,
                    false,
                    changedFiles,
                    backupFiles,
                    "Patch write failed: " + ex.Message,
                    null,
                    null,
                    false,
                    false,
                    false,
                    validationBuildOutputFile,
                    validationTestOutputFile));
            }

            var validation = await RunValidationAsync(repoRoot, runId, cancellationToken);

            return await WriteResultAsync(new PatchApplicationResult(
                runId,
                DateTime.UtcNow,
                true,
                true,
                changedFiles,
                backupFiles,
                null,
                validation.BuildResult.ExitCode,
                validation.TestResult?.ExitCode,
                validation.BuildResult.ExitCode == 0,
                validation.TestResult?.ExitCode == 0,
                validation.BuildResult.ExitCode == 0 && validation.TestResult?.ExitCode == 0,
                validationBuildOutputFile,
                validationTestOutputFile));
        }
        catch (Exception ex)
        {
            await File.WriteAllTextAsync(
                AgentOutputPaths.GetPatchErrorFile(runFolder),
                ex.Message + Environment.NewLine,
                cancellationToken);

            return await WriteSkippedAsync("Patch application failed: " + ex.Message, approved: false);
        }

        async Task<PatchApplicationResult> WriteSkippedAsync(string reason, bool approved)
        {
            return await WriteResultAsync(new PatchApplicationResult(
                runId,
                DateTime.UtcNow,
                approved,
                false,
                [],
                [],
                reason,
                null,
                null,
                false,
                false,
                false,
                validationBuildOutputFile,
                validationTestOutputFile));
        }

        async Task<PatchApplicationResult> WriteResultAsync(PatchApplicationResult result)
        {
            await PatchApplicationResultJsonSerializer.WriteAsync(resultPath, result);
            return result;
        }
    }

    public static string GetBackupPath(string repoRoot, string runId, string relativePath)
    {
        return AgentOutputPaths.GetBackupFile(repoRoot, runId, Normalize(relativePath));
    }

    private static IEnumerable<string> ValidatePlanForPatch(string repoRoot, RepairPlan plan)
    {
        foreach (var change in plan.Changes)
        {
            foreach (var error in PatchPathValidator.ValidateRelativePath(repoRoot, change.FilePath))
            {
                yield return error;
            }

            if (string.IsNullOrWhiteSpace(change.FullReplacement))
            {
                yield return $"Change for '{change.FilePath}' has an empty fullReplacement.";
            }
        }
    }

    private static async Task<(CommandResult BuildResult, CommandResult? TestResult)> RunValidationAsync(
        string repoRoot,
        string runId,
        CancellationToken cancellationToken)
    {
        var validationArtifactsFolder = AgentOutputPaths.GetValidationArtifactsFolder(repoRoot, runId);
        Directory.CreateDirectory(validationArtifactsFolder);

        var solutionPath = Path.Combine(repoRoot, RepoRootLocator.SolutionFileName);
        var validationOutputPath = Path.EndsInDirectorySeparator(validationArtifactsFolder)
            ? validationArtifactsFolder
            : validationArtifactsFolder + Path.DirectorySeparatorChar;
        var outputPathArgument = $"-p:OutputPath=\"{validationOutputPath}\"";
        var runner = new CommandRunner();

        var buildResult = await runner.RunAsync(
            "dotnet",
            $"build \"{solutionPath}\" --no-incremental {outputPathArgument}",
            repoRoot,
            cancellationToken);

        await File.WriteAllTextAsync(
            AgentOutputPaths.GetValidationBuildOutputFile(repoRoot, runId),
            TestOutputFormatter.Format(buildResult),
            cancellationToken);

        if (buildResult.ExitCode != 0)
        {
            await File.WriteAllTextAsync(
                AgentOutputPaths.GetValidationTestOutputFile(repoRoot, runId),
                "Validation tests were not run because validation build failed." + Environment.NewLine,
                cancellationToken);

            return (buildResult, null);
        }

        var testResult = await runner.RunAsync(
            "dotnet",
            $"test \"{solutionPath}\" --no-build {outputPathArgument}",
            repoRoot,
            cancellationToken);

        await File.WriteAllTextAsync(
            AgentOutputPaths.GetValidationTestOutputFile(repoRoot, runId),
            TestOutputFormatter.Format(testResult),
            cancellationToken);

        return (buildResult, testResult);
    }

    private static string GetFullRepoPath(string repoRoot, string relativePath)
    {
        return Path.GetFullPath(Path.Combine(repoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static string Normalize(string relativePath)
    {
        return relativePath.Replace('\\', '/');
    }
}

