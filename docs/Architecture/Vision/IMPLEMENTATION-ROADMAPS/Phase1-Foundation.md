# Phase 1: Foundation Implementation Roadmap
**Duration**: 4-6 weeks  
**Priority**: Critical Foundation  
**Dependencies**: None (Start immediately)  
**Success Criteria**: Core domain entities and basic services operational

## Overview

Phase 1 establishes the **foundational domain layer and basic services** extracted from test architectural intelligence. This phase focuses on implementing the core entities, repository pattern, and basic domain services that form the backbone of the DigitalMe platform.

## Implementation Strategy

### Week 1-2: Core Domain Entities

#### Task 1.1: BaseEntity Infrastructure
**Reference**: `tests/DigitalMe.Tests.Unit/Builders/BaseEntityBuilder.cs`

```csharp
// Foundation entity pattern from test builders
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; protected set; } = DateTime.UtcNow;
    
    protected void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
```

**Implementation Steps**:
1. Create `BaseEntity` abstract class in `DigitalMe.Data.Entities`
2. Implement `ITrackableEntity` interface for timestamp management
3. Add Entity Framework configuration for base properties
4. Create unit tests validating timestamp behavior

**Test Validation**: 
- `BaseEntityTestExtensions.cs` patterns should pass
- Timestamp behavior matches test expectations
- Guid generation works correctly

#### Task 1.2: PersonalityProfile Entity
**Reference**: `PersonalityProfileBuilder.cs`, `PersonalityTestFixtures.cs`

```csharp
// Complete PersonalityProfile implementation from test expectations
public class PersonalityProfile : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int Age { get; private set; }
    
    // Navigation properties
    public virtual ICollection<PersonalityTrait> Traits { get; private set; } = new List<PersonalityTrait>();
    
    // Factory methods (from test builders)
    public static PersonalityProfile CreateForIvan()
    {
        return new PersonalityProfile
        {
            Name = "Ivan",
            Age = 34, 
            Description = "Программист, Head of R&D, прямолинейный и честный"
        };
    }
    
    // Business methods (inferred from tests)
    public void UpdateDescription(string newDescription)
    {
        Description = newDescription;
        UpdateTimestamp();
    }
    
    public PersonalityTrait AddTrait(string category, string name, string description, double weight = 1.0)
    {
        var trait = new PersonalityTrait(Id, category, name, description, weight);
        Traits.Add(trait);
        return trait;
    }
}
```

**Implementation Steps**:
1. Create `PersonalityProfile` entity with private setters
2. Implement business methods for trait management
3. Add Entity Framework configuration and relationships
4. Create comprehensive unit tests using builder patterns

**Test Validation**:
- `PersonalityProfileBuilder.ForIvan()` should create valid entity
- Business rules from `PersonalityServiceTests.cs` should be enforced
- Entity Framework relationships should work correctly

#### Task 1.3: PersonalityTrait Entity
**Reference**: `PersonalityTraitBuilder.cs`, trait weighting tests

```csharp
public class PersonalityTrait : BaseEntity
{
    public Guid PersonalityProfileId { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public double Weight { get; private set; } = 1.0;
    
    // Navigation
    public virtual PersonalityProfile PersonalityProfile { get; private set; } = null!;
    
    // Constructor with validation
    public PersonalityTrait(Guid personalityProfileId, string category, string name, 
                           string description, double weight)
    {
        if (weight < 0.0 || weight > 10.0)
            throw new ArgumentException("Weight must be between 0.0 and 10.0");
            
        PersonalityProfileId = personalityProfileId;
        Category = category;
        Name = name;
        Description = description;
        Weight = weight;
    }
    
    // Standard categories from tests
    public static class Categories
    {
        public const string Communication = "Communication";
        public const string Technical = "Technical"; 
        public const string Leadership = "Leadership";
        public const string Values = "Values";
        public const string WorkStyle = "Work Style";
        public const string Philosophy = "Philosophy";
    }
}
```

**Implementation Steps**:
1. Create `PersonalityTrait` with weight validation
2. Define standard trait categories from test analysis
3. Implement Entity Framework configuration
4. Create unit tests for weight validation and categorization

#### Task 1.4: Conversation & Message Entities
**Reference**: `ConversationBuilder.cs`, `MessageBuilder.cs`, `ConversationServiceTests.cs`

```csharp
public class Conversation : BaseEntity
{
    public string Platform { get; private set; } = string.Empty;   // "Web", "Telegram", etc.
    public string UserId { get; private set; } = string.Empty;     // Platform-specific user ID
    public string Title { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTime StartedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; private set; }
    
    // Navigation
    public virtual ICollection<Message> Messages { get; private set; } = new List<Message>();
    
    // Business methods from test analysis
    public Message AddMessage(string role, string content, Dictionary<string, object>? metadata = null)
    {
        var message = new Message(Id, role, content, metadata);
        Messages.Add(message);
        return message;
    }
    
    public void EndConversation()
    {
        IsActive = false;
        EndedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }
    
    public IEnumerable<Message> GetRecentMessages(int limit = 10)
    {
        return Messages.OrderByDescending(m => m.Timestamp).Take(limit);
    }
}

public class Message : BaseEntity  
{
    public Guid ConversationId { get; private set; }
    public string Role { get; private set; } = string.Empty;        // "user", "assistant", "system"
    public string Content { get; private set; } = string.Empty;
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;
    public string? Metadata { get; private set; }  // JSON serialized
    
    // Navigation
    public virtual Conversation Conversation { get; private set; } = null!;
    
    public Message(Guid conversationId, string role, string content, Dictionary<string, object>? metadata = null)
    {
        if (!IsValidRole(role))
            throw new ArgumentException($"Invalid role: {role}");
            
        ConversationId = conversationId;
        Role = role;
        Content = content;
        
        if (metadata != null)
            Metadata = JsonSerializer.Serialize(metadata);
    }
    
    private static bool IsValidRole(string role) => 
        role is "user" or "assistant" or "system";
}
```

### Week 3-4: Repository Pattern Implementation

#### Task 1.5: Repository Interfaces
**Reference**: Mock setups in `PersonalityServiceTests.cs`, `ConversationServiceTests.cs`

```csharp
// Repository contracts extracted from service test mocking
public interface IPersonalityRepository
{
    // Basic CRUD
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

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(Guid id);
    Task<Conversation> CreateAsync(Conversation conversation);
    Task<Conversation> UpdateAsync(Conversation conversation);
    
    // Business-specific queries from test analysis
    Task<Conversation?> GetActiveConversationAsync(string platform, string userId);
    Task<IEnumerable<Conversation>> GetUserConversationsAsync(string platform, string userId);
    Task<bool> HasActiveConversationAsync(string platform, string userId);
}

public interface IMessageRepository
{
    Task<Message> AddAsync(Message message);
    Task<IEnumerable<Message>> GetByConversationIdAsync(Guid conversationId, int limit = 50);
    Task<IEnumerable<Message>> GetRecentMessagesAsync(Guid conversationId, int count = 10);
    Task<int> GetMessageCountAsync(Guid conversationId);
}
```

#### Task 1.6: Entity Framework DbContext
**Reference**: Context usage patterns in service tests

```csharp
public class DigitalMeDbContext : DbContext
{
    public DbSet<PersonalityProfile> PersonalityProfiles { get; set; } = null!;
    public DbSet<PersonalityTrait> PersonalityTraits { get; set; } = null!;
    public DbSet<Conversation> Conversations { get; set; } = null!;
    public DbSet<Message> Messages { get; set; } = null!;
    
    public DigitalMeDbContext(DbContextOptions<DigitalMeDbContext> options) : base(options) { }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // PersonalityProfile configuration
        modelBuilder.Entity<PersonalityProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000);
        });
        
        // PersonalityTrait configuration
        modelBuilder.Entity<PersonalityTrait>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Category).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Weight).HasPrecision(3, 1);
            
            entity.HasOne(e => e.PersonalityProfile)
                  .WithMany(e => e.Traits)
                  .HasForeignKey(e => e.PersonalityProfileId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Conversation configuration  
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Platform, e.UserId, e.IsActive });
            entity.Property(e => e.Platform).HasMaxLength(20).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(200);
        });
        
        // Message configuration
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ConversationId, e.Timestamp });
            entity.Property(e => e.Role).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Metadata).HasColumnType("jsonb"); // PostgreSQL JSON
            
            entity.HasOne(e => e.Conversation)
                  .WithMany(e => e.Messages)
                  .HasForeignKey(e => e.ConversationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
```

#### Task 1.7: Repository Implementations
**Reference**: Repository patterns from integration tests

```csharp
public class PersonalityRepository : IPersonalityRepository
{
    private readonly DigitalMeDbContext _context;
    
    public PersonalityRepository(DigitalMeDbContext context)
    {
        _context = context;
    }
    
    public async Task<PersonalityProfile?> GetProfileAsync(string name)
    {
        return await _context.PersonalityProfiles
            .Include(p => p.Traits)
            .FirstOrDefaultAsync(p => p.Name == name);
    }
    
    public async Task<PersonalityProfile> CreateProfileAsync(PersonalityProfile profile)
    {
        _context.PersonalityProfiles.Add(profile);
        await _context.SaveChangesAsync();
        return profile;
    }
    
    // Additional methods following same pattern...
}
```

### Week 5-6: Basic Domain Services

#### Task 1.8: PersonalityService Foundation
**Reference**: `PersonalityServiceTests.cs` complete contract

```csharp
public class PersonalityService : IPersonalityService
{
    private readonly IPersonalityRepository _repository;
    private readonly ILogger<PersonalityService> _logger;
    
    public PersonalityService(IPersonalityRepository repository, ILogger<PersonalityService> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    
    // Implement all methods from PersonalityServiceTests.cs
    // Focus on basic CRUD operations and validation
    // System prompt generation can be simplified for Phase 1
}
```

#### Task 1.9: ConversationService Foundation  
**Reference**: `ConversationServiceTests.cs` business rules

```csharp
public class ConversationService : IConversationService
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly ILogger<ConversationService> _logger;
    
    // Implement critical business logic:
    // - Single active conversation rule
    // - Proper message ordering
    // - Lifecycle management
}
```

## Testing Strategy for Phase 1

### Unit Test Implementation
```csharp
// Test structure matching existing test patterns
public class PersonalityServiceFoundationTests : TestBase
{
    // Use same mocking patterns as existing tests
    Mock<IPersonalityRepository> _mockRepository;
    Mock<ILogger<PersonalityService>> _mockLogger;
    PersonalityService _service;
    
    // Implement all test cases from PersonalityServiceTests.cs
    [Fact]
    public async Task GetPersonalityAsync_WithValidName_ReturnsPersonalityProfile()
    {
        // Copy test logic from existing PersonalityServiceTests.cs
    }
}
```

### Integration Test Setup
```csharp
// Integration tests using in-memory database
public class RepositoryIntegrationTests : IDisposable
{
    private readonly DigitalMeDbContext _context;
    
    public RepositoryIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<DigitalMeDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        _context = new DigitalMeDbContext(options);
    }
}
```

## Quality Gates

### Definition of Done for Phase 1
1. ✅ All core entities implemented with proper validation
2. ✅ Repository pattern fully implemented with PostgreSQL
3. ✅ Basic domain services operational
4. ✅ All existing unit tests pass with real implementations
5. ✅ Integration tests demonstrate end-to-end functionality
6. ✅ Entity Framework migrations created and tested
7. ✅ Code coverage > 90% for implemented components

### Performance Criteria  
- Entity queries execute < 100ms
- Repository operations handle concurrent access
- Memory usage stays within reasonable bounds
- Database migrations execute successfully

### Validation Checklist
- [ ] PersonalityProfile CRUD operations work
- [ ] Personality trait management operational  
- [ ] Conversation lifecycle properly managed
- [ ] Message ordering and metadata work correctly
- [ ] Repository interfaces fully implemented
- [ ] Entity Framework relationships configured
- [ ] All business rules from tests enforced
- [ ] Logging and error handling implemented

## Risk Mitigation

### Technical Risks
1. **Entity Framework Configuration** - Complex relationships may cause issues
   - **Mitigation**: Start with simple configurations, add complexity incrementally
   
2. **Business Rule Complexity** - Tests reveal complex conversation management rules
   - **Mitigation**: Implement one business rule at a time with comprehensive testing
   
3. **Database Performance** - Large conversation histories may cause performance issues
   - **Mitigation**: Implement proper indexing from the beginning

### Schedule Risks  
1. **Underestimated Complexity** - Domain entities may be more complex than apparent
   - **Mitigation**: Add 20% buffer time, prioritize critical path items
   
2. **Test Compatibility** - Existing tests may not match implementation exactly
   - **Mitigation**: Analyze tests thoroughly before implementation, document any deviations

## Deliverables

### Week 2 Milestone
- Core entities implemented and tested
- Entity Framework configuration complete
- Basic unit tests passing

### Week 4 Milestone  
- Repository pattern fully implemented
- Database integration working
- Integration tests demonstrate CRUD operations

### Week 6 Milestone (Phase 1 Complete)
- Basic domain services operational
- All foundation tests passing  
- Ready for Phase 2 AI integration

## Dependencies for Next Phase

**Phase 1 Outputs Required for Phase 2**:
1. Stable `PersonalityProfile` and `PersonalityTrait` entities
2. Working `IPersonalityRepository` for system prompt generation
3. Functional `ConversationService` for AI integration
4. Solid test foundation for mocking in AI services

**Success Metrics**:
- Phase 2 can start immediately after Phase 1 completion
- No rework of Phase 1 components required
- Test coverage sufficient to catch regressions
- Database schema stable and migration-ready

---

**Next Phase**: [Phase2-ServiceLayer.md](./Phase2-ServiceLayer.md)  
**Related**: [Service Architecture Roadmap](../SERVICE-ARCHITECTURE-ROADMAP.md)