using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigitalMe.Data.Entities;

/// <summary>
/// Represents a message from Telegram integration.
/// For MVP: minimal implementation, can be expanded later.
/// </summary>
[Table("TelegramMessages")]
public class TelegramMessage : BaseEntity
{
    /// <summary>
    /// Telegram message ID.
    /// </summary>
    public long TelegramMessageId { get; set; }

    /// <summary>
    /// Telegram chat ID where message was sent.
    /// </summary>
    public long ChatId { get; set; }

    /// <summary>
    /// Message text content.
    /// </summary>
    [MaxLength(4000)]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Username who sent the message.
    /// </summary>
    [MaxLength(100)]
    public string? Username { get; set; }

    /// <summary>
    /// When the message was received.
    /// </summary>
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the Telegram message was sent/received (for chronological ordering).
    /// Used by DbContext for indexing and sync operations.
    /// </summary>
    public DateTime MessageDate { get; set; } = DateTime.UtcNow;

    public TelegramMessage() : base() { }
}