namespace DigitalMe.Services.Notifications;

/// <summary>
/// Интерфейс сервиса для отправки уведомлений пользователям.
/// Поддерживает уведомления о квотах, предупреждения и системные сообщения.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Отправляет предупреждение о приближении к лимиту квоты.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="percentUsed">Процент использованной квоты.</param>
    Task SendQuotaWarningAsync(string userId, decimal percentUsed);

    /// <summary>
    /// Отправляет уведомление о превышении квоты.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    Task SendQuotaExceededAsync(string userId);
}