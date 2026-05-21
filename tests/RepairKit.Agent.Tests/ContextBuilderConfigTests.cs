using RepairKit.Agent;

namespace RepairKit.Agent.Tests;

public sealed class ContextBuilderConfigTests : IDisposable
{
    private readonly string _repoRoot = Path.Combine(Path.GetTempPath(), $"repairkit-context-config-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task ContextBuilderUsesConfiguredMaxContextCharacters()
    {
        const string runId = "20260521-120000";
        Directory.CreateDirectory(AgentOutputPaths.GetRunFolder(_repoRoot, runId));
        File.WriteAllText(Path.Combine(_repoRoot, "Demo.sln"), string.Empty);
        await File.WriteAllTextAsync(AgentOutputPaths.GetBuildOutputFile(_repoRoot, runId), new string('A', 100));
        await File.WriteAllTextAsync(AgentOutputPaths.GetTestOutputFile(_repoRoot, runId), "TicketSlaServiceTests failed.");

        var config = RepairKitConfigResolver.Resolve(
            new RepairKitConfig
            {
                SolutionPath = "Demo.sln",
                MaxContextCharacters = 10
            },
            _repoRoot,
            _repoRoot);

        await new ContextBuilder().BuildAsync(config, runId, CreateSummary());

        var packet = await File.ReadAllTextAsync(AgentOutputPaths.GetContextPacketFile(config, runId));
        Assert.Contains("[truncated]", packet);
    }

    public void Dispose()
    {
        if (Directory.Exists(_repoRoot))
        {
            Directory.Delete(_repoRoot, recursive: true);
        }
    }

    private static RunSummary CreateSummary()
    {
        return new RunSummary(
            "20260521-120000",
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
}

