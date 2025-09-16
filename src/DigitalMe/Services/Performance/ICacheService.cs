namespace DigitalMe.Services.Performance;

/// <summary>
/// Service for managing cached responses across integrations
/// </summary>
public interface ICacheService
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
}