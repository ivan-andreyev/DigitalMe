# Configuration and Options

**Родительский план**: [../02-05-interfaces.md](../02-05-interfaces.md)

## MCPOptions Configuration
**Файл**: `src/DigitalMe.Integrations/MCP/Options/MCPOptions.cs:1-20`

```csharp
namespace DigitalMe.Integrations.MCP.Options;

public class MCPOptions
{
    public const string SectionName = "MCP";
    
    [Required]
    public string Endpoint { get; set; } = default!;
    
    [Required]
    public string ApiKey { get; set; } = default!;
    
    [Range(1000, 300000)]
    public int TimeoutMs { get; set; } = 30000;
    
    [Range(1, 10)]
    public int MaxRetries { get; set; } = 3;
    
    public bool EnableCircuitBreaker { get; set; } = true;
    
    [Range(1, 100)]
    public int CircuitBreakerFailureThreshold { get; set; } = 5;
    
    [Range(1, 300)]
    public int CircuitBreakerTimeoutSeconds { get; set; } = 60;
}
```

## Database Configuration
**Файл**: `src/DigitalMe.Data/Options/DatabaseOptions.cs:1-15`

```csharp
namespace DigitalMe.Data.Options;

public class DatabaseOptions
{
    public const string SectionName = "Database";
    
    [Required]
    public string ConnectionString { get; set; } = default!;
    
    [Range(1, 1000)]
    public int CommandTimeoutSeconds { get; set; } = 30;
    
    [Range(1, 10)]
    public int MaxRetryCount { get; set; } = 3;
    
    public bool EnableSensitiveDataLogging { get; set; } = false;
    
    public bool EnableDetailedErrors { get; set; } = false;
}
```

## Caching Configuration
**Файл**: `src/DigitalMe.Core/Options/CacheOptions.cs:1-20`

```csharp
namespace DigitalMe.Core.Options;

public class CacheOptions
{
    public const string SectionName = "Cache";
    
    [Range(60, 86400)] // 1 minute to 24 hours
    public int PersonalityProfileTtlSeconds { get; set; } = 3600; // 1 hour
    
    [Range(60, 3600)] // 1 minute to 1 hour  
    public int ConversationContextTtlSeconds { get; set; } = 900; // 15 minutes
    
    [Range(1, 10000)]
    public int MaxCacheSize { get; set; } = 1000;
    
    public bool EnableDistributedCache { get; set; } = false;
    
    public string? RedisConnectionString { get; set; }
    
    public bool EnableCacheLogging { get; set; } = true;
}
```

## Logging Configuration
**Файл**: `src/DigitalMe.Core/Options/LoggingOptions.cs:1-15`

```csharp
namespace DigitalMe.Core.Options;

public class LoggingOptions
{
    public const string SectionName = "Logging";
    
    public string MinimumLevel { get; set; } = "Information";
    
    public bool EnableStructuredLogging { get; set; } = true;
    
    public bool EnableRequestLogging { get; set; } = true;
    
    public string OutputTemplate { get; set; } = 
        "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";
        
    public Dictionary<string, string> LogLevelOverrides { get; set; } = new()
    {
        ["Microsoft.AspNetCore"] = "Warning",
        ["Microsoft.EntityFrameworkCore.Database.Command"] = "Information"
    };
}
```

## Application Configuration
**Файл**: `src/DigitalMe.API/Options/AppOptions.cs:1-20`

```csharp
namespace DigitalMe.API.Options;

public class AppOptions
{
    public const string SectionName = "App";
    
    [Required]
    public string Name { get; set; } = "DigitalMe";
    
    [Required] 
    public string Version { get; set; } = "1.0.0";
    
    public string Environment { get; set; } = "Development";
    
    public bool EnableSwagger { get; set; } = true;
    
    public bool EnableHealthChecks { get; set; } = true;
    
    [Range(1, 65535)]
    public int Port { get; set; } = 5000;
    
    public string[]? AllowedOrigins { get; set; }
    
    public bool EnableRateLimiting { get; set; } = false;
    
    [Range(1, 10000)]
    public int RateLimitRequestsPerMinute { get; set; } = 100;
}
```

## Configuration Validation
**Файл**: `src/DigitalMe.Core/Extensions/ConfigurationExtensions.cs:1-30`

```csharp
namespace DigitalMe.Core.Extensions;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddValidatedOptions<T>(
        this IServiceCollection services, 
        IConfiguration configuration,
        string sectionName) where T : class
    {
        services.AddOptions<T>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
            
        return services;
    }
    
    public static void ValidateConfiguration(this WebApplication app)
    {
        // Validate critical configuration sections on startup
        var mcpOptions = app.Services.GetRequiredService<IOptions<MCPOptions>>();
        var dbOptions = app.Services.GetRequiredService<IOptions<DatabaseOptions>>();
        var cacheOptions = app.Services.GetRequiredService<IOptions<CacheOptions>>();
        
        // Configuration validation logic would go here
        // Throws exception if configuration is invalid
    }
}
```