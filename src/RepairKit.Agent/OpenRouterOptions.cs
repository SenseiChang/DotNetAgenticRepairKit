namespace RepairKit.Agent;

public sealed record OpenRouterOptions(
    string ApiKey,
    string Model,
    string AppTitle,
    string? HttpReferer)
{
    public const string DefaultModel = "openai/gpt-5.2";
    public const string DefaultAppTitle = "DotNetAgenticRepairKit";

    public static OpenRouterOptions FromEnvironment(bool requireApiKey)
    {
        return FromValues(
            Environment.GetEnvironmentVariable("OPENROUTER_API_KEY"),
            Environment.GetEnvironmentVariable("REPAIRKIT_MODEL"),
            Environment.GetEnvironmentVariable("REPAIRKIT_OPENROUTER_APP_TITLE"),
            Environment.GetEnvironmentVariable("REPAIRKIT_OPENROUTER_HTTP_REFERER"),
            requireApiKey);
    }

    public static OpenRouterOptions FromValues(
        string? apiKey,
        string? model,
        string? appTitle,
        string? httpReferer,
        bool requireApiKey)
    {
        if (requireApiKey && string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "OPENROUTER_API_KEY is required for AI planning. Set it or run with --no-ai.");
        }

        return new OpenRouterOptions(
            apiKey ?? string.Empty,
            string.IsNullOrWhiteSpace(model) ? DefaultModel : model.Trim(),
            string.IsNullOrWhiteSpace(appTitle) ? DefaultAppTitle : appTitle.Trim(),
            string.IsNullOrWhiteSpace(httpReferer) ? null : httpReferer.Trim());
    }
}

