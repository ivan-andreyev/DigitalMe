# Personality Service Architecture 🧠

> **Parent Plan**: [03-02-02-services-implementation.md](../03-02-02-services-implementation.md) | **Plan Type**: SERVICE ARCHITECTURE | **LLM Ready**: ✅ YES  
> **Prerequisites**: IPersonalityRepository interface | **Execution Time**: 2-3 days

📍 **Architecture** → **Implementation** → **Services** → **Personality**

## PersonalityService Architecture Overview

### Core Responsibilities
- **Profile Management**: CRUD operations for personality profiles
- **Trait Processing**: Advanced trait manipulation and validation
- **System Prompt Generation**: Dynamic prompt creation from personality data
- **Caching Strategy**: Performance optimization with intelligent caching
- **Mood Analysis**: Sentiment analysis from user messages

### Class Structure Design

```csharp
namespace DigitalMe.Core.Services;

public class PersonalityService : IPersonalityService
{
    private readonly IPersonalityRepository _personalityRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PersonalityService> _logger;
    
    // Cache configuration constants
    private const int ProfileCacheTtlMinutes = 15;
    private const string ProfileCacheKeyPrefix = "personality_profile_";
    
    // Constructor with DI
    // Profile CRUD methods
    // Trait manipulation methods
    // System prompt generation
    // Caching utility methods
}
```

### Service Interface Architecture

```csharp
public interface IPersonalityService
{
    // Profile Management
    Task<PersonalityProfile?> GetProfileAsync(string name);
    Task<PersonalityProfile> CreateProfileAsync(PersonalityProfile profile);
    Task<PersonalityProfile> UpdateProfileAsync(PersonalityProfile profile);
    Task DeleteProfileAsync(Guid profileId);
    
    // Trait Management
    Task UpdateTraitAsync(Guid profileId, string traitName, object value);
    Task<Dictionary<string, object>> GetTraitsAsync(Guid profileId);
    Task<bool> ValidateTraitValueAsync(string traitName, object value);
    
    // System Prompt Generation
    Task<string> GenerateSystemPromptAsync(string profileName, List<ConversationMessage> history);
    
    // Mood Analysis
    Task<string> AnalyzeMoodFromMessageAsync(string message);
}
```

### Caching Architecture

#### Cache Strategy Design
**Architecture Balance**: 85% caching patterns, 15% implementation stub

```csharp
public async Task<PersonalityProfile?> GetProfileAsync(string name)
{
    _logger.LogInformation("Retrieving personality profile {ProfileName}", name);
    
    // TODO: Implement cache-first strategy
    var cacheKey = $"{ProfileCacheKeyPrefix}{name.ToLowerInvariant()}";
    
    if (_cache.TryGetValue(cacheKey, out PersonalityProfile? cachedProfile))
    {
        _logger.LogDebug("Profile {ProfileName} retrieved from cache", name);
        return cachedProfile;
    }
    
    // TODO: Repository call with error handling
    // TODO: Cache the result with TTL
    // TODO: Return profile or null
    
    throw new NotImplementedException("Profile retrieval with caching implementation pending");
}

private void CacheProfile(PersonalityProfile profile)
{
    var cacheKey = $"{ProfileCacheKeyPrefix}{profile.Name.ToLowerInvariant()}";
    var cacheOptions = new MemoryCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(ProfileCacheTtlMinutes)
    };
    
    // TODO: Implement cache entry with expiration
    // TODO: Add cache size limits
    // TODO: Implement cache invalidation strategy
    
    throw new NotImplementedException("Profile caching implementation pending");
}
```

### Profile CRUD Architecture

#### Create Profile Design
**Architecture Balance**: 85% business logic patterns, 15% implementation stub

```csharp
public async Task<PersonalityProfile> CreateProfileAsync(PersonalityProfile profile)
{
    _logger.LogInformation("Creating personality profile {ProfileName}", profile.Name);
    
    // TODO: Validate profile data
    if (string.IsNullOrWhiteSpace(profile.Name))
    {
        throw new ArgumentException("Profile name cannot be empty", nameof(profile));
    }
    
    // TODO: Check for duplicate names
    var existingProfile = await GetProfileAsync(profile.Name);
    if (existingProfile != null)
    {
        throw new ArgumentException($"Profile '{profile.Name}' already exists", nameof(profile));
    }
    
    // TODO: Set default values and timestamps
    profile.Id = profile.Id == Guid.Empty ? Guid.NewGuid() : profile.Id;
    profile.CreatedAt = DateTime.UtcNow;
    profile.UpdatedAt = DateTime.UtcNow;
    
    // TODO: Repository create call
    // TODO: Cache the new profile
    // TODO: Log success and return created profile
    
    throw new NotImplementedException("Profile creation implementation pending");
}
```

#### Update Profile Design
**Architecture Balance**: 85% update patterns, 15% implementation stub

```csharp
public async Task<PersonalityProfile> UpdateProfileAsync(PersonalityProfile profile)
{
    _logger.LogInformation("Updating personality profile {ProfileId}", profile.Id);
    
    // TODO: Validate profile exists
    var existingProfile = await _personalityRepository.GetByIdAsync(profile.Id);
    if (existingProfile == null)
    {
        throw new PersonalityNotFoundException($"Profile with ID {profile.Id} not found");
    }
    
    // TODO: Update timestamp
    profile.UpdatedAt = DateTime.UtcNow;
    
    // TODO: Repository update call
    // TODO: Invalidate cache for this profile
    // TODO: Cache the updated profile
    
    throw new NotImplementedException("Profile update implementation pending");
}
```

### Trait Management Architecture

#### Trait Update Design
**Architecture Balance**: 85% trait validation patterns, 15% implementation stub

```csharp
public async Task UpdateTraitAsync(Guid profileId, string traitName, object value)
{
    _logger.LogInformation("Updating trait {TraitName} for profile {ProfileId}", traitName, profileId);
    
    // TODO: Validate trait name and value
    if (string.IsNullOrWhiteSpace(traitName))
    {
        throw new ArgumentException("Trait name cannot be empty", nameof(traitName));
    }
    
    if (!await ValidateTraitValueAsync(traitName, value))
    {
        throw new InvalidTraitValueException(traitName, value, "Invalid trait value format");
    }
    
    // TODO: Get profile and update trait
    var profile = await _personalityRepository.GetByIdAsync(profileId);
    if (profile == null)
    {
        throw new PersonalityNotFoundException($"Profile with ID {profileId} not found");
    }
    
    // TODO: Update trait in CoreTraits dictionary
    // TODO: Save to repository
    // TODO: Invalidate cache
    
    throw new NotImplementedException("Trait update implementation pending");
}

public async Task<bool> ValidateTraitValueAsync(string traitName, object value)
{
    // TODO: Implement trait-specific validation rules
    // TODO: Check value type compatibility
    // TODO: Validate value ranges and constraints
    // TODO: Business rule validation
    
    throw new NotImplementedException("Trait validation implementation pending");
}
```

### System Prompt Generation Architecture

#### Dynamic Prompt Creation Design
**Architecture Balance**: 85% prompt engineering patterns, 15% implementation stub

```csharp
public async Task<string> GenerateSystemPromptAsync(string profileName, List<ConversationMessage> history)
{
    _logger.LogInformation("Generating system prompt for profile {ProfileName} with {MessageCount} history messages",
        profileName, history.Count);
    
    // TODO: Check cache for recent prompt
    var cacheKey = $"{SystemPromptCacheKeyPrefix}{profileName}_{history.Count}";
    if (_cache.TryGetValue(cacheKey, out string? cachedPrompt))
    {
        return cachedPrompt;
    }
    
    // TODO: Get personality profile
    var profile = await GetProfileAsync(profileName);
    if (profile == null)
    {
        throw new PersonalityNotFoundException($"Profile '{profileName}' not found");
    }
    
    // TODO: Build prompt components
    var promptBuilder = new StringBuilder();
    
    // TODO: Add personality traits section
    // TODO: Add communication style section
    // TODO: Add conversation context from history
    // TODO: Add behavioral guidelines
    
    // TODO: Cache generated prompt with shorter TTL
    // TODO: Return complete system prompt
    
    throw new NotImplementedException("System prompt generation implementation pending");
}
```

### Mood Analysis Architecture

#### Sentiment Analysis Design
**Architecture Balance**: 85% analysis patterns, 15% implementation stub

```csharp
public async Task<string> AnalyzeMoodFromMessageAsync(string message)
{
    _logger.LogInformation("Analyzing mood from message of length {Length}", message.Length);
    
    // TODO: Input validation
    if (string.IsNullOrWhiteSpace(message))
    {
        return "neutral";
    }
    
    // TODO: Implement sentiment analysis algorithm
    // - Keyword-based analysis
    // - Pattern recognition
    // - Contextual mood detection
    // - Integration with external sentiment API (optional)
    
    // TODO: Return mood classification
    // Possible moods: "positive", "negative", "neutral", "excited", "frustrated", "confused"
    
    throw new NotImplementedException("Mood analysis implementation pending");
}
```

### Error Handling Architecture

```csharp
// Custom exception handling patterns:
public class PersonalityNotFoundException : Exception
{
    public PersonalityNotFoundException(string message) : base(message) { }
}

public class InvalidTraitValueException : Exception
{
    public string TraitName { get; }
    public object? AttemptedValue { get; }
    
    public InvalidTraitValueException(string traitName, object? value, string reason)
        : base($"Invalid value for trait '{traitName}': {reason}")
    {
        TraitName = traitName;
        AttemptedValue = value;
    }
}
```

### Dependency Injection Configuration

```csharp
// Required services in DI container:
services.AddScoped<IPersonalityService, PersonalityService>();
services.AddScoped<IPersonalityRepository, PersonalityRepository>();
services.AddMemoryCache();
services.AddLogging();

// Optional external services:
// services.AddHttpClient<ISentimentAnalysisService>();
```

### Success Criteria

✅ **Caching Strategy**: Profile and prompt caching with TTL
✅ **CRUD Operations**: Complete profile lifecycle management
✅ **Trait Management**: Dynamic trait updates with validation
✅ **System Prompt**: Dynamic prompt generation from personality data
✅ **Mood Analysis**: Sentiment analysis from user messages
✅ **Error Handling**: Custom exceptions for domain-specific errors
✅ **Performance**: Caching reduces database calls

### Implementation Guidance

1. **Start with Interface**: Implement IPersonalityService interface
2. **Add Caching**: Implement caching strategy for profiles
3. **CRUD Operations**: Build profile management methods
4. **Trait System**: Add trait validation and update logic
5. **System Prompts**: Implement dynamic prompt generation
6. **Mood Analysis**: Add sentiment analysis capabilities

---

## 🔗 NAVIGATION & DEPENDENCIES

### Prerequisites
- **IPersonalityRepository**: Repository interface for data access
- **IMemoryCache**: Caching infrastructure
- **PersonalityProfile Entity**: Domain entity model
- **Custom Exception Classes**: PersonalityNotFoundException, InvalidTraitValueException

### Next Steps
- **Implement**: Fill in NotImplementedException stubs
- **Repository Integration**: Connect to actual repository implementation
- **Caching Configuration**: Set up memory cache with proper limits
- **Testing**: Create comprehensive unit tests

### Related Plans
- **Parent**: [03-02-02-services-implementation.md](../03-02-02-services-implementation.md)
- **Repository**: Repository implementation for data persistence
- **Controllers**: Controllers depend on this service

---

## 📊 PLAN METADATA

- **Type**: SERVICE ARCHITECTURE PLAN
- **LLM Ready**: ✅ YES
- **Implementation Depth**: 85% architecture / 15% implementation stubs
- **Execution Time**: 2-3 days
- **Code Coverage**: ~400 lines architectural guidance
- **Balance Compliance**: ✅ ARCHITECTURAL FOCUS maintained

### 🎯 SERVICE ARCHITECTURE INDICATORS
- **✅ Business Logic Design**: Clear service responsibility patterns
- **✅ Caching Strategy**: Performance optimization architecture
- **✅ Error Handling**: Domain-specific exception design
- **✅ Implementation Stubs**: NotImplementedException placeholders
- **✅ Interface Design**: Complete service contract definition
- **✅ Dependency Management**: Clear DI and repository dependencies
- **✅ Performance Patterns**: Caching and optimization strategies