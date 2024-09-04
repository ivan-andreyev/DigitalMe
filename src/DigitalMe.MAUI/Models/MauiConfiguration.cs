namespace DigitalMe.MAUI.Models;

public class MauiConfiguration
{
    public string ApiBaseUrl { get; set; } = "https://localhost:7064";
    public string SignalRHub { get; set; } = "/chathub";
    public AuthenticationConfiguration Authentication { get; set; } = new();
    public FeatureConfiguration Features { get; set; } = new();
}

public class AuthenticationConfiguration
{
    public string JwtSecret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "DigitalMe";
    public string Audience { get; set; } = "DigitalMe-MAUI";
}

public class FeatureConfiguration
{
    public bool UseRealSignalR { get; set; } = true;
    public bool UseRealAuthentication { get; set; } = false;
    public bool EnableLogging { get; set; } = true;
}