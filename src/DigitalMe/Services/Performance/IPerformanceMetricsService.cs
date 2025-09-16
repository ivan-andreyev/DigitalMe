namespace DigitalMe.Services.Performance;

/// <summary>
/// Service for performance metrics and analytics following ISP compliance.
/// Focused interface for performance monitoring functionality only.
/// </summary>
public interface IPerformanceMetricsService
{
    /// <summary>
    /// Batch multiple operations into single request
    /// </summary>
    Task<IEnumerable<TResult>> BatchOperationsAsync<TInput, TResult>(
        IEnumerable<TInput> inputs,
        Func<IEnumerable<TInput>, Task<IEnumerable<TResult>>> batchProcessor,
        int batchSize = 50);

    /// <summary>
    /// Get HTTP client pool statistics
    /// </summary>
    Task<HttpClientPoolStats> GetHttpClientPoolStatsAsync();
}