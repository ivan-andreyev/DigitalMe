namespace DigitalMe.Services.Performance;

/// <summary>
/// Service for monitoring performance metrics and HTTP client pool statistics
/// </summary>
public interface IPerformanceMonitoringService
{
    /// <summary>
    /// Get HTTP client pool statistics
    /// </summary>
    Task<HttpClientPoolStats> GetHttpClientPoolStatsAsync();

    /// <summary>
    /// Record request metrics for performance tracking
    /// </summary>
    void RecordRequestMetrics(string serviceName, TimeSpan responseTime, bool success);
}