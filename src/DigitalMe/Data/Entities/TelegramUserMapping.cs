using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigitalMe.Data.Entities;

/// <summary>
/// Maps Telegram users to internal user system.
/// Minimal MVP implementation.
/// </summary>
[Table("TelegramUserMappings")]
public class TelegramUserMapping : BaseEntity
{
    /// <summary>
    /// Telegram user ID.
    /// </summary>
    public long TelegramUserId { get; set; }

    /// <summary>
    /// Internal user ID.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Telegram username (optional).
    /// </summary>
    [MaxLength(100)]
    public string? TelegramUsername { get; set; }

    public TelegramUserMapping() : base() { }
}
