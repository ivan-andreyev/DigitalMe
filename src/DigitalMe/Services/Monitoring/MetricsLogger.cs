using System;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Monitoring
{
    /// <summary>
    /// Implementation of IMetricsLogger for logging performance metrics
    /// As required by MVP Phase 6 plan specifications
    /// </summary>
    public class MetricsLogger : IMetricsLogger
    {
        private readonly ILogger<MetricsLogger> _logger;

        public MetricsLogger(ILogger<MetricsLogger> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Log API call metrics for monitoring
        /// </summary>
        /// <param name="endpoint">API endpoint called</param>
        /// <param name="duration">Time taken for the call</param>
        /// <param name="success">Whether the call was successful</param>
        public void LogApiCall(string endpoint, TimeSpan duration, bool success)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                _logger.LogWarning("API call logged with empty endpoint");
                return;
            }

            // Log metrics for monitoring with structured logging
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["Endpoint"] = endpoint,
                ["DurationMs"] = duration.TotalMilliseconds,
                ["Success"] = success,
                ["EventType"] = "ApiCall"
            }))
            {
                if (success)
                {
                    _logger.LogInformation("API call to {Endpoint} completed successfully in {Duration:N2}ms",
                        endpoint, duration.TotalMilliseconds);
                }
                else
                {
                    _logger.LogWarning("API call to {Endpoint} failed after {Duration:N2}ms",
                        endpoint, duration.TotalMilliseconds);
                }
            }
        }
    }
}
