using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace DigitalMe.Services.CaptchaSolving;

/// <summary>
/// Implementation of CAPTCHA solving service using 2captcha.com API
/// Provides Ivan-Level CAPTCHA solving capabilities with comprehensive error handling
/// Following Clean Architecture patterns with dependency injection
/// </summary>
public class CaptchaSolvingService : ICaptchaSolvingService
{
    private readonly ILogger<CaptchaSolvingService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly string? _apiKey;
    private const string BaseUrl = "https://2captcha.com";

    public CaptchaSolvingService(ILogger<CaptchaSolvingService> logger, HttpClient httpClient, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        // Try to get API key from configuration first, then from environment variable
        _apiKey = _configuration["TwoCaptcha:ApiKey"];
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            var envVarName = _configuration["TwoCaptcha:ApiKeyEnvironmentVariable"] ?? "TWOCAPTCHA_API_KEY";
            _apiKey = Environment.GetEnvironmentVariable(envVarName);
            _logger.LogInformation($"Attempting to get 2captcha API key from environment variable: {envVarName}");
        }
        
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogError("2captcha API key not configured - this is required for service operation");
            throw new InvalidOperationException("TwoCaptcha API key is required but not configured. Please set TwoCaptcha:ApiKey in configuration or TWOCAPTCHA_API_KEY environment variable.");
        }
        else
        {
            _logger.LogInformation("2captcha API key configured successfully");
        }
        
        // Set default timeout for HTTP client
        _httpClient.Timeout = TimeSpan.FromMinutes(10);
    }

    /// <inheritdoc />
    public async Task<CaptchaSolvingResult> SolveImageCaptchaAsync(string imageBase64, ImageCaptchaOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(imageBase64))
            return CaptchaSolvingResult.ErrorResult("Base64 image data cannot be null or empty");

        if (!IsConfigured())
            return CaptchaSolvingResult.ErrorResult("2captcha API key not configured");

        try
        {
            _logger.LogInformation("Solving image CAPTCHA");
            var startTime = DateTime.UtcNow;

            options ??= new ImageCaptchaOptions();

            // Submit CAPTCHA for solving
            var submitParams = new Dictionary<string, string>
            {
                ["method"] = "base64",
                ["key"] = _apiKey,
                ["body"] = imageBase64,
                ["regsense"] = options.CaseSensitive ? "1" : "0",
                ["language"] = options.Language
            };

            if (options.MinLength > 0)
                submitParams["min_len"] = options.MinLength.ToString();
            if (options.MaxLength > 0)
                submitParams["max_len"] = options.MaxLength.ToString();
            if (!string.IsNullOrEmpty(options.Instructions))
                submitParams["textinstructions"] = options.Instructions;

            var captchaId = await SubmitCaptchaAsync(submitParams);
            if (string.IsNullOrEmpty(captchaId))
            {
                return CaptchaSolvingResult.ErrorResult("Failed to submit CAPTCHA for solving");
            }

            // Wait for solution
            var solution = await WaitForSolutionAsync(captchaId, options.TimeoutSeconds);
            if (solution == null)
            {
                return CaptchaSolvingResult.ErrorResult("CAPTCHA solving timeout or failed");
            }

            var solveTime = DateTime.UtcNow - startTime;
            var cost = 0.001m; // Approximate cost for image CAPTCHA

            _logger.LogInformation("Image CAPTCHA solved successfully in {SolveTime}ms", solveTime.TotalMilliseconds);

            return CaptchaSolvingResult.SuccessResult(solution, "Image CAPTCHA solved successfully", captchaId, solveTime, cost);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to solve image CAPTCHA");
            return CaptchaSolvingResult.ErrorResult($"Image CAPTCHA solving failed: {ex.Message}", ex.ToString());
        }
    }

    /// <inheritdoc />
    public async Task<CaptchaSolvingResult> SolveImageCaptchaFromUrlAsync(string imageUrl, ImageCaptchaOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return CaptchaSolvingResult.ErrorResult("Image URL cannot be null or empty");

        try
        {
            _logger.LogInformation("Solving image CAPTCHA from URL: {ImageUrl}", imageUrl);
            var startTime = DateTime.UtcNow;

            options ??= new ImageCaptchaOptions();

            // Submit CAPTCHA for solving
            var submitParams = new Dictionary<string, string>
            {
                ["method"] = "get",
                ["key"] = _apiKey,
                ["file"] = imageUrl,
                ["regsense"] = options.CaseSensitive ? "1" : "0",
                ["language"] = options.Language
            };

            if (options.MinLength > 0)
                submitParams["min_len"] = options.MinLength.ToString();
            if (options.MaxLength > 0)
                submitParams["max_len"] = options.MaxLength.ToString();
            if (!string.IsNullOrEmpty(options.Instructions))
                submitParams["textinstructions"] = options.Instructions;

            var captchaId = await SubmitCaptchaAsync(submitParams);
            if (string.IsNullOrEmpty(captchaId))
            {
                return CaptchaSolvingResult.ErrorResult("Failed to submit CAPTCHA for solving");
            }

            // Wait for solution
            var solution = await WaitForSolutionAsync(captchaId, options.TimeoutSeconds);
            if (solution == null)
            {
                return CaptchaSolvingResult.ErrorResult("CAPTCHA solving timeout or failed");
            }

            var solveTime = DateTime.UtcNow - startTime;
            var cost = 0.001m; // Approximate cost for image CAPTCHA

            _logger.LogInformation("Image CAPTCHA from URL solved successfully in {SolveTime}ms", solveTime.TotalMilliseconds);

            return CaptchaSolvingResult.SuccessResult(solution, "Image CAPTCHA solved successfully", captchaId, solveTime, cost);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to solve image CAPTCHA from URL");
            return CaptchaSolvingResult.ErrorResult($"Image CAPTCHA solving failed: {ex.Message}", ex.ToString());
        }
    }

    /// <inheritdoc />
    public async Task<CaptchaSolvingResult> SolveRecaptchaV2Async(string siteKey, string pageUrl, RecaptchaOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(siteKey))
            return CaptchaSolvingResult.ErrorResult("Site key cannot be null or empty");

        if (string.IsNullOrWhiteSpace(pageUrl))
            return CaptchaSolvingResult.ErrorResult("Page URL cannot be null or empty");

        try
        {
            _logger.LogInformation("Solving reCAPTCHA v2 for site: {SiteKey}", siteKey);
            var startTime = DateTime.UtcNow;

            options ??= new RecaptchaOptions();

            // Submit reCAPTCHA for solving
            var submitParams = new Dictionary<string, string>
            {
                ["method"] = "userrecaptcha",
                ["key"] = _apiKey,
                ["googlekey"] = siteKey,
                ["pageurl"] = pageUrl,
                ["invisible"] = options.Invisible ? "1" : "0"
            };

            if (!string.IsNullOrEmpty(options.Data))
                submitParams["data"] = options.Data;
            if (!string.IsNullOrEmpty(options.Cookies))
                submitParams["cookies"] = options.Cookies;
            if (!string.IsNullOrEmpty(options.UserAgent))
                submitParams["userAgent"] = options.UserAgent;

            if (options.Proxy != null)
            {
                submitParams["proxy"] = $"{options.Proxy.Host}:{options.Proxy.Port}";
                submitParams["proxytype"] = options.Proxy.Type.ToString().ToUpper();
                if (!string.IsNullOrEmpty(options.Proxy.Username))
                    submitParams["proxylogin"] = options.Proxy.Username;
                if (!string.IsNullOrEmpty(options.Proxy.Password))
                    submitParams["proxypassword"] = options.Proxy.Password;
            }

            var captchaId = await SubmitCaptchaAsync(submitParams);
            if (string.IsNullOrEmpty(captchaId))
            {
                return CaptchaSolvingResult.ErrorResult("Failed to submit reCAPTCHA for solving");
            }

            // Wait for solution
            var solution = await WaitForSolutionAsync(captchaId, options.TimeoutSeconds);
            if (solution == null)
            {
                return CaptchaSolvingResult.ErrorResult("reCAPTCHA solving timeout or failed");
            }

            var solveTime = DateTime.UtcNow - startTime;
            var cost = 0.002m; // Approximate cost for reCAPTCHA

            _logger.LogInformation("reCAPTCHA v2 solved successfully in {SolveTime}ms", solveTime.TotalMilliseconds);

            return CaptchaSolvingResult.SuccessResult(solution, "reCAPTCHA v2 solved successfully", captchaId, solveTime, cost);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to solve reCAPTCHA v2");
            return CaptchaSolvingResult.ErrorResult($"reCAPTCHA v2 solving failed: {ex.Message}", ex.ToString());
        }
    }

    /// <inheritdoc />
    public async Task<CaptchaSolvingResult> SolveRecaptchaV3Async(string siteKey, string pageUrl, string action, double minScore = 0.3, RecaptchaOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(siteKey))
            return CaptchaSolvingResult.ErrorResult("Site key cannot be null or empty");

        if (string.IsNullOrWhiteSpace(pageUrl))
            return CaptchaSolvingResult.ErrorResult("Page URL cannot be null or empty");

        if (string.IsNullOrWhiteSpace(action))
            return CaptchaSolvingResult.ErrorResult("Action cannot be null or empty");

        if (minScore < 0.1 || minScore > 0.9)
            return CaptchaSolvingResult.ErrorResult("Min score must be between 0.1 and 0.9");

        try
        {
            _logger.LogInformation("Solving reCAPTCHA v3 for site: {SiteKey}, action: {Action}", siteKey, action);
            var startTime = DateTime.UtcNow;

            options ??= new RecaptchaOptions();

            // Submit reCAPTCHA v3 for solving
            var submitParams = new Dictionary<string, string>
            {
                ["method"] = "userrecaptcha",
                ["key"] = _apiKey,
                ["googlekey"] = siteKey,
                ["pageurl"] = pageUrl,
                ["version"] = "v3",
                ["action"] = action,
                ["min_score"] = minScore.ToString("F1")
            };

            if (!string.IsNullOrEmpty(options.Data))
                submitParams["data"] = options.Data;
            if (!string.IsNullOrEmpty(options.Cookies))
                submitParams["cookies"] = options.Cookies;
            if (!string.IsNullOrEmpty(options.UserAgent))
                submitParams["userAgent"] = options.UserAgent;

            if (options.Proxy != null)
            {
                submitParams["proxy"] = $"{options.Proxy.Host}:{options.Proxy.Port}";
                submitParams["proxytype"] = options.Proxy.Type.ToString().ToUpper();
                if (!string.IsNullOrEmpty(options.Proxy.Username))
                    submitParams["proxylogin"] = options.Proxy.Username;
                if (!string.IsNullOrEmpty(options.Proxy.Password))
                    submitParams["proxypassword"] = options.Proxy.Password;
            }

            var captchaId = await SubmitCaptchaAsync(submitParams);
            if (string.IsNullOrEmpty(captchaId))
            {
                return CaptchaSolvingResult.ErrorResult("Failed to submit reCAPTCHA v3 for solving");
            }

            // Wait for solution
            var solution = await WaitForSolutionAsync(captchaId, options.TimeoutSeconds);
            if (solution == null)
            {
                return CaptchaSolvingResult.ErrorResult("reCAPTCHA v3 solving timeout or failed");
            }

            var solveTime = DateTime.UtcNow - startTime;
            var cost = 0.002m; // Approximate cost for reCAPTCHA

            _logger.LogInformation("reCAPTCHA v3 solved successfully in {SolveTime}ms", solveTime.TotalMilliseconds);

            return CaptchaSolvingResult.SuccessResult(solution, "reCAPTCHA v3 solved successfully", captchaId, solveTime, cost);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to solve reCAPTCHA v3");
            return CaptchaSolvingResult.ErrorResult($"reCAPTCHA v3 solving failed: {ex.Message}", ex.ToString());
        }
    }

    /// <inheritdoc />
    public async Task<CaptchaSolvingResult> SolveHCaptchaAsync(string siteKey, string pageUrl, HCaptchaOptions? options = null)
    {
        try
        {
            _logger.LogInformation("Solving hCaptcha for site: {SiteKey}", siteKey);
            var startTime = DateTime.UtcNow;

            options ??= new HCaptchaOptions();

            // Submit hCaptcha for solving
            var submitParams = new Dictionary<string, string>
            {
                ["method"] = "hcaptcha",
                ["key"] = _apiKey,
                ["sitekey"] = siteKey,
                ["pageurl"] = pageUrl,
                ["invisible"] = options.Invisible ? "1" : "0"
            };

            if (!string.IsNullOrEmpty(options.Data))
                submitParams["data"] = options.Data;
            if (!string.IsNullOrEmpty(options.Cookies))
                submitParams["cookies"] = options.Cookies;
            if (!string.IsNullOrEmpty(options.UserAgent))
                submitParams["userAgent"] = options.UserAgent;

            if (options.Proxy != null)
            {
                submitParams["proxy"] = $"{options.Proxy.Host}:{options.Proxy.Port}";
                submitParams["proxytype"] = options.Proxy.Type.ToString().ToUpper();
                if (!string.IsNullOrEmpty(options.Proxy.Username))
                    submitParams["proxylogin"] = options.Proxy.Username;
                if (!string.IsNullOrEmpty(options.Proxy.Password))
                    submitParams["proxypassword"] = options.Proxy.Password;
            }

            var captchaId = await SubmitCaptchaAsync(submitParams);
            if (string.IsNullOrEmpty(captchaId))
            {
                return CaptchaSolvingResult.ErrorResult("Failed to submit hCaptcha for solving");
            }

            // Wait for solution
            var solution = await WaitForSolutionAsync(captchaId, options.TimeoutSeconds);
            if (solution == null)
            {
                return CaptchaSolvingResult.ErrorResult("hCaptcha solving timeout or failed");
            }

            var solveTime = DateTime.UtcNow - startTime;
            var cost = 0.001m; // Approximate cost for hCaptcha

            _logger.LogInformation("hCaptcha solved successfully in {SolveTime}ms", solveTime.TotalMilliseconds);

            return CaptchaSolvingResult.SuccessResult(solution, "hCaptcha solved successfully", captchaId, solveTime, cost);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to solve hCaptcha");
            return CaptchaSolvingResult.ErrorResult($"hCaptcha solving failed: {ex.Message}", ex.ToString());
        }
    }

    /// <inheritdoc />
    public async Task<CaptchaSolvingResult> SolveTextCaptchaAsync(string text, TextCaptchaOptions? options = null)
    {
        try
        {
            _logger.LogInformation("Solving text CAPTCHA");
            var startTime = DateTime.UtcNow;

            options ??= new TextCaptchaOptions();

            // Submit text CAPTCHA for solving
            var submitParams = new Dictionary<string, string>
            {
                ["method"] = "textcaptcha",
                ["key"] = _apiKey,
                ["textcaptcha"] = text,
                ["language"] = options.Language
            };

            if (!string.IsNullOrEmpty(options.Instructions))
                submitParams["textinstructions"] = options.Instructions;

            var captchaId = await SubmitCaptchaAsync(submitParams);
            if (string.IsNullOrEmpty(captchaId))
            {
                return CaptchaSolvingResult.ErrorResult("Failed to submit text CAPTCHA for solving");
            }

            // Wait for solution
            var solution = await WaitForSolutionAsync(captchaId, options.TimeoutSeconds);
            if (solution == null)
            {
                return CaptchaSolvingResult.ErrorResult("Text CAPTCHA solving timeout or failed");
            }

            var solveTime = DateTime.UtcNow - startTime;
            var cost = 0.0005m; // Approximate cost for text CAPTCHA

            _logger.LogInformation("Text CAPTCHA solved successfully in {SolveTime}ms", solveTime.TotalMilliseconds);

            return CaptchaSolvingResult.SuccessResult(solution, "Text CAPTCHA solved successfully", captchaId, solveTime, cost);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to solve text CAPTCHA");
            return CaptchaSolvingResult.ErrorResult($"Text CAPTCHA solving failed: {ex.Message}", ex.ToString());
        }
    }

    /// <inheritdoc />
    public async Task<CaptchaSolvingResult> GetBalanceAsync()
    {
        if (!IsConfigured())
            return CaptchaSolvingResult.ErrorResult("2captcha API key not configured");

        try
        {
            _logger.LogInformation("Getting 2captcha account balance");

            var response = await _httpClient.GetAsync($"{BaseUrl}/res.php?key={_apiKey}&action=getbalance");
            response.EnsureSuccessStatusCode();

            var balanceText = await response.Content.ReadAsStringAsync();
            
            // Check for API errors
            if (balanceText.StartsWith("ERROR_WRONG_USER_KEY"))
            {
                return CaptchaSolvingResult.ErrorResult("Invalid API key");
            }
            
            // Handle "OK|balance" format
            if (balanceText.StartsWith("OK|"))
            {
                var balanceStr = balanceText.Substring(3);
                if (decimal.TryParse(balanceStr, out var balance))
                {
                    _logger.LogInformation("Account balance: ${Balance}", balance);
                    return CaptchaSolvingResult.SuccessResult(balance, $"Account balance: ${balance}");
                }
            }
            
            // Try parsing as direct decimal
            if (decimal.TryParse(balanceText, out var directBalance))
            {
                _logger.LogInformation("Account balance: ${Balance}", directBalance);
                return CaptchaSolvingResult.SuccessResult(directBalance, $"Account balance: ${directBalance}");
            }
            
            return CaptchaSolvingResult.ErrorResult($"Failed to parse balance: {balanceText}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get account balance");
            return CaptchaSolvingResult.ErrorResult($"Balance check failed: {ex.Message}", ex.ToString());
        }
    }

    /// <inheritdoc />
    public async Task<CaptchaSolvingResult> ReportIncorrectCaptchaAsync(string captchaId)
    {
        try
        {
            _logger.LogInformation("Reporting incorrect CAPTCHA: {CaptchaId}", captchaId);

            var response = await _httpClient.GetAsync($"{BaseUrl}/res.php?key={_apiKey}&action=reportbad&id={captchaId}");
            response.EnsureSuccessStatusCode();

            var responseText = await response.Content.ReadAsStringAsync();
            
            if (responseText.Contains("OK_REPORT_RECORDED"))
            {
                _logger.LogInformation("Incorrect CAPTCHA reported successfully: {CaptchaId}", captchaId);
                return CaptchaSolvingResult.SuccessResult(null, "Incorrect CAPTCHA reported successfully");
            }
            else
            {
                return CaptchaSolvingResult.ErrorResult($"Failed to report incorrect CAPTCHA: {responseText}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to report incorrect CAPTCHA");
            return CaptchaSolvingResult.ErrorResult($"Report failed: {ex.Message}", ex.ToString());
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsServiceAvailableAsync()
    {
        if (!IsConfigured())
        {
            _logger.LogWarning("2captcha API key not configured - service unavailable");
            return false;
        }
        
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/res.php?key={_apiKey}&action=getbalance");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "2captcha service availability check failed");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<CaptchaSolvingResult> GetServiceStatsAsync()
    {
        try
        {
            // This is a placeholder implementation since 2captcha doesn't provide detailed stats API
            // In production, you might want to maintain local statistics
            var stats = new ServiceStats
            {
                TotalSolved = 0,
                SuccessfulSolutions = 0,
                SuccessRate = 0.95, // Typical 2captcha success rate
                AverageSolveTime = TimeSpan.FromSeconds(45),
                TotalCost = 0,
                LastActivity = DateTime.UtcNow
            };

            return CaptchaSolvingResult.SuccessResult(stats, "Service statistics retrieved");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get service statistics");
            return CaptchaSolvingResult.ErrorResult($"Stats retrieval failed: {ex.Message}", ex.ToString());
        }
    }

    /// <summary>
    /// Check if the service is properly configured with an API key
    /// </summary>
    private bool IsConfigured() => !string.IsNullOrWhiteSpace(_apiKey);

    #region Private Helper Methods

    private async Task<string?> SubmitCaptchaAsync(Dictionary<string, string> parameters)
    {
        try
        {
            var content = new FormUrlEncodedContent(parameters);
            var response = await _httpClient.PostAsync($"{BaseUrl}/in.php", content);
            response.EnsureSuccessStatusCode();

            var responseText = await response.Content.ReadAsStringAsync();
            
            if (responseText.StartsWith("OK|"))
            {
                return responseText.Substring(3);
            }
            else
            {
                _logger.LogWarning("CAPTCHA submission failed: {Response}", responseText);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit CAPTCHA");
            return null;
        }
    }

    private async Task<string?> WaitForSolutionAsync(string captchaId, int timeoutSeconds)
    {
        var startTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);
        var checkInterval = TimeSpan.FromSeconds(5);

        while (DateTime.UtcNow - startTime < timeout)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/res.php?key={_apiKey}&action=get&id={captchaId}");
                response.EnsureSuccessStatusCode();

                var responseText = await response.Content.ReadAsStringAsync();

                if (responseText.StartsWith("OK|"))
                {
                    return responseText.Substring(3);
                }
                else if (responseText == "CAPCHA_NOT_READY")
                {
                    await Task.Delay(checkInterval);
                    continue;
                }
                else
                {
                    _logger.LogWarning("CAPTCHA solution failed: {Response}", responseText);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check CAPTCHA solution status");
                await Task.Delay(checkInterval);
            }
        }

        _logger.LogWarning("CAPTCHA solving timeout after {Timeout} seconds", timeoutSeconds);
        return null;
    }

    #endregion
}

/// <summary>
/// Configuration class for CaptchaSolvingService
/// </summary>
public class CaptchaSolvingServiceConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "http://2captcha.com/";
    public int DefaultTimeout { get; set; } = 300;
    public int PollingInterval { get; set; } = 5;
    public bool EnableDetailedLogging { get; set; } = true;
}