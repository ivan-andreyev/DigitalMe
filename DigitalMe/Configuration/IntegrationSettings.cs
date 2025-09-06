namespace DigitalMe.Configuration;

/// <summary>
/// Standard configuration pattern for external integrations
/// </summary>
public class IntegrationSettings
{
    /// <summary>
    /// Slack integration configuration
    /// </summary>
    public SlackSettings Slack { get; set; } = new();
    
    /// <summary>
    /// ClickUp integration configuration  
    /// </summary>
    public ClickUpSettings ClickUp { get; set; } = new();
    
    /// <summary>
    /// Existing integrations
    /// </summary>
    public TelegramSettings Telegram { get; set; } = new();
    public GitHubSettings GitHub { get; set; } = new();
    public GoogleSettings Google { get; set; } = new();
}

/// <summary>
/// Slack integration configuration
/// </summary>
public class SlackSettings : BaseIntegrationSettings
{
    public string BotToken { get; set; } = string.Empty;
    public string SigningSecret { get; set; } = string.Empty;
    public string WorkspaceId { get; set; } = string.Empty;
}

/// <summary>
/// ClickUp integration configuration
/// </summary>
public class ClickUpSettings : BaseIntegrationSettings
{
    public string ApiToken { get; set; } = string.Empty;
    public string TeamId { get; set; } = string.Empty;
    public string WorkspaceId { get; set; } = string.Empty;
}

/// <summary>
/// Enhanced GitHub integration configuration
/// </summary>
public class GitHubSettings : BaseIntegrationSettings
{
    public string PersonalAccessToken { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string Organization { get; set; } = string.Empty;
}

/// <summary>
/// Telegram integration configuration
/// </summary>
public class TelegramSettings : BaseIntegrationSettings
{
    public string BotToken { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
}

/// <summary>
/// Google services configuration
/// </summary>
public class GoogleSettings : BaseIntegrationSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
}

/// <summary>
/// Base configuration for all integrations
/// </summary>
public abstract class BaseIntegrationSettings
{
    /// <summary>
    /// Whether this integration is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// API endpoint override (optional)
    /// </summary>
    public string? BaseUrl { get; set; }
    
    /// <summary>
    /// Timeout in seconds for API calls
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// Max retries for failed requests
    /// </summary>
    public int MaxRetries { get; set; } = 3;
    
    /// <summary>
    /// Rate limiting - requests per minute
    /// </summary>
    public int RateLimitPerMinute { get; set; } = 60;
}