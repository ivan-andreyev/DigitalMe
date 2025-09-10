using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigitalMe.Data.Entities;

/// <summary>
/// Represents a calendar event from Google Calendar integration.
/// For MVP: minimal implementation, can be expanded later.
/// </summary>
[Table("CalendarEvents")]
public class CalendarEvent : BaseEntity
{
    /// <summary>
    /// Google Calendar event ID.
    /// </summary>
    [MaxLength(200)]
    public string? GoogleEventId { get; set; }

    /// <summary>
    /// Event title/summary.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Event description.
    /// </summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// Event start time.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Event end time.
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Event location.
    /// </summary>
    [MaxLength(500)]
    public string? Location { get; set; }

    /// <summary>
    /// When this event was last synchronized with Google Calendar.
    /// Used for tracking sync operations and detecting changes.
    /// </summary>
    public DateTime? LastSyncAt { get; set; }

    public CalendarEvent() : base() { }
}
