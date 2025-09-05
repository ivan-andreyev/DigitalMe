# Week 2: Core Personality Service Implementation

**Родительский план**: [../03-02-phase1-detailed.md](../03-02-phase1-detailed.md)

## Day 5: PersonalityService Core Methods (4 hours)

**Файл**: `src/DigitalMe.Core/Services/PersonalityService.cs:55-120` (continuation)
```csharp
public async Task<string> GenerateSystemPromptAsync(string profileName, IEnumerable<Message> conversationHistory)
{
    // TODO: Implement dynamic system prompt generation based on personality profile
    // TODO: Add template engine for prompt customization and A/B testing
    // TODO: Add conversation context analysis for mood and topic detection
    // TODO: Add caching for frequently used prompts with TTL
    // TODO: Add prompt versioning and effectiveness tracking
    // Expected: Return contextual system prompt with personality traits, philosophy, and recent context
    // Expected: Include conversation history analysis for better continuity
    // Architecture Decision: Use template-based approach for maintainability
    throw new NotImplementedException("Architecture stub - implement with personality template engine");
}

public async Task<PersonalityMood> AnalyzeMoodFromMessageAsync(string message)
{
    // TODO: Implement NLP-based mood analysis using ML.NET or external service
    // TODO: Add pattern matching for Russian technical language and slang
    // TODO: Add sentiment analysis with confidence scores
    // TODO: Add learning mechanism to improve accuracy over time
    // TODO: Add context consideration (time of day, recent events)
    // Expected: Return PersonalityMood enum with confidence score
    // Architecture Decision: Consider Azure Cognitive Services or local ML model
    throw new NotImplementedException("Architecture stub - implement with NLP mood analysis");
}

// TODO: Implement helper method for safe trait value extraction
// TODO: Add type validation and conversion error handling
// TODO: Add trait value normalization (0.0-1.0 range)
// Expected: Return trait value as double with fallback to default
private double GetTraitValue(PersonalityProfile profile, string traitName, double defaultValue)
{
    throw new NotImplementedException("Architecture stub - implement safe trait value extraction");
}
```

**Файл**: `src/DigitalMe.Core/Models/PersonalityMood.cs:1-10`
```csharp
namespace DigitalMe.Core.Models;

public enum PersonalityMood
{
    Calm = 0,
    Focused = 1,
    Tired = 2,
    Irritated = 3,
    Excited = 4
}
```

**Unit тесты для PersonalityService**:
**Файл**: `tests/DigitalMe.Tests.Unit/PersonalityServiceTests.cs:1-30`
```csharp
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using DigitalMe.Core.Services;
using DigitalMe.Core.Models;
using DigitalMe.Data.Repositories;
using DigitalMe.Data.Entities;

namespace DigitalMe.Tests.Unit;

public class PersonalityServiceTests
{
    private readonly PersonalityService _service;
    private readonly Mock<IPersonalityRepository> _mockRepository;
    private readonly IMemoryCache _cache;
    
    public PersonalityServiceTests()
    {
        // TODO: Setup test dependencies and mocking framework
        // TODO: Add test data builder pattern for complex PersonalityProfile objects
        // TODO: Add integration test setup with in-memory database
        throw new NotImplementedException("Architecture stub - implement comprehensive test suite");
    }
    
    // TODO: Add test cases for mood analysis with different message patterns
    // TODO: Add test cases for system prompt generation with various personality configurations  
    // TODO: Add test cases for caching behavior and cache invalidation
    // TODO: Add test cases for error handling and edge cases
    // TODO: Add performance tests for system prompt generation with large conversation history
    // Architecture Decision: Use AAA pattern (Arrange-Act-Assert) for all tests
}
```

**Команды запуска тестов**:
```bash
# TODO: Implement test execution commands after test implementation
# dotnet test tests/DigitalMe.Tests.Unit --logger console --verbosity normal
# Expected: All unit tests pass with >90% code coverage
# Architecture Decision: Use xUnit with Moq for mocking, FluentAssertions for assertions
```

## Day 6: Ivan Profile Data Initialization (2 hours)

**Файл**: `src/DigitalMe.Data/Seeders/PersonalityProfileSeeder.cs:1-30`
```csharp
using DigitalMe.Data.Context;
using DigitalMe.Data.Entities;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Data.Seeders;

public class PersonalityProfileSeeder
{
    private readonly DigitalMeContext _context;
    private readonly ILogger<PersonalityProfileSeeder> _logger;
    
    public PersonalityProfileSeeder(DigitalMeContext context, ILogger<PersonalityProfileSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task SeedIvanProfileAsync()
    {
        // TODO: Implement profile seeding based on IVAN_PROFILE_DATA.md
        // TODO: Add check for existing profiles to avoid duplicates
        // TODO: Add validation for required profile fields
        // TODO: Add JSON structure validation for traits and communication style
        // TODO: Add transaction support for atomicity
        // Expected: Create Ivan profile with all traits from analysis document
        // Expected: Include CoreTraits, CommunicationStyle, WorkStyle, PersonalPhilosophy
        // Architecture Decision: Load seed data from configuration file for maintainability
        throw new NotImplementedException("Architecture stub - implement profile seeding from config");
    }
}
```

**Команда для сидирования данных**:
```bash
# Create seeding endpoint for development
curl -X POST "http://localhost:5000/api/admin/seed/personality" -H "accept: application/json"
# Expected: HTTP 200, "Ivan profile seeded successfully"
```

## End of Week 2 Success Criteria

- ✅ **Service Architecture**: PersonalityService stubs implemented with comprehensive TODOs
- ✅ **Design Patterns**: Template pattern specified for system prompt generation
- ✅ **Data Modeling**: Ivan's profile structure defined in seed specification
- ✅ **Testing Framework**: Unit test architecture established with AAA pattern
- ✅ **NLP Integration**: Mood analysis architecture specified with ML.NET consideration
- ✅ **Cache Strategy**: Multi-layer caching architecture documented
- ✅ **Error Taxonomy**: Domain exception hierarchy defined
- ✅ **Performance Specs**: Response time targets and optimization strategies outlined

**Время архитектурной разработки**: Week 1-2 общее время 12 часов (архитектурная фаза)
**Время реализации**: 8+ часов для фактической реализации заглушек