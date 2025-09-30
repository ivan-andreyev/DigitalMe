using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigitalMe.Data.Entities;

/// <summary>
/// Конфигурация квот для пользователя.
/// Определяет лимиты использования API для различных провайдеров.
/// </summary>
[Table("UserQuotas")]
public class UserQuota : BaseEntity
{
    /// <summary>
    /// Идентификатор пользователя.
    /// </summary>
    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Название провайдера API (null = применяется ко всем провайдерам).
    /// </summary>
    [MaxLength(100)]
    public string? Provider { get; set; }

    /// <summary>
    /// Дневной лимит токенов.
    /// </summary>
    public int DailyTokenLimit { get; set; } = 10000; // Default: 10K tokens/day

    /// <summary>
    /// Месячный лимит токенов (optional).
    /// </summary>
    public int? MonthlyTokenLimit { get; set; }

    /// <summary>
    /// Уровень подписки пользователя (Free, Basic, Premium).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string SubscriptionTier { get; set; } = "Free";

    /// <summary>
    /// Включены ли уведомления о превышении квоты.
    /// </summary>
    public bool NotificationsEnabled { get; set; } = true;

    /// <summary>
    /// Default constructor for Entity Framework.
    /// </summary>
    public UserQuota() : base()
    {
    }
}