using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text;
using System.Runtime;
using Serilog;
using DigitalMe.Data;
using DigitalMe.Services;
using DigitalMe.Repositories;
using DigitalMe.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Secure Configuration Loading - User Secrets for development, environment variables for production
if (builder.Environment.IsDevelopment())
{
    // User Secrets for development environment
    builder.Configuration.AddUserSecrets<Program>();
}
else if (builder.Environment.IsProduction())
{
    // In production, rely on environment variables
    // Note: AddEnvironmentVariables() is already included by default in WebApplicationBuilder
    builder.Configuration.AddEnvironmentVariables();
    
    // For Azure/cloud deployments, add Key Vault configuration here
    // Example: builder.Configuration.AddAzureKeyVault(...);
}

// Configure runtime optimizations for production
if (builder.Environment.IsProduction())
{
    // Configure runtime optimizations
    var runtimeConfig = builder.Configuration.GetSection("RuntimeOptimizations");
    
    if (runtimeConfig.GetValue<bool>("GCServer"))
    {
        Environment.SetEnvironmentVariable("DOTNET_gcServer", "1");
    }
    
    var minWorkerThreads = runtimeConfig.GetValue<int>("ThreadPoolMinWorkerThreads");
    var minCompletionPortThreads = runtimeConfig.GetValue<int>("ThreadPoolMinCompletionPortThreads");
    
    if (minWorkerThreads > 0 && minCompletionPortThreads > 0)
    {
        ThreadPool.SetMinThreads(minWorkerThreads, minCompletionPortThreads);
    }
}

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// SignalR for real-time chat
builder.Services.AddSignalR();

// Database Context - Force SQLite for development/testing
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// TEMPORARY FIX: Force SQLite to fix Telegram bot integration issues
// Comment out CloudSQL logic to prevent mismatched database connections
// if (!string.IsNullOrEmpty(connectionString) && connectionString.Contains("/cloudsql/"))
// {
//     // Cloud SQL PostgreSQL with optimizations
//     builder.Services.AddDbContext<DigitalMeDbContext>(options => 
//     {
//         options.UseNpgsql(connectionString, npgsqlOptions =>
//         {
//             npgsqlOptions.CommandTimeout(30);
//             npgsqlOptions.EnableRetryOnFailure(
//                 maxRetryCount: 3,
//                 maxRetryDelay: TimeSpan.FromSeconds(5),
//                 errorCodesToAdd: null);
//         });
//         
//         // Production optimizations
//         if (builder.Environment.IsProduction())
//         {
//             options.EnableSensitiveDataLogging(false);
//             options.EnableDetailedErrors(false);
//             options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
//         }
//     });
// }
// else
// {
    // Force SQLite for all environments until CloudSQL is properly configured
    builder.Services.AddDbContext<DigitalMeDbContext>(options => 
        options.UseSqlite("Data Source=digitalme.db"));
// }

// Identity - Using standard IdentityUser since it's already configured in the database schema
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<DigitalMeDbContext>()
    .AddDefaultTokenProviders();

// Configure Identity to not redirect on authentication failures (API behavior)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = 401;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = 403;
        return Task.CompletedTask;
    };
});

// JWT Authentication with Secure Key Management
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Securely get JWT key with environment variable fallback
        var jwtKey = builder.Configuration["JWT:Key"];
        var envJwtKey = Environment.GetEnvironmentVariable("JWT_KEY");
        
        // Use environment variable if available, otherwise use config
        var secureJwtKey = !string.IsNullOrWhiteSpace(envJwtKey) ? envJwtKey : jwtKey;
        
        // Validate key strength
        if (string.IsNullOrWhiteSpace(secureJwtKey) || secureJwtKey.Length < 32)
        {
            if (builder.Environment.IsProduction())
            {
                throw new InvalidOperationException("JWT key must be at least 32 characters in production. Set JWT_KEY environment variable.");
            }
            
            // Generate secure key for development
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var keyBytes = new byte[64];
            rng.GetBytes(keyBytes);
            secureJwtKey = Convert.ToBase64String(keyBytes);
            
            var logger = builder.Services.BuildServiceProvider().GetService<ILogger<Program>>();
            logger?.LogWarning("Generated temporary JWT key for development. For production, set JWT_KEY environment variable.");
        }
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JWT:Issuer"],
            ValidAudience = builder.Configuration["JWT:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secureJwtKey))
        };
        
        // Configure for API - return JSON responses instead of redirects
        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync("{\"error\":\"Unauthorized\",\"message\":\"Token missing or invalid\"}");
            }
        };
    });

// Secrets Management Service - Must be registered before other services that need secrets
builder.Services.AddSingleton<DigitalMe.Services.Configuration.ISecretsManagementService, 
                               DigitalMe.Services.Configuration.SecretsManagementService>();

// DigitalMe Services - Standardized Registration
builder.Services.AddDigitalMeServices(builder.Configuration);

// Database Migration Service
builder.Services.AddScoped<DigitalMe.Services.Database.IDatabaseMigrationService, 
                           DigitalMe.Services.Database.DatabaseMigrationService>();

// Configure Security settings
builder.Services.Configure<DigitalMe.Services.Security.SecuritySettings>(
    builder.Configuration.GetSection("Security"));

// Configure JWT settings
builder.Services.Configure<DigitalMe.Configuration.JwtSettings>(
    builder.Configuration.GetSection("JWT"));

// Health Checks - Standard ASP.NET Core as required by MVP Phase 6 plan
builder.Services.AddHealthChecks()
    .AddDbContextCheck<DigitalMeDbContext>()
    .AddCheck("claude-api", () => 
    {
        var apiKey = builder.Configuration["Anthropic:ApiKey"];
        return !string.IsNullOrEmpty(apiKey) 
            ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy() 
            : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Claude API key not configured");
    });

// MCP Integration with Anthropic
builder.Services.Configure<DigitalMe.Integrations.MCP.AnthropicConfiguration>(
    builder.Configuration.GetSection("Anthropic"));
builder.Services.AddHttpClient<DigitalMe.Integrations.MCP.AnthropicServiceSimple>();
builder.Services.AddScoped<DigitalMe.Integrations.MCP.IAnthropicService, DigitalMe.Integrations.MCP.AnthropicServiceSimple>();
builder.Services.AddScoped<DigitalMe.Integrations.MCP.Tools.ToolExecutor>();
builder.Services.AddSingleton<DigitalMe.Integrations.MCP.Tools.ToolRegistry>();

// MCP Client and Proper Service
builder.Services.AddHttpClient<DigitalMe.Integrations.MCP.MCPClient>();
builder.Services.AddScoped<DigitalMe.Integrations.MCP.IMCPClient, DigitalMe.Integrations.MCP.MCPClient>(provider =>
{
    var httpClient = provider.GetRequiredService<HttpClient>();
    var logger = provider.GetRequiredService<ILogger<DigitalMe.Integrations.MCP.MCPClient>>();
    var mcpServerUrl = builder.Configuration.GetValue<string>("MCP:ServerUrl") ?? "http://localhost:3000";
    return new DigitalMe.Integrations.MCP.MCPClient(httpClient, logger, mcpServerUrl);
});
builder.Services.AddScoped<IMcpService, DigitalMe.Integrations.MCP.MCPServiceProper>();

// Agent Behavior Engine
builder.Services.AddScoped<DigitalMe.Services.AgentBehavior.IAgentBehaviorEngine, DigitalMe.Services.AgentBehavior.AgentBehaviorEngine>();

// Tool Strategy Pattern
builder.Services.AddSingleton<DigitalMe.Services.Tools.IToolRegistry, DigitalMe.Services.Tools.ToolRegistry>();
builder.Services.AddScoped<DigitalMe.Services.Tools.ToolExecutor>();
builder.Services.AddScoped<DigitalMe.Services.Tools.IToolStrategy, DigitalMe.Services.Tools.Strategies.TelegramToolStrategy>();
builder.Services.AddScoped<DigitalMe.Services.Tools.IToolStrategy, DigitalMe.Services.Tools.Strategies.CalendarToolStrategy>();
builder.Services.AddScoped<DigitalMe.Services.Tools.IToolStrategy, DigitalMe.Services.Tools.Strategies.GitHubToolStrategy>();
builder.Services.AddScoped<DigitalMe.Services.Tools.IToolStrategy, DigitalMe.Services.Tools.Strategies.PersonalityToolStrategy>();
builder.Services.AddScoped<DigitalMe.Services.Tools.IToolStrategy, DigitalMe.Services.Tools.Strategies.MemoryToolStrategy>();

// External Integrations
builder.Services.Configure<DigitalMe.Integrations.External.GitHub.GitHubConfiguration>(
    builder.Configuration.GetSection("GitHub"));
builder.Services.AddScoped<DigitalMe.Integrations.External.GitHub.IGitHubService, DigitalMe.Integrations.External.GitHub.GitHubService>();

builder.Services.AddHttpClient<DigitalMe.Integrations.External.Telegram.TelegramService>();
builder.Services.AddScoped<DigitalMe.Integrations.External.Telegram.ITelegramService, DigitalMe.Integrations.External.Telegram.TelegramService>();

// Telegram Services - Refactored for SOLID compliance
builder.Services.AddScoped<DigitalMe.Services.Telegram.ITelegramMessageDispatcher, DigitalMe.Services.Telegram.TelegramMessageDispatcher>();
builder.Services.AddScoped<DigitalMe.Services.Telegram.ITelegramCommandHandler, DigitalMe.Services.Telegram.TelegramCommandHandler>();

// Telegram Commands - Command pattern implementations
builder.Services.AddScoped<DigitalMe.Services.Telegram.Commands.ITelegramCommand, DigitalMe.Services.Telegram.Commands.StartCommand>();
builder.Services.AddScoped<DigitalMe.Services.Telegram.Commands.ITelegramCommand, DigitalMe.Services.Telegram.Commands.HelpCommand>();
builder.Services.AddScoped<DigitalMe.Services.Telegram.Commands.ITelegramCommand, DigitalMe.Services.Telegram.Commands.StatusCommand>();
builder.Services.AddScoped<DigitalMe.Services.Telegram.Commands.ITelegramCommand, DigitalMe.Services.Telegram.Commands.SettingsCommand>();

// User Mapping Service - for Telegram user identity mapping
builder.Services.AddScoped<DigitalMe.Services.UserMapping.IUserMappingService, DigitalMe.Services.UserMapping.UserMappingService>();

// Telegram Configuration Service - abstraction over configuration access
builder.Services.AddScoped<DigitalMe.Services.Configuration.ITelegramConfigurationService, DigitalMe.Services.Configuration.TelegramConfigurationService>();

// Webhook Security Service - for webhook security validation
builder.Services.AddScoped<DigitalMe.Services.Security.IWebhookSecurityService, DigitalMe.Services.Security.WebhookSecurityService>();

// Telegram Webhook Services - refactored for SOLID compliance
builder.Services.AddScoped<DigitalMe.Services.Telegram.ITelegramWebhookService, DigitalMe.Services.Telegram.TelegramWebhookService>();

// Telegram User Preferences Service - for managing user settings
builder.Services.AddScoped<DigitalMe.Services.Telegram.ITelegramUserPreferencesService, DigitalMe.Services.Telegram.TelegramUserPreferencesService>();
builder.Services.AddScoped<DigitalMe.Services.Telegram.IWebhookManagementService, DigitalMe.Services.Telegram.TelegramWebhookService>();

builder.Services.AddHttpClient<DigitalMe.Integrations.External.Google.CalendarService>();
builder.Services.AddScoped<DigitalMe.Integrations.External.Google.ICalendarService, DigitalMe.Integrations.External.Google.CalendarService>();

// Performance & Monitoring Services - All scoped to avoid lifecycle issues
builder.Services.AddScoped<DigitalMe.Services.Monitoring.MetricsAggregator>();
builder.Services.AddScoped<DigitalMe.Services.Monitoring.SystemMetricsCalculator>();
builder.Services.AddScoped<DigitalMe.Services.Monitoring.IPerformanceMetricsService, DigitalMe.Services.Monitoring.PerformanceMetricsService>();
builder.Services.AddScoped<DigitalMe.Services.Monitoring.IHealthCheckService, DigitalMe.Services.Monitoring.HealthCheckService>();

// Add metrics collection as required by MVP Phase 6 plan
builder.Services.AddSingleton<DigitalMe.Services.Monitoring.IMetricsLogger, DigitalMe.Services.Monitoring.MetricsLogger>();

// API Security - Rate Limiting Configuration
builder.Services.AddRateLimiter(options =>
{
    // Fixed window rate limiter for general API endpoints (100 requests per minute per IP)
    options.AddPolicy("api", httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            }));
    
    // Strict rate limiter for authentication endpoints (10 requests per minute per IP)
    options.AddPolicy("auth", httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                QueueLimit = 2
            }));
    
    // Sliding window rate limiter for chat endpoints (50 requests per minute per IP)
    options.AddPolicy("chat", httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new System.Threading.RateLimiting.SlidingWindowRateLimiterOptions
            {
                PermitLimit = 50,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6, // 10-second segments
                QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                QueueLimit = 5
            }));
    
    // Strict rate limiter for webhook endpoints (30 requests per minute per IP)
    options.AddPolicy("webhook", httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                QueueLimit = 5
            }));
    
    // Global rejection response
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", cancellationToken);
    };
});

// Production Security Configuration - HSTS, HTTPS redirection, and security headers
if (builder.Environment.IsProduction())
{
    builder.Services.AddHsts(options =>
    {
        options.Preload = true;
        options.IncludeSubDomains = true;
        options.MaxAge = TimeSpan.FromDays(365);
    });

    builder.Services.AddHttpsRedirection(options =>
    {
        options.HttpsPort = 443;
    });
}

var app = builder.Build();

// Initialize Tool Registry with all available strategies
try
{
    using var toolScope = app.Services.CreateScope();
    var toolRegistry = toolScope.ServiceProvider.GetRequiredService<DigitalMe.Services.Tools.IToolRegistry>();
    var toolStrategies = toolScope.ServiceProvider.GetServices<DigitalMe.Services.Tools.IToolStrategy>();
    
    foreach (var strategy in toolStrategies)
    {
        toolRegistry.RegisterTool(strategy);
    }
    
    var appLogger = app.Services.GetService<ILogger<Program>>();
    appLogger?.LogInformation("🔧 TOOL REGISTRY INITIALIZED with {Count} strategies", toolStrategies.Count());
}
catch (Exception ex)
{
    var appLogger = app.Services.GetService<ILogger<Program>>();
    appLogger?.LogError(ex, "❌ Failed to initialize Tool Registry");
}

// DEBUG: Add detailed logging for migration process
var migrationLogger = app.Services.GetService<ILogger<Program>>();
migrationLogger?.LogInformation("🚀 APPLICATION BUILD COMPLETED - Starting migration check");

// Auto-apply migrations on startup for Cloud SQL
try
{
    migrationLogger?.LogInformation("🔍 STEP 1: Creating service scope for migrations");
    using (var scope = app.Services.CreateScope())
    {
        migrationLogger?.LogInformation("🔍 STEP 2: Service scope created successfully");
        
        migrationLogger?.LogInformation("🔍 STEP 3: Getting DigitalMeDbContext from services");
        var context = scope.ServiceProvider.GetRequiredService<DigitalMeDbContext>();
        migrationLogger?.LogInformation("🔍 STEP 4: DbContext retrieved successfully");
        
        migrationLogger?.LogInformation("🔍 STEP 5: Getting logger from services");
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        migrationLogger?.LogInformation("🔍 STEP 6: Logger retrieved successfully");
        
        try
        {
            // Use DatabaseMigrationService for better separation of concerns
            var migrationService = scope.ServiceProvider.GetRequiredService<DigitalMe.Services.Database.IDatabaseMigrationService>();
            migrationService.ApplyMigrations(context);
                
            // Seed Ivan's personality data for MVP (skip in Test environment for test isolation)
            if (app.Environment.EnvironmentName != "Testing")
            {
                logger.LogInformation("🌱 STEP 16: Seeding Ivan's personality data...");
                DigitalMe.Data.Seeders.IvanDataSeeder.SeedBasicIvanProfile(context);
                logger.LogInformation("✅ STEP 17: Ivan's personality data seeded successfully!");
            }
            else
            {
                logger.LogInformation("🧪 STEP 16: Skipping Ivan's personality seeding in Test environment for test isolation");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ MIGRATION ERROR - Failed to apply database migrations. Error: {ErrorMessage}. Inner: {InnerException}. StackTrace: {StackTrace}", 
                ex.Message, ex.InnerException?.Message, ex.StackTrace);
            // Don't throw - let app start to see migration errors in logs
        }
    }
    migrationLogger?.LogInformation("✅ MIGRATION SCOPE DISPOSED - Migration check completed");
}
catch (Exception scopeEx)
{
    migrationLogger?.LogError(scopeEx, "❌ CRITICAL ERROR - Failed to create service scope for migrations. Error: {ErrorMessage}", scopeEx.Message);
    migrationLogger?.LogError("🔍 SCOPE EXCEPTION DETAILS - Type: {ExceptionType}, StackTrace: {StackTrace}", scopeEx.GetType().Name, scopeEx.StackTrace);
}

migrationLogger?.LogInformation("✅ MIGRATION SECTION COMPLETED - Proceeding to secrets validation and middleware");

// Validate secrets configuration at startup for early error detection
try
{
    var secretsLogger = app.Services.GetService<ILogger<Program>>();
    secretsLogger?.LogInformation("🔐 SECRETS VALIDATION: Starting configuration validation...");
    
    using (var secretsScope = app.Services.CreateScope())
    {
        var secretsService = secretsScope.ServiceProvider.GetRequiredService<DigitalMe.Services.Configuration.ISecretsManagementService>();
        var validation = secretsService.ValidateSecrets();
        
        if (!validation.IsValid)
        {
            secretsLogger?.LogError("❌ SECRETS VALIDATION FAILED: {MissingCount} missing secrets, {WeakCount} weak secrets", 
                validation.MissingSecrets.Count, validation.WeakSecrets.Count);
            
            foreach (var missing in validation.MissingSecrets)
            {
                secretsLogger?.LogError("   Missing: {Secret}", missing);
            }
            
            foreach (var weak in validation.WeakSecrets)
            {
                secretsLogger?.LogWarning("   Weak: {Secret}", weak);
            }
            
            // In production, fail fast for critical secrets (but allow Testing environment)
            var isSecure = secretsService.IsSecureEnvironment();
            var isTest = secretsService.IsTestEnvironment();
            var hasMissing = validation.MissingSecrets.Any();
            
            secretsLogger?.LogInformation("Environment check: IsSecure={IsSecure}, IsTest={IsTest}, HasMissing={HasMissing}", isSecure, isTest, hasMissing);
            
            if (isSecure && !isTest && hasMissing)
            {
                throw new InvalidOperationException($"Critical secrets validation failed in production environment. Missing: {string.Join(", ", validation.MissingSecrets)}");
            }
        }
        else
        {
            secretsLogger?.LogInformation("✅ SECRETS VALIDATION: All critical secrets configured correctly");
        }
        
        // Log warnings and recommendations
        foreach (var warning in validation.Warnings)
        {
            secretsLogger?.LogWarning("⚠️ SECRETS: {Warning}", warning);
        }
        
        foreach (var recommendation in validation.SecurityRecommendations)
        {
            secretsLogger?.LogInformation("💡 SECURITY: {Recommendation}", recommendation);
        }
    }
}
catch (Exception secretsEx)
{
    var secretsLogger = app.Services.GetService<ILogger<Program>>();
    secretsLogger?.LogError(secretsEx, "❌ CRITICAL ERROR: Secrets validation failed");
    
    // In production, fail fast for secrets issues
    if (app.Environment.IsProduction())
    {
        throw;
    }
}

// Configure the HTTP request pipeline
app.UseMiddleware<DigitalMe.Middleware.RequestLoggingMiddleware>();
app.UseMiddleware<DigitalMe.Middleware.GlobalExceptionHandlingMiddleware>();

// Rate Limiting - apply before authentication for security
app.UseRateLimiter();

// Security Headers - add early in pipeline for all environments
app.UseMiddleware<DigitalMe.Middleware.SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Production Security Middleware - apply HSTS in production
if (app.Environment.IsProduction())
{
    app.UseHsts();
}

app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Default route for home page (needed for integration tests)
app.MapGet("/", () => Results.Content("<h1>DigitalMe</h1><p>Digital Ivan Assistant API</p>", "text/html"));

// Chat page route (needed for integration tests)
app.MapGet("/chat", () => Results.Content("<h1>Чат с Иваном</h1><p>Digital Ivan Chat Interface</p>", "text/html"));

// Personality page route (needed for integration tests)
app.MapGet("/personality", () => Results.Content("<h1>Ivan's Personality</h1><p>Personality Configuration</p>", "text/html"));

// SignalR Hub mapping
app.MapHub<DigitalMe.Hubs.ChatHub>("/chathub");

// Standard ASP.NET Core Health Check Endpoints as required by MVP Phase 6 plan
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// Enhanced Health Check Endpoints (preserving existing advanced monitoring)
app.MapGet("/health/enhanced", async (DigitalMe.Services.Monitoring.IHealthCheckService healthCheckService) =>
{
    var healthStatus = await healthCheckService.GetSystemHealthAsync();
    return Results.Ok(healthStatus);
});

// Simple Health Check for Integration Tests (fallback)
app.MapGet("/health/simple", () =>
{
    return Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
});

app.MapGet("/health/enhanced/ready", async (DigitalMe.Services.Monitoring.IHealthCheckService healthCheckService) =>
{
    var readinessStatus = await healthCheckService.GetReadinessAsync();
    return readinessStatus.IsReady ? Results.Ok(readinessStatus) : Results.Problem(statusCode: 503, detail: readinessStatus.Reason);
});

app.MapGet("/health/live", async (DigitalMe.Services.Monitoring.IHealthCheckService healthCheckService) =>
{
    var livenessStatus = await healthCheckService.GetLivenessAsync();
    return livenessStatus.IsAlive ? Results.Ok(livenessStatus) : Results.Problem(statusCode: 503, detail: livenessStatus.Reason);
});

app.MapGet("/health/component/{componentName}", async (string componentName, DigitalMe.Services.Monitoring.IHealthCheckService healthCheckService) =>
{
    var componentHealth = await healthCheckService.GetComponentHealthAsync(componentName);
    return Results.Ok(componentHealth);
});

// Performance Metrics Endpoints
app.MapGet("/metrics", async (DigitalMe.Services.Monitoring.IPerformanceMetricsService metricsService) =>
{
    var metrics = await metricsService.GetMetricsSummaryAsync();
    return Results.Ok(metrics);
});

app.MapGet("/metrics/{timeWindowMinutes:int}", async (int timeWindowMinutes, DigitalMe.Services.Monitoring.IPerformanceMetricsService metricsService) =>
{
    var timeWindow = TimeSpan.FromMinutes(Math.Max(1, Math.Min(timeWindowMinutes, 1440))); // 1 min to 24 hours
    var metrics = await metricsService.GetMetricsSummaryAsync(timeWindow);
    return Results.Ok(metrics);
});

// Operational Information Endpoint - Production monitoring and diagnostics
app.MapGet("/info", () => new
{
    Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
    Timestamp = DateTime.UtcNow,
    RuntimeInformation = new
    {
        FrameworkDescription = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
        OSDescription = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
        ProcessArchitecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString()
    },
    SystemInfo = new
    {
        MachineName = Environment.MachineName,
        ProcessorCount = Environment.ProcessorCount,
        WorkingSet = Environment.WorkingSet,
        TotalPhysicalMemory = GC.GetTotalMemory(false)
    }
});

// Secrets Validation Endpoint - Security monitoring (development/staging only)
app.MapGet("/security/secrets-validation", (DigitalMe.Services.Configuration.ISecretsManagementService secretsService, IWebHostEnvironment environment) =>
{
    // Only allow in development/staging for security reasons
    if (environment.IsProduction())
    {
        return Results.NotFound();
    }
    
    var validation = secretsService.ValidateSecrets();
    return Results.Ok(new
    {
        validation.IsValid,
        MissingSecretsCount = validation.MissingSecrets.Count,
        WeakSecretsCount = validation.WeakSecrets.Count,
        WarningsCount = validation.Warnings.Count,
        SecurityRecommendationsCount = validation.SecurityRecommendations.Count,
        Details = new
        {
            validation.MissingSecrets,
            validation.WeakSecrets,
            validation.Warnings,
            validation.SecurityRecommendations
        },
        IsSecureEnvironment = secretsService.IsSecureEnvironment(),
        Timestamp = DateTime.UtcNow
    });
});

// Runtime optimization validation endpoints
app.MapGet("/runtime/gc", () =>
{
    return Results.Ok(new
    {
        gcMode = GCSettings.IsServerGC ? "Server" : "Workstation",
        gcLatencyMode = GCSettings.LatencyMode.ToString(),
        isLowLatency = GCSettings.IsServerGC && GCSettings.LatencyMode == GCLatencyMode.LowLatency
    });
});

app.MapGet("/runtime/threadpool", () =>
{
    ThreadPool.GetMinThreads(out int minWorkerThreads, out int minCompletionPortThreads);
    ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxCompletionPortThreads);
    ThreadPool.GetAvailableThreads(out int availableWorkerThreads, out int availableCompletionPortThreads);
    
    return Results.Ok(new
    {
        minWorkerThreads,
        minCompletionPortThreads,
        maxWorkerThreads,
        maxCompletionPortThreads,
        availableWorkerThreads,
        availableCompletionPortThreads,
        activeWorkerThreads = maxWorkerThreads - availableWorkerThreads,
        activeCompletionPortThreads = maxCompletionPortThreads - availableCompletionPortThreads
    });
});

// DEBUG: Add critical check point before Telegram initialization
var beforeTelegramLogger = app.Services.GetService<ILogger<Program>>();
beforeTelegramLogger?.LogInformation("🔍 CRITICAL CHECKPOINT: About to start Telegram initialization - this log should always appear");

// Initialize Telegram Bot on startup - always attempt configuration check
var startupLogger = app.Services.GetService<ILogger<Program>>();
startupLogger?.LogInformation("🚀 TELEGRAM BOT: REACHED TELEGRAM INITIALIZATION BLOCK");
startupLogger?.LogInformation("🚀 TELEGRAM BOT: Starting initialization process...");

using (var scope = app.Services.CreateScope())
{
    try
    {
        startupLogger?.LogInformation("🔍 TELEGRAM BOT: Creating service scope...");
        var telegramService = scope.ServiceProvider.GetRequiredService<DigitalMe.Integrations.External.Telegram.ITelegramService>();
        startupLogger?.LogInformation("🔍 TELEGRAM BOT: TelegramService resolved successfully");
        
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        startupLogger?.LogInformation("🔍 TELEGRAM BOT: Configuration resolved successfully");
        
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        startupLogger?.LogInformation("🔍 TELEGRAM BOT: Logger resolved successfully");
        
        var botToken = configuration.GetSection("Telegram:BotToken").Value;
        startupLogger?.LogInformation("🔍 TELEGRAM BOT: Bot token retrieved: {HasToken}", !string.IsNullOrEmpty(botToken));
        
        if (!string.IsNullOrEmpty(botToken) && botToken.Trim() != "" && botToken != "YOUR_BOT_TOKEN_HERE")
        {
            logger.LogInformation("🤖 TELEGRAM BOT: Initializing with configured token...");
            var initialized = telegramService.InitializeAsync(botToken).GetAwaiter().GetResult();
            
            if (initialized)
            {
                logger.LogInformation("✅ TELEGRAM BOT: Successfully initialized and connected");
            }
            else
            {
                logger.LogWarning("⚠️ TELEGRAM BOT: Failed to initialize - invalid token or connection issue");
            }
        }
        else
        {
            logger.LogInformation("ℹ️ TELEGRAM BOT: No valid bot token configured - skipping initialization");
        }
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetService<ILogger<Program>>();
        logger?.LogError(ex, "❌ TELEGRAM BOT: Error during initialization: {ErrorMessage}", ex.Message);
    }
}

app.Run();

// Make the Program class public for integration testing
public partial class Program { }