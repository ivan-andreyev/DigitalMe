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
    /// <summary>
    /// ClickUp API Token (Personal or OAuth).
    /// Format: pk_[team_id]_[token]
    /// </summary>
    public string ApiToken { get; set; } = string.Empty;

    /// <summary>
    /// Team ID for team-specific operations.
    /// Can be found in ClickUp team settings.
    /// </summary>
    public string TeamId { get; set; } = string.Empty;

    /// <summary>
    /// Workspace ID (legacy, kept for compatibility).
    /// </summary>
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>
    /// Default Space ID for operations.
    /// Used when space is not explicitly specified.
    /// </summary>
    public string? DefaultSpaceId { get; set; }

    /// <summary>
    /// Default List ID for task creation.
    /// Used when list is not explicitly specified.
    /// </summary>
    public string? DefaultListId { get; set; }

    /// <summary>
    /// Enable time tracking features.
    /// Set to false if time tracking is not needed.
    /// </summary>
    public bool EnableTimeTracking { get; set; } = true;

    /// <summary>
    /// Enable webhook notifications.
    /// Set to false to disable webhook processing.
    /// </summary>
    public bool EnableWebhooks { get; set; } = true;

    /// <summary>
    /// Webhook secret for signature validation.
    /// Used to verify webhook authenticity.
    /// </summary>
    public string? WebhookSecret { get; set; }

    /// <summary>
    /// Default assignee ID for created tasks.
    /// User ID from ClickUp team members.
    /// </summary>
    public int? DefaultAssigneeId { get; set; }

    /// <summary>
    /// Default priority level for new tasks.
    /// Values: 1=Urgent, 2=High, 3=Normal, 4=Low
    /// </summary>
    public int DefaultPriority { get; set; } = 3;

    /// <summary>
    /// Default status for new tasks.
    /// Should match status name in target list/space.
    /// </summary>
    public string DefaultStatus { get; set; } = "To Do";
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
