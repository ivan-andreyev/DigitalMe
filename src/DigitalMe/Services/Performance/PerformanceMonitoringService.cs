using System.Collections.Concurrent;

namespace DigitalMe.Services.Performance;

/// <summary>
/// Service for monitoring performance metrics and HTTP client pool statistics
/// </summary>
public class PerformanceMonitoringService : IPerformanceMonitoringService
{
    private readonly ConcurrentDictionary<string, RequestMetrics> _requestMetrics;

    public PerformanceMonitoringService()
    {
        _requestMetrics = new ConcurrentDictionary<string, RequestMetrics>();
    }

    public async Task<HttpClientPoolStats> GetHttpClientPoolStatsAsync()
    {
        var stats = new HttpClientPoolStats();

        // Aggregate metrics from our tracking
        foreach (var kvp in _requestMetrics)
        {
            var serviceName = kvp.Key.Replace("_requests", "");
            var metrics = kvp.Value;

            stats.TotalRequests[serviceName] = metrics.TotalRequests;
            stats.FailedRequests[serviceName] = metrics.FailedRequests;
            stats.AverageResponseTime[serviceName] = metrics.AverageResponseTime;
        }

        return stats;
    }

    public void RecordRequestMetrics(string serviceName, TimeSpan responseTime, bool success)
    {
        var key = $"{serviceName}_requests";
        _requestMetrics.AddOrUpdate(key,
            new RequestMetrics
            {
                TotalRequests = 1,
                FailedRequests = success ? 0 : 1,
                TotalResponseTime = responseTime,
                AverageResponseTime = responseTime
            },
            (k, v) =>
            {
                v.TotalRequests++;
                if (!success)
                {
                    v.FailedRequests++;
                }
                v.TotalResponseTime = v.TotalResponseTime.Add(responseTime);
                v.AverageResponseTime = TimeSpan.FromTicks(v.TotalResponseTime.Ticks / v.TotalRequests);
                return v;
            });
    }
}