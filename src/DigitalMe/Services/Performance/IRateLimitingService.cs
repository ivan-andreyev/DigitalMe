namespace DigitalMe.Services.Performance;

/// <summary>
/// Service for rate limiting operations following ISP compliance.
/// Focused interface for rate limiting functionality only.
/// </summary>
public interface IRateLimitingService
{
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
}