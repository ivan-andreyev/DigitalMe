using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigitalMe.Data.Entities;

/// <summary>
/// Telegram user preferences and settings.
/// Minimal MVP implementation.
/// </summary>
[Table("TelegramUserPreferences")]
public class TelegramUserPreferences : BaseEntity
{
    /// <summary>
    /// Telegram user ID.
    /// </summary>
    public long TelegramUserId { get; set; }

    /// <summary>
    /// User language preference.
    /// </summary>
    [MaxLength(10)]
    public string Language { get; set; } = "en";

    /// <summary>
    /// Whether notifications are enabled.
    /// </summary>
    public bool NotificationsEnabled { get; set; } = true;

    public TelegramUserPreferences() : base() { }
}
