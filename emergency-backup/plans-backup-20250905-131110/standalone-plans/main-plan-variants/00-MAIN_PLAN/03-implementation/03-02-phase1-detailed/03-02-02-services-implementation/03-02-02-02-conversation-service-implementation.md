# Conversation Service Implementation Guide 🔧

> **Parent Plan**: [03-02-02-02-conversation-service-architecture.md](03-02-02-02-conversation-service-architecture.md) | **Plan Type**: IMPLEMENTATION GUIDANCE | **LLM Ready**: ✅ YES  
> **Prerequisites**: Architecture patterns defined | **Execution Time**: 1.5 days

📍 **Architecture** → **Implementation** → **Services** → **Conversation** → **Implementation**

## Implementation Execution Guidance

### File Location Specification
**Target File**: `DigitalMe/Core/Services/ConversationService.cs`
**Line References**: Create new file with namespace structure

### Constructor Implementation
**File**: `ConversationService.cs` **Lines**: 15-25
```csharp
public ConversationService(
    IConversationRepository conversationRepository,
    IMessageRepository messageRepository,
    ILogger<ConversationService> logger)
{
    _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
    _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
}
```

### GetOrCreateConversationAsync Implementation
**File**: `ConversationService.cs` **Lines**: 30-70

**Critical Implementation Points**:
- **Line 35**: `if (conversationId.HasValue && conversationId != Guid.Empty)` validation
- **Line 40**: `await _conversationRepository.GetByIdAsync(conversationId.Value)` call
- **Line 50**: `new Conversation { Id = Guid.NewGuid(), ... }` entity creation
- **Line 60**: `await _conversationRepository.AddAsync(newConversation)` persistence
- **Line 65**: `_logger.LogInformation("Created conversation {Id}", newConversation.Id)` logging

**Implementation Template**:
```csharp
public async Task<Conversation> GetOrCreateConversationAsync(
    Guid? conversationId, string profileName, string platform, string userId)
{
    // Replace with actual repository calls and validation logic
    throw new NotImplementedException("Repository integration at lines 35-65 required");
}
```

### GetConversationAsync Implementation
**File**: `ConversationService.cs` **Lines**: 75-90

**Critical Implementation Points**:
- **Line 78**: `ArgumentException` for empty GUID
- **Line 82**: `await _conversationRepository.GetByIdAsync(conversationId)` call
- **Line 85**: Return null or conversation object

### GetUserConversationsAsync Implementation
**File**: `ConversationService.cs` **Lines**: 95-115

**Critical Implementation Points**:
- **Line 100**: `Math.Min(Math.Max(limit, 1), 100)` limit validation
- **Line 105**: `await _conversationRepository.GetByUserIdAsync(userId, limit)` call
- **Line 110**: OrderBy UpdatedAt descending in repository call

### AddMessageAsync Implementation  
**File**: `ConversationService.cs` **Lines**: 120-160

**Critical Implementation Points**:
- **Line 125**: Multi-parameter validation (conversationId, content, role)
- **Line 135**: `IsValidRole(role)` helper method call
- **Line 140**: Conversation existence verification
- **Line 145**: `new ConversationMessage { Id = Guid.NewGuid(), ... }` creation
- **Line 150**: `await _messageRepository.AddAsync(message)` persistence
- **Line 155**: Update conversation.UpdatedAt timestamp

### Message Retrieval Methods Implementation
**File**: `ConversationService.cs` **Lines**: 165-200

**GetRecentMessagesAsync Critical Points**:
- **Line 170**: Parameter validation and clamping
- **Line 175**: `await _messageRepository.GetRecentAsync(conversationId, limit)` call
- **Line 180**: Order by Timestamp DESC in repository

**GetMessagesAsync Critical Points**:
- **Line 185**: Skip/take validation (`Math.Max(skip, 0)`)
- **Line 190**: `await _messageRepository.GetPagedAsync(conversationId, skip, take)` call
- **Line 195**: Order by Timestamp ASC for chronological order

### Analytics Implementation
**File**: `ConversationService.cs` **Lines**: 205-240

**GetConversationSummaryAsync Critical Points**:
- **Line 210**: Conversation existence validation
- **Line 215**: `await GetMessageCountAsync(conversationId)` call
- **Line 220**: `await GetRecentMessagesAsync(conversationId, 5)` for sample
- **Line 225**: ConversationSummary object construction
- **Line 230**: LastMessageAt calculation from messages

### Helper Methods Implementation
**File**: `ConversationService.cs` **Lines**: 245-270

**IsValidRole Method**:
```csharp
private static bool IsValidRole(string role)
{
    var validRoles = new[] { "user", "assistant", "system" };
    return validRoles.Contains(role.ToLowerInvariant());
}
```

**GenerateConversationTitle Method**:
```csharp
private static string GenerateConversationTitle(string profileName, string platform)
{
    return $"Chat with {profileName} on {platform} - {DateTime.Now:MMM dd}";
}
```

### Update and Delete Operations
**File**: `ConversationService.cs` **Lines**: 275-315

**UpdateConversationTitleAsync Critical Points**:
- **Line 280**: Title validation (non-empty, trimmed)
- **Line 285**: Conversation existence check
- **Line 290**: `conversation.UpdatedAt = DateTime.UtcNow` update
- **Line 295**: `await _conversationRepository.UpdateAsync(conversation)` call

**DeleteConversationAsync Critical Points**:
- **Line 305**: Cascade delete pattern (messages first)
- **Line 310**: `await _conversationRepository.DeleteAsync(conversationId)` call

### Error Handling Implementation
**Custom Exceptions Required**:
```csharp
public class ConversationNotFoundException : Exception
{
    public ConversationNotFoundException(string message) : base(message) { }
}
```

### Dependency Injection Registration
**File**: `Program.cs` or `ServiceCollectionExtensions.cs`
```csharp
services.AddScoped<IConversationService, ConversationService>();
```

### Success Criteria - Measurable Implementation

✅ **File Creation**: ConversationService.cs exists in Core/Services/
✅ **Method Count**: 11 public methods implemented (not NotImplementedException)
✅ **Validation Logic**: Input validation on all 11 methods
✅ **Repository Calls**: 8+ actual repository method calls (not stubs)
✅ **Error Handling**: Custom exceptions thrown for business rule violations
✅ **Logging**: Structured logging on all major operations
✅ **Unit Tests**: 15+ test methods covering happy path and edge cases
✅ **Integration Tests**: 5+ tests with real repository dependencies

### Performance Targets
- **GetConversationAsync**: < 50ms response time
- **GetRecentMessagesAsync**: < 100ms for 20 messages
- **AddMessageAsync**: < 150ms including conversation update
- **GetUserConversationsAsync**: < 200ms for 20 conversations

---

## 🔗 NAVIGATION & DEPENDENCIES

### Prerequisites  
- **Architecture**: [03-02-02-02-conversation-service-architecture.md](03-02-02-02-conversation-service-architecture.md) must be reviewed
- **Repository Interfaces**: IConversationRepository, IMessageRepository must exist
- **Entity Models**: Conversation, ConversationMessage entities required

### Next Steps
- **Repository Implementation**: Create repository implementations
- **Controller Integration**: AgentController dependency injection
- **Testing**: Unit and integration test creation

---

## 📊 PLAN METADATA

- **Type**: IMPLEMENTATION GUIDANCE PLAN  
- **LLM Ready**: ✅ YES
- **Implementation Depth**: 15% guidance / 85% architectural direction
- **Execution Time**: 1.5 days
- **Lines**: 185 (under 400 limit)
- **Concrete Targets**: File:line references, measurable success criteria
- **Balance Compliance**: ✅ IMPLEMENTATION GUIDANCE FOCUS maintained