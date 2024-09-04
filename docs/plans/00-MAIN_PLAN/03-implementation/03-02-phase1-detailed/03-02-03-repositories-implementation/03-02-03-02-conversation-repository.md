# Conversation Repository Architecture 💬

> **Parent Plan**: [03-02-03-repositories-implementation.md](../03-02-03-repositories-implementation.md) | **Plan Type**: REPOSITORY ARCHITECTURE | **LLM Ready**: ✅ YES  
> **Prerequisites**: Conversation entity, DbContext | **Execution Time**: 2-3 days

📍 **Architecture** → **Implementation** → **Repositories** → **Conversation**

## ConversationRepository Architecture Overview

### Core Responsibilities
- **Conversation CRUD**: Complete data access for conversation entities
- **User-Based Filtering**: Efficient queries by user ID
- **Platform Filtering**: Conversation filtering by platform type
- **Pagination Support**: Efficient large dataset handling
- **Relationship Management**: Proper handling of conversation-message relationships

### Repository Interface Architecture

```csharp
public interface IConversationRepository : IRepository<Conversation>
{
    // Core CRUD operations (inherited from IRepository<T>)
    
    // User-based queries
    Task<List<Conversation>> GetByUserIdAsync(string userId, int limit = 20);
    Task<List<Conversation>> GetByUserIdAsync(string userId, int skip, int take);
    Task<int> GetCountByUserIdAsync(string userId);
    
    // Platform-based queries
    Task<List<Conversation>> GetByPlatformAsync(string platform, int limit = 20);
    Task<List<Conversation>> GetRecentByPlatformAsync(string platform, TimeSpan timeWindow);
    
    // Advanced filtering
    Task<List<Conversation>> GetByProfileNameAsync(string profileName, int limit = 20);
    Task<List<Conversation>> SearchConversationsAsync(string userId, string? searchTerm, int limit = 20);
    
    // Analytics and reporting
    Task<int> GetTotalCountAsync();
    Task<Dictionary<string, int>> GetConversationCountByPlatformAsync();
}
```

### Entity Framework Implementation Architecture

#### User-Based Queries Design
**Architecture Balance**: 85% query patterns, 15% implementation stub

```csharp
public class ConversationRepository : Repository<Conversation>, IConversationRepository
{
    public ConversationRepository(DigitalMeDbContext context, ILogger<ConversationRepository> logger)
        : base(context, logger)
    {
    }

    public async Task<List<Conversation>> GetByUserIdAsync(string userId, int limit = 20)
    {
        _logger.LogInformation("Retrieving conversations for user {UserId}, limit: {Limit}", userId, limit);
        
        // TODO: Implement user-based query with ordering by UpdatedAt DESC
        // TODO: Apply limit to prevent large result sets
        // TODO: Consider including related entities if needed
        
        throw new NotImplementedException("User conversations query implementation pending");
    }

    public async Task<List<Conversation>> GetByUserIdAsync(string userId, int skip, int take)
    {
        _logger.LogInformation("Retrieving conversations for user {UserId} with pagination: skip={Skip}, take={Take}", 
            userId, skip, take);
        
        // TODO: Implement pagination with Skip/Take
        // TODO: Ensure consistent ordering for reliable pagination
        // TODO: Optimize query performance for large datasets
        
        throw new NotImplementedException("Paginated user conversations implementation pending");
    }

    public async Task<int> GetCountByUserIdAsync(string userId)
    {
        // TODO: Implement efficient count query
        // TODO: Use CountAsync for better performance
        // TODO: Consider caching for frequently accessed counts
        
        throw new NotImplementedException("User conversation count implementation pending");
    }
}
```

#### Platform and Search Queries Design
**Architecture Balance**: 85% filtering patterns, 15% implementation stub

```csharp
public async Task<List<Conversation>> GetByPlatformAsync(string platform, int limit = 20)
{
    _logger.LogInformation("Retrieving conversations for platform {Platform}, limit: {Limit}", platform, limit);
    
    // TODO: Implement platform-based filtering
    // TODO: Order by most recent activity
    // TODO: Apply limit for performance
    
    throw new NotImplementedException("Platform conversations query implementation pending");
}

public async Task<List<Conversation>> SearchConversationsAsync(string userId, string? searchTerm, int limit = 20)
{
    _logger.LogInformation("Searching conversations for user {UserId} with term: {SearchTerm}", 
        userId, searchTerm ?? "null");
    
    // TODO: Implement full-text search on Title
    // TODO: Filter by user ID first for security
    // TODO: Handle null/empty search terms
    // TODO: Consider case-insensitive search
    
    throw new NotImplementedException("Conversation search implementation pending");
}

public async Task<List<Conversation>> GetRecentByPlatformAsync(string platform, TimeSpan timeWindow)
{
    var cutoffTime = DateTime.UtcNow.Subtract(timeWindow);
    
    // TODO: Implement time-based filtering
    // TODO: Combine platform and time window filters
    // TODO: Order by most recent first
    
    throw new NotImplementedException("Recent conversations by platform implementation pending");
}
```

### Database Configuration Architecture

#### Entity Configuration Design
**Architecture Balance**: 85% EF configuration patterns, 15% implementation stub

```csharp
public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        // TODO: Configure table name and primary key
        builder.ToTable("Conversations");
        builder.HasKey(c => c.Id);
        
        // TODO: Configure required properties
        builder.Property(c => c.UserId)
               .IsRequired()
               .HasMaxLength(100);
        
        builder.Property(c => c.ProfileName)
               .IsRequired()
               .HasMaxLength(100);
        
        builder.Property(c => c.Platform)
               .IsRequired()
               .HasMaxLength(50);
        
        builder.Property(c => c.Title)
               .HasMaxLength(200);
        
        // TODO: Configure indexes for common queries
        builder.HasIndex(c => c.UserId);
        builder.HasIndex(c => c.Platform);
        builder.HasIndex(c => c.ProfileName);
        builder.HasIndex(c => c.UpdatedAt);
        
        // TODO: Composite index for user + platform queries
        builder.HasIndex(c => new { c.UserId, c.Platform });
        
        // TODO: Configure relationship with Messages
        builder.HasMany<ConversationMessage>()
               .WithOne()
               .HasForeignKey(m => m.ConversationId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### Performance Optimization Architecture

#### Query Optimization Patterns
**Architecture Balance**: 85% optimization strategies, 15% implementation stub

```csharp
// Performance optimization patterns for ConversationRepository:

// 1. Efficient user conversations with caching
public async Task<List<Conversation>> GetByUserIdAsync(string userId, int limit = 20)
{
    // TODO: Add query optimization
    return await _context.Conversations
        .Where(c => c.UserId == userId)
        .OrderByDescending(c => c.UpdatedAt)
        .Take(limit)
        // TODO: Select only needed fields for list view
        // .Select(c => new ConversationListDto { ... })
        .AsNoTracking() // Read-only optimization
        .ToListAsync();
    
    throw new NotImplementedException("Optimized user conversations query implementation pending");
}

// 2. Pagination with stable ordering
public async Task<List<Conversation>> GetByUserIdAsync(string userId, int skip, int take)
{
    // TODO: Implement stable pagination ordering
    // TODO: Consider using cursor-based pagination for large datasets
    // TODO: Add query plan caching
    
    throw new NotImplementedException("Optimized pagination implementation pending");
}

// 3. Analytics queries optimization
public async Task<Dictionary<string, int>> GetConversationCountByPlatformAsync()
{
    // TODO: Implement efficient grouping query
    // TODO: Consider materialized views for complex analytics
    // TODO: Add caching for expensive aggregate queries
    
    throw new NotImplementedException("Platform analytics query implementation pending");
}
```

### Relationship Management Architecture

```csharp
// Conversation-Message relationship handling patterns:

public async Task<Conversation> CreateWithInitialMessageAsync(
    Conversation conversation, 
    ConversationMessage? initialMessage = null)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    
    try
    {
        // TODO: Create conversation first
        var createdConversation = await CreateAsync(conversation);
        
        // TODO: Add initial message if provided
        if (initialMessage != null)
        {
            initialMessage.ConversationId = createdConversation.Id;
            // TODO: Add message through MessageRepository
        }
        
        await transaction.CommitAsync();
        return createdConversation;
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
    
    throw new NotImplementedException("Transaction-based conversation creation implementation pending");
}

public async Task DeleteWithMessagesAsync(Guid conversationId)
{
    // TODO: Implement cascade delete or explicit message cleanup
    // TODO: Consider soft delete for audit purposes
    // TODO: Handle foreign key constraints properly
    
    throw new NotImplementedException("Conversation deletion with cleanup implementation pending");
}
```

### Success Criteria

✅ **User Queries**: Efficient conversation retrieval by user ID
✅ **Pagination**: Stable pagination for large conversation lists
✅ **Platform Filtering**: Conversation filtering by platform type
✅ **Search Capabilities**: Full-text search on conversation titles
✅ **Performance**: Query optimization and indexing strategies
✅ **Relationships**: Proper conversation-message relationship handling

---

## 🔗 NAVIGATION & DEPENDENCIES

### Prerequisites
- **Conversation Entity**: Domain entity with proper properties
- **DigitalMeDbContext**: EF Core database context
- **ConversationMessage Entity**: Related message entity
- **Base Repository**: Generic repository base class (optional)

### Related Plans
- **Parent**: [03-02-03-repositories-implementation.md](../03-02-03-repositories-implementation.md)
- **Services**: ConversationService depends on this repository
- **Message Repository**: Related repository for message management

---

## 📊 PLAN METADATA

- **Type**: REPOSITORY ARCHITECTURE PLAN
- **LLM Ready**: ✅ YES
- **Implementation Depth**: 85% architecture / 15% implementation stubs
- **Execution Time**: 2-3 days
- **Balance Compliance**: ✅ ARCHITECTURAL FOCUS maintained