namespace DigitalMe.Integrations.External.Slack.Services;

/// <summary>
/// Service for managing Slack connection lifecycle
/// </summary>
public interface ISlackConnectionService
{
    /// <summary>
    /// Initialize connection to Slack with bot token
    /// </summary>
    Task<bool> InitializeAsync(string botToken);

    /// <summary>
    /// Check if currently connected to Slack
    /// </summary>
    Task<bool> IsConnectedAsync();

    /// <summary>
    /// Disconnect from Slack
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Test the connection and API credentials
    /// </summary>
    Task<bool> TestConnectionAsync();
}