using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Voice;

/// <summary>
/// Focused interface for voice service management
/// Handles service availability and statistics
/// Following Interface Segregation Principle
/// </summary>
public interface IVoiceServiceManager
{
    /// <summary>
    /// Checks if voice service is available and properly configured
    /// </summary>
    /// <returns>True if service is ready, false otherwise</returns>
    Task<bool> IsServiceAvailableAsync();

    /// <summary>
    /// Gets service usage statistics
    /// </summary>
    /// <returns>Service usage statistics</returns>
    Task<VoiceResult> GetServiceStatsAsync();
}