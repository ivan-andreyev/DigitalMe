using System.Diagnostics;

namespace DigitalMe.Services.Monitoring;

/// <summary>
/// Service for collecting and tracking performance metrics across the application.
/// Provides response time tracking, memory usage monitoring, and business metrics.
/// </summary>
public interface IPerformanceMetricsService
{
    /// <summary>
    /// Records a response time metric for a specific operation.
    /// </summary>
    void RecordResponseTime(string operationName, TimeSpan duration, bool success = true);

    /// <summary>
    /// Records memory usage at a specific point.
    /// </summary>
    void RecordMemoryUsage(string context);

    /// <summary>
    /// Records database query performance metrics.
    /// </summary>
    void RecordDatabaseQuery(string queryType, TimeSpan duration, int recordCount = 0);

    /// <summary>
    /// Records SignalR connection metrics.
    /// </summary>
    void RecordSignalRMetric(string eventType, string connectionId, TimeSpan? duration = null);

    /// <summary>
    /// Records agent response quality metrics.
    /// </summary>
    void RecordAgentResponse(string responseType, bool success, TimeSpan processingTime, int tokenCount = 0);

    /// <summary>
    /// Records user engagement metrics.
    /// </summary>
    void RecordUserEngagement(string userId, string action, Dictionary<string, object>? metadata = null);

    /// <summary>
    /// Gets current performance metrics summary.
    /// </summary>
    Task<PerformanceMetricsSummary> GetMetricsSummaryAsync(TimeSpan? timeWindow = null);

    /// <summary>
    /// Creates a performance tracking scope that automatically records timing.
    /// </summary>
    IPerformanceTrackingScope StartTracking(string operationName, string? context = null);
}

/// <summary>
/// Summary of current performance metrics.
/// </summary>
public class PerformanceMetricsSummary
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan TimeWindow { get; set; }

    // Response Time Metrics
    public Dictionary<string, ResponseTimeMetric> ResponseTimes { get; set; } = new();

    // System Resource Metrics
    public SystemResourceMetrics SystemResources { get; set; } = new();

    // Database Performance Metrics
    public DatabaseMetrics Database { get; set; } = new();

    // SignalR Connection Metrics
    public SignalRMetrics SignalR { get; set; } = new();

    // Business Metrics
    public BusinessMetrics Business { get; set; } = new();
}

public class ResponseTimeMetric
{
    public string OperationName { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public TimeSpan MinResponseTime { get; set; }
    public TimeSpan MaxResponseTime { get; set; }
    public TimeSpan P95ResponseTime { get; set; }
    public double ErrorRate => TotalRequests > 0 ? (double)(TotalRequests - SuccessfulRequests) / TotalRequests : 0;
}

public class SystemResourceMetrics
{
    public long MemoryUsageMB { get; set; }
    public double CpuUsagePercent { get; set; }
    public int GcCollections { get; set; }
    public int ThreadCount { get; set; }
}

public class DatabaseMetrics
{
    public Dictionary<string, ResponseTimeMetric> Queries { get; set; } = new();
    public int ActiveConnections { get; set; }
    public int TotalConnections { get; set; }
}

public class SignalRMetrics
{
    public int ActiveConnections { get; set; }
    public int TotalConnections { get; set; }
    public int MessagesPerMinute { get; set; }
    public TimeSpan AverageMessageDeliveryTime { get; set; }
}

public class BusinessMetrics
{
    public int AgentResponsesPerMinute { get; set; }
    public double AgentSuccessRate { get; set; }
    public int ActiveUsers { get; set; }
    public int ConversationsStarted { get; set; }
    public Dictionary<string, int> ToolUsageCount { get; set; } = new();
}

/// <summary>
/// Disposable scope for automatic performance tracking.
/// </summary>
public interface IPerformanceTrackingScope : IDisposable
{
    /// <summary>
    /// Records success and optionally adds metadata.
    /// </summary>
    void RecordSuccess(Dictionary<string, object>? metadata = null);

    /// <summary>
    /// Records failure and optionally adds error information.
    /// </summary>
    void RecordFailure(Exception? exception = null, Dictionary<string, object>? metadata = null);

    /// <summary>
    /// Adds metadata to the tracking scope.
    /// </summary>
    void AddMetadata(string key, object value);
}
