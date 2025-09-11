using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.CaptchaSolving;

/// <summary>
/// Service for CAPTCHA solving capabilities using 2captcha.com API
/// Provides Ivan-Level CAPTCHA solving including image recognition and audio processing
/// Following Clean Architecture patterns with dependency injection
/// </summary>
public interface ICaptchaSolvingService
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

    /// <summary>
    /// Solves text-based CAPTCHA
    /// </summary>
    /// <param name="text">CAPTCHA text to solve</param>
    /// <param name="options">Text CAPTCHA solving options</param>
    /// <returns>CAPTCHA solution result</returns>
    Task<CaptchaSolvingResult> SolveTextCaptchaAsync(string text, TextCaptchaOptions? options = null);

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

/// <summary>
/// Result object for CAPTCHA solving operations
/// Contains operation success status, solution data, and error details
/// </summary>
public class CaptchaSolvingResult
{
    public bool Success { get; init; }
    public object? Data { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? ErrorDetails { get; init; }
    public string? CaptchaId { get; init; }
    public TimeSpan? SolveTime { get; init; }
    public decimal? Cost { get; init; }

    /// <summary>
    /// Creates a successful result with solution data and metadata
    /// </summary>
    /// <param name="data">CAPTCHA solution data</param>
    /// <param name="message">Success message</param>
    /// <param name="captchaId">CAPTCHA ID from service</param>
    /// <param name="solveTime">Time taken to solve</param>
    /// <param name="cost">Cost of the solution</param>
    /// <returns>Successful CaptchaSolvingResult</returns>
    public static CaptchaSolvingResult SuccessResult(object? data = null, string message = "CAPTCHA solved successfully", 
        string? captchaId = null, TimeSpan? solveTime = null, decimal? cost = null)
        => new() { Success = true, Data = data, Message = message, CaptchaId = captchaId, SolveTime = solveTime, Cost = cost };

    /// <summary>
    /// Creates an error result with message and details
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="details">Detailed error information</param>
    /// <returns>Error CaptchaSolvingResult</returns>
    public static CaptchaSolvingResult ErrorResult(string message, string? details = null)
        => new() { Success = false, Message = message, ErrorDetails = details };
}

/// <summary>
/// Options for image-based CAPTCHA solving
/// </summary>
public class ImageCaptchaOptions
{
    /// <summary>
    /// Whether the CAPTCHA is case sensitive
    /// </summary>
    public bool CaseSensitive { get; set; } = false;

    /// <summary>
    /// Minimum length of the CAPTCHA text
    /// </summary>
    public int MinLength { get; set; } = 0;

    /// <summary>
    /// Maximum length of the CAPTCHA text
    /// </summary>
    public int MaxLength { get; set; } = 0;

    /// <summary>
    /// Language of the CAPTCHA (ISO 639-1 code)
    /// </summary>
    public string Language { get; set; } = "en";

    /// <summary>
    /// Instructions for the CAPTCHA solver
    /// </summary>
    public string? Instructions { get; set; }

    /// <summary>
    /// Timeout for solving in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;
}

/// <summary>
/// Options for reCAPTCHA solving
/// </summary>
public class RecaptchaOptions
{
    /// <summary>
    /// Whether reCAPTCHA is invisible
    /// </summary>
    public bool Invisible { get; set; } = false;

    /// <summary>
    /// Custom data for reCAPTCHA
    /// </summary>
    public string? Data { get; set; }

    /// <summary>
    /// Cookie data for reCAPTCHA solving
    /// </summary>
    public string? Cookies { get; set; }

    /// <summary>
    /// User agent string
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Proxy configuration
    /// </summary>
    public ProxyConfig? Proxy { get; set; }

    /// <summary>
    /// Timeout for solving in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;
}

/// <summary>
/// Options for hCaptcha solving
/// </summary>
public class HCaptchaOptions
{
    /// <summary>
    /// Whether hCaptcha is invisible
    /// </summary>
    public bool Invisible { get; set; } = false;

    /// <summary>
    /// Custom data for hCaptcha
    /// </summary>
    public string? Data { get; set; }

    /// <summary>
    /// Cookie data for hCaptcha solving
    /// </summary>
    public string? Cookies { get; set; }

    /// <summary>
    /// User agent string
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Proxy configuration
    /// </summary>
    public ProxyConfig? Proxy { get; set; }

    /// <summary>
    /// Timeout for solving in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;
}

/// <summary>
/// Options for text-based CAPTCHA solving
/// </summary>
public class TextCaptchaOptions
{
    /// <summary>
    /// Language of the CAPTCHA text (ISO 639-1 code)
    /// </summary>
    public string Language { get; set; } = "en";

    /// <summary>
    /// Instructions for the text solver
    /// </summary>
    public string? Instructions { get; set; }

    /// <summary>
    /// Timeout for solving in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;
}

/// <summary>
/// Proxy configuration for CAPTCHA solving
/// </summary>
public class ProxyConfig
{
    /// <summary>
    /// Proxy type
    /// </summary>
    public ProxyType Type { get; set; } = ProxyType.Http;

    /// <summary>
    /// Proxy host address
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Proxy port
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Proxy username
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Proxy password
    /// </summary>
    public string? Password { get; set; }
}

/// <summary>
/// Proxy type enumeration
/// </summary>
public enum ProxyType
{
    Http,
    Https,
    Socks4,
    Socks5
}

/// <summary>
/// CAPTCHA solving service statistics
/// </summary>
public class ServiceStats
{
    public int TotalSolved { get; set; }
    public int SuccessfulSolutions { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan AverageSolveTime { get; set; }
    public decimal TotalCost { get; set; }
    public DateTime LastActivity { get; set; }
}