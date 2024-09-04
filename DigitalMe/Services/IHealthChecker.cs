namespace DigitalMe.Services;

public interface IHealthChecker
{
    Task<HealthStatus> GetHealthStatusAsync();
}

public class HealthStatus
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public DatabaseHealth Database { get; set; } = new();
    public ConfigurationHealth Configuration { get; set; } = new();
}

public class DatabaseHealth
{
    public string Status { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public DateTime LastChecked { get; set; }
    public string? Error { get; set; }
}

public class ConfigurationHealth
{
    public string DatabaseProvider { get; set; } = string.Empty;
    public string CloudSqlInstance { get; set; } = string.Empty;
    public bool AnthropicConfigured { get; set; }
    public bool GitHubConfigured { get; set; }
    public bool TelegramConfigured { get; set; }
}