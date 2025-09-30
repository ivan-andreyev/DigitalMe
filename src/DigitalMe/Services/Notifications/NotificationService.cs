using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Notifications;

/// <summary>
/// Базовая реализация сервиса уведомлений для квотного управления.
/// TODO Phase 6: Реализовать отправку email/SMS/push уведомлений.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task SendQuotaWarningAsync(string userId, decimal percentUsed)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
        }

        _logger.LogWarning("Quota warning for user {UserId}: {PercentUsed}% used (stub notification)",
            userId, percentUsed);

        // TODO Phase 6: Implement actual notification delivery
        // - Email notification
        // - SMS notification (Twilio/etc)
        // - Push notification
        // - In-app notification

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SendQuotaExceededAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
        }

        _logger.LogError("Quota exceeded for user {UserId} (stub notification)", userId);

        // TODO Phase 6: Implement actual notification delivery
        // - Email notification with upgrade options
        // - SMS notification
        // - Push notification
        // - In-app notification with action buttons

        await Task.CompletedTask;
    }
}