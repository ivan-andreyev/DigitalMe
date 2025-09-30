namespace DigitalMe.Models.Usage;

/// <summary>
/// Представляет тренд использования за один день.
/// Используется для построения графиков аналитики.
/// </summary>
public record UsageTrend
{
    /// <summary>
    /// Дата для этой точки тренда.
    /// </summary>
    public DateTime Date { get; init; }

    /// <summary>
    /// Общее количество использованных токенов за день.
    /// </summary>
    public int TotalTokens { get; init; }

    /// <summary>
    /// Общая стоимость использования за день.
    /// </summary>
    public decimal TotalCost { get; init; }

    /// <summary>
    /// Количество запросов за день.
    /// </summary>
    public int RequestCount { get; init; }

    /// <summary>
    /// Процент успешных запросов за день.
    /// </summary>
    public decimal SuccessRate { get; init; }
}