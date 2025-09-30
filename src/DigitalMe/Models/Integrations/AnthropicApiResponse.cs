using System.Text.Json.Serialization;

namespace DigitalMe.Models.Integrations;

/// <summary>
/// Структура ответа от Anthropic API (JSON deserialization).
/// </summary>
public class AnthropicApiResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "message";

    [JsonPropertyName("role")]
    public string Role { get; set; } = "assistant";

    [JsonPropertyName("content")]
    public List<AnthropicContentBlock> Content { get; set; } = new();

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("usage")]
    public AnthropicUsage Usage { get; set; } = new();
}

/// <summary>
/// Блок контента в ответе Anthropic.
/// </summary>
public class AnthropicContentBlock
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// Информация об использовании токенов.
/// </summary>
public class AnthropicUsage
{
    [JsonPropertyName("input_tokens")]
    public int InputTokens { get; set; }

    [JsonPropertyName("output_tokens")]
    public int OutputTokens { get; set; }
}