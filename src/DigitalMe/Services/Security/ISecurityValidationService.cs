using System.ComponentModel.DataAnnotations;

namespace DigitalMe.Services.Security;

/// <summary>
/// Service for comprehensive security validation and sanitization
/// </summary>
public interface ISecurityValidationService
{
    /// <summary>
    /// Validates and sanitizes incoming request data
    /// </summary>
    Task<SecurityValidationResult> ValidateRequestAsync<T>(T request) where T : class;

    /// <summary>
    /// Sanitizes string input to prevent XSS and injection attacks
    /// </summary>
    string SanitizeInput(string input);

    /// <summary>
    /// Validates API key format and strength
    /// </summary>
    bool ValidateApiKeyFormat(string apiKey);

    /// <summary>
    /// Validates webhook payload size and structure
    /// </summary>
    Task<bool> ValidateWebhookPayloadAsync(string payload, int maxSizeBytes = 1024 * 1024); // 1MB default

    /// <summary>
    /// Checks if request rate limit is exceeded
    /// </summary>
    Task<bool> IsRateLimitExceededAsync(string clientIdentifier, string endpoint);

    /// <summary>
    /// Validates JWT token and returns claims if valid
    /// </summary>
    Task<SecurityValidationResult> ValidateJwtTokenAsync(string token);

    /// <summary>
    /// Sanitizes response data before sending to client
    /// </summary>
    T SanitizeResponse<T>(T response) where T : class;
}

/// <summary>
/// Result of security validation
/// </summary>
public class SecurityValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public Dictionary<string, object> Claims { get; set; } = new();
    public object? SanitizedData { get; set; }

    public static SecurityValidationResult Success(object? sanitizedData = null)
    {
        return new SecurityValidationResult
        {
            IsValid = true,
            SanitizedData = sanitizedData
        };
    }

    public static SecurityValidationResult Failure(params string[] errors)
    {
        return new SecurityValidationResult
        {
            IsValid = false,
            Errors = errors.ToList()
        };
    }
}

