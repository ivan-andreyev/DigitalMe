using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DigitalMe.Configuration;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace DigitalMe.Services.Performance;

/// <summary>
/// Service for managing performance optimizations across integrations
/// </summary>
public class PerformanceOptimizationService : IPerformanceOptimizationService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<PerformanceOptimizationService> _logger;
    private readonly IntegrationSettings _settings;
    
    // Rate limiting tracking
    private readonly ConcurrentDictionary<string, RateLimitBucket> _rateLimitBuckets;
    
    // Performance metrics
    private readonly ConcurrentDictionary<string, RequestMetrics> _requestMetrics;
    
    public PerformanceOptimizationService(
        IMemoryCache cache,
        ILogger<PerformanceOptimizationService> logger,
        IOptions<IntegrationSettings> integrationSettings)
    {
        _cache = cache;
        _logger = logger;
        _settings = integrationSettings.Value;
        _rateLimitBuckets = new ConcurrentDictionary<string, RateLimitBucket>();
        _requestMetrics = new ConcurrentDictionary<string, RequestMetrics>();
    }

    #region Caching

    public async Task<T?> GetCachedResponseAsync<T>(string cacheKey, TimeSpan? expiration = null) where T : class
    {
        try
        {
            if (_cache.TryGetValue(cacheKey, out var cachedValue) && cachedValue is T result)
            {
                _logger.LogDebug("Cache hit for key: {CacheKey}", cacheKey);
                return result;
            }
            
            _logger.LogDebug("Cache miss for key: {CacheKey}", cacheKey);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving cached response for key: {CacheKey}", cacheKey);
            return null;
        }
    }

    public async Task SetCachedResponseAsync<T>(string cacheKey, T value, TimeSpan? expiration = null) where T : class
    {
        try
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(15),
                SlidingExpiration = TimeSpan.FromMinutes(5),
                Priority = CacheItemPriority.Normal
            };

            _cache.Set(cacheKey, value, options);
            _logger.LogDebug("Cached response for key: {CacheKey}, expiration: {Expiration}", 
                cacheKey, expiration ?? TimeSpan.FromMinutes(15));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error setting cached response for key: {CacheKey}", cacheKey);
        }
    }

    public async Task RemoveCachedResponseAsync(string cacheKey)
    {
        try
        {
            _cache.Remove(cacheKey);
            _logger.LogDebug("Removed cached response for key: {CacheKey}", cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error removing cached response for key: {CacheKey}", cacheKey);
        }
    }

    public async Task<T> GetOrSetCachedResponseAsync<T>(string cacheKey, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class
    {
        var cached = await GetCachedResponseAsync<T>(cacheKey, expiration);
        if (cached != null)
        {
            return cached;
        }

        var result = await factory();
        await SetCachedResponseAsync(cacheKey, result, expiration);
        return result;
    }

    #endregion

    #region Rate Limiting

    public Task<bool> ShouldRateLimitAsync(string serviceName, string identifier)
    {
        var key = $"{serviceName}:{identifier}";
        var bucket = _rateLimitBuckets.GetOrAdd(key, _ => new RateLimitBucket(serviceName, identifier, _settings));
        
        return Task.FromResult(bucket.ShouldRateLimit());
    }

    public Task RecordRateLimitUsageAsync(string serviceName, string identifier)
    {
        var key = $"{serviceName}:{identifier}";
        var bucket = _rateLimitBuckets.GetOrAdd(key, _ => new RateLimitBucket(serviceName, identifier, _settings));
        
        bucket.RecordUsage();
        
        // Update metrics
        var metricsKey = $"{serviceName}_requests";
        _requestMetrics.AddOrUpdate(metricsKey, 
            new RequestMetrics { TotalRequests = 1 },
            (k, v) => { v.TotalRequests++; return v; });
            
        return Task.CompletedTask;
    }

    public Task<RateLimitStatus> GetRateLimitStatusAsync(string serviceName, string identifier)
    {
        var key = $"{serviceName}:{identifier}";
        var bucket = _rateLimitBuckets.GetOrAdd(key, _ => new RateLimitBucket(serviceName, identifier, _settings));
        
        return Task.FromResult(bucket.GetStatus());
    }

    #endregion

    #region Bulk Operations

    public async Task<IEnumerable<TResult>> BatchOperationsAsync<TInput, TResult>(
        IEnumerable<TInput> inputs, 
        Func<IEnumerable<TInput>, Task<IEnumerable<TResult>>> batchProcessor, 
        int batchSize = 50)
    {
        var results = new List<TResult>();
        var inputList = inputs.ToList();
        
        _logger.LogInformation("Processing {TotalItems} items in batches of {BatchSize}", 
            inputList.Count, batchSize);

        for (int i = 0; i < inputList.Count; i += batchSize)
        {
            var batch = inputList.Skip(i).Take(batchSize);
            var batchNumber = (i / batchSize) + 1;
            var totalBatches = (int)Math.Ceiling((double)inputList.Count / batchSize);
            
            _logger.LogDebug("Processing batch {BatchNumber}/{TotalBatches}", batchNumber, totalBatches);
            
            try
            {
                var batchResults = await batchProcessor(batch);
                results.AddRange(batchResults);
                
                // Small delay between batches to be respectful to APIs
                if (i + batchSize < inputList.Count)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(100));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing batch {BatchNumber}/{TotalBatches}", 
                    batchNumber, totalBatches);
                throw;
            }
        }
        
        _logger.LogInformation("Completed processing {TotalItems} items, got {ResultCount} results", 
            inputList.Count, results.Count);
        
        return results;
    }

    #endregion

    #region Performance Monitoring

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
                if (!success) v.FailedRequests++;
                v.TotalResponseTime = v.TotalResponseTime.Add(responseTime);
                v.AverageResponseTime = TimeSpan.FromTicks(v.TotalResponseTime.Ticks / v.TotalRequests);
                return v;
            });
    }

    #endregion
}

/// <summary>
/// Rate limiting bucket for token bucket algorithm
/// </summary>
internal class RateLimitBucket
{
    private readonly string _serviceName;
    private readonly string _identifier;
    private readonly int _maxTokens;
    private readonly TimeSpan _refillInterval;
    private readonly object _lock = new object();
    
    private int _currentTokens;
    private DateTime _lastRefill;

    public RateLimitBucket(string serviceName, string identifier, IntegrationSettings settings)
    {
        _serviceName = serviceName;
        _identifier = identifier;
        
        // Get service-specific rate limits
        _maxTokens = GetServiceRateLimit(serviceName, settings);
        _refillInterval = TimeSpan.FromMinutes(1);
        _currentTokens = _maxTokens;
        _lastRefill = DateTime.UtcNow;
    }

    public bool ShouldRateLimit()
    {
        lock (_lock)
        {
            RefillTokens();
            return _currentTokens <= 0;
        }
    }

    public void RecordUsage()
    {
        lock (_lock)
        {
            RefillTokens();
            if (_currentTokens > 0)
            {
                _currentTokens--;
            }
        }
    }

    public RateLimitStatus GetStatus()
    {
        lock (_lock)
        {
            RefillTokens();
            
            var nextRefill = _lastRefill.Add(_refillInterval);
            return new RateLimitStatus
            {
                ServiceName = _serviceName,
                Identifier = _identifier,
                CurrentUsage = _maxTokens - _currentTokens,
                Limit = _maxTokens,
                ResetTime = nextRefill,
                TimeUntilReset = nextRefill - DateTime.UtcNow,
                IsLimited = _currentTokens <= 0
            };
        }
    }

    private void RefillTokens()
    {
        var now = DateTime.UtcNow;
        var timeSinceLastRefill = now - _lastRefill;
        
        if (timeSinceLastRefill >= _refillInterval)
        {
            _currentTokens = _maxTokens;
            _lastRefill = now;
        }
    }

    private static int GetServiceRateLimit(string serviceName, IntegrationSettings settings)
    {
        return serviceName.ToLower() switch
        {
            "slack" => settings.Slack.RateLimitPerMinute,
            "clickup" => settings.ClickUp.RateLimitPerMinute,
            "github" => settings.GitHub.RateLimitPerMinute,
            "telegram" => settings.Telegram.RateLimitPerMinute,
            _ => 60 // Default
        };
    }
}

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