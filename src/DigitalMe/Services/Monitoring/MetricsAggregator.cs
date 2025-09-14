using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Monitoring;

/// <summary>
/// Responsible for aggregating raw metric data points into statistical summaries.
/// Follows SRP - only handles data aggregation calculations.
/// </summary>
public class MetricsAggregator
{
    private readonly ILogger<MetricsAggregator> _logger;

    public MetricsAggregator(ILogger<MetricsAggregator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Aggregates response time data points into statistical metrics.
    /// </summary>
    public ResponseTimeMetric AggregateResponseTimes(string operationName, IEnumerable<MetricDataPoint> dataPoints)
    {
        var dataList = dataPoints.ToList();
        if (!dataList.Any())
        {
            return new ResponseTimeMetric { OperationName = operationName };
        }

        var values = dataList.Select(dp => dp.Value).OrderBy(v => v).ToList();
        var p95Index = CalculatePercentileIndex(values.Count, 0.95);

        var metric = new ResponseTimeMetric
        {
            OperationName = operationName,
            TotalRequests = dataList.Count,
            SuccessfulRequests = dataList.Count(dp => dp.Success),
            AverageResponseTime = TimeSpan.FromMilliseconds(values.Average()),
            MinResponseTime = TimeSpan.FromMilliseconds(values.First()),
            MaxResponseTime = TimeSpan.FromMilliseconds(values.Last()),
            P95ResponseTime = TimeSpan.FromMilliseconds(values[p95Index])
        };

        _logger.LogDebug("Aggregated {Count} data points for operation {Operation}: Avg={Avg}ms, P95={P95}ms",
            dataList.Count, operationName, metric.AverageResponseTime.TotalMilliseconds, metric.P95ResponseTime.TotalMilliseconds);

        return metric;
    }

    /// <summary>
    /// Aggregates SignalR event data into connection metrics.
    /// </summary>
    public SignalRMetrics AggregateSignalRMetrics(IEnumerable<SignalREvent> events, TimeSpan timeWindow)
    {
        var eventsList = events.ToList();
        var activeConnections = eventsList
            .Where(e => e.EventType == "Connected")
            .Select(e => e.ConnectionId)
            .Distinct()
            .Count();

        var messagesWithDuration = eventsList.Where(e => e.Duration.HasValue).ToList();
        var averageDeliveryTime = messagesWithDuration.Any()
            ? TimeSpan.FromTicks((long)messagesWithDuration.Average(e => e.Duration!.Value.Ticks))
            : TimeSpan.Zero;

        return new SignalRMetrics
        {
            ActiveConnections = activeConnections,
            TotalConnections = eventsList.Select(e => e.ConnectionId).Distinct().Count(),
            MessagesPerMinute = timeWindow.TotalMinutes > 0
                ? (int)(eventsList.Count(e => e.EventType == "MessageSent") / timeWindow.TotalMinutes)
                : 0,
            AverageMessageDeliveryTime = averageDeliveryTime
        };
    }

    /// <summary>
    /// Aggregates agent response events into business metrics.
    /// </summary>
    public BusinessMetrics AggregateBusinessMetrics(IEnumerable<AgentResponseEvent> agentEvents,
        IEnumerable<UserEngagementEvent> engagementEvents, TimeSpan timeWindow)
    {
        var agentEventsList = agentEvents.ToList();
        var engagementEventsList = engagementEvents.ToList();

        var businessMetrics = new BusinessMetrics
        {
            AgentResponsesPerMinute = (int)(agentEventsList.Count / timeWindow.TotalMinutes),
            AgentSuccessRate = agentEventsList.Count > 0
                ? (double)agentEventsList.Count(e => e.Success) / agentEventsList.Count
                : 1.0,
            ActiveUsers = engagementEventsList.Select(e => e.UserId).Distinct().Count(),
            ConversationsStarted = engagementEventsList.Count(e => e.Action == "conversation_started"),
            ToolUsageCount = agentEventsList
                .Where(e => e.ResponseType != "fallback")
                .GroupBy(e => e.ResponseType)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        _logger.LogDebug("Aggregated business metrics: {Responses} responses/min, {Success:P1} success rate, {Users} active users",
            businessMetrics.AgentResponsesPerMinute, businessMetrics.AgentSuccessRate, businessMetrics.ActiveUsers);

        return businessMetrics;
    }

    /// <summary>
    /// Filters metric data points by time window.
    /// </summary>
    public List<MetricDataPoint> FilterByTimeWindow(IEnumerable<MetricDataPoint> dataPoints, DateTime cutoffTime)
    {
        return dataPoints.Where(dp => dp.Timestamp >= cutoffTime).ToList();
    }

    /// <summary>
    /// Calculates the index for a given percentile in a sorted list.
    /// </summary>
    private static int CalculatePercentileIndex(int count, double percentile)
    {
        if (count == 0) return 0;

        var index = (int)Math.Ceiling(count * percentile) - 1;
        return Math.Max(0, Math.Min(index, count - 1));
    }
}

// Public data structures for cross-class usage
public class MetricDataPoint
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public bool Success { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class SignalREvent
{
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public TimeSpan? Duration { get; set; }
}

public class AgentResponseEvent
{
    public DateTime Timestamp { get; set; }
    public string ResponseType { get; set; } = string.Empty;
    public bool Success { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public int TokenCount { get; set; }
}

public class UserEngagementEvent
{
    public DateTime Timestamp { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}
