# Domain Model Blueprint
**Extracted from Test Architectural Intelligence**  
**Document Type**: Domain-Driven Design Specification  
**Analysis Date**: 2025-09-05  
**Source**: Test Entity Builders, Fixtures, and Service Contracts

## Domain Model Overview

The DigitalMe domain model, extracted from comprehensive test analysis, reveals a sophisticated **Personality-Driven Conversation Management System** with rich business rules and entity relationships designed for AI-powered digital identity modeling.

### Core Domain Concepts

```mermaid
graph TB
    subgraph "Personality Domain"
        PP[PersonalityProfile<br/>Core Identity]
        PT[PersonalityTrait<br/>Weighted Characteristics]
        PC[PersonalityContext<br/>Runtime State]
    end
    
    subgraph "Conversation Domain"  
        CONV[Conversation<br/>Chat Session]
        MSG[Message<br/>Chat Exchange]
        FLOW[MessageFlow<br/>Conversation Logic]
    end
    
    subgraph "Agent Domain"
        RESP[AgentResponse<br/>AI-Generated Reply]
        MOOD[MoodAnalysis<br/>Sentiment & Emotion]  
        CTX[ConversationContext<br/>Historical State]
    end
    
    subgraph "Integration Domain"
        TOOL[ToolExecution<br/>AI Capability]
        MCP[MCPIntegration<br/>External AI Services]
        PLAT[Platform<br/>Multi-Channel Support]
    end

    PP ||--o{ PT : "defined by"
    PP ||--|| PC : "generates"
    
    CONV ||--o{ MSG : "contains"
    CONV }o--|| PP : "responds with personality of"
    
    MSG ||--|| RESP : "generates"
    RESP ||--|| MOOD : "includes"
    
    PC --> CTX : "enriches"
    CTX --> RESP : "influences"
    
    RESP ||--o{ TOOL : "may trigger"
    TOOL --> MCP : "executes through"
    
    CONV }o--|| PLAT : "occurs on"

    style PP fill:#ff6b6b
    style CONV fill:#4ecdc4  
    style RESP fill:#45b7d1
    style TOOL fill:#f7b731
```

## Entity Definitions with Business Rules

### PersonalityProfile (Aggregate Root)

**Domain Responsibility**: Represents a complete digital personality with identity, characteristics, and behavioral patterns.

```csharp
// Extracted from PersonalityProfileBuilder.cs and test expectations
public class PersonalityProfile : BaseEntity
{
    // Identity
    public Guid Id { get; private set; }
    public string Name { get; private set; }        // Must be unique
    public string Description { get; private set; }
    public int Age { get; private set; }            // For personality context
    
    // Temporal tracking
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    
    // Composition
    public ICollection<PersonalityTrait> Traits { get; private set; }
    
    // Business Methods (inferred from tests)
    public string GenerateSystemPrompt() { /* Complex prompt generation */ }
    public MoodAnalysis AnalyzeMessageMood(string message) { /* Personality-specific mood analysis */ }
    public bool ShouldTriggerTool(string message) { /* Tool activation logic */ }
    public PersonalityContext CreateContext(IEnumerable<Message> recentMessages) { /* Context generation */ }
}
```

**Business Rules** (from PersonalityServiceTests.cs):
1. **Name Uniqueness**: Each PersonalityProfile must have a unique name
2. **Trait Weighting**: Traits influence personality with weighted importance (0.0-10.0)
3. **System Prompt Generation**: Must combine profile description + all traits
4. **Immutable Creation**: CreatedAt timestamp never changes after creation
5. **Update Tracking**: UpdatedAt modified on any profile or trait changes

### PersonalityTrait (Value Object)

**Domain Responsibility**: Represents a single personality characteristic with categorical organization and importance weighting.

```csharp
// Extracted from PersonalityTraitBuilder.cs and PersonalityTestFixtures.cs
public class PersonalityTrait : BaseEntity
{
    public Guid Id { get; private set; }
    public Guid PersonalityProfileId { get; private set; }
    
    // Categorization
    public string Category { get; private set; }    // e.g., "Technical", "Communication", "Leadership"
    public string Name { get; private set; }        // e.g., "C# Expert", "Direct Communication"
    public string Description { get; private set; } // Rich description for AI context
    
    // Importance weighting  
    public double Weight { get; private set; }      // 0.0 - 10.0, default 1.0
    
    public DateTime CreatedAt { get; private set; }
    
    // Navigation
    public PersonalityProfile PersonalityProfile { get; private set; }
}
```

**Business Rules** (from PersonalityServiceTests.cs):
1. **Weight Range**: Must be between 0.0 and 10.0, defaults to 1.0
2. **Category Organization**: Traits grouped by category for system prompt generation
3. **Required Association**: Must belong to a valid PersonalityProfile
4. **Immutable After Creation**: Traits should not be modified, only added/removed
5. **System Prompt Integration**: Description must be suitable for AI prompt generation

**Category Standards** (from test fixtures):
```csharp
// Standard trait categories extracted from tests
public static class TraitCategories  
{
    public const string Communication = "Communication";    // Communication style traits
    public const string Technical = "Technical";          // Technical expertise areas  
    public const string Leadership = "Leadership";        // Leadership and management traits
    public const string Values = "Values";               // Personal values and priorities
    public const string WorkStyle = "Work Style";        // Work preferences and approaches
    public const string Philosophy = "Philosophy";       // Life philosophy and worldview
}
```

### Conversation (Aggregate Root)

**Domain Responsibility**: Manages the complete lifecycle of a conversation session across platforms with proper state management.

```csharp
// Extracted from ConversationServiceTests.cs and ConversationBuilder.cs  
public class Conversation : BaseEntity
{
    // Identity & Platform
    public Guid Id { get; private set; }
    public string Platform { get; private set; }   // "Web", "Telegram", "Discord", etc.
    public string UserId { get; private set; }     // Platform-specific user identifier
    public string Title { get; private set; }      // Human-readable conversation title
    
    // Lifecycle management
    public bool IsActive { get; private set; }     // Only ONE active per (platform, userId)  
    public DateTime StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }
    
    // Message composition
    public ICollection<Message> Messages { get; private set; }
    
    // Business Methods (inferred from service tests)
    public Message AddMessage(string role, string content, Dictionary<string, object>? metadata = null) { }
    public IEnumerable<Message> GetRecentMessages(int limit) { }
    public void EndConversation() { }
    public bool CanAddMessage() { }
}
```

**Business Rules** (from ConversationServiceTests.cs):
1. **Single Active Rule**: Only one active conversation per (platform, userId) combination
2. **Lifecycle Management**: StartConversationAsync returns existing active or creates new
3. **Proper Termination**: EndConversation sets IsActive=false and EndedAt timestamp
4. **Message Ordering**: Messages maintained in chronological order by Timestamp
5. **Platform Isolation**: Conversations are isolated by platform (Telegram vs Web vs Discord)
6. **Immutable History**: Ended conversations preserved for audit trail, never deleted

### Message (Entity)

**Domain Responsibility**: Represents individual message exchanges within conversations with role-based classification and extensible metadata.

```csharp
// Extracted from MessageBuilder.cs and ConversationServiceTests.cs
public class Message : BaseEntity  
{
    public Guid Id { get; private set; }
    public Guid ConversationId { get; private set; }
    
    // Message classification  
    public string Role { get; private set; }        // "user", "assistant", "system"
    public string Content { get; private set; }     // The actual message text
    public DateTime Timestamp { get; private set; } // For chronological ordering
    
    // Extensibility
    public string? Metadata { get; private set; }   // JSON-serialized Dictionary<string, object>
    
    // Navigation
    public Conversation Conversation { get; private set; }
}
```

**Business Rules** (from ConversationServiceTests.cs):
1. **Role Classification**: Must be "user", "assistant", or "system"
2. **Chronological Ordering**: Messages retrieved by Timestamp descending (newest first)
3. **Conversation Association**: Must belong to valid, existing conversation
4. **Immutable Content**: Message content never modified after creation  
5. **Metadata Serialization**: Metadata stored as JSON for flexible schema evolution
6. **Timestamp Precision**: Timestamp set at creation time, never modified

## Advanced Domain Objects

### PersonalityContext (Value Object)

**Domain Responsibility**: Provides rich runtime context for personality-driven AI responses.

```csharp
// Extracted from AgentBehaviorEngineTests.cs and MCPIntegrationTests.cs
public class PersonalityContext
{
    public PersonalityProfile Profile { get; init; }
    public IEnumerable<Message> RecentMessages { get; init; }
    public Dictionary<string, object> CurrentState { get; init; }
    
    // Helper methods for AI integration
    public string GenerateContextPrompt() { }
    public bool HasRecentContext() { }
    public string GetPlatformContext() { }
}
```

### AgentResponse (Entity)

**Domain Responsibility**: Represents AI-generated responses with personality-aware metadata and mood analysis.

```csharp
// Extracted from AgentBehaviorEngineTests.cs
public class AgentResponse
{
    public string Content { get; init; }
    public MoodAnalysis Mood { get; init; }
    public int ConfidenceScore { get; init; }       // 0-100
    public Dictionary<string, object> Metadata { get; init; }
    
    // Business logic
    public bool IsHighConfidence => ConfidenceScore > 75;
    public bool RequiresFollowUp => Mood.Intensity < 0.3;
}
```

### MoodAnalysis (Value Object)

**Domain Responsibility**: Captures emotional and sentiment analysis tied to personality characteristics.

```csharp
// Extracted from AgentBehaviorEngineTests.cs  
public class MoodAnalysis
{
    public string PrimaryMood { get; init; }        // "positive", "negative", "neutral"
    public double Intensity { get; init; }          // 0.0 - 1.0
    public Dictionary<string, double> MoodScores { get; init; } // happiness, frustration, etc.
    
    // Business methods
    public bool IsPositive => PrimaryMood == "positive";
    public bool IsHighIntensity => Intensity > 0.7;
    public string GetDominantEmotion() { }
}
```

## Domain Service Contracts

### PersonalityService (Domain Service)

**Responsibility**: Orchestrates personality-related business operations and AI integration.

```csharp
// Extracted from PersonalityServiceTests.cs
public interface IPersonalityService
{
    // Personality management
    Task<PersonalityProfile?> GetPersonalityAsync(string name);
    Task<PersonalityProfile> CreatePersonalityAsync(string name, string description);
    Task<PersonalityProfile> UpdatePersonalityAsync(Guid id, string description);
    Task<bool> DeletePersonalityAsync(Guid id);
    
    // System prompt generation (core business logic)
    Task<string> GenerateSystemPromptAsync(Guid personalityId);
    
    // Trait management
    Task<PersonalityTrait> AddTraitAsync(Guid personalityId, string category, string name, 
                                        string description, double weight = 1.0);
    Task<IEnumerable<PersonalityTrait>> GetPersonalityTraitsAsync(Guid personalityId);
}
```

**Key Business Operations**:
1. **System Prompt Generation**: Complex algorithm combining profile + traits
2. **Trait Weighting**: Applies weighted importance to personality characteristics  
3. **Russian Language Context**: Personality responses in native language
4. **Personality Validation**: Ensures business rule compliance

### ConversationService (Domain Service)  

**Responsibility**: Manages conversation lifecycle and message flow with business rule enforcement.

```csharp
// Extracted from ConversationServiceTests.cs
public interface IConversationService
{
    // Conversation lifecycle
    Task<Conversation> StartConversationAsync(string platform, string userId, string title);
    Task<bool> EndConversationAsync(Guid conversationId);
    Task<IEnumerable<Conversation>> GetUserConversationsAsync(string platform, string userId);
    
    // Message management
    Task<Message> AddMessageAsync(Guid conversationId, string role, string content, 
                                 Dictionary<string, object>? metadata = null);
    Task<IEnumerable<Message>> GetConversationHistoryAsync(Guid conversationId, int limit = 50);
}
```

**Key Business Operations**:
1. **Single Active Conversation Rule**: Enforces one active conversation per user per platform
2. **Message Ordering**: Returns messages in chronological order for AI context
3. **Lifecycle Management**: Proper conversation start/end with audit trail
4. **Platform Isolation**: Maintains separation between platforms

### AgentBehaviorEngine (Domain Service)

**Responsibility**: Orchestrates AI-powered personality-driven response generation with advanced mood and tool integration.

```csharp
// Extracted from AgentBehaviorEngineTests.cs
public interface IAgentBehaviorEngine  
{
    // Core AI orchestration
    Task<AgentResponse> ProcessMessageAsync(string message, PersonalityContext context);
    Task<MoodAnalysis> AnalyzeMoodAsync(string message, PersonalityProfile personality);
    
    // Advanced capabilities (inferred from tests)
    Task<IEnumerable<ToolExecution>> GetTriggeredToolsAsync(string message, PersonalityContext context);
    Task<string> GeneratePersonalityResponseAsync(string message, PersonalityProfile personality, 
                                                  IEnumerable<Message> recentMessages);
}
```

## Domain Events (Inferred)

Based on test patterns, the system likely supports domain events for cross-cutting concerns:

```csharp
// Inferred from integration test patterns
public abstract class DomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
}

public class ConversationStarted : DomainEvent
{
    public Guid ConversationId { get; init; }
    public string Platform { get; init; }  
    public string UserId { get; init; }
}

public class MessageProcessed : DomainEvent
{
    public Guid MessageId { get; init; }
    public Guid ConversationId { get; init; }
    public string Role { get; init; }
    public AgentResponse? AgentResponse { get; init; }
}

public class PersonalityProfileUpdated : DomainEvent
{
    public Guid PersonalityProfileId { get; init; }
    public string[]? AddedTraits { get; init; }
    public string[]? RemovedTraits { get; init; }
}
```

## Repository Contracts (Domain Boundaries)

### IPersonalityRepository

```csharp  
// Extracted from PersonalityServiceTests.cs mocking patterns
public interface IPersonalityRepository
{
    // Profile operations
    Task<PersonalityProfile?> GetProfileAsync(string name);
    Task<PersonalityProfile?> GetProfileByIdAsync(Guid id);
    Task<PersonalityProfile> CreateProfileAsync(PersonalityProfile profile);
    Task<PersonalityProfile> UpdateProfileAsync(PersonalityProfile profile);
    Task<bool> DeleteProfileAsync(Guid id);
    
    // Trait operations  
    Task<IEnumerable<PersonalityTrait>> GetTraitsAsync(Guid personalityId);
    Task<PersonalityTrait> AddTraitAsync(PersonalityTrait trait);
    Task<bool> RemoveTraitAsync(Guid traitId);
}
```

### IConversationRepository

```csharp
// Extracted from ConversationServiceTests.cs usage patterns
public interface IConversationRepository
{
    // Basic CRUD
    Task<Conversation?> GetByIdAsync(Guid id);
    Task<Conversation> CreateAsync(Conversation conversation);
    Task<Conversation> UpdateAsync(Conversation conversation);
    
    // Business queries
    Task<Conversation?> GetActiveConversationAsync(string platform, string userId);
    Task<IEnumerable<Conversation>> GetUserConversationsAsync(string platform, string userId);
    Task<bool> HasActiveConversationAsync(string platform, string userId);
}
```

### IMessageRepository

```csharp
// Extracted from ConversationServiceTests.cs message handling
public interface IMessageRepository  
{
    Task<Message> AddAsync(Message message);
    Task<IEnumerable<Message>> GetByConversationIdAsync(Guid conversationId, int limit = 50);
    Task<IEnumerable<Message>> GetRecentMessagesAsync(Guid conversationId, int count = 10);
    Task<int> GetMessageCountAsync(Guid conversationId);
}
```

## Invariants and Business Constraints

### System-Wide Invariants

1. **Conversation Uniqueness**: At most one active conversation per (platform, userId)
2. **Message Ordering**: Messages within conversation must be chronologically ordered
3. **Personality Integrity**: All personality traits must belong to valid personality profiles
4. **Platform Isolation**: Conversations, users, and messages are isolated by platform
5. **Audit Trail**: Ended conversations and all messages are preserved indefinitely

### Data Consistency Rules

1. **Referential Integrity**: All foreign key relationships must be valid
2. **Temporal Consistency**: EndedAt must be greater than StartedAt when present
3. **Role Validation**: Message roles must be from allowed set ("user", "assistant", "system")  
4. **Weight Bounds**: Personality trait weights must be between 0.0 and 10.0
5. **Active State**: Conversation.IsActive must be false if EndedAt is not null

### Business Logic Constraints

1. **Name Uniqueness**: PersonalityProfile names must be unique across system
2. **Platform Support**: Only registered platforms allowed for conversations
3. **Message History**: Maximum message history limits to prevent unbounded growth
4. **Trait Categories**: Personality traits should use standard category names
5. **Russian Language**: Personality responses generated in Russian for authenticity

---

**Implementation Priority**: 
1. **PersonalityProfile + PersonalityTrait** (Core Domain)
2. **Conversation + Message** (Essential Operations)  
3. **AgentResponse + MoodAnalysis** (AI Integration)
4. **PersonalityContext** (Advanced AI Features)
5. **Domain Events** (Cross-cutting Concerns)

**Next Documents**:
- [Service Architecture Roadmap](./SERVICE-ARCHITECTURE-ROADMAP.md)
- [Implementation Roadmaps](./IMPLEMENTATION-ROADMAPS/)
- [Technical Debt Analysis](./TECHNICAL-DEBT-ANALYSIS.md)