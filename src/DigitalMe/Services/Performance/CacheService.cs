using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Performance;

/// <summary>
/// Service for managing cached responses across integrations
/// </summary>
public class CacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheService> _logger;

    public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

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
}