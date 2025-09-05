# Entity Models Configuration

**Родительский план**: [../02-01-database-design.md](../02-01-database-design.md)

## Overview
Detailed specifications for all entity models with PostgreSQL JSONB support, relationships, and configuration attributes for the DigitalMe personality system.

## Core Personality Entities

### PersonalityProfile Entity
**File**: `src/DigitalMe.Data/Entities/PersonalityProfile.cs`

```csharp
public class PersonalityProfile : BaseEntity
{
    public string Name { get; set; }                    // "Ivan"
    public string Description { get; set; }             // Full biography
    public JsonDocument Traits { get; set; }            // JSONB personality traits
    public JsonDocument CommunicationStyle { get; set; } // JSONB communication patterns
    public JsonDocument WorkStyle { get; set; }         // JSONB work preferences
    public PersonalityMood CurrentMood { get; set; } = PersonalityMood.Calm;
    public DateTime LastInteraction { get; set; }
    
    // Navigation properties
    public virtual ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    public virtual ICollection<PersonalityTrait> PersonalityTraits { get; set; } = new List<PersonalityTrait>();
}
```

### PersonalityTrait Entity
```csharp
public class PersonalityTrait : BaseEntity
{
    public Guid PersonalityProfileId { get; set; }
    public string TraitName { get; set; }               // "Openness", "Conscientiousness" 
    public double TraitValue { get; set; }              // 0.0 - 1.0 scale
    public string Context { get; set; }                 // When this trait applies
    public DateTime LastUpdated { get; set; }
    
    // Navigation properties
    public virtual PersonalityProfile PersonalityProfile { get; set; }
}
```

## Communication System Entities

### Conversation Entity
```csharp
public class Conversation : BaseEntity
{
    public Guid PersonalityProfileId { get; set; }
    public string Platform { get; set; }                // "Web", "Telegram", "Mobile"
    public string Title { get; set; }
    public JsonDocument Context { get; set; }           // JSONB conversation context
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation properties  
    public virtual PersonalityProfile PersonalityProfile { get; set; }
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
```

### Message Entity
```csharp
public class Message : BaseEntity
{
    public Guid ConversationId { get; set; }
    public string Content { get; set; }
    public MessageType Type { get; set; }               // User, Assistant, System
    public JsonDocument Metadata { get; set; }          // JSONB additional data
    public DateTime SentAt { get; set; }
    public PersonalityMood DetectedMood { get; set; }
    public double ConfidenceScore { get; set; }
    
    // Navigation properties
    public virtual Conversation Conversation { get; set; }
}
```

## Integration Entities

### TelegramMessage Entity
```csharp
public class TelegramMessage : BaseEntity
{
    public long ChatId { get; set; }
    public int MessageId { get; set; }
    public string Username { get; set; }
    public string Content { get; set; }
    public DateTime ReceivedAt { get; set; }
    public bool Processed { get; set; } = false;
    
    // Link to core conversation
    public Guid? ConversationId { get; set; }
    public virtual Conversation Conversation { get; set; }
}
```

### CalendarEvent Entity
```csharp
public class CalendarEvent : BaseEntity
{
    public string GoogleEventId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public JsonDocument Attendees { get; set; }         // JSONB attendee list
    public string Location { get; set; }
    public DateTime LastSynced { get; set; }
}
```

### GitHubRepository Entity
```csharp
public class GitHubRepository : BaseEntity
{
    public long GitHubId { get; set; }
    public string Name { get; set; }
    public string FullName { get; set; }               // "owner/repo"
    public string Description { get; set; }
    public string Language { get; set; }
    public int Stars { get; set; }
    public bool IsPrivate { get; set; }
    public DateTime LastCommitAt { get; set; }
    public JsonDocument Metadata { get; set; }         // JSONB additional GitHub data
}
```

## System Monitoring Entities

### AgentAction Entity
```csharp
public class AgentAction : BaseEntity
{
    public Guid? PersonalityProfileId { get; set; }
    public string ActionType { get; set; }             // "MessageSent", "PersonalityUpdated"
    public string Description { get; set; }
    public JsonDocument Parameters { get; set; }       // JSONB action parameters
    public ActionResult Result { get; set; }
    public DateTime ExecutedAt { get; set; }
    public TimeSpan Duration { get; set; }
    
    // Navigation properties
    public virtual PersonalityProfile PersonalityProfile { get; set; }
}
```

### IntegrationLog Entity
```csharp
public class IntegrationLog : BaseEntity
{
    public string IntegrationName { get; set; }        // "Telegram", "Google", "GitHub"
    public string Operation { get; set; }              // "FetchMessages", "SyncCalendar"
    public IntegrationStatus Status { get; set; }
    public string ErrorMessage { get; set; }
    public JsonDocument RequestData { get; set; }      // JSONB request details
    public JsonDocument ResponseData { get; set; }     // JSONB response details
    public DateTime Timestamp { get; set; }
    public TimeSpan Duration { get; set; }
}
```

## Base Entity & Enums

### BaseEntity Abstract Class
```csharp
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public string CreatedBy { get; set; }
    public string UpdatedBy { get; set; }
}
```

### Supporting Enums
```csharp
public enum PersonalityMood
{
    Calm = 0, Focused = 1, Tired = 2, Irritated = 3, Excited = 4
}

public enum MessageType  
{
    User = 0, Assistant = 1, System = 2
}

public enum ActionResult
{
    Success = 0, Failed = 1, Partial = 2
}

public enum IntegrationStatus
{
    Success = 0, Failed = 1, Timeout = 2, RateLimit = 3
}
```

## Entity Framework Configurations

### PersonalityProfile Configuration
**File**: `src/DigitalMe.Data/Configurations/PersonalityProfileConfiguration.cs`

```csharp
public class PersonalityProfileConfiguration : IEntityTypeConfiguration<PersonalityProfile>
{
    public void Configure(EntityTypeBuilder<PersonalityProfile> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Description).HasMaxLength(2000);
        
        // JSONB configuration for PostgreSQL
        builder.Property(p => p.Traits)
               .HasColumnType("jsonb")
               .HasConversion(
                   v => v.RootElement.GetRawText(),
                   v => JsonDocument.Parse(v));
                   
        builder.Property(p => p.CommunicationStyle)
               .HasColumnType("jsonb")
               .HasConversion(
                   v => v.RootElement.GetRawText(), 
                   v => JsonDocument.Parse(v));
                   
        // Indexes for performance
        builder.HasIndex(p => p.Name).IsUnique();
        builder.HasIndex(p => p.CurrentMood);
        builder.HasIndex(p => p.LastInteraction);
    }
}
```

## Success Criteria
- [ ] All entities compile without errors
- [ ] JSONB properties properly configured for PostgreSQL
- [ ] Entity relationships correctly established  
- [ ] Configuration classes implement IEntityTypeConfiguration
- [ ] Indexes created for performance optimization
- [ ] BaseEntity provides audit trail functionality

## Navigation
- **Parent**: [Database Design Specification](../02-01-database-design.md)
- **Previous**: [DbContext Implementation](./02-01-01-dbcontext-implementation.md)
- **Next**: [Migration Scripts](./02-01-03-migrations.md)