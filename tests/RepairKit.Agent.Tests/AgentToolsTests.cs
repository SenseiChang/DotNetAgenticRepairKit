using System.Text.Json;
using RepairKit.Agent;

namespace RepairKit.Agent.Tests;

public sealed class AgentToolsTests : IDisposable
{
    private readonly string _repoRoot = Path.Combine(Path.GetTempPath(), $"repairkit-tool-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task BuildRepoIndexToolCallsIndexerAndReturnsSuccess()
    {
        var config = CreateConfig("20260521-170000");
        var tool = new BuildRepoIndexTool(new FakeRepoIndexer());

        var result = await tool.ExecuteAsync(CreateContext(config, "20260521-170000"), "{}");
        var output = JsonDocument.Parse(result.OutputJson).RootElement;

        Assert.True(result.Succeeded);
        Assert.Equal("build_repo_index", result.ToolName);
        Assert.Equal(3, output.GetProperty("indexedFileCount").GetInt32());
    }

    [Fact]
    public async Task BuildContextPacketToolRunsContextBuilder()
    {
        const string runId = "20260521-170001";
        var config = CreateConfig(runId);
        CreateFile("src/RepairKit.Core/Services/TicketSlaService.cs", "public sealed class TicketSlaService { }");
        CreateFile("tests/RepairKit.Tests/TicketSlaServiceTests.cs", "public sealed class TicketSlaServiceTests { }");
        CreateFile("src/RepairKit.Core/Models/Ticket.cs", "public sealed class Ticket { }");
        CreateFile("src/RepairKit.Core/Models/CustomerTier.cs", "public enum CustomerTier { Standard }");
        CreateFile("src/RepairKit.Core/Models/Severity.cs", "public enum Severity { Critical }");
        CreateFile("src/RepairKit.Core/Models/TicketStatus.cs", "public enum TicketStatus { New }");
        CreateFile("src/RepairKit.Core/Models/AssignedTeam.cs", "public enum AssignedTeam { Support }");
        await File.WriteAllTextAsync(AgentOutputPaths.GetBuildOutputFile(config, runId), "Build succeeded.");
        await File.WriteAllTextAsync(AgentOutputPaths.GetTestOutputFile(config, runId), "TicketSlaServiceTests failed.");
        await RunSummaryJsonSerializer.WriteAsync(AgentOutputPaths.GetRunSummaryFile(config, runId), CreateSummary(runId));
        var tool = new BuildContextPacketTool(new ContextBuilder(
            new FakeRepoIndexer(),
            new MissingRepoIndexStore(),
            new FakeContextRetriever([])));

        var result = await tool.ExecuteAsync(CreateContext(config, runId), "{}");

        Assert.True(result.Succeeded);
        Assert.True(File.Exists(AgentOutputPaths.GetContextPacketFile(config, runId)));
    }

    [Fact]
    public async Task CaptureGitDiffToolHandlesSuccessAndFailure()
    {
        var successConfig = CreateConfig("20260521-170002");
        var success = await new CaptureGitDiffTool().ExecuteAsync(
            CreateContext(successConfig, "20260521-170002", new FakeCommandRunner(0, "diff --git", string.Empty)),
            "{}");
        Assert.True(success.Succeeded);
        Assert.True(File.Exists(AgentOutputPaths.GetGitDiffFile(successConfig, "20260521-170002")));

        var failureConfig = CreateConfig("20260521-170003");
        var failure = await new CaptureGitDiffTool().ExecuteAsync(
            CreateContext(failureConfig, "20260521-170003", new FakeCommandRunner(1, string.Empty, "git failed")),
            "{}");
        Assert.False(failure.Succeeded);
        Assert.True(File.Exists(AgentOutputPaths.GetGitDiffErrorFile(failureConfig, "20260521-170003")));
    }

    [Fact]
    public async Task ToolEventsJsonlIsAppendedWithoutSecrets()
    {
        var config = CreateConfig("20260521-170004");
        var result = await new BuildRepoIndexTool(new FakeRepoIndexer()).ExecuteAsync(
            CreateContext(config, "20260521-170004"),
            """{ "OPENROUTER_API_KEY": "secret-value" }""");

        var eventsPath = Path.Combine(AgentOutputPaths.GetRunFolder(config, "20260521-170004"), "tool-events.jsonl");
        var events = await File.ReadAllTextAsync(eventsPath);

        Assert.True(result.Succeeded);
        Assert.Contains("build_repo_index", events);
        Assert.DoesNotContain("secret-value", events);
        Assert.DoesNotContain("OPENROUTER_API_KEY", events);
    }

    [Fact]
    public async Task ReadArtifactToolRejectsUnsafePaths()
    {
        var config = CreateConfig("20260521-170005");

        var result = await new ReadArtifactTool().ExecuteAsync(
            CreateContext(config, "20260521-170005"),
            """{ "fileName": "../secrets.txt" }""");

        Assert.False(result.Succeeded);
        Assert.Contains("not allowed", result.ErrorMessage);
    }

    public void Dispose()
    {
        if (Directory.Exists(_repoRoot))
        {
            Directory.Delete(_repoRoot, recursive: true);
        }
    }

    private RepairKitConfig CreateConfig(string runId)
    {
        Directory.CreateDirectory(AgentOutputPaths.GetRunFolder(_repoRoot, runId));
        File.WriteAllText(Path.Combine(_repoRoot, "Demo.sln"), string.Empty);
        return RepairKitConfigResolver.Resolve(
            new RepairKitConfig
            {
                SolutionPath = "Demo.sln",
                AllowedEditPaths = ["src/", "tests/"]
            },
            _repoRoot,
            _repoRoot);
    }

    private AgentToolContext CreateContext(
        RepairKitConfig config,
        string runId,
        ICommandRunner? commandRunner = null)
    {
        return new AgentToolContext(
            config,
            config.ResolvedRepoRoot,
            runId,
            AgentOutputPaths.GetRunFolder(config, runId),
            commandRunner ?? new FakeCommandRunner(0, string.Empty, string.Empty));
    }

    private void CreateFile(string relativePath, string contents)
    {
        var path = Path.Combine(_repoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
    }

    private static RunSummary CreateSummary(string runId)
    {
        return new RunSummary(
            runId,
            DateTime.UtcNow,
            DateTime.UtcNow,
            1,
            @"H:\repo",
            "dotnet build",
            0,
            true,
            "dotnet test",
            1,
            false,
            false,
            "build-output.txt",
            "test-output.txt");
    }

    private sealed class FakeRepoIndexer : IRepoIndexer
    {
        public Task<RepoIndexBuildResult> BuildAsync(
            RepairKitConfig config,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new RepoIndexBuildResult(
                AgentOutputPaths.GetRepoIndexFile(config),
                3,
                2,
                ["a.cs", "b.cs", "c.cs"],
                ["bin/a.cs", ".agent/run.json"]));
        }
    }

    private sealed class MissingRepoIndexStore : IRepoIndexStore
    {
        public Task<bool> ExistsAsync(RepairKitConfig config, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<RepoIndex> ReadAsync(RepairKitConfig config, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task WriteAsync(RepairKitConfig config, RepoIndex index, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeContextRetriever(IReadOnlyList<string> includedFiles) : IContextRetriever
    {
        public Task<ContextRetrievalResult> RetrieveAsync(
            ContextRetrievalRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ContextRetrievalResult(
                request.DetectedKeywords,
                includedFiles,
                [],
                []));
        }
    }

    private sealed class FakeCommandRunner(int exitCode, string stdout, string stderr) : ICommandRunner
    {
        public Task<CommandResult> RunAsync(
            string fileName,
            string arguments,
            string workingDirectory,
            CancellationToken cancellationToken = default)
        {
            var started = DateTime.UtcNow;
            return Task.FromResult(new CommandResult(
                $"{fileName} {arguments}".Trim(),
                workingDirectory,
                stdout,
                stderr,
                exitCode,
                started,
                started.AddMilliseconds(10)));
        }
    }
}
