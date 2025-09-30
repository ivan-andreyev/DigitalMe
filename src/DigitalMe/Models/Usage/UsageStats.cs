namespace DigitalMe.Models.Usage;

/// <summary>
/// Агрегированная статистика использования API за период.
/// Используется для отображения аналитики и трендов.
/// </summary>
public record UsageStats
{
    /// <summary>
    /// Общее количество использованных токенов.
    /// </summary>
    public long TotalTokens { get; init; }

    /// <summary>
    /// Общая расчетная стоимость использования в долларах США.
    /// </summary>
    public decimal TotalCost { get; init; }

    /// <summary>
    /// Общее количество запросов.
    /// </summary>
    public int RequestCount { get; init; }

    /// <summary>
    /// Процент успешных запросов (0-100).
    /// </summary>
    public decimal SuccessRate { get; init; }

    /// <summary>
    /// Среднее время отклика в миллисекундах.
    /// </summary>
    public double AverageResponseTime { get; init; }

    /// <summary>
    /// Статистика по каждому провайдеру.
    /// Ключ - название провайдера, значение - статистика провайдера.
    /// </summary>
    public Dictionary<string, ProviderStats> ByProvider { get; init; } = new();
}

/// <summary>
/// Статистика использования для конкретного провайдера API.
/// </summary>
public record ProviderStats
{
    /// <summary>
    /// Количество использованных токенов для данного провайдера.
    /// </summary>
    public long Tokens { get; init; }

    /// <summary>
    /// Расчетная стоимость использования данного провайдера в долларах США.
    /// </summary>
    public decimal Cost { get; init; }

    /// <summary>
    /// Количество запросов к данному провайдеру.
    /// </summary>
    public int Requests { get; init; }
}