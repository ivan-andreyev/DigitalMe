using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.CaptchaSolving;

/// <summary>
/// Focused interface for image-based CAPTCHA solving
/// Handles traditional image CAPTCHAs and text CAPTCHAs
/// Following Interface Segregation Principle
/// </summary>
public interface ICaptchaImageSolver
{
    /// <summary>
    /// Solves image-based CAPTCHA using 2captcha service
    /// </summary>
    /// <param name="imageBase64">Base64 encoded CAPTCHA image</param>
    /// <param name="options">CAPTCHA solving options</param>
    /// <returns>CAPTCHA solution result</returns>
    Task<CaptchaSolvingResult> SolveImageCaptchaAsync(string imageBase64, ImageCaptchaOptions? options = null);

    /// <summary>
    /// Solves image-based CAPTCHA from URL
    /// </summary>
    /// <param name="imageUrl">URL of the CAPTCHA image</param>
    /// <param name="options">CAPTCHA solving options</param>
    /// <returns>CAPTCHA solution result</returns>
    Task<CaptchaSolvingResult> SolveImageCaptchaFromUrlAsync(string imageUrl, ImageCaptchaOptions? options = null);

    /// <summary>
    /// Solves text-based CAPTCHA
    /// </summary>
    /// <param name="text">CAPTCHA text to solve</param>
    /// <param name="options">Text CAPTCHA solving options</param>
    /// <returns>CAPTCHA solution result</returns>
    Task<CaptchaSolvingResult> SolveTextCaptchaAsync(string text, TextCaptchaOptions? options = null);
}