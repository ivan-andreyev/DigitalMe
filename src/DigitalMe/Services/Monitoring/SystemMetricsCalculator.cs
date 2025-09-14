using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Monitoring;

/// <summary>
/// Responsible for calculating current system resource metrics.
/// Follows SRP - only handles system resource calculations and sampling.
/// </summary>
public class SystemMetricsCalculator
{
    private readonly ILogger<SystemMetricsCalculator> _logger;

    public SystemMetricsCalculator(ILogger<SystemMetricsCalculator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Calculates current system resource metrics.
    /// </summary>
    public SystemResourceMetrics CalculateSystemResources(string context = "general")
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var memoryUsage = process.WorkingSet64;
            var memoryUsageMb = memoryUsage / 1024 / 1024;
            var gcCollections = GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2);
            var threadCount = process.Threads.Count;

            var metrics = new SystemResourceMetrics
            {
                MemoryUsageMb = memoryUsageMb,
                CpuUsagePercent = CalculateCpuUsage(process),
                GcCollections = gcCollections,
                ThreadCount = threadCount
            };

            _logger.LogDebug("Calculated system metrics for {Context}: Memory={Memory}MB, Threads={Threads}, GC={GC}",
                context, memoryUsageMb, threadCount, gcCollections);

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate system metrics for context {Context}", context);
            return new SystemResourceMetrics(); // Return default values
        }
    }

    /// <summary>
    /// Creates a metric data point for memory usage.
    /// </summary>
    public MetricDataPoint CreateMemoryDataPoint(string context)
    {
        var systemMetrics = CalculateSystemResources(context);

        return new MetricDataPoint
        {
            Timestamp = DateTime.UtcNow,
            Value = systemMetrics.MemoryUsageMb,
            Success = true,
            Metadata = new Dictionary<string, object>
            {
                ["context"] = context,
                ["gcCollections"] = systemMetrics.GcCollections,
                ["threadCount"] = systemMetrics.ThreadCount
            }
        };
    }

    /// <summary>
    /// Calculates database connection metrics summary.
    /// </summary>
    public DatabaseMetrics CalculateDatabaseMetrics(ConcurrentDictionary<string, List<MetricDataPoint>> databaseMetrics,
        DateTime cutoffTime, MetricsAggregator aggregator)
    {
        var dbMetrics = new DatabaseMetrics();

        foreach (var (queryType, dataPoints) in databaseMetrics)
        {
            var windowedData = aggregator.FilterByTimeWindow(dataPoints, cutoffTime);
            if (windowedData.Any())
            {
                dbMetrics.Queries[queryType] = aggregator.AggregateResponseTimes(queryType, windowedData);
            }
        }

        // Note: ActiveConnections and TotalConnections would need to be tracked separately
        // This is a simplified implementation
        dbMetrics.ActiveConnections = 0;
        dbMetrics.TotalConnections = 0;

        return dbMetrics;
    }

    /// <summary>
    /// Calculates overall system metrics from aggregated performance data.
    /// </summary>
    public SystemMetrics CalculateOverallSystemMetrics(PerformanceMetricsSummary performanceMetrics)
    {
        var averageResponseTime = TimeSpan.Zero;
        if (performanceMetrics.ResponseTimes.Any())
        {
            var avgMs = performanceMetrics.ResponseTimes.Values
                .Average(rt => rt.AverageResponseTime.TotalMilliseconds);
            averageResponseTime = TimeSpan.FromMilliseconds(avgMs);
        }

        return new SystemMetrics
        {
            MemoryUsageMb = performanceMetrics.SystemResources.MemoryUsageMb,
            CpuUsagePercent = performanceMetrics.SystemResources.CpuUsagePercent,
            ActiveConnections = performanceMetrics.SignalR.ActiveConnections,
            RequestsPerMinute = performanceMetrics.Business.AgentResponsesPerMinute,
            ErrorRate = 1.0 - performanceMetrics.Business.AgentSuccessRate,
            AverageResponseTime = averageResponseTime,
            DatabaseConnections = performanceMetrics.Database.ActiveConnections
        };
    }

    /// <summary>
    /// Calculates CPU usage percentage. 
    /// Note: This is a simplified calculation - in production, you'd want more sophisticated CPU monitoring.
    /// </summary>
    private double CalculateCpuUsage(Process process)
    {
        try
        {
            // This is a simplified CPU calculation
            // In production, you'd use PerformanceCounter or more sophisticated metrics
            var totalProcessorTime = process.TotalProcessorTime;
            var currentTime = DateTime.UtcNow;

            // Return 0 for now - proper CPU calculation requires time-based sampling
            return 0.0;
        }
        catch
        {
            return 0.0;
        }
    }
}
