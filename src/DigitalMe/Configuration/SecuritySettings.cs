namespace DigitalMe.Configuration;

/// <summary>
/// Security configuration settings
/// </summary>
public class SecuritySettings
{
    private const int DefaultMaxPayloadSizeBytes = 1024 * 1024; // 1MB
    private const int DefaultRateLimitRequestsPerMinute = 100;
    private const int DefaultJwtTokenExpiryMinutes = 60;

    public int MaxPayloadSizeBytes { get; set; } = DefaultMaxPayloadSizeBytes;
    public int RateLimitRequestsPerMinute { get; set; } = DefaultRateLimitRequestsPerMinute;
    public int JwtTokenExpiryMinutes { get; set; } = DefaultJwtTokenExpiryMinutes;
    public bool EnableInputSanitization { get; set; } = true;
    public bool EnableRateLimiting { get; set; } = true;
    public List<string> AllowedOrigins { get; set; } = new() { "localhost" };
    public List<string> BlockedIpRanges { get; set; } = new();
}