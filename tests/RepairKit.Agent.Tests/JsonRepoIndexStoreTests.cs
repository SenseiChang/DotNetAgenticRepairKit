using RepairKit.Agent;

namespace RepairKit.Agent.Tests;

public sealed class JsonRepoIndexStoreTests : IDisposable
{
    private readonly string _repoRoot = Path.Combine(Path.GetTempPath(), $"repairkit-index-store-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task PreservesRepoIndexReadWriteBehavior()
    {
        var config = CreateConfig();
        var store = new JsonRepoIndexStore();
        var index = new RepoIndex(
            DateTime.UtcNow,
            config.ResolvedRepoRoot,
            [
                new RepoIndexEntry(
                    "src/Demo.cs",
                    "Demo.cs",
                    ".cs",
                    10,
                    "hash",
                    ["Demo"],
                    ["Sample"],
                    ["demo"],
                    "public sealed class Demo { }",
                    DateTime.UtcNow)
            ]);

        await store.WriteAsync(config, index);
        var exists = await store.ExistsAsync(config);
        var roundTrip = await store.ReadAsync(config);

        Assert.True(exists);
        Assert.Equal("src/Demo.cs", Assert.Single(roundTrip.Entries).FilePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_repoRoot))
        {
            Directory.Delete(_repoRoot, recursive: true);
        }
    }

    private RepairKitConfig CreateConfig()
    {
        Directory.CreateDirectory(_repoRoot);
        File.WriteAllText(Path.Combine(_repoRoot, "Demo.sln"), string.Empty);
        return RepairKitConfigResolver.Resolve(
            new RepairKitConfig { SolutionPath = "Demo.sln" },
            _repoRoot,
            _repoRoot);
    }
}
