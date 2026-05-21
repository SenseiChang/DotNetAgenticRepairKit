using System.Text.Json.Serialization;

namespace RepairKit.Agent;

public sealed record OpenRouterChatRequest(
    string Model,
    IReadOnlyList<OpenRouterChatMessage> Messages,
    double Temperature,
    bool Stream,
    [property: JsonPropertyName("response_format")] OpenRouterResponseFormat ResponseFormat);

public sealed record OpenRouterChatMessage(
    string Role,
    string Content);

public sealed record OpenRouterResponseFormat(
    string Type);

public sealed record OpenRouterChatResponse(
    IReadOnlyList<OpenRouterChoice> Choices);

public sealed record OpenRouterChoice(
    OpenRouterChatMessage Message);

