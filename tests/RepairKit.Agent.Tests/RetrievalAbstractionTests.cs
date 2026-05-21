using RepairKit.Agent;

namespace RepairKit.Agent.Tests;

public sealed class RetrievalAbstractionTests : IDisposable
{
    private readonly string _repoRoot = Path.Combine(Path.GetTempPath(), $"repairkit-retrieval-abstraction-tests-{Guid.NewGuid():N}");

    [Fact]
    public void RepoIndexerImplementsInterface()
    {
        Assert.IsAssignableFrom<IRepoIndexer>(new RepoIndexer());
    }

    [Fact]
    public void RepoIndexContextRetrieverImplementsInterface()
    {
        Assert.IsAssignableFrom<IContextRetriever>(new RepoIndexContextRetriever());
    }

    [Fact]
    public async Task ContextBuilderUsesInjectedContextRetriever()
    {
        const string runId = "20260521-160000";
        var config = CreateConfig(runId);
        CreateFile("src/Injected/File.cs", "public sealed class InjectedFile { }");
        await File.WriteAllTextAsync(AgentOutputPaths.GetBuildOutputFile(config, runId), "Build succeeded.");
        await File.WriteAllTextAsync(AgentOutputPaths.GetTestOutputFile(config, runId), "Injected failure.");
        var builder = new ContextBuilder(
            new NoOpRepoIndexer(),
            new ExistingRepoIndexStore(),
            new FakeContextRetriever(["src/Injected/File.cs"]));

        var metadata = await builder.BuildAsync(config, runId, CreateSummary(runId));
        var packet = await File.ReadAllTextAsync(AgentOutputPaths.GetContextPacketFile(config, runId));

        Assert.Equal("repo-index", metadata.RetrievalMode);
        Assert.Contains("src/Injected/File.cs", metadata.IncludedFiles);
        Assert.Contains("### File: src/Injected/File.cs", packet);
    }

    [Fact]
    public async Task ContextBuilderFallsBackWhenRetrieverReturnsNoFiles()
    {
        const string runId = "20260521-160001";
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
        var builder = new ContextBuilder(
            new NoOpRepoIndexer(),
            new ExistingRepoIndexStore(),
            new FakeContextRetriever([]));

        var metadata = await builder.BuildAsync(config, runId, CreateSummary(runId));

        Assert.Equal("fallback-keyword", metadata.RetrievalMode);
        Assert.Contains("src/RepairKit.Core/Services/TicketSlaService.cs", metadata.IncludedFiles);
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

    private sealed class NoOpRepoIndexer : IRepoIndexer
    {
        public Task<RepoIndexBuildResult> BuildAsync(
            RepairKitConfig config,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new RepoIndexBuildResult(
                AgentOutputPaths.GetRepoIndexFile(config),
                0,
                0,
                [],
                []));
        }
    }

    private sealed class ExistingRepoIndexStore : IRepoIndexStore
    {
        public Task<bool> ExistsAsync(
            RepairKitConfig config,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task<RepoIndex> ReadAsync(
            RepairKitConfig config,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new RepoIndex(DateTime.UtcNow, config.ResolvedRepoRoot, []));
        }

        public Task WriteAsync(
            RepairKitConfig config,
            RepoIndex index,
            CancellationToken cancellationToken = default)
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
                includedFiles.Select(file => new RetrievedContextFile(file, 100, ["fake"])).ToArray(),
                []));
        }
    }
}
