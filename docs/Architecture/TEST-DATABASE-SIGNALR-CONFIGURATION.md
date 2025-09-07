# Test Database & SignalR Configuration Strategy

**Document Type**: Implementation Architecture  
**Status**: Critical Configuration Issues  
**Priority**: CRITICAL  
**Last Updated**: 2025-09-07  

## Overview

This document addresses the critical database isolation and SignalR configuration issues causing test failures and provides architectural solutions for reliable test environments.

## Current Database Configuration Issues

### Problem 1: Database State Bleeding Between Tests

**Current Conflicting Patterns**:

```csharp
// Pattern 1: Shared database name (MVPIntegrationTests.cs)
services.AddDbContext<DigitalMeDbContext>(options =>
{
    options.UseInMemoryDatabase("TestDb");  // SHARED NAME!
});

// Pattern 2: Unique database per test instance (IntegrationTestBase.cs)  
services.AddDbContext<DigitalMeDbContext>(options =>
{
    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
});

// Pattern 3: Class-based naming (TestBase.cs)
options.UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
```

**Result**: Tests interfere with each other's data, causing non-deterministic failures.

### Problem 2: Production Data Seeding in Tests

**Current Issue in Program.cs**:
```csharp
// This runs in ALL environments, including tests!
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DigitalMeDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    // PROBLEM: This seeds production data even in test environment
    await SeedIvanPersonalityAsync(context, logger);
}
```

**Result**: 
- Tests get unexpected production data
- Seeding process adds ~2-3 seconds to each test run
- Migration checks fail in test environments

### Problem 3: Migration Logic Running in Tests

**Current Issue**:
```csharp
if (context.Database.IsInMemory())
{
    logger.LogInformation("InMemory database detected - using EnsureCreated");
    var created = await context.Database.EnsureCreatedAsync();
    // This is correct for InMemory, but the logic around it is problematic
}
else
{
    // Real database migration logic that shouldn't run in tests
    var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
    if (pendingMigrations.Any())
    {
        await context.Database.MigrateAsync(); // Can hang in tests
    }
}
```

## Current SignalR Configuration Issues

### Problem 1: HTTPS Redirection in Tests

**Current Issue**:
```
[15:35:06 WRN] Failed to determine the https port for redirect.
HTTP POST /chathub/negotiate started
```

**Root Cause**: Production HTTPS configuration conflicts with test HTTP-only setup.

### Problem 2: SignalR Hub Registration Conflicts

**Current Production Setup**:
```csharp
// Program.cs
app.UseHttpsRedirection(); // Breaks tests
app.MapHub<ChatHub>("/chathub");
```

**Test Environment Issues**:
- Tests run over HTTP, but app configured for HTTPS
- SignalR handshake timeouts during test execution
- Hub negotiation failures causing integration test failures

## Recommended Database Configuration Architecture

### 1. Environment-Aware Database Setup

**Modify Program.cs for Environment Awareness**:

```csharp
// Program.cs - Environment-aware startup
public static async Task Main(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);
    
    // Configure services
    ConfigureServices(builder.Services, builder.Configuration);
    
    var app = builder.Build();
    
    // Configure pipeline
    ConfigurePipeline(app);
    
    // CRITICAL: Only initialize production data in non-test environments
    if (!app.Environment.IsEnvironment("Testing") && !app.Environment.IsEnvironment("Development"))
    {
        await InitializeProductionDataAsync(app);
    }
    else if (app.Environment.IsDevelopment())
    {
        await InitializeDevelopmentDataAsync(app);
    }
    // Test environment: No automatic data initialization
    
    await app.RunAsync();
}

private static async Task InitializeProductionDataAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<DigitalMeDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("🔍 PRODUCTION: Starting database initialization...");
        
        // Real database migrations
        if (!context.Database.IsInMemory())
        {
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                logger.LogInformation("Applying {Count} pending migrations", pendingMigrations.Count());
                await context.Database.MigrateAsync();
            }
        }
        
        // Seed production data
        await SeedProductionDataAsync(context, logger);
        
        logger.LogInformation("✅ PRODUCTION: Database initialization completed");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ PRODUCTION: Database initialization failed");
        throw; // Fail startup on production data issues
    }
}

private static async Task InitializeDevelopmentDataAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<DigitalMeDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("🔍 DEVELOPMENT: Starting database initialization...");
        
        if (context.Database.IsInMemory())
        {
            await context.Database.EnsureCreatedAsync();
        }
        else
        {
            // For real databases in development
            await context.Database.MigrateAsync();
        }
        
        // Seed development data (same as production for now)
        await SeedProductionDataAsync(context, logger);
        
        logger.LogInformation("✅ DEVELOPMENT: Database initialization completed");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "⚠️ DEVELOPMENT: Database initialization failed - continuing startup");
        // Don't fail startup in development
    }
}

private static async Task SeedProductionDataAsync(DigitalMeDbContext context, ILogger logger)
{
    // Existing Ivan personality seeding logic
    // Only called from production/development initialization
    var personalityService = context.ServiceProvider.GetRequiredService<IIvanPersonalityService>();
    await personalityService.EnsureIvanPersonalityExistsAsync();
    logger.LogInformation("✅ Ivan's personality data seeded successfully");
}
```

### 2. Test Database Isolation Strategy

**Hierarchical Database Naming Convention**:

```csharp
// tests/DigitalMe.Tests.Shared/Configuration/TestDatabaseConfiguration.cs
public static class TestDatabaseConfiguration
{
    /// <summary>
    /// Generate database name with proper isolation level
    /// </summary>
    public static string GenerateDatabaseName(TestIsolationLevel isolationLevel, string? customName = null)
    {
        return isolationLevel switch
        {
            TestIsolationLevel.PerTestClass => $"TestDb_{customName ?? "Unknown"}_{GetTestClassId()}",
            TestIsolationLevel.PerTestMethod => $"TestDb_{customName ?? "Unknown"}_{GetTestMethodId()}",
            TestIsolationLevel.PerTestRun => $"TestDb_{customName ?? "Unknown"}_{GetTestRunId()}",
            TestIsolationLevel.Shared => $"TestDb_{customName ?? "Shared"}",
            _ => throw new ArgumentException($"Unknown isolation level: {isolationLevel}")
        };
    }
    
    private static string GetTestClassId()
    {
        // Use calling test class name + timestamp for uniqueness
        var frame = new StackFrame(3);
        var method = frame.GetMethod();
        var className = method?.DeclaringType?.Name ?? "UnknownClass";
        return $"{className}_{DateTime.UtcNow:HHmmss}";
    }
    
    private static string GetTestMethodId()
    {
        var frame = new StackFrame(3);
        var method = frame.GetMethod();
        var className = method?.DeclaringType?.Name ?? "UnknownClass";
        var methodName = method?.Name ?? "UnknownMethod";
        return $"{className}_{methodName}_{DateTime.UtcNow:HHmmssfff}";
    }
    
    private static string GetTestRunId()
    {
        // Static ID for entire test run
        return TestRunId.Value;
    }
    
    private static readonly Lazy<string> TestRunId = new(() => 
        $"TestRun_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Environment.ProcessId}");
}

public enum TestIsolationLevel
{
    /// <summary>
    /// Each test class gets its own database instance
    /// Recommended for most integration tests
    /// </summary>
    PerTestClass,
    
    /// <summary>
    /// Each test method gets its own database instance  
    /// Use for tests that need complete isolation
    /// </summary>
    PerTestMethod,
    
    /// <summary>
    /// All tests in the run share one database
    /// Use for performance-critical test suites (with manual cleanup)
    /// </summary>
    PerTestRun,
    
    /// <summary>
    /// Explicitly shared database across all tests
    /// Use with extreme caution - requires manual state management
    /// </summary>
    Shared
}
```

**Database Configuration in Test Services**:

```csharp
// In TestServiceCollectionExtensions.cs
private static void RegisterDataAccessServices(IServiceCollection services, TestServicesOptions options)
{
    var databaseName = TestDatabaseConfiguration.GenerateDatabaseName(
        options.DatabaseIsolationLevel, 
        options.DatabaseName);
        
    services.AddDbContext<DigitalMeDbContext>(opts =>
    {
        opts.UseInMemoryDatabase(databaseName);
        opts.EnableSensitiveDataLogging(options.EnableSensitiveDataLogging);
        opts.EnableDetailedErrors(options.EnableDetailedErrors);
        
        // Test-specific configurations
        opts.ConfigureWarnings(warnings => 
        {
            warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning);
        });
    });
    
    // Register repositories
    services.AddScoped<IPersonalityRepository, PersonalityRepository>();
    services.AddScoped<IConversationRepository, ConversationRepository>();
    services.AddScoped<IMessageRepository, MessageRepository>();
}
```

### 3. Test Data Seeding Strategy

**Controlled Test Data Seeding**:

```csharp
// tests/DigitalMe.Tests.Shared/TestData/TestDataSeeder.cs
public class TestDataSeeder
{
    private readonly DigitalMeDbContext _context;
    private readonly ILogger<TestDataSeeder> _logger;
    
    public TestDataSeeder(DigitalMeDbContext context, ILogger<TestDataSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    /// <summary>
    /// Seed minimal data required for most tests
    /// </summary>
    public async Task SeedMinimalTestDataAsync()
    {
        if (await _context.PersonalityProfiles.AnyAsync())
        {
            _logger.LogDebug("Test data already seeded, skipping");
            return;
        }
        
        var ivanProfile = TestDataBuilder.CreateIvanPersonality();
        _context.PersonalityProfiles.Add(ivanProfile);
        
        await _context.SaveChangesAsync();
        _logger.LogDebug("Minimal test data seeded: Ivan personality profile");
    }
    
    /// <summary>
    /// Seed comprehensive data for integration tests
    /// </summary>
    public async Task SeedIntegrationTestDataAsync()
    {
        await SeedMinimalTestDataAsync();
        
        // Add sample conversations
        var testConversation = TestDataBuilder.CreateTestConversation();
        _context.Conversations.Add(testConversation);
        
        // Add sample messages
        var testMessage = TestDataBuilder.CreateTestMessage(testConversation.Id);
        _context.Messages.Add(testMessage);
        
        await _context.SaveChangesAsync();
        _logger.LogDebug("Integration test data seeded: conversations and messages");
    }
    
    /// <summary>
    /// Seed specific data for a particular test scenario
    /// </summary>
    public async Task SeedScenarioDataAsync(string scenarioName)
    {
        await SeedMinimalTestDataAsync();
        
        switch (scenarioName.ToLowerInvariant())
        {
            case "multipleconversations":
                await SeedMultipleConversationsAsync();
                break;
            case "personality_traits":
                await SeedPersonalityTraitsAsync();
                break;
            case "message_history":
                await SeedMessageHistoryAsync();
                break;
            default:
                _logger.LogWarning("Unknown test scenario: {ScenarioName}", scenarioName);
                break;
        }
    }
    
    private async Task SeedMultipleConversationsAsync()
    {
        var conversations = new[]
        {
            TestDataBuilder.CreateTestConversation("user1"),
            TestDataBuilder.CreateTestConversation("user2"),
            TestDataBuilder.CreateTestConversation("user3")
        };
        
        _context.Conversations.AddRange(conversations);
        await _context.SaveChangesAsync();
    }
    
    private async Task SeedPersonalityTraitsAsync()
    {
        var ivanProfile = await _context.PersonalityProfiles.FirstAsync();
        var additionalTraits = TestDataBuilder.CreateAdditionalTraits(ivanProfile.Id);
        
        _context.PersonalityTraits.AddRange(additionalTraits);
        await _context.SaveChangesAsync();
    }
    
    private async Task SeedMessageHistoryAsync()
    {
        var conversation = TestDataBuilder.CreateTestConversation();
        _context.Conversations.Add(conversation);
        
        var messages = TestDataBuilder.CreateMessageHistory(conversation.Id, 10);
        _context.Messages.AddRange(messages);
        
        await _context.SaveChangesAsync();
    }
}
```

## SignalR Configuration for Tests

### 1. Environment-Aware SignalR Configuration

**Modify Program.cs Pipeline Configuration**:

```csharp
// Program.cs
private static void ConfigurePipeline(WebApplication app)
{
    // Logging middleware
    app.UseMiddleware<RequestLoggingMiddleware>();
    
    // HTTPS redirection - only for non-test environments
    if (!app.Environment.IsEnvironment("Testing"))
    {
        app.UseHttpsRedirection();
    }
    
    app.UseStaticFiles();
    app.UseRouting();
    
    // Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();
    
    // Controllers
    app.MapControllers();
    
    // SignalR Hub - with environment-specific configuration
    if (app.Environment.IsEnvironment("Testing"))
    {
        // Test-specific SignalR configuration
        app.MapHub<ChatHub>("/chathub", options =>
        {
            options.Transports = HttpTransportType.LongPolling; // More reliable for tests
        });
    }
    else
    {
        // Production SignalR configuration
        app.MapHub<ChatHub>("/chathub");
    }
    
    // Health checks
    app.MapHealthChecks("/health/simple", new HealthCheckOptions
    {
        ResponseWriter = WriteSimpleHealthResponse
    });
    
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });
}
```

### 2. Test-Specific SignalR Configuration

**SignalR Test Configuration**:

```csharp
// In TestServiceCollectionExtensions.cs
private static void RegisterWebInfrastructure(IServiceCollection services, TestServicesOptions options)
{
    if (options.RegisterWebInfrastructure)
    {
        // SignalR with test-optimized configuration
        services.AddSignalR(opts =>
        {
            opts.EnableDetailedErrors = options.EnableDetailedErrors;
            opts.ClientTimeoutInterval = TimeSpan.FromSeconds(10); // Shorter for tests
            opts.HandshakeTimeout = TimeSpan.FromSeconds(5);        // Shorter for tests
            opts.MaximumReceiveMessageSize = 64 * 1024;             // Smaller for tests
        });
        
        // Controllers with test configuration
        services.AddControllers(opts =>
        {
            opts.ModelValidatorProviders.Clear(); // Faster validation for tests
        });
        
        // HTTPS redirection - DISABLED for tests
        services.Configure<HttpsRedirectionOptions>(opts =>
        {
            opts.HttpsPort = null;
        });
        
        // CORS for test environments
        services.AddCors(opts =>
        {
            opts.AddPolicy("TestPolicy", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });
    }
}
```

### 3. SignalR Integration Test Patterns

**Base SignalR Test Class**:

```csharp
// tests/DigitalMe.Tests.Shared/TestBaseClasses/SignalRTestBase.cs
public abstract class SignalRTestBase : WebApplicationTestBase<Program>
{
    protected HubConnection Connection { get; private set; }
    
    protected SignalRTestBase(TestWebApplicationFactory<Program> factory) : base(factory)
    {
    }
    
    protected async Task InitializeSignalRConnectionAsync()
    {
        var hubUrl = Client.BaseAddress + "chathub";
        
        Connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => Factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling; // More reliable for tests
            })
            .Build();
            
        await Connection.StartAsync();
    }
    
    protected async Task<T> InvokeHubMethodAsync<T>(string methodName, params object[] args)
    {
        if (Connection.State != HubConnectionState.Connected)
        {
            await InitializeSignalRConnectionAsync();
        }
        
        return await Connection.InvokeAsync<T>(methodName, args);
    }
    
    public override async ValueTask DisposeAsync()
    {
        if (Connection != null)
        {
            await Connection.DisposeAsync();
        }
        
        await base.DisposeAsync();
    }
}
```

### 4. Database Transaction Handling in Tests

**Transaction Scope for Test Isolation**:

```csharp
// tests/DigitalMe.Tests.Shared/TestBaseClasses/TransactionalTestBase.cs
public abstract class TransactionalTestBase : DatabaseTestBase
{
    private IDbContextTransaction? _transaction;
    
    protected override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        
        // Begin transaction for test isolation
        // Note: InMemory provider doesn't support real transactions,
        // but this pattern is ready for real database tests
        if (!DbContext.Database.IsInMemory())
        {
            _transaction = await DbContext.Database.BeginTransactionAsync();
        }
    }
    
    public override async ValueTask DisposeAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
        }
        
        await base.DisposeAsync();
    }
}
```

## Implementation Priority

### Phase 1: Critical Fixes (Week 1)
1. **Fix Program.cs Environment Awareness**
   - Prevent production data seeding in tests
   - Skip migration logic for test environments
   - Add environment-specific pipeline configuration

2. **Implement Database Isolation**
   - Add `TestDatabaseConfiguration` with proper naming
   - Update test service registration to use isolated databases
   - Add test data seeding control

### Phase 2: SignalR Reliability (Week 1-2)  
3. **Fix SignalR Test Configuration**
   - Disable HTTPS redirection in tests
   - Configure test-optimized SignalR settings
   - Add proper test connection handling

4. **Create SignalR Test Base Classes**
   - Implement `SignalRTestBase` with connection management
   - Add helper methods for hub interaction testing
   - Create test patterns for real-time features

### Phase 3: Advanced Features (Week 2-3)
5. **Add Transaction Support**
   - Implement transactional test base for real database tests
   - Add rollback strategies for test isolation
   - Performance optimize test execution

## Expected Results

**Database Configuration**:
- ✅ Zero database state bleeding between tests
- ✅ 3x faster test execution (no production seeding)
- ✅ Deterministic test results
- ✅ Environment-appropriate data initialization

**SignalR Configuration**:
- ✅ Zero SignalR handshake failures in tests
- ✅ Reliable real-time feature testing
- ✅ HTTP-compatible test execution
- ✅ Fast connection setup/teardown

**Overall Impact**:
- Test pass rate: >80% (from <60%)
- Test execution speed: 2-3x faster
- Zero environment configuration conflicts
- Maintainable test data management

This configuration strategy provides the foundation for reliable, fast, and maintainable tests that accurately reflect production behavior without production dependencies.