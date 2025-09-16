using DigitalMe.Integrations.External.Slack.Models;

namespace DigitalMe.Integrations.External.Slack.Services;

/// <summary>
/// Service for managing Slack connection lifecycle
/// </summary>
public class SlackConnectionService : ISlackConnectionService
{
    private readonly SlackApiClient _apiClient;
    private readonly ILogger<SlackConnectionService> _logger;

    private bool _isConnected = false;

    public SlackConnectionService(SlackApiClient apiClient, ILogger<SlackConnectionService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<bool> InitializeAsync(string botToken)
    {
        if (string.IsNullOrWhiteSpace(botToken))
        {
            _logger.LogError("Slack bot token is required for initialization");
            return false;
        }

        try
        {
            _logger.LogInformation("Initializing Slack connection...");

            _apiClient.SetBotToken(botToken);

            // Test the connection by calling auth.test
            var isConnected = await TestConnectionAsync();

            if (isConnected)
            {
                _isConnected = true;
                _logger.LogInformation("✅ Successfully connected to Slack");
                return true;
            }
            else
            {
                _logger.LogError("❌ Failed to connect to Slack - invalid token or API error");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error initializing Slack connection");
            return false;
        }
    }

    public Task<bool> IsConnectedAsync()
    {
        return Task.FromResult(_isConnected);
    }

    public Task DisconnectAsync()
    {
        _isConnected = false;
        _logger.LogInformation("Disconnected from Slack");
        return Task.CompletedTask;
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            _logger.LogDebug("Testing Slack connection...");

            var response = await _apiClient.GetAsync<SlackAuthResponse>("auth.test");

            if (response != null && response.Ok)
            {
                _logger.LogDebug("Connection test successful - User: {User}, Team: {TeamName}",
                    response.User ?? "Unknown",
                    response.Team ?? "Unknown");
                return true;
            }
            else
            {
                _logger.LogWarning("Connection test failed - Response: {@Response}", response);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Slack connection");
            return false;
        }
    }
}