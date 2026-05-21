using RepairKit.Agent;

namespace RepairKit.Agent.Tests;

public sealed class PatchApplierTests : IDisposable
{
    private readonly string _repoRoot = Path.Combine(Path.GetTempPath(), $"repairkit-patch-tests-{Guid.NewGuid():N}");
    private const string RunId = "20260521-100312";
    private const string TargetPath = "src/RepairKit.Core/Services/TicketSlaService.cs";

    [Fact]
    public async Task RefusesToApplyWithoutApprovalDecision()
    {
        CreateTargetFile("original");
        WritePlan(TargetPath);

        var result = await new PatchApplier().ApplyAsync(_repoRoot, RunId);

        Assert.False(result.Applied);
        Assert.Contains("approval-decision.json is missing", result.SkippedReason);
        Assert.Equal("original", ReadTargetFile());
    }

    [Fact]
    public async Task RefusesToApplyWhenApprovalIsFalse()
    {
        CreateTargetFile("original");
        WritePlan(TargetPath);
        WriteApproval(approved: false);

        var result = await new PatchApplier().ApplyAsync(_repoRoot, RunId);

        Assert.False(result.Applied);
        Assert.Contains("not approved", result.SkippedReason);
        Assert.Equal("original", ReadTargetFile());
    }

    [Fact]
    public async Task AppliesWhenApprovedAndPlanIsValid()
    {
        CreateTargetFile("original");
        WritePlan(TargetPath, replacement: "replacement");
        WriteApproval(approved: true);

        var result = await new PatchApplier().ApplyAsync(_repoRoot, RunId);

        Assert.True(result.Applied);
        Assert.Contains(TargetPath, result.ChangedFiles);
        Assert.Equal("replacement", ReadTargetFile());
        Assert.Single(result.BackupFiles);
        Assert.True(File.Exists(result.BackupFiles[0]));
        Assert.Equal("original", File.ReadAllText(result.BackupFiles[0]));
    }

    [Theory]
    [InlineData("C:/repo/src/RepairKit.Core/Services/TicketSlaService.cs")]
    [InlineData("/repo/src/RepairKit.Core/Services/TicketSlaService.cs")]
    public async Task RejectsAbsolutePaths(string path)
    {
        await AssertInvalidPathDoesNotModifyTarget(path);
    }

    [Fact]
    public async Task RejectsTraversalPaths()
    {
        await AssertInvalidPathDoesNotModifyTarget("../src/RepairKit.Core/Services/TicketSlaService.cs");
    }

    [Theory]
    [InlineData("src/RepairKit.Core/secret-token.cs")]
    [InlineData("src/RepairKit.Core/password.cs")]
    [InlineData("src/RepairKit.Core/appsettings.json")]
    public async Task RejectsBlockedPathTerms(string path)
    {
        await AssertInvalidPathDoesNotModifyTarget(path);
    }

    [Theory]
    [InlineData("scripts/repair.cs")]
    [InlineData("docs/repair.cs")]
    [InlineData(".agent/runs/file.cs")]
    [InlineData(".git/hooks/file.cs")]
    [InlineData("bin/file.cs")]
    [InlineData("obj/file.cs")]
    public async Task RejectsBlockedDirectories(string path)
    {
        await AssertInvalidPathDoesNotModifyTarget(path);
    }

    [Fact]
    public void CreatesExpectedBackupPath()
    {
        var path = PatchApplier.GetBackupPath(_repoRoot, RunId, TargetPath);

        Assert.Equal(
            Path.Combine(_repoRoot, ".agent", "runs", RunId, "backups", "src", "RepairKit.Core", "Services", "TicketSlaService.cs"),
            path);
    }

    [Fact]
    public async Task DoesNotModifyFilesIfAnyPlannedFileIsInvalid()
    {
        CreateTargetFile("original");
        WritePlan(TargetPath, "replacement", "scripts/blocked.cs");
        WriteApproval(approved: true);

        var result = await new PatchApplier().ApplyAsync(_repoRoot, RunId);

        Assert.False(result.Applied);
        Assert.Equal("original", ReadTargetFile());
        Assert.False(File.Exists(PatchApplier.GetBackupPath(_repoRoot, RunId, TargetPath)));
    }

    public void Dispose()
    {
        if (Directory.Exists(_repoRoot))
        {
            Directory.Delete(_repoRoot, recursive: true);
        }
    }

    private async Task AssertInvalidPathDoesNotModifyTarget(string invalidPath)
    {
        CreateTargetFile("original");
        WritePlan(invalidPath);
        WriteApproval(approved: true);

        var result = await new PatchApplier().ApplyAsync(_repoRoot, RunId);

        Assert.False(result.Applied);
        Assert.Equal("original", ReadTargetFile());
    }

    private void CreateTargetFile(string contents)
    {
        var path = Path.Combine(_repoRoot, TargetPath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
    }

    private string ReadTargetFile()
    {
        return File.ReadAllText(Path.Combine(_repoRoot, TargetPath.Replace('/', Path.DirectorySeparatorChar)));
    }

    private void WritePlan(params string[] paths)
    {
        WritePlan(paths[0], "replacement", paths.Skip(1).ToArray());
    }

    private void WritePlan(string firstPath, string replacement = "replacement", params string[] additionalPaths)
    {
        var targetFiles = new[] { firstPath }.Concat(additionalPaths).ToArray();
        var changes = targetFiles
            .Select(path => new RepairPlanChange(path, "Test change.", replacement))
            .ToArray();
        var plan = new RepairPlan(
            "Test repair.",
            "low",
            targetFiles,
            changes,
            ["dotnet test"]);

        Directory.CreateDirectory(AgentOutputPaths.GetRunFolder(_repoRoot, RunId));
        File.WriteAllText(
            AgentOutputPaths.GetRepairPlanFile(AgentOutputPaths.GetRunFolder(_repoRoot, RunId)),
            RepairPlanJsonSerializer.Serialize(plan));
    }

    private void WriteApproval(bool approved)
    {
        var decision = new RepairApprovalDecision(
            RunId,
            DateTime.UtcNow,
            approved,
            approved ? "APPLY" : "no",
            "low",
            [TargetPath],
            1,
            "Test decision.");

        Directory.CreateDirectory(AgentOutputPaths.GetRunFolder(_repoRoot, RunId));
        File.WriteAllText(
            AgentOutputPaths.GetApprovalDecisionFile(AgentOutputPaths.GetRunFolder(_repoRoot, RunId)),
            RepairApprovalDecisionJsonSerializer.Serialize(decision));
    }
}

