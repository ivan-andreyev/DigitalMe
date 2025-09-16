using DigitalMe.Configuration;

namespace DigitalMe.Services.Performance;

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