# Core Entity Models

**Родительский план**: [../02-05-interfaces.md](../02-05-interfaces.md)

## PersonalityProfile Entity
**Файл**: `src/DigitalMe.Data/Entities/PersonalityProfile.cs:1-60`

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
    public Dictionary<string, object> CoreTraits { get; set; } = new();
    
    /// <summary>
    /// Communication preferences
    /// JSON structure: {"style": "direct", "preferred_platforms": ["telegram", "slack"]}
    /// </summary>
    [Column(TypeName = "jsonb")]
    public Dictionary<string, object> CommunicationStyle { get; set; } = new();
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public ICollection<PersonalityTrait> PersonalityTraits { get; set; } = new List<PersonalityTrait>();
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
}
```

## Message Entity
**Файл**: `src/DigitalMe.Data/Entities/Message.cs:1-30`

```csharp
[Table("messages")]
public class Message
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid ConversationId { get; set; }
    
    [Required]
    [MaxLength(4000)]
    public string Content { get; set; } = default!;
    
    public bool IsFromUser { get; set; }
    
    [Column(TypeName = "jsonb")]
    public Dictionary<string, object>? Metadata { get; set; }
    
    public DateTime Timestamp { get; set; }
    
    // Navigation properties
    public Conversation Conversation { get; set; } = default!;
}
```

## Conversation Entity  
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

## PersonalityTrait Entity
**Файл**: `src/DigitalMe.Data/Entities/PersonalityTrait.cs:1-25`

```csharp
[Table("personality_traits")]
public class PersonalityTrait
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid PersonalityProfileId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = default!;
    
    [Column(TypeName = "jsonb")]
    public Dictionary<string, object> Value { get; set; } = new();
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public PersonalityProfile PersonalityProfile { get; set; } = default!;
}
```

## Domain Enums
**Файл**: `src/DigitalMe.Core/Models/Enums.cs:1-20`

```csharp
namespace DigitalMe.Core.Models;

public enum PersonalityMood
{
    Calm = 0,
    Focused = 1,
    Tired = 2,
    Irritated = 3,
    Excited = 4,
    Stressed = 5
}

public enum Platform
{
    Web = 0,
    Telegram = 1,
    Mobile = 2,
    Slack = 3
}

public enum MessageType
{
    Text = 0,
    Command = 1,
    Media = 2,
    System = 3
}
```