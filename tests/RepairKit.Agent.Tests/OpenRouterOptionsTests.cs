using RepairKit.Agent;

namespace RepairKit.Agent.Tests;

public sealed class OpenRouterOptionsTests
{
    [Fact]
    public void MissingApiKeyThrowsWhenRequired()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            OpenRouterOptions.FromValues(null, null, null, null, requireApiKey: true));

        Assert.Contains("OPENROUTER_API_KEY", exception.Message);
    }

    [Fact]
    public void ModelDefaultsToGpt52()
    {
        var options = OpenRouterOptions.FromValues(
            "test-key",
            null,
            null,
            null,
            requireApiKey: true);

        Assert.Equal("openai/gpt-5.2", options.Model);
        Assert.Equal("DotNetAgenticRepairKit", options.AppTitle);
    }
}

