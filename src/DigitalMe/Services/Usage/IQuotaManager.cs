using DigitalMe.Data.Entities;
using DigitalMe.Models.Usage;

namespace DigitalMe.Services.Usage;

/// <summary>
/// Интерфейс сервиса для управления квотами использования API.
/// Проверяет лимиты, отслеживает использование и отправляет уведомления.
/// </summary>
public interface IQuotaManager
{
    /// <summary>
    /// Проверяет, может ли пользователь использовать указанное количество токенов.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="provider">Название провайдера API.</param>
    /// <param name="tokens">Количество токенов для проверки.</param>
    /// <returns>True, если использование разрешено (не превышает квоту).</returns>
    Task<bool> CanUseTokensAsync(string userId, string provider, int tokens);

    /// <summary>
    /// Получает текущий статус квоты для пользователя и провайдера.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="provider">Название провайдера API.</param>
    /// <returns>Статус квоты с информацией об использовании.</returns>
    Task<QuotaStatus> GetQuotaStatusAsync(string userId, string provider);

    /// <summary>
    /// Получает или создает дневное использование для пользователя и провайдера.
    /// Автоматически сбрасывает использование при переходе на новый день.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="provider">Название провайдера API.</param>
    /// <returns>Запись дневного использования для текущего дня.</returns>
    Task<DailyUsage> GetOrCreateDailyUsageAsync(string userId, string provider);

    /// <summary>
    /// Обновляет использование и проверяет пороги для отправки уведомлений.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="provider">Название провайдера API.</param>
    /// <param name="tokensUsed">Количество использованных токенов для добавления.</param>
    Task UpdateUsageAsync(string userId, string provider, int tokensUsed);
}