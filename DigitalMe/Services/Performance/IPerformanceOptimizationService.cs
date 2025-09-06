using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace DigitalMe.Services.Performance;

/// <summary>
/// Service for managing performance optimizations across integrations
/// </summary>
public interface IPerformanceOptimizationService
{
    /// <summary>
    /// Get cached response if available
    /// </summary>
    Task<T?> GetCachedResponseAsync<T>(string cacheKey, TimeSpan? expiration = null) where T : class;
    
    /// <summary>
    /// Set cached response
    /// </summary>
    Task SetCachedResponseAsync<T>(string cacheKey, T value, TimeSpan? expiration = null) where T : class;
    
    /// <summary>
    /// Remove cached response
    /// </summary>
    Task RemoveCachedResponseAsync(string cacheKey);
    
    /// <summary>
    /// Get or set cached response with factory
    /// </summary>
    Task<T> GetOrSetCachedResponseAsync<T>(string cacheKey, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class;
    
    /// <summary>
    /// Check if request should be rate limited
    /// </summary>
    Task<bool> ShouldRateLimitAsync(string serviceName, string identifier);
    
    /// <summary>
    /// Record rate limit usage
    /// </summary>
    Task RecordRateLimitUsageAsync(string serviceName, string identifier);
    
    /// <summary>
    /// Get rate limit status
    /// </summary>
    Task<RateLimitStatus> GetRateLimitStatusAsync(string serviceName, string identifier);
    
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

/// <summary>
/// Rate limit status information
/// </summary>
public class RateLimitStatus
{
    public string ServiceName { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
    public int CurrentUsage { get; set; }
    public int Limit { get; set; }
    public DateTime ResetTime { get; set; }
    public TimeSpan TimeUntilReset { get; set; }
    public bool IsLimited { get; set; }
}

/// <summary>
/// HTTP client pool statistics
/// </summary>
public class HttpClientPoolStats
{
    public Dictionary<string, int> ActiveConnections { get; set; } = new();
    public Dictionary<string, int> PooledConnections { get; set; } = new();
    public Dictionary<string, TimeSpan> AverageResponseTime { get; set; } = new();
    public Dictionary<string, int> TotalRequests { get; set; } = new();
    public Dictionary<string, int> FailedRequests { get; set; } = new();
}