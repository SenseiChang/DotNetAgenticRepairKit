using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RepairKit.Agent;

public sealed class OpenRouterRepairPlanner : IAiRepairPlanner
{
    public const string Endpoint = "https://openrouter.ai/api/v1/chat/completions";

    private const string SystemMessage =
        "You are a senior .NET engineer producing a safe repair plan for a failing test suite. Return strict JSON only. Do not include Markdown. Do not include explanations outside JSON. Prefer minimal test-driven changes. Do not modify secrets, generated files, configuration files, bin, obj, .git, or .agent output.";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly HttpClient _httpClient;
    private readonly OpenRouterOptions _options;

    public OpenRouterRepairPlanner(HttpClient httpClient, OpenRouterOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<RepairPlanResult> CreateRepairPlanAsync(
        string contextPacket,
        string runFolder,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("OPENROUTER_API_KEY is required for AI planning.");
        }

        var request = CreateRequest(_options.Model, contextPacket);
        var modelRequestPath = AgentOutputPaths.GetModelRequestFile(runFolder);
        var modelResponsePath = AgentOutputPaths.GetModelResponseFile(runFolder);
        var repairPlanPath = AgentOutputPaths.GetRepairPlanFile(runFolder);

        await File.WriteAllTextAsync(
            modelRequestPath,
            CreateSanitizedRequestJson(request),
            cancellationToken);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        httpRequest.Headers.TryAddWithoutValidation("X-OpenRouter-Title", _options.AppTitle);

        if (!string.IsNullOrWhiteSpace(_options.HttpReferer))
        {
            httpRequest.Headers.TryAddWithoutValidation("HTTP-Referer", _options.HttpReferer);
        }

        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(request, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(CreateHttpErrorMessage(response.StatusCode, responseBody));
        }

        var chatResponse = JsonSerializer.Deserialize<OpenRouterChatResponse>(responseBody, JsonOptions)
            ?? throw new InvalidOperationException("OpenRouter returned an empty response.");

        var assistantContent = chatResponse.Choices.FirstOrDefault()?.Message.Content;
        if (string.IsNullOrWhiteSpace(assistantContent))
        {
            throw new InvalidOperationException("OpenRouter response did not include assistant content.");
        }

        await File.WriteAllTextAsync(modelResponsePath, assistantContent, cancellationToken);

        var plan = RepairPlanJsonSerializer.Parse(assistantContent);
        await File.WriteAllTextAsync(repairPlanPath, RepairPlanJsonSerializer.Serialize(plan), cancellationToken);

        return new RepairPlanResult(
            plan,
            _options.Model,
            repairPlanPath,
            modelRequestPath,
            modelResponsePath,
            chatResponse.Usage?.PromptTokens,
            chatResponse.Usage?.CompletionTokens,
            chatResponse.Usage?.TotalTokens);
    }

    public static OpenRouterChatRequest CreateRequest(string model, string contextPacket)
    {
        return new OpenRouterChatRequest(
            model,
            [
                new OpenRouterChatMessage("system", SystemMessage),
                new OpenRouterChatMessage("user", CreateUserMessage(contextPacket))
            ],
            0.1,
            false,
            new OpenRouterResponseFormat("json_object"));
    }

    public static string CreateSanitizedRequestJson(OpenRouterChatRequest request)
    {
        var sanitized = new
        {
            endpoint = Endpoint,
            method = "POST",
            headers = new
            {
                contentType = "application/json",
                authorization = "Bearer [redacted]"
            },
            body = request
        };

        return JsonSerializer.Serialize(sanitized, JsonOptions);
    }

    private static string CreateUserMessage(string contextPacket)
    {
        return """
Phase 5 is plan-only. No file changes will be applied yet.

Use the context packet below to produce a strict JSON repair plan matching this exact schema:

{
  "summary": "string",
  "riskLevel": "low|medium|high",
  "targetFiles": ["relative/path.cs"],
  "changes": [
    {
      "filePath": "relative/path.cs",
      "reason": "string",
      "fullReplacement": "string"
    }
  ],
  "validationCommands": ["dotnet test"]
}

Return JSON only. Do not include Markdown.

Context packet:
""" + Environment.NewLine + contextPacket;
    }

    private static string CreateHttpErrorMessage(HttpStatusCode statusCode, string responseBody)
    {
        return statusCode switch
        {
            HttpStatusCode.Unauthorized => "OpenRouter request failed with HTTP 401. Check OPENROUTER_API_KEY.",
            HttpStatusCode.PaymentRequired => "OpenRouter request failed with HTTP 402. Check account credits or payment status.",
            (HttpStatusCode)429 => "OpenRouter request failed with HTTP 429. Rate limit exceeded.",
            >= HttpStatusCode.InternalServerError => $"OpenRouter request failed with HTTP {(int)statusCode}. Provider/server error. {responseBody}",
            _ => $"OpenRouter request failed with HTTP {(int)statusCode}. {responseBody}"
        };
    }
}
