using DigitalMe.Integrations.External.Slack.Models;

namespace DigitalMe.Integrations.External.Slack.Services;

/// <summary>
/// Service for Slack channel management
/// </summary>
public interface ISlackChannelService
{
    /// <summary>
    /// Get list of all channels the bot has access to
    /// </summary>
    Task<IEnumerable<SlackChannel>> GetChannelsAsync(bool includePrivate = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get information about a specific channel
    /// </summary>
    Task<SlackChannel?> GetChannelInfoAsync(string channelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new channel
    /// </summary>
    Task<SlackChannel> CreateChannelAsync(string name, bool isPrivate = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Join an existing channel
    /// </summary>
    Task<bool> JoinChannelAsync(string channelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Leave a channel
    /// </summary>
    Task<bool> LeaveChannelAsync(string channelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invite a user to a channel
    /// </summary>
    Task<bool> InviteUserToChannelAsync(string channelId, string userId, CancellationToken cancellationToken = default);
}