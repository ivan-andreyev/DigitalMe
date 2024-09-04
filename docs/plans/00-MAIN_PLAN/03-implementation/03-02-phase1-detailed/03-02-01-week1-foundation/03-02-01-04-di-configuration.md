# Day 3-4: Repository Implementation & DI Configuration

**Родительский план**: [../03-02-01-week1-foundation.md](../03-02-01-week1-foundation.md)

## Day 3-4: Basic Repository Implementation (3 hours)

**Файл**: `src/DigitalMe.Data/Repositories/PersonalityRepository.cs:1-80`
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DigitalMe.Data.Context;
using DigitalMe.Data.Entities;

namespace DigitalMe.Data.Repositories;

public class PersonalityRepository : IPersonalityRepository
{
    private readonly DigitalMeContext _context;
    private readonly ILogger<PersonalityRepository> _logger;
    
    public PersonalityRepository(DigitalMeContext context, ILogger<PersonalityRepository> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task<PersonalityProfile?> GetByNameAsync(string name)
    {
        // TODO: Implement with EF Core Include for PersonalityTraits and recent Conversations  
        // TODO: Add caching layer for frequently accessed profiles
        // TODO: Add structured logging for debugging and monitoring
        // TODO: Add comprehensive error handling with specific exceptions
        // Expected: Return profile with calculated traits, null if not found
        // Expected: Include related PersonalityTraits and last 5 Conversations with 10 recent Messages each
        throw new NotImplementedException("Architecture stub - implement in development phase");
    }
    
    public async Task<PersonalityProfile> CreateAsync(PersonalityProfile profile)
    {
        // TODO: Implement profile creation with auto-generated GUID and timestamps
        // TODO: Add validation for required fields and duplicate name checking
        // TODO: Add transaction support for atomicity
        // TODO: Add structured logging for audit trail
        // Expected: Return created profile with generated Id and timestamps
        // Expected: Throw specific validation exceptions for invalid data
        throw new NotImplementedException("Architecture stub - implement in development phase");
    }
    
    public async Task UpdateTraitAsync(Guid profileId, string traitName, object value)
    {
        // TODO: Implement upsert pattern - update existing trait or create new one
        // TODO: Add JSON value validation and type checking
        // TODO: Add optimistic concurrency handling with UpdatedAt timestamps  
        // TODO: Add trait value history tracking for temporal analysis
        // Expected: Update existing trait or create new one if not exists
        // Expected: Validate value type and structure before persisting
        throw new NotImplementedException("Architecture stub - implement in development phase");
    }
}
```

**Команды тестирования repository**:
```bash
# Unit test for repository
dotnet new xunit -n DigitalMe.Tests.Unit -o tests/DigitalMe.Tests.Unit
# Add test for GetByNameAsync method
```

**Критерии успеха (измеримые)**:
- ✅ Repository методы выполняются без исключений
- ✅ Include() загружает связанные данные корректно
- ✅ Логирование работает на всех уровнях (Debug, Info, Error)
- ✅ Unit тесты проходят: `dotnet test --logger console`

## Day 4: Cross-Cutting Concerns Specifications (1 hour)

**Архитектурная спецификация логирования**:
```csharp
// TODO: Implement structured logging pattern across all services
// Pattern: Use Serilog with structured properties for searchability
// Format: [{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj} {Properties:j}{NewLine}{Exception}
// Required Properties: UserId, ConversationId, PersonalityProfileId for correlation
// Performance: Async logging to avoid blocking main thread
// Monitoring: Integration with Application Insights or ELK stack

public interface IStructuredLogger<T>
{
    void LogPersonalityEvent(string eventType, string profileName, object eventData);
    void LogConversationEvent(Guid conversationId, string eventType, object eventData);
    void LogIntegrationEvent(string platform, string eventType, object eventData);
}
```

**Архитектурная спецификация кеширования**:
```csharp
// TODO: Implement multi-layer caching strategy
// Layer 1: In-memory cache for hot personality profiles (15-minute TTL)
// Layer 2: Distributed cache (Redis) for conversation context (1-hour TTL)  
// Layer 3: Database with optimized indexes for cold data
// Invalidation: Event-driven cache invalidation on personality updates
// Monitoring: Cache hit/miss ratios and performance metrics

public interface ICacheManager
{
    Task<T?> GetAsync<T>(string key, CacheLayer layer = CacheLayer.Memory);
    Task SetAsync<T>(string key, T value, TimeSpan expiry, CacheLayer layer = CacheLayer.Memory);
    Task InvalidateAsync(string pattern); // For personality profile updates
}
```

**Архитектурная спецификация обработки ошибок**:
```csharp
// TODO: Implement tiered error handling strategy
// Tier 1: Business logic exceptions (PersonalityNotFoundException, InvalidTraitValueException)
// Tier 2: Infrastructure exceptions (DatabaseConnectionException, CacheException)
// Tier 3: Integration exceptions (TelegramApiException, McpConnectionException)
// Recovery: Exponential backoff for transient failures, circuit breaker for external services
// Observability: Error correlation with conversation context and user impact

public abstract class DomainException : Exception
{
    public abstract string ErrorCode { get; }
    public abstract ErrorSeverity Severity { get; }
    public Dictionary<string, object> Context { get; init; } = new();
}
```

## DI Container Setup & Configuration (1 hour)

**Файл**: `src/DigitalMe.API/Program.cs:1-50`
```csharp
using Microsoft.EntityFrameworkCore;
using Serilog;
using DigitalMe.Data.Context;
using DigitalMe.Data.Repositories;
using DigitalMe.Core.Interfaces;
using DigitalMe.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Serilog configuration - line 10-15
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Database configuration - line 16-25
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<DigitalMeContext>(options =>
{
    options.UseNpgsql(connectionString);
    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
    options.EnableDetailedErrors(builder.Environment.IsDevelopment());
});

// Repository registration - line 26-30
builder.Services.AddScoped<IPersonalityRepository, PersonalityRepository>();

// Core services registration - line 31-35
builder.Services.AddScoped<IPersonalityService, PersonalityService>();
builder.Services.AddMemoryCache();

// API services - line 36-40
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Health checks - line 41-45
builder.Services.AddHealthChecks()
    .AddDbContext<DigitalMeContext>("database")
    .AddCheck("self", () => HealthCheckResult.Healthy("API is running"));

var app = builder.Build();

// Configure pipeline - line 46-60
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseRouting();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
```

**Файл**: `src/DigitalMe.API/appsettings.Development.json:1-25`
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=digitalme_dev;Username=postgres;Password=dev_password;SSL Mode=Disable;"
  },
  "Serilog": {
    "MinimumLevel": "Debug",
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  }
}
```

**Команды проверки DI контейнера**:
```bash
dotnet run --project src/DigitalMe.API --urls="http://localhost:5000"
curl -X GET "http://localhost:5000/health" -H "accept: application/json"
# Expected: {"status":"Healthy","totalDuration":"00:00:00.0123456"}
```

## End of Week 1 Measurable Success Criteria

- ✅ **Compilation**: `dotnet build` (0 errors, 0 warnings)
- ✅ **Database**: Migrations applied, connection verified in health check
- ✅ **API Startup**: Service starts on port 5000, Swagger UI accessible
- ✅ **Health Monitoring**: `/health` endpoint returns comprehensive status
- ✅ **Architecture Documentation**: ADRs created for major decisions
- ✅ **Cross-Cutting Concerns**: Specifications defined for logging, caching, error handling
- ✅ **Repository Pattern**: Architecture stubs implemented with clear TODO specifications
- ✅ **Dependency Injection**: All services registered and resolvable
- ✅ **Structured Logging**: Serilog configured with JSON output and correlation IDs

## Navigation
- [Previous: Day 3: Entity Models Implementation](03-02-01-03-entity-models.md)
- [Day 1: Solution Setup](03-02-01-01-solution-setup.md)
- [Day 2: Database Context](03-02-01-02-database-context.md)
- [Overview: Week 1 Foundation](../03-02-01-week1-foundation.md)