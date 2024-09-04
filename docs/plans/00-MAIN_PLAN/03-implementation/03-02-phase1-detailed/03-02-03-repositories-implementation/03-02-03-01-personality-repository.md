# Personality Repository Architecture 💾

> **Parent Plan**: [03-02-03-repositories-implementation.md](../03-02-03-repositories-implementation.md) | **Plan Type**: REPOSITORY ARCHITECTURE | **LLM Ready**: ✅ YES  
> **Prerequisites**: PersonalityProfile entity, DbContext | **Execution Time**: 2-3 days

📍 **Architecture** → **Implementation** → **Repositories** → **Personality**

## PersonalityRepository Architecture Overview

### Core Responsibilities
- **PersonalityProfile CRUD**: Complete data access for personality profiles
- **Name-Based Queries**: Efficient lookups by profile name
- **Trait Management**: JSON column operations for traits and communication styles
- **Performance Optimization**: Query optimization and caching strategies
- **Data Validation**: Entity-level validation before persistence

### Repository Interface Architecture

```csharp
public interface IPersonalityRepository : IRepository<PersonalityProfile>
{
    // Core CRUD operations (inherited from IRepository<T>)
    
    // Specialized queries
    Task<PersonalityProfile?> GetByNameAsync(string name);
    Task<List<PersonalityProfile>> GetByNamesAsync(List<string> names);
    Task<bool> ExistsByNameAsync(string name);
    
    // Trait-based queries
    Task<List<PersonalityProfile>> GetByTraitAsync(string traitName, object traitValue);
    Task<List<PersonalityProfile>> SearchByTraitsAsync(Dictionary<string, object> traitFilters);
    
    // Analytics and reporting
    Task<int> GetTotalCountAsync();
    Task<List<string>> GetAllProfileNamesAsync();
}
```

### Entity Framework Implementation Architecture

#### CRUD Operations Design
**Architecture Balance**: 85% EF patterns, 15% implementation stub

```csharp
public class PersonalityRepository : Repository<PersonalityProfile>, IPersonalityRepository
{
    public PersonalityRepository(DigitalMeDbContext context, ILogger<PersonalityRepository> logger)
        : base(context, logger)
    {
    }

    public async Task<PersonalityProfile?> GetByNameAsync(string name)
    {
        _logger.LogInformation("Retrieving personality profile by name: {ProfileName}", name);
        
        // TODO: Implement name-based query with case-insensitive comparison
        // TODO: Include related entities if needed
        // TODO: Add query caching for frequently accessed profiles
        
        throw new NotImplementedException("Profile name lookup implementation pending");
    }

    public async Task<List<PersonalityProfile>> GetByNamesAsync(List<string> names)
    {
        _logger.LogInformation("Retrieving {Count} personality profiles by names", names.Count);
        
        // TODO: Implement bulk name lookup with IN query
        // TODO: Optimize for large name lists
        // TODO: Return profiles in same order as input names
        
        throw new NotImplementedException("Bulk profile lookup implementation pending");
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        // TODO: Implement efficient existence check using Any()
        // TODO: Use case-insensitive comparison
        // TODO: Optimize for performance (no need to load full entity)
        
        throw new NotImplementedException("Profile existence check implementation pending");
    }
}
```

#### Trait-Based Queries Design
**Architecture Balance**: 85% JSON query patterns, 15% implementation stub

```csharp
public async Task<List<PersonalityProfile>> GetByTraitAsync(string traitName, object traitValue)
{
    _logger.LogInformation("Querying profiles by trait: {TraitName} = {TraitValue}", traitName, traitValue);
    
    // TODO: Implement JSON column query for CoreTraits
    // TODO: Support different trait value types (string, double, bool)
    // TODO: Add query optimization for common traits
    
    // Example EF JSON query pattern:
    // return await _context.PersonalityProfiles
    //     .Where(p => p.CoreTraits.ContainsKey(traitName) && 
    //                 p.CoreTraits[traitName].ToString() == traitValue.ToString())
    //     .ToListAsync();
    
    throw new NotImplementedException("Trait-based query implementation pending");
}

public async Task<List<PersonalityProfile>> SearchByTraitsAsync(Dictionary<string, object> traitFilters)
{
    _logger.LogInformation("Searching profiles by {FilterCount} trait filters", traitFilters.Count);
    
    // TODO: Build dynamic query for multiple trait filters
    // TODO: Support AND/OR logic for trait combinations
    // TODO: Optimize query performance for complex filters
    
    throw new NotImplementedException("Multi-trait search implementation pending");
}
```

### Database Configuration Architecture

#### Entity Configuration Design
**Architecture Balance**: 85% EF configuration patterns, 15% implementation stub

```csharp
public class PersonalityProfileConfiguration : IEntityTypeConfiguration<PersonalityProfile>
{
    public void Configure(EntityTypeBuilder<PersonalityProfile> builder)
    {
        // TODO: Configure table name and primary key
        builder.ToTable("PersonalityProfiles");
        builder.HasKey(p => p.Id);
        
        // TODO: Configure name column with unique constraint
        builder.Property(p => p.Name)
               .IsRequired()
               .HasMaxLength(100);
        builder.HasIndex(p => p.Name)
               .IsUnique();
        
        // TODO: Configure JSON columns for traits
        builder.Property(p => p.CoreTraits)
               .HasConversion(
                   v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                   v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new()
               );
        
        builder.Property(p => p.CommunicationStyle)
               .HasConversion(
                   v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                   v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new()
               );
        
        // TODO: Configure indexes for performance
        builder.HasIndex(p => p.CreatedAt);
        builder.HasIndex(p => p.UpdatedAt);
    }
}
```

### Performance Optimization Architecture

#### Query Optimization Patterns
**Architecture Balance**: 85% optimization strategies, 15% implementation stub

```csharp
// Query optimization patterns for PersonalityRepository:

// 1. Name-based lookups with caching
private readonly IMemoryCache _cache;
private const string PROFILE_CACHE_PREFIX = "profile:";
private const int CACHE_TTL_MINUTES = 15;

public async Task<PersonalityProfile?> GetByNameAsync(string name)
{
    var cacheKey = $"{PROFILE_CACHE_PREFIX}{name.ToLowerInvariant()}";
    
    if (_cache.TryGetValue(cacheKey, out PersonalityProfile? cachedProfile))
    {
        return cachedProfile;
    }
    
    // TODO: Implement database query
    // TODO: Cache successful results
    // TODO: Handle cache invalidation on updates
    
    throw new NotImplementedException("Cached profile lookup implementation pending");
}

// 2. Bulk operations optimization
public async Task<List<PersonalityProfile>> GetByNamesAsync(List<string> names)
{
    // TODO: Check cache for each name first
    // TODO: Query database only for cache misses
    // TODO: Use IN query for efficient bulk lookup
    // TODO: Cache individual results
    
    throw new NotImplementedException("Optimized bulk lookup implementation pending");
}

// 3. JSON query optimization
public async Task<List<PersonalityProfile>> GetByTraitAsync(string traitName, object traitValue)
{
    // TODO: Use EF Core JSON functions for efficient querying
    // TODO: Consider computed columns for frequently queried traits
    // TODO: Add database indexes on JSON paths if supported
    
    throw new NotImplementedException("Optimized JSON query implementation pending");
}
```

### Error Handling Architecture

```csharp
// Repository-specific exception handling patterns:
public async Task<PersonalityProfile> CreateAsync(PersonalityProfile entity)
{
    try
    {
        // TODO: Validate entity before save
        // TODO: Check for duplicate names
        // TODO: Call base repository Create method
        // TODO: Invalidate relevant caches
        
        throw new NotImplementedException("Profile creation with validation implementation pending");
    }
    catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE constraint") == true)
    {
        throw new InvalidOperationException($"Profile with name '{entity.Name}' already exists", ex);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating personality profile {ProfileName}", entity.Name);
        throw;
    }
}
```

### Success Criteria

✅ **CRUD Operations**: Complete PersonalityProfile data access
✅ **Name-Based Queries**: Efficient profile lookups by name
✅ **JSON Queries**: Trait-based search capabilities  
✅ **Performance**: Query optimization and caching strategies
✅ **Entity Configuration**: Proper EF Core mapping and constraints
✅ **Error Handling**: Database-specific exception management

### Implementation Guidance

1. **Start with Base Repository**: Inherit from generic Repository<T> base class
2. **Add Specialized Methods**: Implement name-based and trait-based queries
3. **Configure Entity**: Set up EF Core entity configuration with JSON columns
4. **Add Caching**: Implement memory caching for frequently accessed profiles
5. **Optimize Queries**: Add database indexes and query optimization
6. **Test Performance**: Verify query performance with sample data

---

## 🔗 NAVIGATION & DEPENDENCIES

### Prerequisites
- **PersonalityProfile Entity**: Domain entity with proper properties
- **DigitalMeDbContext**: EF Core database context
- **Base Repository**: Generic repository base class (optional)
- **Memory Cache**: IMemoryCache for performance optimization

### Related Plans
- **Parent**: [03-02-03-repositories-implementation.md](../03-02-03-repositories-implementation.md)
- **Services**: PersonalityService depends on this repository
- **Entity**: PersonalityProfile entity definition and relationships

---

## 📊 PLAN METADATA

- **Type**: REPOSITORY ARCHITECTURE PLAN
- **LLM Ready**: ✅ YES
- **Implementation Depth**: 85% architecture / 15% implementation stubs
- **Execution Time**: 2-3 days
- **Balance Compliance**: ✅ ARCHITECTURAL FOCUS maintained