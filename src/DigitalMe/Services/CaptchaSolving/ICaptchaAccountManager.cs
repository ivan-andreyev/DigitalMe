using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.CaptchaSolving;

/// <summary>
/// Focused interface for CAPTCHA service account management
/// Handles balance, reporting, and service availability
/// Following Interface Segregation Principle
/// </summary>
public interface ICaptchaAccountManager
{
    /// <summary>
    /// Gets account balance from 2captcha service
    /// </summary>
    /// <returns>Current account balance in USD</returns>
    Task<CaptchaSolvingResult> GetBalanceAsync();

    /// <summary>
    /// Reports incorrect CAPTCHA solution to improve service quality
    /// </summary>
    /// <param name="captchaId">ID of the solved CAPTCHA</param>
    /// <returns>Report operation result</returns>
    Task<CaptchaSolvingResult> ReportIncorrectCaptchaAsync(string captchaId);

    /// <summary>
    /// Checks if CAPTCHA solving service is available and properly configured
    /// </summary>
    /// <returns>True if service is ready, false otherwise</returns>
    Task<bool> IsServiceAvailableAsync();

    /// <summary>
    /// Gets service statistics including solve rates and response times
    /// </summary>
    /// <returns>Service statistics data</returns>
    Task<CaptchaSolvingResult> GetServiceStatsAsync();
}