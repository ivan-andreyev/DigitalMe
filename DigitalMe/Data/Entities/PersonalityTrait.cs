using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigitalMe.Data.Entities;

/// <summary>
/// Represents a specific personality trait or behavioral characteristic.
/// Traits are weighted components that contribute to overall personality modeling
/// and are used for generating context-aware system prompts and responses.
/// </summary>
[Table("PersonalityTraits")]
public class PersonalityTrait : BaseEntity
{
    /// <summary>
    /// Foreign key reference to the associated personality profile.
    /// </summary>
    [Required]
    public Guid PersonalityProfileId { get; set; }

    /// <summary>
    /// Category grouping for the trait (e.g., "Communication", "Technical", "Values").
    /// Helps organize traits into logical groups for system prompt generation.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Specific name of the personality trait.
    /// Example: "Direct Communication", "Pragmatic Decision Making", "Technical Expertise"
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of how this trait manifests in behavior.
    /// Used for generating specific behavioral instructions in system prompts.
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Relative importance weight of this trait (0.0 to 10.0).
    /// Higher weights have more influence on personality modeling and system prompts.
    /// </summary>
    [Range(0.0, 10.0)]
    public double Weight { get; set; } = 1.0;

    /// <summary>
    /// Confidence level in the accuracy of this trait (0.0 to 1.0).
    /// Based on validation against actual behavior and feedback.
    /// </summary>
    [Range(0.0, 1.0)]
    public double ConfidenceLevel { get; set; } = 0.5;

    /// <summary>
    /// Contextual tags for when this trait is most applicable.
    /// JSON array of context identifiers (e.g., ["work", "technical", "leadership"]).
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string ContextTags { get; set; } = "[]";

    /// <summary>
    /// Example phrases or expressions that demonstrate this trait.
    /// JSON array of strings used for training and validation.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string ExampleExpressions { get; set; } = "[]";

    /// <summary>
    /// Behavioral impact level: Low, Medium, High, Critical.
    /// Indicates how significantly this trait affects overall behavior.
    /// </summary>
    [MaxLength(50)]
    public string ImpactLevel { get; set; } = "Medium";

    /// <summary>
    /// Source of this trait information (e.g., "Interview", "Observation", "Analysis").
    /// Tracks the reliability and origin of trait data.
    /// </summary>
    [MaxLength(100)]
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this trait is currently active in personality modeling.
    /// Allows for disabling traits without deletion during refinement.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Timestamp of the last validation or update to this trait.
    /// </summary>
    public DateTime LastValidated { get; set; } = DateTime.UtcNow;

    // Navigation Properties

    /// <summary>
    /// Navigation property to the associated personality profile.
    /// </summary>
    [ForeignKey(nameof(PersonalityProfileId))]
    public virtual PersonalityProfile Profile { get; set; } = null!;

    /// <summary>
    /// Navigation property to the PersonalityProfile this trait belongs to.
    /// Used by Entity Framework for relationship mapping and queries.
    /// </summary>
    public virtual PersonalityProfile PersonalityProfile { get; set; } = null!;

    /// <summary>
    /// Default constructor for Entity Framework.
    /// </summary>
    public PersonalityTrait() : base() { }

    /// <summary>
    /// Constructor for creating a new personality trait.
    /// </summary>
    /// <param name="personalityProfileId">Associated profile ID</param>
    /// <param name="category">Trait category</param>
    /// <param name="name">Trait name</param>
    /// <param name="description">Trait description</param>
    /// <param name="weight">Importance weight (default: 1.0)</param>
    public PersonalityTrait(
        Guid personalityProfileId,
        string category,
        string name,
        string description,
        double weight = 1.0)
        : base()
    {
        PersonalityProfileId = personalityProfileId;
        Category = category;
        Name = name;
        Description = description;
        Weight = weight;
        LastValidated = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the confidence level and validation timestamp.
    /// </summary>
    /// <param name="newConfidence">New confidence level (0.0 to 1.0)</param>
    public void UpdateConfidence(double newConfidence)
    {
        if (newConfidence is >= 0.0 and <= 1.0)
        {
            ConfidenceLevel = newConfidence;
            LastValidated = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Adjusts the weight of this trait based on validation feedback.
    /// </summary>
    /// <param name="newWeight">New weight value (0.0 to 10.0)</param>
    public void AdjustWeight(double newWeight)
    {
        if (newWeight is >= 0.0 and <= 10.0)
        {
            Weight = newWeight;
            LastValidated = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Activates or deactivates this trait in personality modeling.
    /// </summary>
    /// <param name="active">Whether the trait should be active</param>
    public void SetActive(bool active)
    {
        IsActive = active;
        LastValidated = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Represents a temporal behavior pattern associated with a personality profile.
/// Defines how personality traits manifest differently based on context, time, or situation.
/// </summary>
[Table("TemporalBehaviorPatterns")]
public class TemporalBehaviorPattern : BaseEntity
{
    /// <summary>
    /// Foreign key reference to the associated personality profile.
    /// </summary>
    [Required]
    public Guid PersonalityProfileId { get; set; }

    /// <summary>
    /// Name of the temporal pattern (e.g., "Morning Energy", "Work Mode", "Stress Response").
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string PatternName { get; set; } = string.Empty;

    /// <summary>
    /// Contextual trigger for this pattern (e.g., "time_of_day", "work_context", "stress_level").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ContextTrigger { get; set; } = string.Empty;

    /// <summary>
    /// Specific condition that activates this pattern.
    /// JSON object with trigger conditions (e.g., {"hour": "09-17", "day_type": "workday"}).
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string TriggerConditions { get; set; } = "{}";

    /// <summary>
    /// Behavioral modifications when this pattern is active.
    /// JSON object describing changes to communication style, decision-making, etc.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string BehaviorModifications { get; set; } = "{}";

    /// <summary>
    /// Priority level for pattern application when multiple patterns are active.
    /// </summary>
    [Range(1, 10)]
    public int Priority { get; set; } = 5;

    /// <summary>
    /// Indicates whether this temporal pattern is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation Properties

    /// <summary>
    /// Navigation property to the associated personality profile.
    /// </summary>
    [ForeignKey(nameof(PersonalityProfileId))]
    public virtual PersonalityProfile Profile { get; set; } = null!;

    /// <summary>
    /// Default constructor for Entity Framework.
    /// </summary>
    public TemporalBehaviorPattern() : base() { }
}