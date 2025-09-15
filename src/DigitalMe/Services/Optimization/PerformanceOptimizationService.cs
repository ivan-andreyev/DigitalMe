using DigitalMe.Data.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Optimization;

/// <summary>
/// Performance optimization service for Ivan-Level Agent system.
/// Provides caching, memory management, and performance monitoring capabilities.
/// </summary>
public interface IPerformanceOptimizationService
{
    /// <summary>
    /// Gets cached data or executes the provided function and caches the result
    /// </summary>
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiry = null);

    /// <summary>
    /// Clears performance caches to free memory
    /// </summary>
    void ClearCaches();

    /// <summary>
    /// Gets current memory usage statistics
    /// </summary>
    MemoryUsageStats GetMemoryUsage();

    /// <summary>
    /// Optimizes memory usage by running garbage collection
    /// </summary>
    void OptimizeMemoryUsage();

    /// <summary>
    /// Gets performance metrics for monitoring
    /// </summary>
    PerformanceMetrics GetPerformanceMetrics();

    /// <summary>
    /// Checks if rate limit should be applied for the given key
    /// </summary>
    Task<bool> ShouldRateLimitAsync(string category, string key, int requestsPerMinute = 100);
}

/// <summary>
/// Memory usage statistics
/// </summary>
public class MemoryUsageStats
{
    public long TotalMemoryBytes { get; set; }
    public long Gen0Collections { get; set; }
    public long Gen1Collections { get; set; }
    public long Gen2Collections { get; set; }
    public double MemoryUsageMB => TotalMemoryBytes / (1024.0 * 1024.0);
}

/// <summary>
/// Performance metrics for system monitoring
/// </summary>
public class PerformanceMetrics
{
    public int CacheHitCount { get; set; }
    public int CacheMissCount { get; set; }
    public double CacheHitRatio => CacheHitCount + CacheMissCount > 0 ? (double)CacheHitCount / (CacheHitCount + CacheMissCount) : 0;
    public TimeSpan AverageResponseTime { get; set; }
    public int ActiveServiceInstances { get; set; }
    public MemoryUsageStats MemoryStats { get; set; } = new();
}

/// <summary>
/// Implementation of performance optimization service
/// </summary>
public class PerformanceOptimizationService : IPerformanceOptimizationService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<PerformanceOptimizationService> _logger;
    private int _cacheHitCount = 0;
    private int _cacheMissCount = 0;
    private readonly List<TimeSpan> _responseTimes = new();

    // Cache configuration
    private static readonly MemoryCacheEntryOptions DefaultCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
        SlidingExpiration = TimeSpan.FromMinutes(5),
        Priority = CacheItemPriority.Normal
    };

    // Performance-critical cache keys
    private static readonly Dictionary<string, TimeSpan> CacheKeyExpiryMap = new()
    {
        ["ivan_personality"] = TimeSpan.FromHours(24), // Personality data rarely changes
        ["vocabulary_preferences"] = TimeSpan.FromHours(2), // Context vocabulary preferences
        ["system_prompts"] = TimeSpan.FromHours(12), // System prompts are relatively stable
        ["communication_styles"] = TimeSpan.FromMinutes(30), // Communication styles can vary
        ["file_processing_cache"] = TimeSpan.FromMinutes(10) // File processing results
    };

    public PerformanceOptimizationService(IMemoryCache cache, ILogger<PerformanceOptimizationService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiry = null)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // Try to get from cache first
            if (_cache.TryGetValue(key, out T? cachedValue) && cachedValue != null)
            {
                Interlocked.Increment(ref _cacheHitCount);
                _logger.LogDebug("Cache hit for key: {Key}", key);

                RecordResponseTime(DateTime.UtcNow - startTime);
                return cachedValue;
            }

            // Cache miss - execute the function
            Interlocked.Increment(ref _cacheMissCount);
            _logger.LogDebug("Cache miss for key: {Key}, executing function", key);

            var result = await getItem();

            // Cache the result
            var cacheOptions = CreateCacheOptions(key, expiry);
            _cache.Set(key, result, cacheOptions);

            _logger.LogDebug("Cached result for key: {Key} with expiry: {Expiry}",
                key, cacheOptions.AbsoluteExpirationRelativeToNow);

            RecordResponseTime(DateTime.UtcNow - startTime);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetOrSetAsync for key: {Key}", key);

            // If caching fails, still try to execute the function
            var result = await getItem();
            RecordResponseTime(DateTime.UtcNow - startTime);
            return result;
        }
    }

    public void ClearCaches()
    {
        _logger.LogInformation("Clearing performance caches");

        try
        {
            // Clear the memory cache
            if (_cache is MemoryCache memoryCache)
            {
                memoryCache.Clear();
            }

            // Reset performance counters
            _cacheHitCount = 0;
            _cacheMissCount = 0;
            _responseTimes.Clear();

            _logger.LogInformation("Performance caches cleared successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing caches");
        }
    }

    public MemoryUsageStats GetMemoryUsage()
    {
        return new MemoryUsageStats
        {
            TotalMemoryBytes = GC.GetTotalMemory(false),
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2)
        };
    }

    public void OptimizeMemoryUsage()
    {
        _logger.LogInformation("Optimizing memory usage");

        var beforeMemory = GC.GetTotalMemory(false);

        try
        {
            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var afterMemory = GC.GetTotalMemory(false);
            var memoryFreed = beforeMemory - afterMemory;

            _logger.LogInformation("Memory optimization completed. Freed: {MemoryFreed:F2} MB",
                memoryFreed / (1024.0 * 1024.0));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during memory optimization");
        }
    }

    public PerformanceMetrics GetPerformanceMetrics()
    {
        var averageResponseTime = _responseTimes.Count > 0
            ? TimeSpan.FromMilliseconds(_responseTimes.Average(t => t.TotalMilliseconds))
            : TimeSpan.Zero;

        return new PerformanceMetrics
        {
            CacheHitCount = _cacheHitCount,
            CacheMissCount = _cacheMissCount,
            AverageResponseTime = averageResponseTime,
            ActiveServiceInstances = 1, // This could be extended to track multiple instances
            MemoryStats = GetMemoryUsage()
        };
    }

    private MemoryCacheEntryOptions CreateCacheOptions(string key, TimeSpan? expiry)
    {
        // Determine expiry based on key pattern or provided value
        TimeSpan expiryTime = expiry ?? GetExpiryForKey(key);

        return new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiryTime,
            SlidingExpiration = TimeSpan.FromMinutes(Math.Min(expiryTime.TotalMinutes / 3, 60)), // 1/3 of absolute expiry, max 1 hour
            Priority = GetCachePriority(key),
            PostEvictionCallbacks = { new PostEvictionCallbackRegistration
            {
                EvictionCallback = OnCacheEviction,
                State = key
            }}
        };
    }

    private TimeSpan GetExpiryForKey(string key)
    {
        // Check for known key patterns
        foreach (var kvp in CacheKeyExpiryMap)
        {
            if (key.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Value;
            }
        }

        // Default expiry
        return DefaultCacheOptions.AbsoluteExpirationRelativeToNow ?? TimeSpan.FromMinutes(30);
    }

    private CacheItemPriority GetCachePriority(string key)
    {
        // High priority for core system data
        if (key.Contains("ivan_personality") || key.Contains("system_prompt"))
        {
            return CacheItemPriority.High;
        }

        // Low priority for temporary processing results
        if (key.Contains("temp") || key.Contains("processing"))
        {
            return CacheItemPriority.Low;
        }

        return CacheItemPriority.Normal;
    }

    private void OnCacheEviction(object key, object? value, EvictionReason reason, object? state)
    {
        _logger.LogDebug("Cache entry evicted: Key={Key}, Reason={Reason}", key, reason);
    }

    private void RecordResponseTime(TimeSpan responseTime)
    {
        lock (_responseTimes)
        {
            _responseTimes.Add(responseTime);

            // Keep only the last 1000 response times to avoid memory growth
            if (_responseTimes.Count > 1000)
            {
                _responseTimes.RemoveAt(0);
            }
        }
    }

    public async Task<bool> ShouldRateLimitAsync(string category, string key, int requestsPerMinute = 100)
    {
        var rateLimitKey = $"rate_limit_{category}_{key}";
        var currentMinute = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");
        var fullKey = $"{rateLimitKey}_{currentMinute}";

        try
        {
            // Get current request count for this minute
            if (_cache.TryGetValue(fullKey, out int currentCount))
            {
                if (currentCount >= requestsPerMinute)
                {
                    _logger.LogWarning("Rate limit exceeded for {Category}:{Key} - {Count}/{Limit} requests per minute",
                        category, key, currentCount, requestsPerMinute);
                    return true; // Rate limit exceeded
                }

                // Increment counter
                _cache.Set(fullKey, currentCount + 1, TimeSpan.FromMinutes(2)); // Keep for 2 minutes to handle clock skew
            }
            else
            {
                // First request in this minute
                _cache.Set(fullKey, 1, TimeSpan.FromMinutes(2));
            }

            return false; // Rate limit not exceeded
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit for {Category}:{Key}", category, key);
            return false; // Allow request on error to avoid blocking legitimate traffic
        }
    }
}