namespace DigitalMe.Services.Performance;

/// <summary>
/// Service for memory caching operations following ISP compliance.
/// Focused interface for caching functionality only.
/// </summary>
public interface ICachingService
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
    /// Get or set cached response with factory (simplified method name for backward compatibility)
    /// </summary>
    Task<T> GetOrSetAsync<T>(string cacheKey, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class;
}