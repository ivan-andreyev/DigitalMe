using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Polly.Bulkhead;
using DigitalMe.Configuration;
using System.Net;

namespace DigitalMe.Services.Resilience;

/// <summary>
/// Service for managing resilience policies across integrations
/// </summary>
public class ResiliencePolicyService : IResiliencePolicyService
{
    private readonly ILogger<ResiliencePolicyService> _logger;
    private readonly IntegrationSettings _settings;
    private readonly Dictionary<string, IAsyncPolicy<HttpResponseMessage>> _retryPolicies;
    private readonly Dictionary<string, IAsyncPolicy<HttpResponseMessage>> _circuitBreakerPolicies;
    private readonly Dictionary<string, IAsyncPolicy<HttpResponseMessage>> _combinedPolicies;
    private readonly Dictionary<string, IAsyncPolicy> _timeoutPolicies;
    private readonly Dictionary<string, IAsyncPolicy> _bulkheadPolicies;

    public ResiliencePolicyService(
        ILogger<ResiliencePolicyService> logger,
        IOptions<IntegrationSettings> integrationSettings)
    {
        _logger = logger;
        _settings = integrationSettings.Value;

        _retryPolicies = new Dictionary<string, IAsyncPolicy<HttpResponseMessage>>();
        _circuitBreakerPolicies = new Dictionary<string, IAsyncPolicy<HttpResponseMessage>>();
        _combinedPolicies = new Dictionary<string, IAsyncPolicy<HttpResponseMessage>>();
        _timeoutPolicies = new Dictionary<string, IAsyncPolicy>();
        _bulkheadPolicies = new Dictionary<string, IAsyncPolicy>();

        InitializePolicies();
    }

    public IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(string serviceName)
    {
        return _retryPolicies.GetValueOrDefault(serviceName, _retryPolicies["default"]);
    }

    public IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(string serviceName)
    {
        return _circuitBreakerPolicies.GetValueOrDefault(serviceName, _circuitBreakerPolicies["default"]);
    }

    public IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy(string serviceName)
    {
        return _combinedPolicies.GetValueOrDefault(serviceName, _combinedPolicies["default"]);
    }

    public IAsyncPolicy GetTimeoutPolicy(string serviceName)
    {
        return _timeoutPolicies.GetValueOrDefault(serviceName, _timeoutPolicies["default"]);
    }

    public IAsyncPolicy GetBulkheadPolicy(string serviceName)
    {
        return _bulkheadPolicies.GetValueOrDefault(serviceName, _bulkheadPolicies["default"]);
    }

    private void InitializePolicies()
    {
        // Default policies
        InitializeDefaultPolicies();

        // Service-specific policies
        InitializeSlackPolicies();
        InitializeClickUpPolicies();
        InitializeGitHubPolicies();
        InitializeTelegramPolicies();
    }

    private void InitializeDefaultPolicies()
    {
        // Default retry policy: 3 retries with exponential backoff
        _retryPolicies["default"] = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("Retry {RetryCount} for {ServiceName} after {Delay}s due to: {Reason}",
                        retryCount, context.GetValueOrDefault("ServiceName", "Unknown"), timespan.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.ReasonPhrase);
                });

        // Default circuit breaker: break after 5 consecutive failures, stay open for 30s
        _circuitBreakerPolicies["default"] = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutException>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (exception, duration) =>
                {
                    _logger.LogError("Circuit breaker opened for {Duration}s due to: {Reason}",
                        duration.TotalSeconds, exception.Exception?.Message ?? exception.Result?.ReasonPhrase);
                },
                onReset: () =>
                {
                    _logger.LogInformation("Circuit breaker reset");
                });

        // Default combined policy
        _combinedPolicies["default"] = Policy.WrapAsync(_retryPolicies["default"], _circuitBreakerPolicies["default"]);

        // Default timeout policy: 30 seconds
        _timeoutPolicies["default"] = Policy.TimeoutAsync(30, TimeoutStrategy.Optimistic);

        // Default bulkhead policy: max 10 concurrent operations
        _bulkheadPolicies["default"] = Policy.BulkheadAsync(10, 5);
    }

    private void InitializeSlackPolicies()
    {
        var settings = _settings.Slack;

        // Slack has strict rate limits, so be more conservative
        _retryPolicies["slack"] = Policy
            .HandleResult<HttpResponseMessage>(r =>
                r.StatusCode == HttpStatusCode.TooManyRequests ||
                r.StatusCode >= HttpStatusCode.InternalServerError)
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: settings.MaxRetries,
                sleepDurationProvider: retryAttempt =>
                {
                    // Respect rate limiting with longer delays
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt) * 2);
                    return delay > TimeSpan.FromMinutes(1) ? TimeSpan.FromMinutes(1) : delay;
                },
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("Slack retry {RetryCount} after {Delay}s due to: {Reason}",
                        retryCount, timespan.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.ReasonPhrase);
                });

        _circuitBreakerPolicies["slack"] = Policy
            .HandleResult<HttpResponseMessage>(r =>
                r.StatusCode >= HttpStatusCode.InternalServerError ||
                r.StatusCode == HttpStatusCode.TooManyRequests)
            .CircuitBreakerAsync(3, TimeSpan.FromMinutes(2));

        _combinedPolicies["slack"] = Policy.WrapAsync(_retryPolicies["slack"], _circuitBreakerPolicies["slack"]);
        _timeoutPolicies["slack"] = Policy.TimeoutAsync(settings.TimeoutSeconds, TimeoutStrategy.Optimistic);
        _bulkheadPolicies["slack"] = Policy.BulkheadAsync(5, 3); // Lower concurrency for Slack
    }

    private void InitializeClickUpPolicies()
    {
        var settings = _settings.ClickUp;

        _retryPolicies["clickup"] = Policy
            .HandleResult<HttpResponseMessage>(r =>
                r.StatusCode == HttpStatusCode.TooManyRequests ||
                r.StatusCode >= HttpStatusCode.InternalServerError)
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: settings.MaxRetries,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(1.5, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("ClickUp retry {RetryCount} after {Delay}s due to: {Reason}",
                        retryCount, timespan.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.ReasonPhrase);
                });

        _circuitBreakerPolicies["clickup"] = Policy
            .HandleResult<HttpResponseMessage>(r => r.StatusCode >= HttpStatusCode.InternalServerError)
            .CircuitBreakerAsync(4, TimeSpan.FromMinutes(1));

        _combinedPolicies["clickup"] = Policy.WrapAsync(_retryPolicies["clickup"], _circuitBreakerPolicies["clickup"]);
        _timeoutPolicies["clickup"] = Policy.TimeoutAsync(settings.TimeoutSeconds, TimeoutStrategy.Optimistic);
        _bulkheadPolicies["clickup"] = Policy.BulkheadAsync(8, 4);
    }

    private void InitializeGitHubPolicies()
    {
        var settings = _settings.GitHub;

        // GitHub has generous rate limits, so can be more aggressive
        _retryPolicies["github"] = Policy
            .HandleResult<HttpResponseMessage>(r =>
                r.StatusCode == HttpStatusCode.TooManyRequests ||
                r.StatusCode >= HttpStatusCode.InternalServerError ||
                r.StatusCode == HttpStatusCode.BadGateway ||
                r.StatusCode == HttpStatusCode.ServiceUnavailable ||
                r.StatusCode == HttpStatusCode.GatewayTimeout)
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: settings.MaxRetries,
                sleepDurationProvider: retryAttempt =>
                {
                    // Check for rate limit reset header in real implementation
                    return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                },
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("GitHub retry {RetryCount} after {Delay}s due to: {Reason}",
                        retryCount, timespan.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.ReasonPhrase);
                });

        _circuitBreakerPolicies["github"] = Policy
            .HandleResult<HttpResponseMessage>(r => r.StatusCode >= HttpStatusCode.InternalServerError)
            .CircuitBreakerAsync(5, TimeSpan.FromMinutes(1));

        _combinedPolicies["github"] = Policy.WrapAsync(_retryPolicies["github"], _circuitBreakerPolicies["github"]);
        _timeoutPolicies["github"] = Policy.TimeoutAsync(settings.TimeoutSeconds, TimeoutStrategy.Optimistic);
        _bulkheadPolicies["github"] = Policy.BulkheadAsync(15, 8); // Higher concurrency for GitHub
    }

    private void InitializeTelegramPolicies()
    {
        var settings = _settings.Telegram;

        _retryPolicies["telegram"] = Policy
            .HandleResult<HttpResponseMessage>(r =>
                r.StatusCode == HttpStatusCode.TooManyRequests ||
                r.StatusCode >= HttpStatusCode.InternalServerError)
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: settings.MaxRetries,
                sleepDurationProvider: retryAttempt =>
                {
                    // Telegram has specific rate limiting rules
                    return retryAttempt switch
                    {
                        1 => TimeSpan.FromSeconds(1),
                        2 => TimeSpan.FromSeconds(3),
                        _ => TimeSpan.FromSeconds(10)
                    };
                },
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning("Telegram retry {RetryCount} after {Delay}s due to: {Reason}",
                        retryCount, timespan.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.ReasonPhrase);
                });

        _circuitBreakerPolicies["telegram"] = Policy
            .HandleResult<HttpResponseMessage>(r => r.StatusCode >= HttpStatusCode.InternalServerError)
            .CircuitBreakerAsync(3, TimeSpan.FromMinutes(2));

        _combinedPolicies["telegram"] = Policy.WrapAsync(_retryPolicies["telegram"], _circuitBreakerPolicies["telegram"]);
        _timeoutPolicies["telegram"] = Policy.TimeoutAsync(settings.TimeoutSeconds, TimeoutStrategy.Optimistic);
        _bulkheadPolicies["telegram"] = Policy.BulkheadAsync(6, 3);
    }
}
