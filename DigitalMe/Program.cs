using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Runtime;
using Serilog;
using DigitalMe.Data;
using DigitalMe.Services;
using DigitalMe.Repositories;
using DigitalMe.Extensions;

var builder = WebApplication.CreateBuilder(args);

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

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JWT:Issuer"],
            ValidAudience = builder.Configuration["JWT:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"]!))
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

// DigitalMe Services - Standardized Registration
builder.Services.AddDigitalMeServices(builder.Configuration);

// Configure Security settings
builder.Services.Configure<DigitalMe.Services.Security.SecuritySettings>(
    builder.Configuration.GetSection("Security"));

// Configure JWT settings
builder.Services.Configure<DigitalMe.Configuration.JwtSettings>(
    builder.Configuration.GetSection("JWT"));

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<DigitalMeDbContext>("database")
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

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
            ApplyDatabaseMigrations(context, logger, app);
                
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

migrationLogger?.LogInformation("✅ MIGRATION SECTION COMPLETED - Proceeding to middleware and Telegram initialization");

// Configure the HTTP request pipeline
app.UseMiddleware<DigitalMe.Middleware.RequestLoggingMiddleware>();
app.UseMiddleware<DigitalMe.Middleware.GlobalExceptionHandlingMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
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

// Enhanced Health Check Endpoints
app.MapGet("/health", async (DigitalMe.Services.Monitoring.IHealthCheckService healthCheckService) =>
{
    var healthStatus = await healthCheckService.GetSystemHealthAsync();
    return Results.Ok(healthStatus);
});

// Simple Health Check for Integration Tests (fallback)
app.MapGet("/health/simple", () =>
{
    return Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow });
});

app.MapGet("/health/ready", async (DigitalMe.Services.Monitoring.IHealthCheckService healthCheckService) =>
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

/// <summary>
/// Applies database migrations with SQLite synchronization handling and recovery mechanisms
/// </summary>
/// <param name="context">Database context for migration operations</param>
/// <param name="logger">Logger for tracking migration progress and errors</param>
/// <param name="app">Web application instance for environment checking</param>
static void ApplyDatabaseMigrations(DigitalMeDbContext context, ILogger<Program> logger, WebApplication app)
{
    logger.LogInformation("🔍 STEP 7: Starting database migration check...");
    
    logger.LogInformation("🔍 STEP 8: Checking database provider...");
    var isInMemory = context.Database.ProviderName?.Contains("InMemory") == true;
    logger.LogInformation("🔍 STEP 9: Database provider is InMemory: {IsInMemory}", isInMemory);
    
    if (isInMemory)
    {
        logger.LogInformation("🔍 STEP 10: InMemory database detected - using EnsureCreated instead of migrations");
        var created = context.Database.EnsureCreated();
        logger.LogInformation("✅ STEP 11: InMemory database created: {Created}", created);
        return;
    }

    logger.LogInformation("🔍 STEP 10: Checking database connection...");
    var canConnect = context.Database.CanConnect();
    logger.LogInformation("🔍 STEP 11: Database connection check result: {CanConnect}", canConnect);
    
    if (!canConnect)
    {
        HandleDatabaseCreation(context, logger);
        return;
    }
    
    HandleMigrationSync(context, logger, app);
}

/// <summary>
/// Handles database creation when connection fails
/// </summary>
/// <param name="context">Database context for creation operations</param>
/// <param name="logger">Logger for tracking creation progress</param>
static void HandleDatabaseCreation(DigitalMeDbContext context, ILogger<Program> logger)
{
    logger.LogWarning("⚠️ Cannot connect to database - attempting to create...");
    try
    {
        context.Database.EnsureCreated();
        logger.LogInformation("✅ Database created successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Failed to create database: {ErrorMessage}", ex.Message);
    }
}

/// <summary>
/// Handles migration synchronization checking and application
/// </summary>
/// <param name="context">Database context for migration operations</param>
/// <param name="logger">Logger for tracking migration progress</param>
/// <param name="app">Web application instance for environment checking</param>
static void HandleMigrationSync(DigitalMeDbContext context, ILogger<Program> logger, WebApplication app)
{
    logger.LogInformation("🔍 STEP 12: Checking migration history consistency...");
    try
    {
        var appliedMigrations = context.Database.GetAppliedMigrations().ToList();
        var pendingMigrations = context.Database.GetPendingMigrations().ToList();
        
        logger.LogInformation("🔍 STEP 13: Applied migrations: [{Applied}]", string.Join(", ", appliedMigrations));
        logger.LogInformation("🔍 STEP 14: Pending migrations: [{Pending}]", string.Join(", ", pendingMigrations));
        
        CheckMigrationConsistency(appliedMigrations, pendingMigrations, context, logger);
        
        if (!pendingMigrations.Any())
        {
            logger.LogInformation("✅ STEP 15: Database is up to date - no migrations to apply");
            return;
        }

        ApplyPendingMigrations(pendingMigrations, context, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Migration check failed: {ErrorMessage}", ex.Message);
        AttemptSqliteRecovery(context, logger, app);
    }
}

/// <summary>
/// Checks for migration synchronization issues and handles stale migration history
/// </summary>
/// <param name="appliedMigrations">List of applied migrations</param>
/// <param name="pendingMigrations">List of pending migrations</param>
/// <param name="context">Database context</param>
/// <param name="logger">Logger for tracking issues</param>
static void CheckMigrationConsistency(List<string> appliedMigrations, List<string> pendingMigrations, DigitalMeDbContext context, ILogger<Program> logger)
{
    var allMigrations = context.Database.GetMigrations().ToList();
    logger.LogInformation("🔍 All available migrations: [{All}]", string.Join(", ", allMigrations));
    
    if (!appliedMigrations.Any() && !pendingMigrations.Any())
    {
        return;
    }
    
    // Check for stale migration entries - migrations applied in DB but no longer exist in codebase
    var staleMigrations = appliedMigrations.Where(applied => !allMigrations.Contains(applied)).ToList();
    if (staleMigrations.Any())
    {
        logger.LogError("🚨 STALE MIGRATION HISTORY DETECTED - Applied migrations no longer exist in codebase:");
        foreach (var staleMigration in staleMigrations)
        {
            logger.LogError("   - {StaleMigration}", staleMigration);
        }
        logger.LogError("🔧 CRITICAL: Database must be recreated or migration history manually cleaned");
        throw new InvalidOperationException($"Migration history contains {staleMigrations.Count} stale entries. Database recreation required.");
    }
    
    var hasGapInHistory = appliedMigrations.Count + pendingMigrations.Count != allMigrations.Count;
    if (!hasGapInHistory)
    {
        return;
    }
    
    logger.LogWarning("⚠️ MIGRATION SYNCHRONIZATION ISSUE DETECTED");
    logger.LogWarning("Applied: {AppliedCount}, Pending: {PendingCount}, Total: {TotalCount}", 
        appliedMigrations.Count, pendingMigrations.Count, allMigrations.Count);
}

/// <summary>
/// Applies pending migrations and verifies success
/// </summary>
/// <param name="pendingMigrations">List of pending migrations</param>
/// <param name="context">Database context</param>
/// <param name="logger">Logger for tracking application progress</param>
static void ApplyPendingMigrations(List<string> pendingMigrations, DigitalMeDbContext context, ILogger<Program> logger)
{
    logger.LogInformation("🔄 STEP 15: Applying {Count} database migrations...", pendingMigrations.Count);
    context.Database.Migrate();
    logger.LogInformation("✅ STEP 16: Database migrations applied successfully!");
    
    var remainingPending = context.Database.GetPendingMigrations().ToList();
    if (!remainingPending.Any())
    {
        logger.LogInformation("✅ Migration verification: All migrations applied successfully");
        return;
    }
    
    logger.LogWarning("⚠️ Some migrations still pending after apply: [{Pending}]", 
        string.Join(", ", remainingPending));
}

/// <summary>
/// Attempts SQLite-specific recovery for migration issues
/// </summary>
/// <param name="context">Database context</param>
/// <param name="logger">Logger for tracking recovery attempts</param>
/// <param name="app">Web application instance for environment checking</param>
static void AttemptSqliteRecovery(DigitalMeDbContext context, ILogger<Program> logger, WebApplication app)
{
    if (context.Database.ProviderName?.Contains("Sqlite") != true)
    {
        return;
    }
    
    logger.LogWarning("🔧 Attempting SQLite synchronization recovery...");
    try
    {
        if (!app.Environment.IsDevelopment() && app.Environment.EnvironmentName != "Testing")
        {
            logger.LogError("❌ Production environment: Manual intervention required for migration issues");
            return;
        }
        
        logger.LogInformation("🔄 Development environment: Attempting database recreation for clean start");
        
        // For development, recreate the database if migration sync is broken
        if (context.Database.CanConnect())
        {
            logger.LogWarning("🗑️ Dropping existing database to resolve migration synchronization issues");
            context.Database.EnsureDeleted();
        }
        
        logger.LogInformation("🆕 Creating fresh database with current migration set");
        context.Database.Migrate();
        logger.LogInformation("✅ SQLite database recreated successfully with clean migration history");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Migration recovery failed: {ErrorMessage}", ex.Message);
        logger.LogError("🔧 Manual intervention required - consider deleting database files and restarting");
    }
}

// Make the Program class public for integration testing
public partial class Program { }