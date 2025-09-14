using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.CaptchaSolving;

/// <summary>
/// Focused interface for interactive CAPTCHA solving
/// Handles reCAPTCHA v2/v3 and hCaptcha challenges
/// Following Interface Segregation Principle
/// </summary>
public interface ICaptchaInteractiveSolver
{
    /// <summary>
    /// Solves reCAPTCHA v2 challenge
    /// </summary>
    /// <param name="siteKey">reCAPTCHA site key</param>
    /// <param name="pageUrl">URL of the page with reCAPTCHA</param>
    /// <param name="options">reCAPTCHA solving options</param>
    /// <returns>CAPTCHA solution result with response token</returns>
    Task<CaptchaSolvingResult> SolveRecaptchaV2Async(string siteKey, string pageUrl, RecaptchaOptions? options = null);

    /// <summary>
    /// Solves reCAPTCHA v3 challenge
    /// </summary>
    /// <param name="siteKey">reCAPTCHA site key</param>
    /// <param name="pageUrl">URL of the page with reCAPTCHA</param>
    /// <param name="action">Action parameter for reCAPTCHA v3</param>
    /// <param name="minScore">Minimum score required (0.1-0.9)</param>
    /// <param name="options">reCAPTCHA solving options</param>
    /// <returns>CAPTCHA solution result with response token</returns>
    Task<CaptchaSolvingResult> SolveRecaptchaV3Async(string siteKey, string pageUrl, string action, double minScore = 0.3, RecaptchaOptions? options = null);

    /// <summary>
    /// Solves hCaptcha challenge
    /// </summary>
    /// <param name="siteKey">hCaptcha site key</param>
    /// <param name="pageUrl">URL of the page with hCaptcha</param>
    /// <param name="options">hCaptcha solving options</param>
    /// <returns>CAPTCHA solution result with response token</returns>
    Task<CaptchaSolvingResult> SolveHCaptchaAsync(string siteKey, string pageUrl, HCaptchaOptions? options = null);
}