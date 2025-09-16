using DigitalMe.Integrations.External.Slack.Models;

namespace DigitalMe.Integrations.External.Slack.Services;

/// <summary>
/// Service for Slack message operations
/// </summary>
public class SlackMessageService : ISlackMessageService
{
    private readonly SlackApiClient _apiClient;
    private readonly ILogger<SlackMessageService> _logger;

    public SlackMessageService(SlackApiClient apiClient, ILogger<SlackMessageService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<SlackMessageResponse> SendMessageAsync(string channel, string message, SlackAttachment[]? attachments = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("📤 Sending message to channel {Channel}", channel);

            var request = new SlackMessageRequest
            {
                Channel = channel,
                Text = message,
                Attachments = attachments
            };

            var response = await _apiClient.PostAsync<SlackMessageResponse>("chat.postMessage", request, cancellationToken);

            if (response != null && response.Ok)
            {
                _logger.LogInformation("✅ Message sent successfully to channel {Channel}, timestamp: {Timestamp}",
                    channel, response.Timestamp);
                return response;
            }
            else
            {
                _logger.LogError("❌ Failed to send message to channel {Channel}: {Error}",
                    channel, response?.Error ?? "Unknown error");
                return new SlackMessageResponse { Ok = false, Error = response?.Error ?? "Failed to send message" };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception sending message to channel {Channel}", channel);
            return new SlackMessageResponse { Ok = false, Error = $"Exception: {ex.Message}" };
        }
    }

    public async Task<SlackMessageResponse> UpdateMessageAsync(string channel, string timestamp, string message, SlackAttachment[]? attachments = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("📝 Updating message in channel {Channel}, timestamp: {Timestamp}", channel, timestamp);

            var request = new
            {
                channel = channel,
                ts = timestamp,
                text = message,
                attachments = attachments
            };

            var response = await _apiClient.PostAsync<SlackMessageResponse>("chat.update", request, cancellationToken);

            if (response != null && response.Ok)
            {
                _logger.LogInformation("✅ Message updated successfully in channel {Channel}", channel);
                return response;
            }
            else
            {
                _logger.LogError("❌ Failed to update message in channel {Channel}: {Error}",
                    channel, response?.Error ?? "Unknown error");
                return new SlackMessageResponse { Ok = false, Error = response?.Error ?? "Failed to update message" };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception updating message in channel {Channel}", channel);
            return new SlackMessageResponse { Ok = false, Error = $"Exception: {ex.Message}" };
        }
    }

    public async Task<bool> DeleteMessageAsync(string channel, string timestamp, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("🗑️ Deleting message in channel {Channel}, timestamp: {Timestamp}", channel, timestamp);

            var request = new
            {
                channel = channel,
                ts = timestamp
            };

            var response = await _apiClient.PostAsync<SlackApiResponse>("chat.delete", request, cancellationToken);

            if (response != null && response.Ok)
            {
                _logger.LogInformation("✅ Message deleted successfully from channel {Channel}", channel);
                return true;
            }
            else
            {
                _logger.LogError("❌ Failed to delete message from channel {Channel}: {Error}",
                    channel, response?.Error ?? "Unknown error");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception deleting message from channel {Channel}", channel);
            return false;
        }
    }

    public async Task<SlackMessagesResponse> GetMessageHistoryAsync(string channel, int limit = 100, string? oldest = null, string? latest = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("📜 Getting message history from channel {Channel}, limit: {Limit}", channel, limit);

            var parameters = new Dictionary<string, string>
            {
                ["channel"] = channel,
                ["limit"] = limit.ToString()
            };

            if (!string.IsNullOrEmpty(oldest))
            {
                parameters["oldest"] = oldest;
            }

            if (!string.IsNullOrEmpty(latest))
            {
                parameters["latest"] = latest;
            }

            var response = await _apiClient.GetAsync<SlackMessagesResponse>("conversations.history", parameters, cancellationToken);

            if (response != null && response.Ok)
            {
                _logger.LogInformation("✅ Retrieved {MessageCount} messages from channel {Channel}",
                    response.Messages?.Count() ?? 0, channel);
                return response;
            }
            else
            {
                _logger.LogError("❌ Failed to get message history from channel {Channel}: {Error}",
                    channel, response?.Error ?? "Unknown error");
                return new SlackMessagesResponse { Ok = false, Error = response?.Error ?? "Failed to get message history" };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception getting message history from channel {Channel}", channel);
            return new SlackMessagesResponse { Ok = false, Error = $"Exception: {ex.Message}" };
        }
    }

    public async Task<SlackMessageResponse> SendDirectMessageAsync(string userId, string message, SlackAttachment[]? attachments = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("💬 Sending direct message to user {UserId}", userId);

            // For direct messages, we can use the user ID directly as the channel
            // Slack API accepts user IDs in the channel parameter for DMs
            return await SendMessageAsync(userId, message, attachments, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception sending direct message to user {UserId}", userId);
            return new SlackMessageResponse { Ok = false, Error = $"Exception: {ex.Message}" };
        }
    }
}