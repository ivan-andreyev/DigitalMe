using DigitalMe.Integrations.External.Slack.Models;

namespace DigitalMe.Integrations.External.Slack.Services;

/// <summary>
/// Service for Slack message operations
/// </summary>
public interface ISlackMessageService
{
    /// <summary>
    /// Send a message to a Slack channel
    /// </summary>
    Task<SlackMessageResponse> SendMessageAsync(string channel, string message, SlackAttachment[]? attachments = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing message
    /// </summary>
    Task<SlackMessageResponse> UpdateMessageAsync(string channel, string timestamp, string message, SlackAttachment[]? attachments = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a message
    /// </summary>
    Task<bool> DeleteMessageAsync(string channel, string timestamp, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get message history from a channel
    /// </summary>
    Task<SlackMessagesResponse> GetMessageHistoryAsync(string channel, int limit = 100, string? oldest = null, string? latest = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a direct message to a user
    /// </summary>
    Task<SlackMessageResponse> SendDirectMessageAsync(string userId, string message, SlackAttachment[]? attachments = null, CancellationToken cancellationToken = default);
}