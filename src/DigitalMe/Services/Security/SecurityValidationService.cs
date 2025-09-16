using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DigitalMe.Common;
using DigitalMe.Configuration;
using DigitalMe.Services.Optimization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DigitalMe.Services.Security;

/// <summary>
/// Comprehensive security validation and sanitization service
/// </summary>
public class SecurityValidationService : ISecurityValidationService
{
    private readonly ILogger<SecurityValidationService> _logger;
    private readonly IMemoryCache _cache;
    private readonly IPerformanceOptimizationService _performanceService;
    private readonly SecuritySettings _securitySettings;
    private readonly JwtSettings _jwtSettings;

    // XSS protection patterns
    private readonly Regex _scriptPattern = new(@"<script[^>]*>.*?</script>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private readonly Regex _onEventPattern = new(@"on\w+\s*=", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private readonly Regex _javascriptPattern = new(@"javascript:", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // SQL Injection protection patterns
    private readonly Regex _sqlPattern = new(@"(;|\||'|--|\*|/\*|\*/|xp_|sp_)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public SecurityValidationService(
        ILogger<SecurityValidationService> logger,
        IMemoryCache cache,
        IPerformanceOptimizationService performanceService,
        IOptions<SecuritySettings> securitySettings,
        IOptions<JwtSettings> jwtSettings)
    {
        _logger = logger;
        _cache = cache;
        _performanceService = performanceService;
        _securitySettings = securitySettings.Value;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<Result<SecurityValidationData>> ValidateRequestAsync<T>(T request) where T : class
    {
        return await ResultExtensions.TryAsync(async () =>
        {
            var errors = new List<string>();

            // 1. Data annotation validation
            var validationContext = new ValidationContext(request);
            var validationResults = new List<ValidationResult>();

            if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
            {
                errors.AddRange(validationResults.Select(vr => vr.ErrorMessage ?? "Validation error"));
            }

            if (errors.Any())
            {
                throw new ValidationException(string.Join("; ", errors));
            }

            // 2. Input sanitization if enabled
            var sanitizedData = _securitySettings.EnableInputSanitization ? SanitizeObject(request) : request;

            return new SecurityValidationData
            {
                SanitizedData = sanitizedData
            };
        }, $"Error validating request of type {typeof(T).Name}");
    }

    public Result<string> SanitizeInput(string input)
    {
        return ResultExtensions.Try(() =>
        {
            if (string.IsNullOrEmpty(input))
                return input;

            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Remove script tags
            input = _scriptPattern.Replace(input, string.Empty);

            // Remove on* event handlers
            input = _onEventPattern.Replace(input, string.Empty);

            // Remove javascript: protocols
            input = _javascriptPattern.Replace(input, string.Empty);

            // Basic HTML encoding for special characters
            input = input.Replace("<", "&lt;")
                        .Replace(">", "&gt;")
                        .Replace("\"", "&quot;")
                        .Replace("'", "&#x27;")
                        .Replace("/", "&#x2F;");

            // Remove potential SQL injection patterns (conservative approach)
            if (_sqlPattern.IsMatch(input))
            {
                _logger.LogWarning("Potential SQL injection pattern detected in input, sanitizing");
                input = _sqlPattern.Replace(input, string.Empty);
            }

            return input.Trim();
        }, $"Error sanitizing input");
    }

    public Result<bool> ValidateApiKeyFormat(string apiKey)
    {
        return ResultExtensions.Try(() =>
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return false;

            // API key should be at least MinimumApiKeyLength characters and contain alphanumeric + allowed special chars
            if (apiKey.Length < MinimumApiKeyLength)
                return false;

            // Check for reasonable API key pattern (no whitespace, reasonable characters)
            var apiKeyPattern = new Regex(@"^[a-zA-Z0-9\-_\.]+$", RegexOptions.Compiled);
            return apiKeyPattern.IsMatch(apiKey);
        }, "Error validating API key format");
    }

    private const int MinimumApiKeyLength = 32;

    public async Task<Result<bool>> ValidateWebhookPayloadAsync(string payload, int maxSizeBytes = DefaultMaxPayloadSize)
    {
        if (string.IsNullOrEmpty(payload))
        {
            _logger.LogWarning("Empty webhook payload received");
            return false;
        }

        var payloadBytes = Encoding.UTF8.GetByteCount(payload);
        if (payloadBytes > maxSizeBytes)
        {
            _logger.LogWarning("Webhook payload too large: {Size} bytes (max: {MaxSize})",
                payloadBytes, maxSizeBytes);
            return false;
        }

        try
        {
            JsonDocument.Parse(payload);
            return true;
        }
        catch (JsonException)
        {
            _logger.LogWarning("Invalid JSON in webhook payload");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating webhook payload");
            return false;
        }
    }

    private const int DefaultMaxPayloadSize = 1048576; // 1MB

    public async Task<Result<bool>> IsRateLimitExceededAsync(string clientIdentifier, string endpoint)
    {
        if (!_securitySettings.EnableRateLimiting)
            return false;

        try
        {
            return await _performanceService.ShouldRateLimitAsync("security", $"{clientIdentifier}:{endpoint}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit for {ClientId}:{Endpoint}",
                clientIdentifier, endpoint);
            return false; // Allow request on error to avoid blocking legitimate traffic
        }
    }

    public async Task<Result<SecurityValidationData>> ValidateJwtTokenAsync(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return SecurityValidationResult.Failure("Missing JWT token");
            }

            var tokenHandler = new JwtSecurityTokenHandler();

            // Remove Bearer prefix if present
            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = token.Substring("Bearer ".Length);
            }

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key)),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            var claims = principal.Claims.ToDictionary(c => c.Type, c => (object)c.Value);

            return new SecurityValidationResult
            {
                IsValid = true,
                Claims = claims
            };
        }
        catch (SecurityTokenExpiredException)
        {
            return SecurityValidationResult.Failure("Token has expired");
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning("JWT validation failed: {Message}", ex.Message);
            return SecurityValidationResult.Failure("Invalid token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating JWT token");
            return SecurityValidationResult.Failure("Token validation error");
        }
    }

    public Result<T> SanitizeResponse<T>(T response) where T : class
    {
        if (!_securitySettings.EnableInputSanitization)
            return response;

        try
        {
            return SanitizeObject(response) ?? response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sanitizing response of type {ResponseType}", typeof(T).Name);
            return response; // Return original on error
        }
    }

    private T? SanitizeObject<T>(T obj) where T : class
    {
        if (obj == null)
            return null;

        try
        {
            var json = JsonSerializer.Serialize(obj);
            var sanitizedJson = SanitizeInput(json);

            // Only return sanitized version if it's different and still valid JSON
            if (sanitizedJson != json)
            {
                try
                {
                    return JsonSerializer.Deserialize<T>(sanitizedJson);
                }
                catch (JsonException)
                {
                    // Return original if sanitized version is not valid JSON
                    return obj;
                }
            }

            return obj;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sanitizing object of type {Type}", typeof(T).Name);
            return obj;
        }
    }
}
