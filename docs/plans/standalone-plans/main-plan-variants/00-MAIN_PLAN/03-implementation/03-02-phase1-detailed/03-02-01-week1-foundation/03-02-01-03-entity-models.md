# Day 3: Entity Models & Architectural Decisions

**Родительский план**: [../03-02-01-week1-foundation.md](../03-02-01-week1-foundation.md)

## Day 3: Entity Models Implementation (2 hours)

**Файл**: `src/DigitalMe.Data/Entities/PersonalityProfile.cs:1-40`
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigitalMe.Data.Entities;

[Table("personality_profiles")]
public class PersonalityProfile
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = default!;
    
    [MaxLength(2000)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Core personality traits from IVAN_PROFILE_DATA.md
    /// JSON structure: {"directness": 0.9, "technical_focus": 0.95, "honesty": 1.0}
    /// </summary>
    [Column(TypeName = "jsonb")]
    public Dictionary<string, object> CoreTraits { get; set; } = new()
    {
        ["directness"] = 0.9,
        ["technical_focus"] = 0.95, 
        ["honesty"] = 1.0,
        ["workaholic_tendency"] = 0.85,
        ["fomo_syndrome"] = 0.7
    };
    
    /// <summary>
    /// Communication preferences
    /// JSON structure: {"style": "direct", "preferred_platforms": ["telegram", "slack"]}
    /// </summary>
    [Column(TypeName = "jsonb")]
    public Dictionary<string, object> CommunicationStyle { get; set; } = new()
    {
        ["style"] = "direct",
        ["response_speed"] = "fast",
        ["emoji_usage"] = "minimal",
        ["preferred_platforms"] = new[] { "telegram", "slack" }
    };
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public ICollection<PersonalityTrait> PersonalityTraits { get; set; } = new List<PersonalityTrait>();
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
}
```

**Файл**: `src/DigitalMe.Data/Entities/Conversation.cs:1-30`
```csharp
[Table("conversations")]
public class Conversation
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = default!;
    
    [Required]
    [MaxLength(50)]
    public string Platform { get; set; } = default!; // "Telegram", "Web", "Mobile"
    
    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = default!; // Platform-specific user ID
    
    public Guid? PersonalityProfileId { get; set; }
    
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    
    // Navigation properties
    public PersonalityProfile? PersonalityProfile { get; set; }
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
```

## Day 3: Architectural Decision Records (ADRs) (1 hour)

**Файл**: `docs/architecture/ADRs/ADR-001-database-technology.md`
```markdown
# ADR-001: Database Technology Choice

## Status: Accepted
## Date: 2025-08-28
## Decision Makers: Technical Team

## Context
Digital clone requires persistent storage for personality profiles, conversation history, and integration data.

## Decision
Use PostgreSQL with Entity Framework Core for the following reasons:
- JSONB support for flexible personality trait storage
- Strong consistency for conversation threading
- Excellent .NET integration with EF Core
- Production-ready with good observability

## Consequences
- **Positive**: Type-safe queries, migration support, rich JSONB querying
- **Negative**: Learning curve for JSONB optimization, requires PostgreSQL expertise

## Alternatives Considered
- MongoDB: Rejected due to lack of strong consistency
- SQL Server: Rejected due to limited JSON support in older versions
```

**Файл**: `docs/architecture/ADRs/ADR-002-personality-modeling.md`
```markdown
# ADR-002: Personality Modeling Approach

## Status: Accepted
## Date: 2025-08-28

## Context
Need flexible way to store and evolve personality traits over time.

## Decision
Use hybrid approach: Core traits in JSONB + separate PersonalityTrait entities for temporal tracking.

## Rationale
- JSONB for fast trait access and flexible schema evolution
- Separate entities for historical tracking and analytics
- Enables both real-time queries and temporal analysis

## Implementation Guidelines
- Core traits: Store in PersonalityProfile.CoreTraits JSONB column
- Temporal data: Use PersonalityTrait entities with timestamps
- Validation: JSON schema validation for trait structure
```

## Navigation
- [Previous: Day 2: Database Context Setup](03-02-01-02-database-context.md)
- [Next: Day 4: DI Container & Configuration](03-02-01-04-di-configuration.md)
- [Overview: Week 1 Foundation](../03-02-01-week1-foundation.md)