namespace DigitalMe.Models.Integrations;

/// <summary>
/// Ответ от Anthropic API.
/// Содержит текст ответа и информацию об использовании токенов.
/// </summary>
public record AnthropicResponse
{
    /// <summary>
    /// Текст ответа от модели.
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Общее количество использованных токенов (input + output).
    /// </summary>
    public int TokensUsed { get; init; }

    /// <summary>
    /// Количество токенов на входе (prompt).
    /// </summary>
    public int InputTokens { get; init; }

    /// <summary>
    /// Количество токенов на выходе (completion).
    /// </summary>
    public int OutputTokens { get; init; }

    /// <summary>
    /// Время отклика API в миллисекундах.
    /// </summary>
    public long ResponseTimeMs { get; init; }
}