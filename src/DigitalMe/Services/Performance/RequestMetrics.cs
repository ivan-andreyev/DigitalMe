namespace DigitalMe.Services.Performance;

/// <summary>
/// Request metrics tracking
/// </summary>
internal class RequestMetrics
{
    public int TotalRequests { get; set; }
    public int FailedRequests { get; set; }
    public TimeSpan TotalResponseTime { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
}