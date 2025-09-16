using System.ComponentModel.DataAnnotations;
using DigitalMe.Common;

namespace DigitalMe.Services.Security;

/// <summary>
/// Service for comprehensive security validation and sanitization
/// </summary>
public interface ISecurityValidationService
{
    /// <summary>
    /// Validates and sanitizes incoming request data
    /// </summary>
    Task<Result<SecurityValidationData>> ValidateRequestAsync<T>(T request) where T : class;

    /// <summary>
    /// Sanitizes string input to prevent XSS and injection attacks
    /// </summary>
    Result<string> SanitizeInput(string input);

    /// <summary>
    /// Validates API key format and strength
    /// </summary>
    Result<bool> ValidateApiKeyFormat(string apiKey);

    /// <summary>
    /// Validates webhook payload size and structure
    /// </summary>
    Task<Result<bool>> ValidateWebhookPayloadAsync(string payload, int maxSizeBytes = 1024 * 1024); // 1MB default

    /// <summary>
    /// Checks if request rate limit is exceeded
    /// </summary>
    Task<Result<bool>> IsRateLimitExceededAsync(string clientIdentifier, string endpoint);

    /// <summary>
    /// Validates JWT token and returns claims if valid
    /// </summary>
    Task<Result<SecurityValidationData>> ValidateJwtTokenAsync(string token);

    /// <summary>
    /// Sanitizes response data before sending to client
    /// </summary>
    Result<T> SanitizeResponse<T>(T response) where T : class;
}

/// <summary>
/// Data container for security validation results when using Result<T> pattern
/// </summary>
public class SecurityValidationData
{
    public Dictionary<string, object> Claims { get; set; } = new();
    public object? SanitizedData { get; set; }
}

/// <summary>
/// Legacy SecurityValidationResult for backward compatibility
/// </summary>
[Obsolete("Use Result<SecurityValidationData> pattern instead", false)]
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

