using RepairKit.Agent;

namespace RepairKit.Agent.Tests;

public sealed class RepoIndexerTests : IDisposable
{
    private readonly string _repoRoot = Path.Combine(Path.GetTempPath(), $"repairkit-indexer-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task ExcludesBlockedDirectories()
    {
        var config = CreateConfig();
        CreateFile("src/Allowed/Included.cs", "namespace Demo; public sealed class Included { }");
        CreateFile("src/Allowed/bin/Generated.cs", "public sealed class Generated { }");
        CreateFile(".agent/runs/1/context-packet.md", "# context");
        CreateFile("node_modules/pkg/file.cs", "public sealed class PackageFile { }");

        var result = await new RepoIndexer().BuildAsync(config);
        var index = await RepoIndexJsonSerializer.ReadAsync(config.ResolvedRepoIndexPath);

        Assert.Contains("src/Allowed/Included.cs", result.IndexedFiles);
        Assert.DoesNotContain(index.Entries, entry => entry.FilePath.Contains("/bin/", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(index.Entries, entry => entry.FilePath.StartsWith(".agent/", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(index.Entries, entry => entry.FilePath.StartsWith("node_modules/", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExcludesSecretAndConfigSensitiveFiles()
    {
        var config = CreateConfig();
        CreateFile("src/Allowed/Safe.cs", "public sealed class Safe { }");
        CreateFile("src/Allowed/ApiKey.cs", "public sealed class ApiKey { }");
        CreateFile("src/Allowed/appsettings.json", "{}");
        CreateFile("set-agent-env.local.cmd", "set OPENROUTER_API_KEY=secret");

        await new RepoIndexer().BuildAsync(config);
        var index = await RepoIndexJsonSerializer.ReadAsync(config.ResolvedRepoIndexPath);

        Assert.Contains(index.Entries, entry => entry.FilePath == "src/Allowed/Safe.cs");
        Assert.DoesNotContain(index.Entries, entry => entry.FilePath.Contains("ApiKey", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(index.Entries, entry => entry.FilePath.Contains("appsettings", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(index.Entries, entry => entry.FilePath.EndsWith(".local.cmd", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DetectsCSharpDeclaredTypes()
    {
        var config = CreateConfig();
        CreateFile(
            "src/Allowed/Types.cs",
            """
namespace Demo.Sample;
public sealed class TicketSlaService { }
public interface ITicketPolicy { }
public enum Severity { Critical }
""");

        await new RepoIndexer().BuildAsync(config);
        var index = await RepoIndexJsonSerializer.ReadAsync(config.ResolvedRepoIndexPath);
        var entry = Assert.Single(index.Entries, entry => entry.FilePath == "src/Allowed/Types.cs");

        Assert.Contains("TicketSlaService", entry.DeclaredTypes);
        Assert.Contains("ITicketPolicy", entry.DeclaredTypes);
        Assert.Contains("Severity", entry.DeclaredTypes);
        Assert.Contains("Demo.Sample", entry.Namespaces);
        Assert.Contains("ticket", entry.Keywords);
        Assert.Contains("sla", entry.Keywords);
    }

    [Fact]
    public async Task DetectsRazorPageRoute()
    {
        var config = CreateConfig();
        CreateFile("src/Allowed/AgentDashboard.razor", "@page \"/agent-dashboard\"\n<h1>Agent Dashboard</h1>");

        await new RepoIndexer().BuildAsync(config);
        var index = await RepoIndexJsonSerializer.ReadAsync(config.ResolvedRepoIndexPath);
        var entry = Assert.Single(index.Entries, entry => entry.FilePath == "src/Allowed/AgentDashboard.razor");

        Assert.Contains("/agent-dashboard", entry.DeclaredTypes);
        Assert.Contains("agent", entry.Keywords);
        Assert.Contains("dashboard", entry.Keywords);
    }

    [Fact]
    public async Task WritesRepoIndexJson()
    {
        var config = CreateConfig();
        CreateFile("src/Allowed/Included.cs", "public sealed class Included { }");

        var result = await new RepoIndexer().BuildAsync(config);

        Assert.True(File.Exists(config.ResolvedRepoIndexPath));
        Assert.Equal(config.ResolvedRepoIndexPath, result.IndexFile);
        Assert.Equal(1, result.IndexedFileCount);
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
            new RepairKitConfig
            {
                SolutionPath = "Demo.sln",
                AllowedEditPaths = ["src/Allowed/", "tests/"],
                BlockedPathSegments = [".git", ".agent", "bin", "obj", ".vs", "node_modules", "TestResults", "test-results"],
                BlockedPathTerms = [".env", "secret", "token", "password", "key", "appsettings"]
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
}
