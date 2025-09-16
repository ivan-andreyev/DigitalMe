using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Performance;

/// <summary>
/// Facade service that provides all performance optimization capabilities
/// Delegates to specialized services for each concern
/// </summary>
public class PerformanceOptimizationService : IPerformanceOptimizationService
{
    private readonly ICacheService _cacheService;
    private readonly IRateLimitService _rateLimitService;
    private readonly IBatchProcessingService _batchProcessingService;
    private readonly IPerformanceMonitoringService _monitoringService;
    private readonly ILogger<PerformanceOptimizationService> _logger;

    public PerformanceOptimizationService(
        ICacheService cacheService,
        IRateLimitService rateLimitService,
        IBatchProcessingService batchProcessingService,
        IPerformanceMonitoringService monitoringService,
        ILogger<PerformanceOptimizationService> logger)
    {
        _cacheService = cacheService;
        _rateLimitService = rateLimitService;
        _batchProcessingService = batchProcessingService;
        _monitoringService = monitoringService;
        _logger = logger;
    }

    #region Caching - Delegate to CacheService

    public async Task<T?> GetCachedResponseAsync<T>(string cacheKey, TimeSpan? expiration = null) where T : class
    {
        return await _cacheService.GetCachedResponseAsync<T>(cacheKey, expiration);
    }

    public async Task SetCachedResponseAsync<T>(string cacheKey, T value, TimeSpan? expiration = null) where T : class
    {
        await _cacheService.SetCachedResponseAsync(cacheKey, value, expiration);
    }

    public async Task RemoveCachedResponseAsync(string cacheKey)
    {
        await _cacheService.RemoveCachedResponseAsync(cacheKey);
    }

    public async Task<T> GetOrSetCachedResponseAsync<T>(string cacheKey, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class
    {
        return await _cacheService.GetOrSetCachedResponseAsync(cacheKey, factory, expiration);
    }

    #endregion

    #region Rate Limiting - Delegate to RateLimitService

    public async Task<bool> ShouldRateLimitAsync(string serviceName, string identifier)
    {
        return await _rateLimitService.ShouldRateLimitAsync(serviceName, identifier);
    }

    public async Task RecordRateLimitUsageAsync(string serviceName, string identifier)
    {
        await _rateLimitService.RecordRateLimitUsageAsync(serviceName, identifier);
    }

    public async Task<RateLimitStatus> GetRateLimitStatusAsync(string serviceName, string identifier)
    {
        return await _rateLimitService.GetRateLimitStatusAsync(serviceName, identifier);
    }

    #endregion

    #region Batch Operations - Delegate to BatchProcessingService

    public async Task<IEnumerable<TResult>> BatchOperationsAsync<TInput, TResult>(
        IEnumerable<TInput> inputs,
        Func<IEnumerable<TInput>, Task<IEnumerable<TResult>>> batchProcessor,
        int batchSize = 50)
    {
        return await _batchProcessingService.BatchOperationsAsync(inputs, batchProcessor, batchSize);
    }

    #endregion

    #region Performance Monitoring - Delegate to PerformanceMonitoringService

    public async Task<HttpClientPoolStats> GetHttpClientPoolStatsAsync()
    {
        return await _monitoringService.GetHttpClientPoolStatsAsync();
    }

    public void RecordRequestMetrics(string serviceName, TimeSpan responseTime, bool success)
    {
        _monitoringService.RecordRequestMetrics(serviceName, responseTime, success);
    }

    #endregion
}
