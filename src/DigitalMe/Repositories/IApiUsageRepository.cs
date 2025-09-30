using DigitalMe.Data.Entities;

namespace DigitalMe.Repositories;

/// <summary>
/// Интерфейс репозитория для работы с записями использования API.
/// Предоставляет методы для сохранения и получения метрик использования.
/// </summary>
public interface IApiUsageRepository
{
    /// <summary>
    /// Сохраняет запись об использовании API.
    /// </summary>
    /// <param name="record">Запись об использовании для сохранения.</param>
    /// <returns>Сохраненная запись с присвоенным Id.</returns>
    /// <exception cref="ArgumentNullException">Если record равен null.</exception>
    Task<ApiUsageRecord> SaveUsageRecordAsync(ApiUsageRecord record);

    /// <summary>
    /// Получает записи об использовании для пользователя за период.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="startDate">Начало периода (включительно).</param>
    /// <param name="endDate">Конец периода (включительно).</param>
    /// <returns>Список записей об использовании.</returns>
    /// <exception cref="ArgumentException">Если userId пуст или null.</exception>
    Task<List<ApiUsageRecord>> GetUsageRecordsAsync(
        string userId,
        DateTime startDate,
        DateTime endDate);

    /// <summary>
    /// Получает дневное использование для пользователя и провайдера за дату.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="provider">Название провайдера API.</param>
    /// <param name="date">Дата использования.</param>
    /// <returns>Запись дневного использования или null, если не найдено.</returns>
    Task<DailyUsage?> GetDailyUsageAsync(string userId, string provider, DateTime date);

    /// <summary>
    /// Обновляет существующую запись дневного использования.
    /// </summary>
    /// <param name="dailyUsage">Запись для обновления.</param>
    /// <returns>Обновленная запись.</returns>
    /// <exception cref="ArgumentNullException">Если dailyUsage равен null.</exception>
    Task<DailyUsage> UpdateDailyUsageAsync(DailyUsage dailyUsage);

    /// <summary>
    /// Создает новую запись дневного использования.
    /// </summary>
    /// <param name="dailyUsage">Запись для создания.</param>
    /// <returns>Созданная запись с присвоенным Id.</returns>
    /// <exception cref="ArgumentNullException">Если dailyUsage равен null.</exception>
    Task<DailyUsage> CreateDailyUsageAsync(DailyUsage dailyUsage);
}