using System.Collections.Concurrent;
using DigitalMe.Configuration;
using Microsoft.Extensions.Options;

namespace DigitalMe.Services.Performance;

/// <summary>
/// Service for managing rate limiting across integrations
/// </summary>
public class RateLimitService : IRateLimitService
{
    private readonly IntegrationSettings _settings;
    private readonly ConcurrentDictionary<string, RateLimitBucket> _rateLimitBuckets;

    public RateLimitService(IOptions<IntegrationSettings> integrationSettings)
    {
        _settings = integrationSettings.Value;
        _rateLimitBuckets = new ConcurrentDictionary<string, RateLimitBucket>();
    }

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
        return Task.CompletedTask;
    }

    public Task<RateLimitStatus> GetRateLimitStatusAsync(string serviceName, string identifier)
    {
        var key = $"{serviceName}:{identifier}";
        var bucket = _rateLimitBuckets.GetOrAdd(key, _ => new RateLimitBucket(serviceName, identifier, _settings));

        return Task.FromResult(bucket.GetStatus());
    }
}