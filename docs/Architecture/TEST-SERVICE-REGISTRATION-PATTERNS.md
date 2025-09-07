# Test Service Registration Patterns - Implementation Guide

**Document Type**: Implementation Architecture  
**Status**: Implementation Required  
**Priority**: CRITICAL  
**Last Updated**: 2025-09-07  

## Overview

This document defines the standardized service registration patterns for test environments to eliminate the current dependency injection conflicts causing widespread test failures.

## Current Service Registration Issues

### Problem: Multiple Conflicting Registration Strategies

**Production Registration** (`Program.cs`):
```csharp
builder.Services.AddDigitalMeServices(builder.Configuration);

// This registers:
// - Real database connections
// - External API clients (Anthropic, Slack, ClickUp, GitHub)
// - MCP client requiring running server
// - All tool strategies with external dependencies
// - SignalR with HTTPS requirements
```

**Current Test Attempts** (Failing):
```csharp
// Unit Test Factory
services.Remove(dbContextDescriptor);              // Remove production DB
services.AddDbContext<DigitalMeDbContext>(...);    // Add test DB
services.AddDigitalMeServices(context.Configuration); // RE-ADD PRODUCTION SERVICES!

// Integration Test Factory  
var toolStrategies = services.Where(s => s.ServiceType == typeof(IToolStrategy));
foreach (var strategy in toolStrategies) { services.Remove(strategy); } // Remove tools
services.AddScoped<IToolStrategy, MemoryToolStrategy>(); // Add test tool
// But other production services still registered!
```

**Result**: Tests fail because production services override test mocks and require external dependencies.

## Standardized Test Service Registration Architecture

### 1. Test-Specific Service Extension

**File**: `tests/DigitalMe.Tests.Shared/Extensions/ServiceCollectionExtensions.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using DigitalMe.Data;
using DigitalMe.Services;
using DigitalMe.Repositories;
using DigitalMe.Integrations.MCP;
using DigitalMe.Services.Tools;

namespace DigitalMe.Tests.Shared.Extensions;

public static class TestServiceCollectionExtensions
{
    /// <summary>
    /// Register all services needed for tests with test-appropriate implementations.
    /// This replaces AddDigitalMeServices for test environments.
    /// </summary>
    public static IServiceCollection AddDigitalMeTestServices(
        this IServiceCollection services,
        IConfiguration configuration,
        TestServicesOptions? options = null)
    {
        options ??= new TestServicesOptions();
        
        // === CORE INFRASTRUCTURE ===
        RegisterCoreInfrastructure(services, configuration, options);
        
        // === DATA ACCESS LAYER ===
        RegisterDataAccessServices(services, options);
        
        // === BUSINESS SERVICES ===
        RegisterBusinessServices(services, options);
        
        // === MVP SERVICES (WORKING) ===
        RegisterMVPServices(services, options);
        
        // === EXTERNAL INTEGRATIONS ===
        RegisterExternalServices(services, configuration, options);
        
        // === TOOL STRATEGIES ===
        RegisterToolStrategies(services, options);
        
        // === WEB INFRASTRUCTURE ===
        RegisterWebInfrastructure(services, options);
        
        return services;
    }
    
    private static void RegisterCoreInfrastructure(IServiceCollection services, IConfiguration configuration, TestServicesOptions options)
    {
        // Logging - reduced verbosity for tests
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(options.LogLevel);
            if (options.EnableConsoleLogging)
            {
                builder.AddConsole();
            }
        });
        
        // Memory cache
        services.AddMemoryCache();
        
        // Configuration
        services.AddSingleton(configuration);
    }
    
    private static void RegisterDataAccessServices(IServiceCollection services, TestServicesOptions options)
    {
        // Database Context - Always in-memory for tests
        services.AddDbContext<DigitalMeDbContext>(opts =>
        {
            opts.UseInMemoryDatabase(options.DatabaseName);
            opts.EnableSensitiveDataLogging(options.EnableSensitiveDataLogging);
            opts.EnableDetailedErrors(options.EnableDetailedErrors);
        });
        
        // Repositories
        services.AddScoped<IPersonalityRepository, PersonalityRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        
        if (options.RegisterAdditionalRepositories)
        {
            // Add other repositories as they're implemented
            // services.AddScoped<IUserRepository, UserRepository>();
        }
    }
    
    private static void RegisterBusinessServices(IServiceCollection services, TestServicesOptions options)
    {
        // Core business services
        services.AddScoped<IPersonalityService, PersonalityService>();
        services.AddScoped<IConversationService, ConversationService>();
        
        // Agent behavior engine
        if (options.RegisterAgentBehaviorEngine)
        {
            services.AddScoped<IAgentBehaviorEngine, AgentBehaviorEngine>();
        }
    }
    
    private static void RegisterMVPServices(IServiceCollection services, TestServicesOptions options)
    {
        // MVP services are working and should always be registered
        services.AddScoped<IIvanPersonalityService, MVPPersonalityService>();
        services.AddScoped<IMessageProcessor, MVPMessageProcessor>();
    }
    
    private static void RegisterExternalServices(IServiceCollection services, IConfiguration configuration, TestServicesOptions options)
    {
        // Anthropic Service
        if (options.UseMockExternalServices)
        {
            services.AddSingleton<IAnthropicService, MockAnthropicService>();
        }
        else
        {
            services.Configure<AnthropicConfiguration>(configuration.GetSection("Anthropic"));
            services.AddHttpClient<IAnthropicService, AnthropicServiceSimple>();
        }
        
        // MCP Services
        if (options.UseMockExternalServices || !options.MCPServerAvailable)
        {
            services.AddSingleton<IMCPClient, MockMCPClient>();
            services.AddSingleton<IMcpService, MockMcpService>();
        }
        else
        {
            services.Configure<MCPConfiguration>(configuration.GetSection("MCP"));
            services.AddSingleton<IMCPClient, MCPClient>();
            services.AddScoped<IMcpService, McpService>();
        }
        
        // Telegram Service
        if (options.UseMockExternalServices)
        {
            services.AddSingleton<ITelegramService, MockTelegramService>();
        }
        else if (options.RegisterTelegramService)
        {
            services.Configure<TelegramConfiguration>(configuration.GetSection("Telegram"));
            services.AddSingleton<ITelegramService, TelegramService>();
        }
        
        // Other external services (Slack, ClickUp, GitHub)
        if (options.RegisterIntegrationServices && !options.UseMockExternalServices)
        {
            RegisterIntegrationServices(services, configuration);
        }
    }
    
    private static void RegisterIntegrationServices(IServiceCollection services, IConfiguration configuration)
    {
        // Only register if actually needed for integration tests
        // Most unit tests should use mocks
        
        // Slack Integration
        services.Configure<SlackConfiguration>(configuration.GetSection("Slack"));
        services.AddHttpClient<ISlackService, SlackService>();
        
        // ClickUp Integration  
        services.Configure<ClickUpConfiguration>(configuration.GetSection("ClickUp"));
        services.AddHttpClient<IClickUpService, ClickUpService>();
        
        // GitHub Integration
        services.Configure<GitHubConfiguration>(configuration.GetSection("GitHub"));
        services.AddHttpClient<IGitHubService, GitHubService>();
    }
    
    private static void RegisterToolStrategies(IServiceCollection services, TestServicesOptions options)
    {
        // Tool Registry
        services.AddSingleton<IToolRegistry, ToolRegistry>();
        services.AddSingleton<ToolExecutor>();
        
        // Always register Memory tool (no external dependencies)
        services.AddScoped<IToolStrategy, MemoryToolStrategy>();
        
        if (options.IncludePersonalityToolStrategy)
        {
            services.AddScoped<IToolStrategy, PersonalityToolStrategy>();
        }
        
        if (options.IncludeIntegrationToolStrategies && !options.UseMockExternalServices)
        {
            // Only for integration tests that actually test external integrations
            services.AddScoped<IToolStrategy, SlackToolStrategy>();
            services.AddScoped<IToolStrategy, ClickUpToolStrategy>();
            services.AddScoped<IToolStrategy, GitHubToolStrategy>();
        }
    }
    
    private static void RegisterWebInfrastructure(IServiceCollection services, TestServicesOptions options)
    {
        if (options.RegisterWebInfrastructure)
        {
            // SignalR with test-appropriate configuration
            services.AddSignalR(opts =>
            {
                opts.EnableDetailedErrors = options.EnableDetailedErrors;
                opts.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
                opts.HandshakeTimeout = TimeSpan.FromSeconds(10);
            });
            
            // Controllers
            services.AddControllers();
            
            // HTTPS Redirection - disabled for tests
            services.Configure<HttpsRedirectionOptions>(opts =>
            {
                opts.HttpsPort = null; // Disable HTTPS redirect in tests
            });
        }
    }
}

/// <summary>
/// Configuration options for test service registration
/// </summary>
public class TestServicesOptions
{
    // Database Configuration
    public string DatabaseName { get; set; } = $"TestDb_{Guid.NewGuid()}";
    public bool EnableSensitiveDataLogging { get; set; } = true;
    public bool EnableDetailedErrors { get; set; } = true;
    
    // Logging Configuration
    public LogLevel LogLevel { get; set; } = LogLevel.Warning;
    public bool EnableConsoleLogging { get; set; } = false;
    
    // Service Registration Flags
    public bool UseMockExternalServices { get; set; } = true;
    public bool RegisterWebInfrastructure { get; set; } = false;
    public bool RegisterAgentBehaviorEngine { get; set; } = false;
    public bool RegisterAdditionalRepositories { get; set; } = false;
    public bool RegisterTelegramService { get; set; } = false;
    public bool RegisterIntegrationServices { get; set; } = false;
    
    // Tool Strategy Configuration
    public bool IncludePersonalityToolStrategy { get; set; } = false;
    public bool IncludeIntegrationToolStrategies { get; set; } = false;
    
    // External Service Flags
    public bool MCPServerAvailable { get; set; } = false;
    
    /// <summary>
    /// Preset for unit tests - minimal services, all mocks
    /// </summary>
    public static TestServicesOptions ForUnitTests(string? databaseName = null)
    {
        return new TestServicesOptions
        {
            DatabaseName = databaseName ?? $"UnitTest_{Guid.NewGuid()}",
            UseMockExternalServices = true,
            RegisterWebInfrastructure = false,
            RegisterAgentBehaviorEngine = false,
            RegisterIntegrationServices = false,
            LogLevel = LogLevel.Error, // Minimal logging for unit tests
            EnableConsoleLogging = false
        };
    }
    
    /// <summary>
    /// Preset for integration tests - more services, selective mocking
    /// </summary>
    public static TestServicesOptions ForIntegrationTests(string? databaseName = null)
    {
        return new TestServicesOptions
        {
            DatabaseName = databaseName ?? $"IntegrationTest_{Guid.NewGuid()}",
            UseMockExternalServices = true, // Still use mocks by default
            RegisterWebInfrastructure = true,
            RegisterAgentBehaviorEngine = true,
            MCPServerAvailable = false, // Set to true if MCP server running
            IncludePersonalityToolStrategy = true,
            LogLevel = LogLevel.Warning,
            EnableConsoleLogging = true,
            EnableDetailedErrors = true
        };
    }
    
    /// <summary>
    /// Preset for end-to-end tests - real services where possible
    /// </summary>
    public static TestServicesOptions ForE2ETests(string? databaseName = null)
    {
        return new TestServicesOptions
        {
            DatabaseName = databaseName ?? $"E2ETest_{Guid.NewGuid()}",
            UseMockExternalServices = false, // Use real services
            RegisterWebInfrastructure = true,
            RegisterAgentBehaviorEngine = true,
            RegisterIntegrationServices = true,
            RegisterTelegramService = true,
            MCPServerAvailable = true, // Requires running MCP server
            IncludePersonalityToolStrategy = true,
            IncludeIntegrationToolStrategies = true,
            LogLevel = LogLevel.Information,
            EnableConsoleLogging = true
        };
    }
}
```

### 2. Mock Service Implementations

**Base Mock Interface**:
```csharp
// tests/DigitalMe.Tests.Shared/Mocks/IMockService.cs
public interface IMockService
{
    bool IsSimulated { get; }
    void ResetState();
}
```

**Anthropic Service Mock**:
```csharp
// tests/DigitalMe.Tests.Shared/Mocks/MockAnthropicService.cs
public class MockAnthropicService : IAnthropicService, IMockService
{
    private readonly ILogger<MockAnthropicService> _logger;
    private readonly List<(string Message, PersonalityProfile Personality, string Response)> _callHistory = new();
    
    public bool IsSimulated => true;
    public IReadOnlyList<(string Message, PersonalityProfile Personality, string Response)> CallHistory => _callHistory.AsReadOnly();
    
    public MockAnthropicService(ILogger<MockAnthropicService> logger)
    {
        _logger = logger;
    }
    
    public async Task<string> SendMessageAsync(string message, PersonalityProfile personality)
    {
        await Task.Delay(10); // Simulate network delay
        
        var response = GeneratePersonalityAwareResponse(message, personality);
        _callHistory.Add((message, personality, response));
        
        _logger.LogDebug("Mock Anthropic: {Message} -> {Response}", message, response);
        return response;
    }
    
    private string GeneratePersonalityAwareResponse(string message, PersonalityProfile personality)
    {
        // Generate contextually appropriate responses for tests
        if (personality.Name.Contains("Ivan"))
        {
            return message.ToLower() switch
            {
                var msg when msg.Contains("привет") || msg.Contains("hello") => 
                    "Привет! Я Иван. Система работает корректно.",
                var msg when msg.Contains("как дела") => 
                    "Всё отлично, работаю над интересными техническими задачами.",
                var msg when msg.Contains("решение") || msg.Contains("problem") => 
                    "Структурированный подход: сначала анализируем факторы, потом принимаем решение.",
                _ => $"Получил сообщение '{message}'. Система MCP протокол работает корректно."
            };
        }
        
        return $"Hello! I'm {personality.Name}. Received: '{message}'. System working correctly.";
    }
    
    public void ResetState()
    {
        _callHistory.Clear();
    }
}
```

### 3. Usage Patterns in Test Classes

**Unit Test Pattern**:
```csharp
public class PersonalityServiceTests : TestBase
{
    protected override void ConfigureTestServices(IServiceCollection services)
    {
        var configuration = CreateTestConfiguration();
        services.AddDigitalMeTestServices(configuration, TestServicesOptions.ForUnitTests());
    }
    
    private IConfiguration CreateTestConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Anthropic:ApiKey"] = "test-key",
                ["Anthropic:Model"] = "claude-3-5-sonnet-20241022"
            })
            .Build();
    }
}
```

**Integration Test Pattern**:
```csharp
public class MVPIntegrationTests : WebApplicationTestBase<Program>
{
    public MVPIntegrationTests(TestWebApplicationFactory<Program> factory) : base(factory)
    {
    }
    
    // Factory configured with TestServicesOptions.ForIntegrationTests()
}
```

**Web Application Factory Configuration**:
```csharp
public class TestWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    private readonly TestServicesOptions _options;
    
    public TestWebApplicationFactory(TestServicesOptions? options = null)
    {
        _options = options ?? TestServicesOptions.ForIntegrationTests();
    }
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        builder.ConfigureServices((context, services) =>
        {
            // CRITICAL: Clear all existing services first
            services.Clear();
            
            // Register test services using our standardized extension
            services.AddDigitalMeTestServices(context.Configuration, _options);
        });
        
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(GetTestConfiguration());
        });
    }
    
    private Dictionary<string, string> GetTestConfiguration()
    {
        return new Dictionary<string, string>
        {
            ["Anthropic:ApiKey"] = "test-key-sk-ant-test",
            ["Anthropic:Model"] = "claude-3-5-sonnet-20241022",
            ["MCP:ServerUrl"] = "http://localhost:3000/mcp",
            ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:"
        };
    }
}
```

## Migration Strategy

### Phase 1: Create Test Infrastructure (Week 1)
1. **Create Shared Test Project**:
   ```bash
   dotnet new classlib -n DigitalMe.Tests.Shared
   dotnet add tests/DigitalMe.Tests.Unit/DigitalMe.Tests.Unit.csproj reference tests/DigitalMe.Tests.Shared/DigitalMe.Tests.Shared.csproj
   dotnet add tests/DigitalMe.Tests.Integration/DigitalMe.Tests.Integration.csproj reference tests/DigitalMe.Tests.Shared/DigitalMe.Tests.Shared.csproj
   ```

2. **Implement Service Extensions**:
   - Create `AddDigitalMeTestServices` extension
   - Implement `TestServicesOptions` with presets
   - Create base mock implementations

### Phase 2: Migrate Test Classes (Week 2)
3. **Update Unit Tests**:
   - Migrate to new `TestBase` classes
   - Replace manual service setup with `AddDigitalMeTestServices`
   - Update mock configurations from Strict to Loose

4. **Update Integration Tests**:
   - Replace multiple factory implementations with unified `TestWebApplicationFactory`
   - Configure appropriate `TestServicesOptions` per test suite
   - Fix SignalR and database conflicts

### Phase 3: Validation (Week 3)
5. **Test Coverage Validation**:
   - Achieve >80% test pass rate
   - Verify test isolation (no state bleeding)
   - Performance testing for test execution speed

## Expected Results

**Before Migration**:
- Test Pass Rate: <60%
- 3+ different service registration patterns
- Database state bleeding between tests
- External dependency failures in unit tests

**After Migration**:
- Test Pass Rate: >80%
- Single standardized service registration pattern
- Perfect test isolation
- No external dependencies in unit tests
- 50% faster test execution due to reduced setup overhead

This standardized approach ensures consistent, reliable test behavior while maintaining the flexibility to test different scenarios through configuration options.