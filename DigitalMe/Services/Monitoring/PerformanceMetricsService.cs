using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Monitoring;

/// <summary>
/// Implementation of performance metrics collection service.
/// Thread-safe, high-performance metrics collector using in-memory storage.
/// Now follows SRP - delegates aggregation and calculations to specialized classes.
/// </summary>
public class PerformanceMetricsService : IPerformanceMetricsService
{
    private readonly ILogger<PerformanceMetricsService> _logger;
    private readonly MetricsAggregator _aggregator;
    private readonly SystemMetricsCalculator _systemCalculator;
    private readonly ConcurrentDictionary<string, List<MetricDataPoint>> _responseTimeMetrics = new();
    private readonly ConcurrentDictionary<string, List<MetricDataPoint>> _databaseMetrics = new();
    private readonly ConcurrentDictionary<string, MetricDataPoint> _latestSystemMetrics = new();
    private readonly ConcurrentQueue<SignalREvent> _signalREvents = new();
    private readonly ConcurrentQueue<UserEngagementEvent> _userEngagementEvents = new();
    private readonly ConcurrentQueue<AgentResponseEvent> _agentResponseEvents = new();

    public PerformanceMetricsService(
        ILogger<PerformanceMetricsService> logger,
        MetricsAggregator aggregator,
        SystemMetricsCalculator systemCalculator)
    {
        _logger = logger;
        _aggregator = aggregator;
        _systemCalculator = systemCalculator;
    }

    public void RecordResponseTime(string operationName, TimeSpan duration, bool success = true)
    {
        var dataPoint = new MetricDataPoint
        {
            Timestamp = DateTime.UtcNow,
            Value = duration.TotalMilliseconds,
            Success = success,
            Metadata = new Dictionary<string, object> { ["operation"] = operationName }
        };

        _responseTimeMetrics.AddOrUpdate(operationName, 
            new List<MetricDataPoint> { dataPoint },
            (key, existingList) => 
            {
                lock (existingList)
                {
                    existingList.Add(dataPoint);
                    // Keep only last 1000 entries per operation to prevent memory leaks
                    if (existingList.Count > 1000)
                    {
                        existingList.RemoveRange(0, existingList.Count - 1000);
                    }
                }
                return existingList;
            });

        _logger.LogDebug("Recorded response time for {Operation}: {Duration}ms (Success: {Success})", 
            operationName, duration.TotalMilliseconds, success);
    }

    public void RecordMemoryUsage(string context)
    {
        var dataPoint = _systemCalculator.CreateMemoryDataPoint(context);
        _latestSystemMetrics.AddOrUpdate($"memory_{context}", dataPoint, (key, existing) => dataPoint);
        
        _logger.LogDebug("Recorded memory usage for {Context}: {Memory}MB", context, dataPoint.Value);
    }

    public void RecordDatabaseQuery(string queryType, TimeSpan duration, int recordCount = 0)
    {
        var dataPoint = new MetricDataPoint
        {
            Timestamp = DateTime.UtcNow,
            Value = duration.TotalMilliseconds,
            Success = true,
            Metadata = new Dictionary<string, object>
            {
                ["queryType"] = queryType,
                ["recordCount"] = recordCount
            }
        };

        _databaseMetrics.AddOrUpdate(queryType,
            new List<MetricDataPoint> { dataPoint },
            (key, existingList) =>
            {
                lock (existingList)
                {
                    existingList.Add(dataPoint);
                    if (existingList.Count > 500) // Keep smaller history for DB queries
                    {
                        existingList.RemoveRange(0, existingList.Count - 500);
                    }
                }
                return existingList;
            });

        _logger.LogDebug("Recorded database query {QueryType}: {Duration}ms ({Records} records)", 
            queryType, duration.TotalMilliseconds, recordCount);
    }

    public void RecordSignalRMetric(string eventType, string connectionId, TimeSpan? duration = null)
    {
        var signalREvent = new SignalREvent
        {
            Timestamp = DateTime.UtcNow,
            EventType = eventType,
            ConnectionId = connectionId,
            Duration = duration
        };

        _signalREvents.Enqueue(signalREvent);
        
        // Keep only last 1000 SignalR events
        if (_signalREvents.Count > 1000)
        {
            _signalREvents.TryDequeue(out _);
        }

        _logger.LogDebug("Recorded SignalR event {EventType} for connection {ConnectionId}", 
            eventType, connectionId);
    }

    public void RecordAgentResponse(string responseType, bool success, TimeSpan processingTime, int tokenCount = 0)
    {
        var agentEvent = new AgentResponseEvent
        {
            Timestamp = DateTime.UtcNow,
            ResponseType = responseType,
            Success = success,
            ProcessingTime = processingTime,
            TokenCount = tokenCount
        };

        _agentResponseEvents.Enqueue(agentEvent);
        
        // Keep only last 1000 agent events
        if (_agentResponseEvents.Count > 1000)
        {
            _agentResponseEvents.TryDequeue(out _);
        }

        _logger.LogDebug("Recorded agent response {ResponseType}: {ProcessingTime}ms (Success: {Success}, Tokens: {Tokens})", 
            responseType, processingTime.TotalMilliseconds, success, tokenCount);
    }

    public void RecordUserEngagement(string userId, string action, Dictionary<string, object>? metadata = null)
    {
        var engagementEvent = new UserEngagementEvent
        {
            Timestamp = DateTime.UtcNow,
            UserId = userId,
            Action = action,
            Metadata = metadata ?? new Dictionary<string, object>()
        };

        _userEngagementEvents.Enqueue(engagementEvent);
        
        // Keep only last 2000 engagement events
        if (_userEngagementEvents.Count > 2000)
        {
            _userEngagementEvents.TryDequeue(out _);
        }

        _logger.LogDebug("Recorded user engagement for {UserId}: {Action}", userId, action);
    }

    public async Task<PerformanceMetricsSummary> GetMetricsSummaryAsync(TimeSpan? timeWindow = null)
    {
        var window = timeWindow ?? TimeSpan.FromMinutes(15);
        var cutoffTime = DateTime.UtcNow - window;

        var summary = new PerformanceMetricsSummary
        {
            TimeWindow = window
        };

        try
        {
            // Delegate response time aggregation
            await AggregateResponseTimeMetrics(summary, cutoffTime);

            // Delegate system resource calculations
            CalculateSystemResourceMetrics(summary);

            // Delegate database metrics aggregation
            AggregateDatabaseMetrics(summary, cutoffTime);

            // Delegate SignalR metrics aggregation
            var recentSignalREvents = _signalREvents.Where(e => e.Timestamp >= cutoffTime);
            summary.SignalR = _aggregator.AggregateSignalRMetrics(recentSignalREvents, window);

            // Delegate business metrics aggregation
            var recentAgentEvents = _agentResponseEvents.Where(e => e.Timestamp >= cutoffTime);
            var recentEngagementEvents = _userEngagementEvents.Where(e => e.Timestamp >= cutoffTime);
            summary.Business = _aggregator.AggregateBusinessMetrics(recentAgentEvents, recentEngagementEvents, window);

            _logger.LogInformation("Generated performance metrics summary for {TimeWindow} window", window);
            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate performance metrics summary");
            // Return partial summary with available data
            return summary;
        }
    }

    private async Task AggregateResponseTimeMetrics(PerformanceMetricsSummary summary, DateTime cutoffTime)
    {
        await Task.Run(() =>
        {
            foreach (var (operationName, dataPoints) in _responseTimeMetrics)
            {
                List<MetricDataPoint> windowedData;
                lock (dataPoints)
                {
                    windowedData = _aggregator.FilterByTimeWindow(dataPoints, cutoffTime);
                }

                if (windowedData.Any())
                {
                    summary.ResponseTimes[operationName] = _aggregator.AggregateResponseTimes(operationName, windowedData);
                }
            }
        });
    }

    private void CalculateSystemResourceMetrics(PerformanceMetricsSummary summary)
    {
        RecordMemoryUsage("metrics_collection"); // Update current metrics
        
        var latestMemoryMetric = _latestSystemMetrics.Values
            .Where(m => m.Metadata.ContainsKey("context") && 
                       m.Metadata["context"].ToString() == "metrics_collection")
            .OrderByDescending(m => m.Timestamp)
            .FirstOrDefault();

        if (latestMemoryMetric != null)
        {
            summary.SystemResources = new SystemResourceMetrics
            {
                MemoryUsageMB = (long)latestMemoryMetric.Value,
                GcCollections = (int)(latestMemoryMetric.Metadata.GetValueOrDefault("gcCollections", 0)),
                ThreadCount = (int)(latestMemoryMetric.Metadata.GetValueOrDefault("threadCount", 0))
            };
        }
        else
        {
            summary.SystemResources = _systemCalculator.CalculateSystemResources("fallback");
        }
    }

    private void AggregateDatabaseMetrics(PerformanceMetricsSummary summary, DateTime cutoffTime)
    {
        summary.Database = _systemCalculator.CalculateDatabaseMetrics(_databaseMetrics, cutoffTime, _aggregator);
    }

    public IPerformanceTrackingScope StartTracking(string operationName, string? context = null)
    {
        return new PerformanceTrackingScope(this, operationName, context);
    }
}

/// <summary>
/// Implementation of performance tracking scope with automatic disposal.
/// </summary>
internal class PerformanceTrackingScope : IPerformanceTrackingScope
{
    private readonly IPerformanceMetricsService _metricsService;
    private readonly string _operationName;
    private readonly string? _context;
    private readonly Stopwatch _stopwatch;
    private readonly Dictionary<string, object> _metadata = new();
    private bool _disposed;
    private bool _resultRecorded;

    public PerformanceTrackingScope(IPerformanceMetricsService metricsService, string operationName, string? context = null)
    {
        _metricsService = metricsService;
        _operationName = operationName;
        _context = context;
        _stopwatch = Stopwatch.StartNew();
        
        if (!string.IsNullOrEmpty(context))
        {
            _metadata["context"] = context;
        }
    }

    public void RecordSuccess(Dictionary<string, object>? metadata = null)
    {
        if (_resultRecorded) return;
        
        if (metadata != null)
        {
            foreach (var kvp in metadata)
            {
                _metadata[kvp.Key] = kvp.Value;
            }
        }
        
        _metricsService.RecordResponseTime(_operationName, _stopwatch.Elapsed, success: true);
        _resultRecorded = true;
    }

    public void RecordFailure(Exception? exception = null, Dictionary<string, object>? metadata = null)
    {
        if (_resultRecorded) return;
        
        if (metadata != null)
        {
            foreach (var kvp in metadata)
            {
                _metadata[kvp.Key] = kvp.Value;
            }
        }
        
        if (exception != null)
        {
            _metadata["exception"] = exception.GetType().Name;
            _metadata["error_message"] = exception.Message;
        }
        
        _metricsService.RecordResponseTime(_operationName, _stopwatch.Elapsed, success: false);
        _resultRecorded = true;
    }

    public void AddMetadata(string key, object value)
    {
        _metadata[key] = value;
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _stopwatch.Stop();
        
        // If no explicit result was recorded, default to success
        if (!_resultRecorded)
        {
            RecordSuccess();
        }
        
        _disposed = true;
    }
}