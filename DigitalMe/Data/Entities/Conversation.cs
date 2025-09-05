using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigitalMe.Data.Entities;

/// <summary>
/// Represents a conversation thread with the digital personality.
/// Contains conversation metadata and message history.
/// </summary>
[Table("Conversations")]
public class Conversation : BaseEntity
{
    /// <summary>
    /// Display title for the conversation.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Optional description or summary of conversation topic.
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// ID of the personality profile used for this conversation.
    /// </summary>
    [Required]
    public Guid PersonalityProfileId { get; set; }

    /// <summary>
    /// Navigation property to the personality profile.
    /// </summary>
    public virtual PersonalityProfile PersonalityProfile { get; set; } = null!;

    /// <summary>
    /// Collection of messages in this conversation.
    /// </summary>
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    /// <summary>
    /// Timestamp of the last message in this conversation.
    /// </summary>
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Total number of messages in this conversation.
    /// </summary>
    public int MessageCount { get; set; } = 0;

    /// <summary>
    /// Indicates if the conversation is active or archived.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Platform where conversation originated (e.g., "web", "telegram").
    /// </summary>
    [MaxLength(50)]
    public string Platform { get; set; } = "web";

    /// <summary>
    /// User ID who started the conversation.
    /// </summary>
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// When the conversation was started.
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the conversation was ended (if applicable).
    /// </summary>
    public DateTime? EndedAt { get; set; }

    /// <summary>
    /// Default constructor for Entity Framework.
    /// </summary>
    public Conversation() : base() { }

    /// <summary>
    /// Constructor for creating a new conversation.
    /// </summary>
    /// <param name="title">Conversation title</param>
    /// <param name="personalityProfileId">ID of personality profile to use</param>
    /// <param name="platform">Platform name</param>
    /// <param name="userId">User ID</param>
    public Conversation(string title, Guid personalityProfileId, string platform = "web", string userId = "") : base()
    {
        Title = title;
        PersonalityProfileId = personalityProfileId;
        Platform = platform;
        UserId = userId;
        StartedAt = DateTime.UtcNow;
        LastMessageAt = DateTime.UtcNow;
    }
}