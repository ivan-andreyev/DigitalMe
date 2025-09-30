namespace DigitalMe.Models.Usage;

/// <summary>
/// Текущий статус квоты пользователя для провайдера.
/// Содержит информацию об использовании и оставшихся ресурсах.
/// </summary>
public record QuotaStatus
{
    /// <summary>
    /// Дневной лимит токенов для пользователя.
    /// </summary>
    public int DailyLimit { get; init; }

    /// <summary>
    /// Количество использованных токенов сегодня.
    /// </summary>
    public int Used { get; init; }

    /// <summary>
    /// Количество оставшихся токенов до лимита.
    /// </summary>
    public int Remaining { get; init; }

    /// <summary>
    /// Процент использованной квоты (0-100).
    /// </summary>
    public decimal PercentUsed { get; init; }

    /// <summary>
    /// Время сброса квоты (начало следующего дня UTC).
    /// </summary>
    public DateTime ResetsAt { get; init; }
}