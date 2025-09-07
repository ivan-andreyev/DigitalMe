using System;

namespace DigitalMe.Services.Monitoring
{
    /// <summary>
    /// Interface for logging performance metrics as required by MVP Phase 6 plan
    /// </summary>
    public interface IMetricsLogger
    {
        /// <summary>
        /// Log API call metrics for monitoring
        /// </summary>
        /// <param name="endpoint">API endpoint called</param>
        /// <param name="duration">Time taken for the call</param>
        /// <param name="success">Whether the call was successful</param>
        void LogApiCall(string endpoint, TimeSpan duration, bool success);
    }
}