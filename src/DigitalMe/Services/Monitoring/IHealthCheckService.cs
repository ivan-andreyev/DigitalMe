namespace DigitalMe.Services.Monitoring;

/// <summary>
/// Enhanced health checking service that provides detailed system health information.
/// Extends the basic health checker with detailed component status and performance metrics.
/// </summary>
public interface IHealthCheckService
{
    /// <summary>
    /// Gets comprehensive system health status including all components.
    /// </summary>
    Task<SystemHealthStatus> GetSystemHealthAsync();

    /// <summary>
    /// Gets health status for a specific component.
    /// </summary>
    Task<ComponentHealthStatus> GetComponentHealthAsync(string componentName);

    /// <summary>
    /// Gets readiness status (can the system handle requests).
    /// </summary>
    Task<ReadinessStatus> GetReadinessAsync();

    /// <summary>
    /// Gets liveness status (is the system running).
    /// </summary>
    Task<LivenessStatus> GetLivenessAsync();

    /// <summary>
    /// Registers a custom health check.
    /// </summary>
    void RegisterHealthCheck(string name, Func<Task<ComponentHealthStatus>> healthCheck);
}

/// <summary>
/// Overall system health status.
/// </summary>
public class SystemHealthStatus
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public HealthStatus OverallStatus { get; set; }
    public string Version { get; set; } = string.Empty;
    public TimeSpan Uptime { get; set; }
    public Dictionary<string, ComponentHealthStatus> Components { get; set; } = new();
    public SystemMetrics Metrics { get; set; } = new();
    public List<HealthAlert> Alerts { get; set; } = new();
}

/// <summary>
/// Health status for individual components.
/// </summary>
public class ComponentHealthStatus
{
    public string ComponentName { get; set; } = string.Empty;
    public HealthStatus Status { get; set; }
    public string? Description { get; set; }
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;
    public TimeSpan? ResponseTime { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Readiness check result (Kubernetes-style).
/// </summary>
public class ReadinessStatus
{
    public bool IsReady { get; set; }
    public List<string> ReadyComponents { get; set; } = new();
    public List<string> NotReadyComponents { get; set; } = new();
    public string? Reason { get; set; }
}

/// <summary>
/// Liveness check result (Kubernetes-style).
/// </summary>
public class LivenessStatus
{
    public bool IsAlive { get; set; }
    public DateTime LastHeartbeat { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// System-wide metrics snapshot.
/// </summary>
public class SystemMetrics
{
    public long MemoryUsageMB { get; set; }
    public double CpuUsagePercent { get; set; }
    public int ActiveConnections { get; set; }
    public int RequestsPerMinute { get; set; }
    public double ErrorRate { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public int DatabaseConnections { get; set; }
}

/// <summary>
/// Health alert for monitoring systems.
/// </summary>
public class HealthAlert
{
    public string Component { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Health status enumeration.
/// </summary>
public enum HealthStatus
{
    Healthy = 0,
    Degraded = 1,
    Unhealthy = 2,
    Unknown = 3
}

/// <summary>
/// Alert severity levels.
/// </summary>
public enum AlertSeverity
{
    Info = 0,
    Warning = 1,
    Critical = 2,
    Emergency = 3
}
