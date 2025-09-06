using Polly;

namespace DigitalMe.Services.Resilience;

/// <summary>
/// Service for managing resilience policies across integrations
/// </summary>
public interface IResiliencePolicyService
{
    /// <summary>
    /// Get retry policy for external API calls
    /// </summary>
    IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(string serviceName);

    /// <summary>
    /// Get circuit breaker policy for external API calls  
    /// </summary>
    IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(string serviceName);

    /// <summary>
    /// Get combined retry + circuit breaker policy
    /// </summary>
    IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy(string serviceName);

    /// <summary>
    /// Get timeout policy for external API calls
    /// </summary>
    IAsyncPolicy GetTimeoutPolicy(string serviceName);

    /// <summary>
    /// Get bulkhead policy for limiting concurrent operations
    /// </summary>
    IAsyncPolicy GetBulkheadPolicy(string serviceName);
}