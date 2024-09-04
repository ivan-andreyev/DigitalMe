# Conversation Service Architecture 💬

> **Parent Plan**: [03-02-02-services-implementation.md](../03-02-02-services-implementation.md) | **Plan Type**: SERVICE ARCHITECTURE | **LLM Ready**: ✅ YES  
> **Prerequisites**: IConversationRepository interface | **Execution Time**: 1.5 days

📍 **Architecture** → **Implementation** → **Services** → **Conversation** → **Architecture**

## ConversationService Architecture Overview

### Core Responsibilities
- **Conversation Lifecycle**: Create, manage, and organize conversations
- **Message Management**: Store, retrieve, and organize conversation messages
- **Context Tracking**: Maintain conversation context and history
- **Performance Optimization**: Efficient message retrieval with pagination
- **Conversation Analytics**: Track conversation metrics and patterns

### Architectural Patterns

#### Domain-Driven Design Patterns
- **Service Layer Pattern**: Clear separation of business logic
- **Repository Pattern Integration**: Clean data access abstraction
- **Dependency Injection**: Testable and maintainable dependencies
- **Validation Pattern**: Comprehensive input validation
- **Exception Handling Pattern**: Custom exceptions for domain errors

#### Performance Architecture
- **Pagination Strategy**: Skip/take patterns for large datasets
- **Lazy Loading**: Minimal data retrieval for common operations
- **Caching Strategy**: Conversation metadata caching
- **Query Optimization**: Index-friendly query patterns

### Class Structure Design

```csharp
namespace DigitalMe.Core.Services;

public class ConversationService : IConversationService
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly ILogger<ConversationService> _logger;
    
    // Constructor with dependency injection
    // Conversation management methods
    // Message management methods
    // Context tracking methods
    // Analytics methods
}
```

### Service Interface Architecture

```csharp
public interface IConversationService
{
    // Conversation Management
    Task<Conversation> GetOrCreateConversationAsync(Guid? conversationId, string profileName, string platform, string userId);
    Task<Conversation?> GetConversationAsync(Guid conversationId);
    Task<List<Conversation>> GetUserConversationsAsync(string userId, int limit = 20);
    Task UpdateConversationTitleAsync(Guid conversationId, string title);
    Task DeleteConversationAsync(Guid conversationId);
    
    // Message Management
    Task<ConversationMessage> AddMessageAsync(Guid conversationId, string content, string role, string? senderId = null);
    Task<List<ConversationMessage>> GetRecentMessagesAsync(Guid conversationId, int limit = 20);
    Task<List<ConversationMessage>> GetMessagesAsync(Guid conversationId, int skip = 0, int take = 20);
    Task DeleteMessageAsync(Guid messageId);
    
    // Context and Analytics
    Task<ConversationSummary> GetConversationSummaryAsync(Guid conversationId);
    Task<int> GetMessageCountAsync(Guid conversationId);
}
```

### Conversation Management Architecture

#### Get or Create Pattern
**Business Logic**: Efficient conversation retrieval with fallback creation
- **Validation Layer**: ConversationId validation, ProfileName validation
- **Retrieval Strategy**: Try existing first, create if needed
- **Creation Logic**: Generate meaningful titles, set timestamps
- **Error Handling**: Null handling, validation exceptions
- **Logging Strategy**: Structured logging for operations

#### Conversation Retrieval Pattern  
**Query Optimization**: Direct ID-based retrieval with null safety
- **Validation Strategy**: Empty GUID validation, argument validation
- **Repository Interface**: Clean abstraction for data access
- **Error Handling**: Graceful null returns, exception management
- **Performance**: Single query retrieval, minimal data transfer

#### User Conversations Pattern
**Pagination Architecture**: Efficient multi-conversation retrieval
- **Input Validation**: UserId validation, limit clamping (1-100)
- **Sorting Strategy**: Most recent first (UpdatedAt DESC)
- **Performance**: Index-friendly queries, controlled result sets
- **Business Rules**: Reasonable limits, user isolation

### Message Management Architecture

#### Add Message Pattern
**Validation Architecture**: Multi-layer validation for message integrity
- **Input Validation**: ConversationId, content, role validation
- **Business Rules**: Valid roles (user/assistant/system), content requirements
- **Dependency Validation**: Conversation existence verification
- **Entity Creation**: Proper entity construction with timestamps
- **Side Effects**: Conversation UpdatedAt maintenance

#### Message Retrieval Patterns
**Recent Messages**: Optimized for chat interface display
- **Performance**: Descending timestamp order, limited results
- **Display Logic**: Chronological reversal for UI consumption
- **Validation**: Conversation existence, limit validation

**Paginated Messages**: Full conversation history access
- **Pagination Logic**: Skip/take pattern, ascending order
- **Parameter Validation**: Non-negative skip, reasonable take limits
- **Performance**: Index-friendly queries, controlled memory usage

### Analytics Architecture

#### Conversation Summary Pattern
**Aggregation Strategy**: Multi-source data consolidation
- **Statistics Gathering**: Message counts, timing analysis
- **Metadata Assembly**: Conversation properties, calculated fields
- **Extensibility Design**: Prepared for additional analytics
- **Performance**: Efficient multi-query coordination

### DTOs and Data Transfer Objects

```csharp
public class ConversationSummary
{
    public Guid ConversationId { get; set; }
    public string Title { get; set; } = default!;
    public int MessageCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastMessageAt { get; set; }
    public string ProfileName { get; set; } = default!;
    public string Platform { get; set; } = default!;
    
    // Extensibility for future analytics
    // public TimeSpan AverageResponseTime { get; set; }
    // public string DominantMood { get; set; }
    // public List<string> Topics { get; set; }
}
```

### Exception Handling Architecture

#### Custom Exception Strategy
- **ConversationNotFoundException**: Domain-specific error handling
- **ValidationException**: Input validation failures
- **BusinessRuleException**: Business logic violations
- **RepositoryException**: Data access failures

#### Error Response Patterns
- **Graceful Degradation**: Null returns vs exceptions
- **Logging Strategy**: Structured error logging
- **Client Communication**: Clear error messages
- **Recovery Strategies**: Retry logic where appropriate

### Dependency Management

#### Repository Dependencies
- **IConversationRepository**: Conversation data persistence
- **IMessageRepository**: Message data persistence
- **Unit of Work Pattern**: Transaction management
- **Connection Management**: Database connection lifecycle

#### Infrastructure Dependencies
- **ILogger<T>**: Structured logging
- **Configuration**: Service configuration
- **Health Checks**: Service monitoring
- **Metrics Collection**: Performance monitoring

### Performance Considerations

#### Query Optimization Strategies
- **Index Strategy**: ConversationId, UserId, Timestamp indices
- **Query Patterns**: Efficient WHERE clauses, proper JOINs
- **Data Transfer**: Minimal column selection
- **Caching Strategy**: Conversation metadata caching

#### Scalability Architecture
- **Connection Pooling**: Database connection management
- **Async Patterns**: Non-blocking operations
- **Resource Management**: Memory-efficient operations
- **Load Distribution**: Stateless service design

### Testing Architecture

#### Unit Testing Strategy
- **Service Logic Testing**: Business rule validation
- **Mock Dependencies**: Repository and infrastructure mocks
- **Edge Case Coverage**: Null inputs, invalid data
- **Exception Testing**: Error condition validation

#### Integration Testing Strategy
- **Repository Integration**: Database operation testing
- **End-to-End Flows**: Complete conversation workflows
- **Performance Testing**: Response time validation
- **Concurrency Testing**: Multi-user scenarios

---

## 🔗 NAVIGATION & DEPENDENCIES

### Prerequisites
- **IConversationRepository**: Repository for conversation data access
- **IMessageRepository**: Repository for message data access
- **Entity Models**: Conversation and ConversationMessage entities
- **Custom Exceptions**: ConversationNotFoundException

### Implementation Guidance
- **Next**: [03-02-02-02-conversation-service-implementation.md](03-02-02-02-conversation-service-implementation.md)

---

## 📊 PLAN METADATA

- **Type**: SERVICE ARCHITECTURE PLAN
- **LLM Ready**: ✅ YES
- **Implementation Depth**: 95% architecture / 5% code stubs
- **Execution Time**: 1.5 days
- **Lines**: 196 (under 400 limit)
- **Balance Compliance**: ✅ PURE ARCHITECTURAL FOCUS maintained