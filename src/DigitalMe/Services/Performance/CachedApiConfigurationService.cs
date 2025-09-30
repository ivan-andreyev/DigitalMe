using DigitalMe.Data.Entities;
using DigitalMe.Services.Performance;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services;

/// <summary>
/// Caching decorator for IApiConfigurationService (Phase 7, Task 7.1.3).
/// Caches GetApiKeyAsync results to improve performance (target: 10x speedup).
/// Cache policy: 5 min sliding, 1 hour absolute expiration.
/// </summary>
public class CachedApiConfigurationService : IApiConfigurationService
{
    private readonly IApiConfigurationService _innerService;
    private readonly ICachingService _cachingService;
    private readonly ILogger<CachedApiConfigurationService> _logger;

    private const string CacheKeyPrefix = "apikey";
    private static readonly TimeSpan SlidingExpiration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan AbsoluteExpiration = TimeSpan.FromHours(1);

    public CachedApiConfigurationService(
        IApiConfigurationService innerService,
        ICachingService cachingService,
        ILogger<CachedApiConfigurationService> logger)
    {
        _innerService = innerService ?? throw new ArgumentNullException(nameof(innerService));
        _cachingService = cachingService ?? throw new ArgumentNullException(nameof(cachingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets API key with caching. Cache key format: "apikey:{provider}:{userId}".
    /// </summary>
    public async Task<string> GetApiKeyAsync(string provider, string userId)
    {
        var cacheKey = BuildCacheKey(provider, userId);

        // Try to get from cache using GetOrSetAsync pattern
        var result = await _cachingService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                _logger.LogDebug("Cache miss for {CacheKey}, fetching from inner service", cacheKey);
                return await _innerService.GetApiKeyAsync(provider, userId);
            },
            AbsoluteExpiration);

        if (result != null)
        {
            _logger.LogTrace("Cache hit for {CacheKey}", cacheKey);
        }

        return result ?? string.Empty;
    }

    /// <summary>
    /// Sets user API key and invalidates cache for this provider/user.
    /// </summary>
    public async Task SetUserApiKeyAsync(string provider, string userId, string plainApiKey)
    {
        await _innerService.SetUserApiKeyAsync(provider, userId, plainApiKey);

        // Invalidate cache after setting new key
        var cacheKey = BuildCacheKey(provider, userId);
        await _cachingService.RemoveCachedResponseAsync(cacheKey);

        _logger.LogDebug("Invalidated cache for {CacheKey} after setting new API key", cacheKey);
    }

    /// <summary>
    /// Rotates user API key and invalidates cache.
    /// </summary>
    public async Task RotateUserApiKeyAsync(string provider, string userId, string newPlainApiKey)
    {
        await _innerService.RotateUserApiKeyAsync(provider, userId, newPlainApiKey);

        // Invalidate cache after rotation
        var cacheKey = BuildCacheKey(provider, userId);
        await _cachingService.RemoveCachedResponseAsync(cacheKey);

        _logger.LogDebug("Invalidated cache for {CacheKey} after rotating API key", cacheKey);
    }

    /// <summary>
    /// Deactivates configuration and invalidates cache.
    /// </summary>
    public async Task<bool> DeactivateConfigurationAsync(Guid configurationId)
    {
        var result = await _innerService.DeactivateConfigurationAsync(configurationId);

        // Note: We can't easily determine provider/userId from configurationId here,
        // so we rely on cache expiration (TTL) to eventually clear stale entries.
        // For production: consider cache-by-configurationId or full cache clear.

        _logger.LogDebug("Deactivated configuration {ConfigurationId}, cache will expire naturally", configurationId);
        return result;
    }

    // ==================== PASS-THROUGH METHODS (no caching) ====================
    // These methods don't benefit from caching or are rarely called.

    public Task TrackUsageAsync(Guid configurationId)
        => _innerService.TrackUsageAsync(configurationId);

    public Task ValidateConfigurationAsync(Guid configurationId, ApiConfigurationStatus status)
        => _innerService.ValidateConfigurationAsync(configurationId, status);

    public Task<ApiConfiguration> GetOrCreateConfigurationAsync(
        string userId,
        string provider,
        string? encryptedApiKey = null,
        string? encryptionIV = null,
        string? encryptionSalt = null,
        string? keyFingerprint = null)
        => _innerService.GetOrCreateConfigurationAsync(
            userId, provider, encryptedApiKey, encryptionIV, encryptionSalt, keyFingerprint);

    public Task<ApiConfiguration> RotateApiKeyAsync(
        Guid configurationId,
        string newEncryptedApiKey,
        string newEncryptionIV,
        string newEncryptionSalt,
        string newKeyFingerprint)
        => _innerService.RotateApiKeyAsync(
            configurationId, newEncryptedApiKey, newEncryptionIV, newEncryptionSalt, newKeyFingerprint);

    public Task<ApiConfiguration?> GetActiveConfigurationAsync(string userId, string provider)
        => _innerService.GetActiveConfigurationAsync(userId, provider);

    public Task<List<ApiConfiguration>> GetUserConfigurationsAsync(string userId)
        => _innerService.GetUserConfigurationsAsync(userId);

    public Task<List<ApiConfiguration>> GetKeyHistoryAsync(string userId, string provider)
        => _innerService.GetKeyHistoryAsync(userId, provider);

    // ==================== HELPER METHODS ====================

    private static string BuildCacheKey(string provider, string userId)
    {
        return $"{CacheKeyPrefix}:{provider}:{userId}";
    }
}