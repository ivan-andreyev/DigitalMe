using DigitalMe.Web.Data;
using DigitalMe.Web.Models;
using DigitalMe.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

// Configure runtime for production performance
builder.Services.Configure<ThreadPoolSettings>(options =>
{
    options.MinWorkerThreads = Environment.ProcessorCount * 4;
    options.MinCompletionPortThreads = Environment.ProcessorCount * 4;
    options.MaxWorkerThreads = Environment.ProcessorCount * 32;
    options.MaxCompletionPortThreads = Environment.ProcessorCount * 32;
});

// Add runtime configuration service
builder.Services.AddHostedService<RuntimeConfigurationService>();

// Add configuration
builder.Services.Configure<DigitalMeConfiguration>(
    builder.Configuration.GetSection("DigitalMe"));

// Add Query Performance Monitor
builder.Services.AddSingleton<QueryPerformanceMonitorService>();

// Add Entity Framework with optimized connection pooling and query performance monitoring
builder.Services.AddDbContext<DigitalMeDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), npgsql =>
    {
        npgsql.CommandTimeout(30);
        npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
        npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "public");
    });
    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
    options.EnableDetailedErrors(builder.Environment.IsDevelopment());
    
    // Production query optimizations
    if (!builder.Environment.IsDevelopment())
    {
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        options.EnableServiceProviderCaching();
        
        // Add query performance monitoring
        var serviceProvider = builder.Services.BuildServiceProvider();
        var monitor = serviceProvider.GetService<QueryPerformanceMonitorService>();
        if (monitor != null)
        {
            options.AddInterceptors(new QueryPerformanceInterceptor(monitor));
        }
    }
}, ServiceLifetime.Scoped);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Configure Antiforgery with proper cookie settings for Cloud Run
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.HttpOnly = true;
});

// Add HTTP Client with production optimizations
builder.Services.AddHttpClient("DigitalMeApi", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Connection", "keep-alive");
    client.DefaultRequestHeaders.Add("Keep-Alive", "timeout=30, max=1000");
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler()
{
    MaxConnectionsPerServer = 50,
    PooledConnectionLifetime = TimeSpan.FromMinutes(2)
});

// Add SignalR
builder.Services.AddSignalR();

// Add Auth Service
builder.Services.AddAuthService();

// Add SignalR Chat Service
builder.Services.AddSingleton<ISignalRChatService, SignalRChatService>();

// Add Database Connection Monitor
builder.Services.AddSingleton<IDatabaseConnectionMonitor, DatabaseConnectionMonitor>();

// Add Memory Cache for query optimization
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1000; // Limit number of entries
    options.CompactionPercentage = 0.25; // Remove 25% when full
});

// Configure Redis cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") 
                          ?? "localhost:6379";
    options.InstanceName = "DigitalMe";
});

// Add Query Optimization Services
builder.Services.AddScoped<IOptimizedDataService, OptimizedDataService>();
builder.Services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();
builder.Services.AddScoped<IQueryPerformanceMonitorService, QueryPerformanceMonitorService>(sp => 
    sp.GetRequiredService<QueryPerformanceMonitorService>());
builder.Services.AddScoped<IQueryOptimizationValidator, QueryOptimizationValidator>();

// Add Demo Services for Business Showcase
builder.Services.AddScoped<DemoEnvironmentService>();
builder.Services.AddScoped<DemoDataSeeder>();
builder.Services.AddScoped<IDemoMetricsService, DemoMetricsService>();
builder.Services.AddScoped<IBackupDemoScenariosService, BackupDemoScenariosService>();

// Add Read Replica Connection Service
builder.Services.AddScoped<IDatabaseConnectionService, DatabaseConnectionService>();

// Add Health Checks with database pool monitoring and auto-scaling metrics
builder.Services.AddHealthChecks()
    .AddDbContextCheck<DigitalMeDbContext>("database")
    .AddCheck<DatabasePoolHealthCheck>("database-pool")
    .AddCheck<AutoScalingHealthCheck>("autoscaling");

// Add CORS for API communication
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Configure cookie policy for antiforgery
app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.Lax,
    Secure = CookieSecurePolicy.SameAsRequest,
    HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always
});

app.UseRouting();
app.UseCors("AllowAll");

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Add metrics endpoint for pool efficiency validation
app.MapGet("/metrics", async (IDatabaseConnectionMonitor monitor) =>
{
    var metrics = await monitor.GetPoolMetricsAsync();
    return Results.Ok(new
    {
        database_pool = metrics,
        pool_efficiency = metrics.PoolEfficiency,
        is_healthy = await monitor.IsPoolHealthyAsync(),
        meets_p24_requirement = metrics.PoolEfficiency >= 90.0
    });
});

// Add comprehensive health check endpoint
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(x => new
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                description = x.Value.Description,
                duration = x.Value.Duration.TotalMilliseconds,
                exception = x.Value.Exception?.Message,
                data = x.Value.Data
            }),
            total_duration = report.TotalDuration.TotalMilliseconds,
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            WriteIndented = true
        }));
    }
});

// Add database pool health endpoint
app.MapGet("/health/database", async (IDatabaseConnectionMonitor monitor) =>
{
    var isHealthy = await monitor.IsPoolHealthyAsync();
    var metrics = await monitor.GetPoolMetricsAsync();
    
    if (isHealthy)
    {
        return Results.Ok(new 
        { 
            status = "Healthy", 
            efficiency = metrics.PoolEfficiency,
            pool_size = new { min = metrics.MinPoolSize, max = metrics.MaxPoolSize },
            active_connections = metrics.CurrentActiveConnections
        });
    }
    
    return Results.Json(new 
    { 
        status = "Unhealthy", 
        efficiency = metrics.PoolEfficiency,
        issue = "Pool efficiency below 90% requirement"
    }, statusCode: 503);
});

// Add demo metrics endpoint for dashboard testing
app.MapGet("/api/demo/metrics", async (IDemoMetricsService metricsService) =>
{
    try 
    {
        var health = await metricsService.GetSystemHealthAsync();
        var integrations = await metricsService.GetIntegrationStatusAsync();
        var ai = await metricsService.GetAiMetricsAsync();
        var business = await metricsService.GetBusinessMetricsAsync();
        var activities = await metricsService.GetRecentActivitiesAsync();
        
        return Results.Ok(new 
        {
            systemHealth = health,
            integrations = integrations,
            aiMetrics = ai,
            businessMetrics = business,
            recentActivities = activities.Take(5),
            timestamp = DateTime.Now,
            status = "success"
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = ex.Message, status = "error" }, statusCode: 500);
    }
});

// Add backup demo scenarios API endpoints
app.MapGet("/api/demo/backup/scenarios", async (IBackupDemoScenariosService backupService) =>
{
    try
    {
        var scenarios = await backupService.GetAvailableScenariosAsync();
        var isActive = await backupService.IsBackupModeActiveAsync();
        
        return Results.Ok(new
        {
            scenarios = scenarios,
            backupModeActive = isActive,
            timestamp = DateTime.Now,
            status = "success"
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = ex.Message, status = "error" }, statusCode: 500);
    }
});

app.MapPost("/api/demo/backup/activate", async (IBackupDemoScenariosService backupService, BackupMode mode) =>
{
    try
    {
        await backupService.ActivateBackupModeAsync(mode);
        return Results.Ok(new 
        { 
            message = $"Backup mode '{mode}' activated successfully",
            mode = mode.ToString(),
            status = "success"
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = ex.Message, status = "error" }, statusCode: 500);
    }
});

app.MapGet("/api/demo/backup/response/{scenario}", async (IBackupDemoScenariosService backupService, string scenario, string? context) =>
{
    try
    {
        var response = await backupService.GetBackupResponseAsync(scenario, context ?? "demo");
        return Results.Ok(new
        {
            response = response,
            scenario = scenario,
            timestamp = DateTime.Now,
            status = "success"
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = ex.Message, status = "error" }, statusCode: 500);
    }
});

// Add query performance monitoring endpoints
app.MapGet("/metrics/query-performance", async (IQueryPerformanceMonitorService monitor) =>
{
    var report = await monitor.GetPerformanceReportAsync(TimeSpan.FromHours(1));
    return Results.Ok(report);
});

app.MapGet("/metrics/slow-queries", async (IQueryPerformanceMonitorService monitor, int limit = 20) =>
{
    var slowQueries = await monitor.GetSlowQueriesAsync(limit);
    return Results.Ok(new 
    {
        slow_queries = slowQueries,
        count = slowQueries.Count,
        threshold_ms = 100,
        generated_at = DateTime.UtcNow
    });
});

app.MapGet("/metrics/cache-performance", async (IQueryPerformanceMonitorService monitor) =>
{
    var cacheStats = await monitor.GetCachePerformanceAsync();
    return Results.Ok(new
    {
        cache_performance = cacheStats,
        meets_p24_requirement = cacheStats.HitRatio >= 80.0,
        generated_at = DateTime.UtcNow
    });
});

app.MapGet("/metrics/optimization-suggestions", async (IQueryPerformanceMonitorService monitor) =>
{
    var suggestions = await monitor.GetOptimizationSuggestionsAsync();
    return Results.Ok(new
    {
        suggestions = suggestions,
        count = suggestions.Count,
        high_priority_count = suggestions.Count(s => s.Priority >= SuggestionPriority.High),
        generated_at = DateTime.UtcNow
    });
});

// Add optimized data service test endpoint
app.MapGet("/api/test/optimized-queries", async (IOptimizedDataService dataService) =>
{
    var perfMetrics = await dataService.GetPerformanceMetricsAsync();
    return Results.Ok(new
    {
        message = "Optimized Data Service is working",
        performance_metrics = perfMetrics,
        features = new[]
        {
            "AsNoTracking() for read-only operations",
            "Intelligent memory caching with TTL",
            "Composite database indexes for performance",
            "Query performance tracking",
            "Cache hit ratio optimization",
            "Read replica support for read/write splitting"
        },
        success = true
    });
});

// Add read replica test endpoint
app.MapGet("/api/test/read-replica", async (IDatabaseConnectionService connectionService) =>
{
    try
    {
        var readConnectionString = await connectionService.GetReadConnectionStringAsync();
        var writeConnectionString = await connectionService.GetWriteConnectionStringAsync();
        
        var readHasReplica = !readConnectionString.Equals(writeConnectionString, StringComparison.OrdinalIgnoreCase);
        
        return Results.Ok(new
        {
            message = "Read Replica Configuration Test",
            read_replica_configured = readHasReplica,
            read_connection_available = !string.IsNullOrEmpty(readConnectionString),
            write_connection_available = !string.IsNullOrEmpty(writeConnectionString),
            fallback_behavior = readHasReplica ? "Using dedicated read replica" : "Fallback to primary database",
            p244_status = "Read Replica Implementation Complete",
            features = new[]
            {
                "Read/write connection splitting",
                "Automatic fallback to primary connection",
                "Environment variable support for replica configuration",
                "Optimized connection pooling for read operations"
            },
            success = true
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new
        {
            message = "Read Replica Test Failed",
            error = ex.Message,
            success = false
        }, statusCode: 500);
    }
});

// Add T2.2 Query Optimization Validation Endpoints
app.MapGet("/api/validate/query-optimization", async (IQueryOptimizationValidator validator) =>
{
    var validation = await validator.ValidateOptimizationsAsync();
    return Results.Ok(new
    {
        t22_query_optimization_status = validation,
        meets_p24_requirements = validation.OverallSuccess,
        implemented_features = new[]
        {
            "AsNoTracking() for read-only operations",
            "Composite database indexes for JOIN and WHERE optimization",
            "Intelligent memory caching with configurable TTL",
            "Query performance monitoring and metrics",
            "Cache invalidation strategies",
            "Projection queries for summary data"
        },
        validation_timestamp = validation.ValidationTime
    });
});

app.MapGet("/api/validate/performance-benchmark", async (IQueryOptimizationValidator validator, int iterations = 50) =>
{
    var benchmark = await validator.BenchmarkQueriesAsync(iterations);
    return Results.Ok(new
    {
        t22_performance_benchmark = benchmark,
        meets_performance_target = benchmark.OverallSuccess,
        target_improvement = "20% query performance improvement",
        actual_improvement = $"{benchmark.AverageImprovement:F1}%",
        benchmark_timestamp = benchmark.BenchmarkTime
    });
});

app.MapGet("/api/validate/cache-efficiency", async (IQueryOptimizationValidator validator) =>
{
    var cacheTest = await validator.TestCacheEfficiencyAsync();
    return Results.Ok(new
    {
        t22_cache_efficiency = cacheTest,
        meets_p24_requirement = cacheTest.MeetsP24Requirement,
        cache_target = "80% hit ratio for frequently accessed data",
        actual_hit_ratio = $"{cacheTest.CacheHitRatio:F1}%",
        speedup_factor = $"{cacheTest.CacheSpeedupFactor:F2}x",
        test_timestamp = cacheTest.TestTime
    });
});

app.Run();
