# Message Repository Architecture 📝

> **Parent Plan**: [03-02-03-repositories-implementation.md](../03-02-03-repositories-implementation.md) | **Plan Type**: REPOSITORY ARCHITECTURE | **LLM Ready**: ✅ YES  
> **Prerequisites**: ConversationMessage entity, DbContext | **Execution Time**: 2-3 days

📍 **Architecture** → **Implementation** → **Repositories** → **Message**

## MessageRepository Architecture Overview

### Core Responsibilities
- **Message CRUD**: Complete data access for conversation messages
- **Conversation Filtering**: Efficient message retrieval by conversation ID
- **Temporal Queries**: Time-based message filtering and ordering
- **Performance Optimization**: Large conversation history handling
- **Bulk Operations**: Efficient batch message operations

### Repository Interface Architecture

```csharp
public interface IMessageRepository : IRepository<ConversationMessage>
{
    // Core CRUD operations (inherited from IRepository<T>)
    
    // Conversation-based queries
    Task<List<ConversationMessage>> GetByConversationIdAsync(Guid conversationId, int limit = 20);
    Task<List<ConversationMessage>> GetByConversationIdAsync(Guid conversationId, int take, int skip = 0);
    Task<int> GetMessageCountAsync(Guid conversationId);
    
    // Temporal queries
    Task<List<ConversationMessage>> GetRecentAsync(Guid conversationId, TimeSpan timeWindow);
    Task<List<ConversationMessage>> GetFromDateAsync(Guid conversationId, DateTime fromDate, int limit = 100);
    Task<List<ConversationMessage>> GetDateRangeAsync(Guid conversationId, DateTime fromDate, DateTime toDate);
    
    // Role-based queries
    Task<List<ConversationMessage>> GetByRoleAsync(Guid conversationId, string role, int limit = 20);
    Task<int> GetCountByRoleAsync(Guid conversationId, string role);
    
    // Bulk operations
    Task DeleteByConversationIdAsync(Guid conversationId);
    Task<int> DeleteOldMessagesAsync(Guid conversationId, DateTime cutoffDate);
    Task<List<ConversationMessage>> GetLastNMessagesAsync(Guid conversationId, int count);
}
```

### Entity Framework Implementation Architecture

#### Conversation-Based Queries Design
**Architecture Balance**: 85% query patterns, 15% implementation stub

```csharp
public class MessageRepository : Repository<ConversationMessage>, IMessageRepository
{
    public MessageRepository(DigitalMeDbContext context, ILogger<MessageRepository> logger)
        : base(context, logger)
    {
    }

    public async Task<List<ConversationMessage>> GetByConversationIdAsync(Guid conversationId, int limit = 20)
    {
        _logger.LogInformation("Retrieving messages for conversation {ConversationId}, limit: {Limit}", 
            conversationId, limit);
        
        // TODO: Implement conversation-based query with ordering by Timestamp DESC
        // TODO: Apply limit for performance
        // TODO: Use AsNoTracking for read-only scenarios
        
        throw new NotImplementedException("Conversation messages query implementation pending");
    }

    public async Task<List<ConversationMessage>> GetByConversationIdAsync(Guid conversationId, int take, int skip = 0)
    {
        _logger.LogInformation("Retrieving messages for conversation {ConversationId} with pagination: skip={Skip}, take={Take}", 
            conversationId, skip, take);
        
        // TODO: Implement pagination with Skip/Take
        // TODO: Ensure consistent ordering by Timestamp ASC for chronological display
        // TODO: Optimize for large conversation histories
        
        throw new NotImplementedException("Paginated messages query implementation pending");
    }

    public async Task<int> GetMessageCountAsync(Guid conversationId)
    {
        // TODO: Implement efficient count query
        // TODO: Consider caching for frequently accessed counts
        // TODO: Use CountAsync for async performance
        
        throw new NotImplementedException("Message count query implementation pending");
    }
}
```

#### Temporal and Role-Based Queries Design
**Architecture Balance**: 85% filtering patterns, 15% implementation stub

```csharp
public async Task<List<ConversationMessage>> GetRecentAsync(Guid conversationId, TimeSpan timeWindow)
{
    var cutoffTime = DateTime.UtcNow.Subtract(timeWindow);
    _logger.LogInformation("Retrieving recent messages for conversation {ConversationId} since {CutoffTime}", 
        conversationId, cutoffTime);
    
    // TODO: Implement time-based filtering
    // TODO: Combine conversation and time filters efficiently
    // TODO: Order by timestamp for chronological display
    
    throw new NotImplementedException("Recent messages query implementation pending");
}

public async Task<List<ConversationMessage>> GetByRoleAsync(Guid conversationId, string role, int limit = 20)
{
    _logger.LogInformation("Retrieving messages by role {Role} for conversation {ConversationId}", 
        role, conversationId);
    
    // TODO: Implement role-based filtering
    // TODO: Validate role parameter
    // TODO: Combine with conversation filter for security
    
    throw new NotImplementedException("Role-based messages query implementation pending");
}

public async Task<List<ConversationMessage>> GetLastNMessagesAsync(Guid conversationId, int count)
{
    _logger.LogInformation("Retrieving last {Count} messages for conversation {ConversationId}", 
        count, conversationId);
    
    // TODO: Implement efficient "last N" query
    // TODO: Order by Timestamp DESC and take N
    // TODO: Reverse result for chronological display
    
    throw new NotImplementedException("Last N messages query implementation pending");
}
```

### Database Configuration Architecture

#### Entity Configuration Design
**Architecture Balance**: 85% EF configuration patterns, 15% implementation stub

```csharp
public class ConversationMessageConfiguration : IEntityTypeConfiguration<ConversationMessage>
{
    public void Configure(EntityTypeBuilder<ConversationMessage> builder)
    {
        // TODO: Configure table name and primary key
        builder.ToTable("ConversationMessages");
        builder.HasKey(m => m.Id);
        
        // TODO: Configure required properties
        builder.Property(m => m.ConversationId)
               .IsRequired();
        
        builder.Property(m => m.Content)
               .IsRequired()
               .HasMaxLength(10000); // Large text content
        
        builder.Property(m => m.Role)
               .IsRequired()
               .HasMaxLength(20);
        
        builder.Property(m => m.SenderId)
               .HasMaxLength(100);
        
        // TODO: Configure indexes for query performance
        builder.HasIndex(m => m.ConversationId);
        builder.HasIndex(m => m.Timestamp);
        builder.HasIndex(m => m.Role);
        
        // TODO: Composite indexes for common queries
        builder.HasIndex(m => new { m.ConversationId, m.Timestamp });
        builder.HasIndex(m => new { m.ConversationId, m.Role });
        
        // TODO: Configure relationship with Conversation
        builder.HasOne<Conversation>()
               .WithMany()
               .HasForeignKey(m => m.ConversationId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### Performance Optimization Architecture

#### Query Optimization Patterns
**Architecture Balance**: 85% optimization strategies, 15% implementation stub

```csharp
// Performance optimization patterns for MessageRepository:

// 1. Efficient conversation message loading
public async Task<List<ConversationMessage>> GetByConversationIdAsync(Guid conversationId, int limit = 20)
{
    // TODO: Implement optimized query with covering index
    return await _context.Messages
        .Where(m => m.ConversationId == conversationId)
        .OrderByDescending(m => m.Timestamp) // Latest first
        .Take(limit)
        .AsNoTracking() // Read-only optimization
        .ToListAsync();
    
    throw new NotImplementedException("Optimized conversation messages query implementation pending");
}

// 2. Bulk operations optimization
public async Task DeleteByConversationIdAsync(Guid conversationId)
{
    // TODO: Implement efficient bulk delete
    // TODO: Consider batch size for large conversations
    // TODO: Use ExecuteDeleteAsync for EF Core 7+
    
    throw new NotImplementedException("Bulk message deletion implementation pending");
}

// 3. Temporal query optimization
public async Task<List<ConversationMessage>> GetFromDateAsync(Guid conversationId, DateTime fromDate, int limit = 100)
{
    // TODO: Implement date range query with index optimization
    // TODO: Use covering index on (ConversationId, Timestamp)
    // TODO: Consider partition pruning for large datasets
    
    throw new NotImplementedException("Optimized temporal query implementation pending");
}
```

### Bulk Operations Architecture

```csharp
// Bulk operations for message management:

public async Task<int> DeleteOldMessagesAsync(Guid conversationId, DateTime cutoffDate)
{
    _logger.LogInformation("Deleting old messages for conversation {ConversationId} before {CutoffDate}", 
        conversationId, cutoffDate);
    
    // TODO: Implement batch deletion with size limits
    // TODO: Use ExecuteDeleteAsync for better performance
    // TODO: Return count of deleted messages
    // TODO: Consider archiving instead of deleting
    
    throw new NotImplementedException("Old message cleanup implementation pending");
}

public async Task<List<ConversationMessage>> BulkInsertAsync(List<ConversationMessage> messages)
{
    // TODO: Implement efficient bulk insert
    // TODO: Use AddRange for multiple messages
    // TODO: Consider EF Core bulk extensions for large batches
    // TODO: Validate all messages have same ConversationId
    
    throw new NotImplementedException("Bulk message insert implementation pending");
}

public async Task UpdateMessageContentAsync(Guid messageId, string newContent)
{
    // TODO: Implement efficient single-field update
    // TODO: Add timestamp update for audit purposes
    // TODO: Use ExecuteUpdateAsync for better performance
    
    throw new NotImplementedException("Message content update implementation pending");
}
```

### Message Analytics Architecture

```csharp
// Analytics queries for message data:

public async Task<Dictionary<string, int>> GetMessageCountByRoleAsync(Guid conversationId)
{
    // TODO: Implement role-based message counting
    // TODO: Use GroupBy for efficient aggregation
    // TODO: Return dictionary with role -> count mapping
    
    throw new NotImplementedException("Message role analytics implementation pending");
}

public async Task<TimeSpan> GetAverageResponseTimeAsync(Guid conversationId)
{
    // TODO: Calculate time between user and assistant messages
    // TODO: Filter out system messages from calculation
    // TODO: Handle edge cases (no responses, single messages)
    
    throw new NotImplementedException("Response time analytics implementation pending");
}

public async Task<List<ConversationMessage>> GetLongestMessagesAsync(Guid conversationId, int count = 10)
{
    // TODO: Order messages by content length
    // TODO: Return top N longest messages
    // TODO: Include metadata for analysis
    
    throw new NotImplementedException("Message length analytics implementation pending");
}
```

### Success Criteria

✅ **Conversation Queries**: Efficient message retrieval by conversation
✅ **Pagination**: Stable pagination for large message histories
✅ **Temporal Filtering**: Time-based message queries
✅ **Role Filtering**: Message filtering by role (user/assistant/system)
✅ **Bulk Operations**: Efficient batch operations for large datasets
✅ **Performance**: Query optimization for high-volume conversations

---

## 🔗 NAVIGATION & DEPENDENCIES

### Prerequisites
- **ConversationMessage Entity**: Domain entity with proper properties
- **DigitalMeDbContext**: EF Core database context
- **Conversation Entity**: Related conversation entity
- **Base Repository**: Generic repository base class (optional)

### Related Plans
- **Parent**: [03-02-03-repositories-implementation.md](../03-02-03-repositories-implementation.md)
- **Services**: ConversationService depends on this repository
- **Conversation Repository**: Related repository for conversation management

---

## 📊 PLAN METADATA

- **Type**: REPOSITORY ARCHITECTURE PLAN
- **LLM Ready**: ✅ YES
- **Implementation Depth**: 85% architecture / 15% implementation stubs
- **Execution Time**: 2-3 days
- **Balance Compliance**: ✅ ARCHITECTURAL FOCUS maintained