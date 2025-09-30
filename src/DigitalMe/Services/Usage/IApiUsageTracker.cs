using DigitalMe.Models.Usage;

namespace DigitalMe.Services.Usage;

/// <summary>
/// Интерфейс сервиса для отслеживания использования API.
/// Записывает метрики запросов, рассчитывает стоимость и предоставляет аналитику.
/// </summary>
public interface IApiUsageTracker
{
    /// <summary>
    /// Записывает информацию об использовании API.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="provider">Название провайдера API.</param>
    /// <param name="details">Детали использования (токены, время отклика, успешность).</param>
    /// <returns>Task для асинхронной операции.</returns>
    Task RecordUsageAsync(string userId, string provider, UsageDetails details);

    /// <summary>
    /// Рассчитывает стоимость использования для указанного провайдера.
    /// </summary>
    /// <param name="provider">Название провайдера API.</param>
    /// <param name="tokens">Количество использованных токенов.</param>
    /// <returns>Расчетная стоимость в долларах США.</returns>
    decimal CalculateCost(string provider, int tokens);

    /// <summary>
    /// Получает агрегированную статистику использования за период.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="startDate">Начало периода (включительно).</param>
    /// <param name="endDate">Конец периода (включительно).</param>
    /// <returns>Агрегированная статистика использования.</returns>
    Task<UsageStats> GetUsageStatsAsync(string userId, DateTime startDate, DateTime endDate);
}