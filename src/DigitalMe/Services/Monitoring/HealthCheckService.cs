using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using DigitalMe.Data;
using DigitalMe.Services.Tools;
using DigitalMe.Integrations.MCP;

namespace DigitalMe.Services.Monitoring;

/// <summary>
/// Implementation of enhanced health checking service.
/// Provides comprehensive health monitoring for all system components.
/// </summary>
public class HealthCheckService : IHealthCheckService
{
    private readonly ILogger<HealthCheckService> _logger;
    private readonly DigitalMeDbContext _dbContext;
    private readonly IToolRegistry _toolRegistry;
    private readonly IMCPClient _mcpClient;
    private readonly IPerformanceMetricsService _metricsService;
    private readonly Dictionary<string, Func<Task<ComponentHealthStatus>>> _customHealthChecks = new();
    private readonly DateTime _startTime = DateTime.UtcNow;

    public HealthCheckService(
        ILogger<HealthCheckService> logger,
        DigitalMeDbContext dbContext,
        IToolRegistry toolRegistry,
        IMCPClient mcpClient,
        IPerformanceMetricsService metricsService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _toolRegistry = toolRegistry;
        _mcpClient = mcpClient;
        _metricsService = metricsService;
    }

    public async Task<SystemHealthStatus> GetSystemHealthAsync()
    {
        using var scope = _metricsService.StartTracking("system_health_check");

        var components = new Dictionary<string, ComponentHealthStatus>();
        var alerts = new List<HealthAlert>();

        try
        {
            // Check all standard components
            var componentChecks = new Dictionary<string, Func<Task<ComponentHealthStatus>>>
            {
                ["database"] = CheckDatabaseHealthAsync,
                ["tool_registry"] = CheckToolRegistryHealthAsync,
                ["mcp_client"] = CheckMCPClientHealthAsync,
                ["memory"] = CheckMemoryHealthAsync,
                ["performance"] = CheckPerformanceHealthAsync
            };

            // Add custom health checks
            foreach (var customCheck in _customHealthChecks)
            {
                componentChecks[customCheck.Key] = customCheck.Value;
            }

            // Execute all health checks concurrently
            var healthCheckTasks = componentChecks.Select(async kvp =>
            {
                try
                {
                    var result = await kvp.Value();
                    return (kvp.Key, result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Health check failed for component {Component}", kvp.Key);
                    return (kvp.Key, new ComponentHealthStatus
                    {
                        ComponentName = kvp.Key,
                        Status = HealthStatus.Unhealthy,
                        ErrorMessage = ex.Message,
                        Description = $"Health check threw exception: {ex.GetType().Name}"
                    });
                }
            });

            var healthResults = await Task.WhenAll(healthCheckTasks);

            foreach (var (componentName, status) in healthResults)
            {
                components[componentName] = status;

                // Generate alerts for unhealthy components
                if (status.Status != HealthStatus.Healthy)
                {
                    alerts.Add(new HealthAlert
                    {
                        Component = componentName,
                        Severity = status.Status == HealthStatus.Unhealthy ? AlertSeverity.Critical : AlertSeverity.Warning,
                        Message = status.ErrorMessage ?? $"Component {componentName} is {status.Status}",
                        Metadata = new Dictionary<string, object>
                        {
                            ["component_status"] = status.Status.ToString(),
                            ["response_time"] = status.ResponseTime?.TotalMilliseconds ?? 0
                        }
                    });
                }
            }

            // Calculate overall status
            var overallStatus = CalculateOverallStatus(components.Values);

            // Get system metrics
            var performanceMetrics = await _metricsService.GetMetricsSummaryAsync(TimeSpan.FromMinutes(5));
            var systemMetrics = new SystemMetrics
            {
                MemoryUsageMB = performanceMetrics.SystemResources.MemoryUsageMB,
                CpuUsagePercent = performanceMetrics.SystemResources.CpuUsagePercent,
                ActiveConnections = performanceMetrics.SignalR.ActiveConnections,
                RequestsPerMinute = performanceMetrics.Business.AgentResponsesPerMinute,
                ErrorRate = 1.0 - performanceMetrics.Business.AgentSuccessRate,
                AverageResponseTime = performanceMetrics.ResponseTimes.Values
                    .DefaultIfEmpty(new ResponseTimeMetric())
                    .Average(rt => rt.AverageResponseTime.TotalMilliseconds) > 0
                    ? TimeSpan.FromMilliseconds(performanceMetrics.ResponseTimes.Values
                        .DefaultIfEmpty(new ResponseTimeMetric())
                        .Average(rt => rt.AverageResponseTime.TotalMilliseconds))
                    : TimeSpan.Zero,
                DatabaseConnections = performanceMetrics.Database.ActiveConnections
            };

            var healthStatus = new SystemHealthStatus
            {
                OverallStatus = overallStatus,
                Version = GetApplicationVersion(),
                Uptime = DateTime.UtcNow - _startTime,
                Components = components,
                Metrics = systemMetrics,
                Alerts = alerts
            };

            scope.RecordSuccess();
            return healthStatus;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system health status");
            scope.RecordFailure(ex);

            return new SystemHealthStatus
            {
                OverallStatus = HealthStatus.Unknown,
                Version = GetApplicationVersion(),
                Uptime = DateTime.UtcNow - _startTime,
                Components = components,
                Alerts = new List<HealthAlert>
                {
                    new HealthAlert
                    {
                        Component = "system",
                        Severity = AlertSeverity.Emergency,
                        Message = $"System health check failed: {ex.Message}"
                    }
                }
            };
        }
    }

    public async Task<ComponentHealthStatus> GetComponentHealthAsync(string componentName)
    {
        var healthCheckFunction = componentName.ToLower() switch
        {
            "database" => CheckDatabaseHealthAsync,
            "tool_registry" => CheckToolRegistryHealthAsync,
            "mcp_client" => CheckMCPClientHealthAsync,
            "memory" => CheckMemoryHealthAsync,
            "performance" => CheckPerformanceHealthAsync,
            _ => _customHealthChecks.GetValueOrDefault(componentName)
        };

        if (healthCheckFunction == null)
        {
            return new ComponentHealthStatus
            {
                ComponentName = componentName,
                Status = HealthStatus.Unknown,
                ErrorMessage = $"Unknown component: {componentName}"
            };
        }

        try
        {
            return await healthCheckFunction();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for component {Component}", componentName);
            return new ComponentHealthStatus
            {
                ComponentName = componentName,
                Status = HealthStatus.Unhealthy,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<ReadinessStatus> GetReadinessAsync()
    {
        var systemHealth = await GetSystemHealthAsync();
        var criticalComponents = new[] { "database", "tool_registry" };

        var notReadyComponents = systemHealth.Components
            .Where(kvp => criticalComponents.Contains(kvp.Key) && kvp.Value.Status == HealthStatus.Unhealthy)
            .Select(kvp => kvp.Key)
            .ToList();

        var readyComponents = systemHealth.Components
            .Where(kvp => criticalComponents.Contains(kvp.Key) && kvp.Value.Status != HealthStatus.Unhealthy)
            .Select(kvp => kvp.Key)
            .ToList();

        return new ReadinessStatus
        {
            IsReady = !notReadyComponents.Any(),
            ReadyComponents = readyComponents,
            NotReadyComponents = notReadyComponents,
            Reason = notReadyComponents.Any()
                ? $"Critical components not ready: {string.Join(", ", notReadyComponents)}"
                : "All critical components ready"
        };
    }

    public async Task<LivenessStatus> GetLivenessAsync()
    {
        try
        {
            // Simple liveness check - can we access basic services?
            var memoryCheck = await CheckMemoryHealthAsync();
            var dbCanConnect = await CheckDatabaseConnectivityAsync();

            var isAlive = memoryCheck.Status != HealthStatus.Unhealthy && dbCanConnect;

            return new LivenessStatus
            {
                IsAlive = isAlive,
                LastHeartbeat = DateTime.UtcNow,
                Reason = isAlive ? "System is alive and responsive" : "System components failing"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Liveness check failed");
            return new LivenessStatus
            {
                IsAlive = false,
                LastHeartbeat = DateTime.UtcNow,
                Reason = $"Liveness check exception: {ex.Message}"
            };
        }
    }

    public void RegisterHealthCheck(string name, Func<Task<ComponentHealthStatus>> healthCheck)
    {
        _customHealthChecks[name] = healthCheck;
        _logger.LogInformation("Registered custom health check: {HealthCheckName}", name);
    }

    /// <summary>
    /// Generic health check execution pattern that eliminates code duplication.
    /// Handles timing, exception handling, and result mapping consistently.
    /// </summary>
    private async Task<ComponentHealthStatus> ExecuteHealthCheck<T>(
        string componentName,
        Func<Task<T>> healthCheckFunction,
        Func<T, ComponentHealthStatus> resultMapper)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await healthCheckFunction();
            stopwatch.Stop();

            var status = resultMapper(result);
            status.ComponentName = componentName;
            status.ResponseTime = stopwatch.Elapsed;
            status.LastChecked = DateTime.UtcNow;

            return status;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Health check failed for component {Component}", componentName);

            return new ComponentHealthStatus
            {
                ComponentName = componentName,
                Status = HealthStatus.Unhealthy,
                Description = $"Health check failed with exception: {ex.GetType().Name}",
                ResponseTime = stopwatch.Elapsed,
                LastChecked = DateTime.UtcNow,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Generic health check execution for functions returning HealthCheckResult.
    /// </summary>
    private async Task<ComponentHealthStatus> ExecuteHealthCheck(
        string componentName,
        Func<Task<HealthCheckResult>> healthCheckFunction)
    {
        return await ExecuteHealthCheck(componentName, healthCheckFunction, result =>
            new ComponentHealthStatus
            {
                ComponentName = componentName,
                Status = result.Status,
                Description = result.Description,
                Details = result.Details,
                ErrorMessage = result.ErrorMessage
            });
    }

    // Individual component health checks using generic pattern
    private async Task<ComponentHealthStatus> CheckDatabaseHealthAsync()
    {
        return await ExecuteHealthCheck("database", async () =>
        {
            var canConnect = await _dbContext.Database.CanConnectAsync();

            if (canConnect)
            {
                // Quick query to test actual functionality
                var conversationCount = await _dbContext.Conversations.CountAsync();

                return new HealthCheckResult
                {
                    IsSuccessful = true,
                    Status = HealthStatus.Healthy,
                    Description = "Database connection and query successful",
                    Details = new Dictionary<string, object>
                    {
                        ["connection_status"] = "Connected",
                        ["conversation_count"] = conversationCount,
                        ["database_provider"] = _dbContext.Database.ProviderName ?? "Unknown"
                    }
                };
            }
            else
            {
                return new HealthCheckResult
                {
                    IsSuccessful = false,
                    Status = HealthStatus.Unhealthy,
                    Description = "Cannot connect to database",
                    ErrorMessage = "Database connection failed"
                };
            }
        });
    }

    private async Task<bool> CheckDatabaseConnectivityAsync()
    {
        try
        {
            return await _dbContext.Database.CanConnectAsync();
        }
        catch
        {
            return false;
        }
    }

    private async Task<ComponentHealthStatus> CheckToolRegistryHealthAsync()
    {
        return await ExecuteHealthCheck("tool_registry", async () =>
        {
            await Task.Delay(1); // Make it async
            var allTools = _toolRegistry.GetAllTools().ToList();
            var status = allTools.Count >= 1 ? HealthStatus.Healthy : HealthStatus.Degraded;

            return new HealthCheckResult
            {
                IsSuccessful = status == HealthStatus.Healthy,
                Status = status,
                Description = $"Tool registry operational with {allTools.Count} tools",
                Details = new Dictionary<string, object>
                {
                    ["tool_count"] = allTools.Count,
                    ["registered_tools"] = allTools.Select(t => t.ToolName).ToList()
                }
            };
        });
    }

    private async Task<ComponentHealthStatus> CheckMCPClientHealthAsync()
    {
        return await ExecuteHealthCheck("mcp_client", async () =>
        {
            // Try to initialize the MCP client (non-blocking)
            var initTask = _mcpClient.InitializeAsync();
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));

            var completedTask = await Task.WhenAny(initTask, timeoutTask);

            if (completedTask == initTask)
            {
                var isInitialized = await initTask;
                return new HealthCheckResult
                {
                    IsSuccessful = isInitialized,
                    Status = isInitialized ? HealthStatus.Healthy : HealthStatus.Degraded,
                    Description = isInitialized ? "MCP Client initialized successfully" : "MCP Client initialization failed (fallback available)",
                    Details = new Dictionary<string, object>
                    {
                        ["initialized"] = isInitialized,
                        ["fallback_available"] = true
                    }
                };
            }
            else
            {
                return new HealthCheckResult
                {
                    IsSuccessful = false,
                    Status = HealthStatus.Degraded,
                    Description = "MCP Client initialization timeout (fallback available)",
                    Details = new Dictionary<string, object>
                    {
                        ["timeout"] = true,
                        ["fallback_available"] = true
                    }
                };
            }
        });
    }

    private async Task<ComponentHealthStatus> CheckMemoryHealthAsync()
    {
        return await ExecuteHealthCheck("memory", async () =>
        {
            await Task.Delay(1); // Make it async
            var process = Process.GetCurrentProcess();
            var memoryUsage = process.WorkingSet64;
            var memoryUsageMB = memoryUsage / 1024 / 1024;

            // Memory thresholds (in MB)
            const long warningThreshold = 500;
            const long criticalThreshold = 1000;

            var status = memoryUsageMB switch
            {
                > criticalThreshold => HealthStatus.Unhealthy,
                > warningThreshold => HealthStatus.Degraded,
                _ => HealthStatus.Healthy
            };

            return new HealthCheckResult
            {
                IsSuccessful = status != HealthStatus.Unhealthy,
                Status = status,
                Description = $"Memory usage: {memoryUsageMB} MB",
                Details = new Dictionary<string, object>
                {
                    ["memory_usage_mb"] = memoryUsageMB,
                    ["memory_usage_bytes"] = memoryUsage,
                    ["warning_threshold_mb"] = warningThreshold,
                    ["critical_threshold_mb"] = criticalThreshold,
                    ["gc_total_memory"] = GC.GetTotalMemory(false),
                    ["gc_collections_gen0"] = GC.CollectionCount(0),
                    ["gc_collections_gen1"] = GC.CollectionCount(1),
                    ["gc_collections_gen2"] = GC.CollectionCount(2)
                }
            };
        });
    }

    private async Task<ComponentHealthStatus> CheckPerformanceHealthAsync()
    {
        return await ExecuteHealthCheck("performance", async () =>
        {
            var metrics = await _metricsService.GetMetricsSummaryAsync(TimeSpan.FromMinutes(5));

            // Performance health thresholds
            var avgResponseTime = metrics.ResponseTimes.Values
                .DefaultIfEmpty(new ResponseTimeMetric())
                .Average(rt => rt.AverageResponseTime.TotalMilliseconds);

            var errorRate = 1.0 - metrics.Business.AgentSuccessRate;

            var status = (avgResponseTime, errorRate) switch
            {
                ( > 5000, _) => HealthStatus.Unhealthy, // Very slow responses
                (_, > 0.1) => HealthStatus.Unhealthy,  // High error rate
                ( > 2000, _) => HealthStatus.Degraded,  // Slow responses
                (_, > 0.05) => HealthStatus.Degraded,  // Moderate error rate
                _ => HealthStatus.Healthy
            };

            return new HealthCheckResult
            {
                IsSuccessful = status == HealthStatus.Healthy,
                Status = status,
                Description = $"Avg response: {avgResponseTime:F0}ms, Error rate: {errorRate:P1}",
                Details = new Dictionary<string, object>
                {
                    ["average_response_time_ms"] = avgResponseTime,
                    ["error_rate"] = errorRate,
                    ["requests_per_minute"] = metrics.Business.AgentResponsesPerMinute,
                    ["active_users"] = metrics.Business.ActiveUsers,
                    ["memory_usage_mb"] = metrics.SystemResources.MemoryUsageMB,
                    ["active_connections"] = metrics.SignalR.ActiveConnections
                }
            };
        });
    }

    // Helper methods
    private static HealthStatus CalculateOverallStatus(IEnumerable<ComponentHealthStatus> components)
    {
        var statuses = components.Select(c => c.Status).ToList();

        if (statuses.Any(s => s == HealthStatus.Unhealthy))
            return HealthStatus.Unhealthy;

        if (statuses.Any(s => s == HealthStatus.Degraded))
            return HealthStatus.Degraded;

        if (statuses.Any(s => s == HealthStatus.Unknown))
            return HealthStatus.Unknown;

        return HealthStatus.Healthy;
    }

    private static string GetApplicationVersion()
    {
        try
        {
            var assembly = Assembly.GetEntryAssembly();
            var version = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                         ?? assembly?.GetName().Version?.ToString()
                         ?? "Unknown";
            return version;
        }
        catch
        {
            return "Unknown";
        }
    }
}

/// <summary>
/// Internal result class for health check operations.
/// Used to standardize health check return values before mapping to ComponentHealthStatus.
/// </summary>
internal class HealthCheckResult
{
    public bool IsSuccessful { get; set; }
    public HealthStatus Status { get; set; }
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Details { get; set; } = new();
    public string? ErrorMessage { get; set; }
}
