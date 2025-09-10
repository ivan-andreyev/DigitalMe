namespace DigitalMe.Configuration;

/// <summary>
/// JWT configuration settings
/// </summary>
public class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpireHours { get; set; } = 24;
}
