using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigitalMe.Data.Entities;

/// <summary>
/// Represents a comprehensive personality profile for digital clone modeling.
/// Contains core personality data, traits, and behavioral patterns used
/// for generating personality-aware system prompts and responses.
/// </summary>
[Table("PersonalityProfiles")]
public class PersonalityProfile : BaseEntity
{
    /// <summary>
    /// Display name for the personality profile (e.g., "Ivan", "Digital Clone").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Comprehensive biographical description and personality context.
    /// Contains background, life experience, worldview, and core characteristics.
    /// </summary>
    [Required]
    [MaxLength(5000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Current age of the person being modeled.
    /// Used for temporal behavior modeling and life stage considerations.
    /// </summary>
    [Range(0, 150)]
    public int Age { get; set; }

    /// <summary>
    /// Primary profession or role (e.g., "Head of R&D", "Software Developer").
    /// Influences technical expertise level and professional communication style.
    /// </summary>
    [MaxLength(200)]
    public string Profession { get; set; } = string.Empty;

    /// <summary>
    /// Geographic location or cultural background.
    /// Affects communication patterns, references, and cultural context.
    /// </summary>
    [MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Core values and principles that drive decision-making.
    /// JSON array of value statements used for behavioral consistency.
    /// Example: ["A5< ?>EC9", "!8;0 2 ?@0245", "828 8 409 68BL 4@C38<"]
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string CoreValues { get; set; } = "[]";

    /// <summary>
    /// Communication style preferences and patterns.
    /// JSON object describing tone, formality, technical language usage.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string CommunicationStyle { get; set; } = "{}";

    /// <summary>
    /// Technical preferences and expertise areas.
    /// JSON object with technologies, methodologies, and skill levels.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string TechnicalPreferences { get; set; } = "{}";

    /// <summary>
    /// Indicates whether this is the active/primary personality profile.
    /// Only one profile should be active at a time for system prompt generation.
    /// </summary>
    public bool IsActive { get; set; } = false;

    /// <summary>
    /// Overall accuracy score for personality modeling (0.0 to 1.0).
    /// Updated based on feedback and validation against real behavior.
    /// </summary>
    [Range(0.0, 1.0)]
    public double AccuracyScore { get; set; } = 0.0;

    /// <summary>
    /// Timestamp of the last update to personality data.
    /// Tracks when the profile was last modified or refined.
    /// </summary>
    public DateTime LastProfileUpdate { get; set; } = DateTime.UtcNow;

    // Navigation Properties

    /// <summary>
    /// Collection of detailed personality traits associated with this profile.
    /// Each trait represents a specific behavioral or psychological characteristic.
    /// </summary>
    public virtual ICollection<PersonalityTrait> Traits { get; set; } = new List<PersonalityTrait>();

    /// <summary>
    /// Collection of temporal behavior patterns for context-aware responses.
    /// Tracks how behavior changes based on time, mood, or situation.
    /// </summary>
    public virtual ICollection<TemporalBehaviorPattern> TemporalPatterns { get; set; } = new List<TemporalBehaviorPattern>();

    /// <summary>
    /// Default constructor for Entity Framework.
    /// </summary>
    public PersonalityProfile() : base() { }

    /// <summary>
    /// Constructor for creating a new personality profile with basic information.
    /// </summary>
    /// <param name="name">Profile display name</param>
    /// <param name="description">Biographical description</param>
    /// <param name="age">Current age</param>
    /// <param name="profession">Primary profession</param>
    public PersonalityProfile(string name, string description, int age = 0, string profession = "")
        : base()
    {
        Name = name;
        Description = description;
        Age = age;
        Profession = profession;
        LastProfileUpdate = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the accuracy score and tracks when the profile was modified.
    /// </summary>
    /// <param name="newScore">New accuracy score (0.0 to 1.0)</param>
    public void UpdateAccuracyScore(double newScore)
    {
        if (newScore is >= 0.0 and <= 1.0)
        {
            AccuracyScore = newScore;
            LastProfileUpdate = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Activates this personality profile and deactivates others.
    /// Ensures only one active profile exists for system prompt generation.
    /// </summary>
    public void SetAsActive()
    {
        IsActive = true;
        LastProfileUpdate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
