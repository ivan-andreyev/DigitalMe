namespace DigitalMe.Models.Usage;

/// <summary>
/// Детали использования API для записи метрик.
/// Передается в ApiUsageTracker для фиксации использования.
/// </summary>
public record UsageDetails
{
    /// <summary>
    /// Тип запроса (например, "chat.completion", "embedding", "image.generation").
    /// </summary>
    public string RequestType { get; init; } = string.Empty;

    /// <summary>
    /// Количество использованных токенов в запросе.
    /// </summary>
    public int TokensUsed { get; init; }

    /// <summary>
    /// Название провайдера API (например, "Anthropic", "OpenAI").
    /// </summary>
    public string Provider { get; init; } = string.Empty;

    /// <summary>
    /// Время отклика API в миллисекундах.
    /// </summary>
    public long ResponseTime { get; init; }

    /// <summary>
    /// Успешно ли завершился запрос.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Тип ошибки, если запрос не удался (например, "Timeout", "RateLimitExceeded").
    /// </summary>
    public string? ErrorType { get; init; }
}