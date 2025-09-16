using DigitalMe.Integrations.External.Slack.Models;

namespace DigitalMe.Integrations.External.Slack.Services;

/// <summary>
/// Service for Slack channel management
/// </summary>
public class SlackChannelService : ISlackChannelService
{
    private readonly SlackApiClient _apiClient;
    private readonly ILogger<SlackChannelService> _logger;

    public SlackChannelService(SlackApiClient apiClient, ILogger<SlackChannelService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<IEnumerable<SlackChannel>> GetChannelsAsync(bool includePrivate = false, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("📋 Getting channels list, includePrivate: {IncludePrivate}", includePrivate);

            var parameters = new Dictionary<string, string>
            {
                ["exclude_archived"] = "true",
                ["types"] = includePrivate ? "public_channel,private_channel" : "public_channel"
            };

            var response = await _apiClient.GetAsync<SlackChannelsResponse>("conversations.list", parameters, cancellationToken);

            if (response != null && response.Ok)
            {
                _logger.LogInformation("✅ Retrieved {ChannelCount} channels", response.Channels?.Count() ?? 0);
                return response.Channels ?? Enumerable.Empty<SlackChannel>();
            }
            else
            {
                _logger.LogError("❌ Failed to get channels: {Error}", response?.Error ?? "Unknown error");
                return Enumerable.Empty<SlackChannel>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception getting channels");
            return Enumerable.Empty<SlackChannel>();
        }
    }

    public async Task<SlackChannel?> GetChannelInfoAsync(string channelId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("📄 Getting channel info for {ChannelId}", channelId);

            var parameters = new Dictionary<string, string>
            {
                ["channel"] = channelId
            };

            var response = await _apiClient.GetAsync<SlackChannelResponse>("conversations.info", parameters, cancellationToken);

            if (response != null && response.Ok)
            {
                _logger.LogInformation("✅ Retrieved channel info for {ChannelId}: {ChannelName}",
                    channelId, response.Channel?.Name ?? "Unknown");
                return response.Channel;
            }
            else
            {
                _logger.LogError("❌ Failed to get channel info for {ChannelId}: {Error}",
                    channelId, response?.Error ?? "Unknown error");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception getting channel info for {ChannelId}", channelId);
            return null;
        }
    }

    public async Task<SlackChannel> CreateChannelAsync(string name, bool isPrivate = false, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("🆕 Creating channel {ChannelName}, private: {IsPrivate}", name, isPrivate);

            var request = new
            {
                name = name,
                is_private = isPrivate
            };

            var response = await _apiClient.PostAsync<SlackChannelResponse>("conversations.create", request, cancellationToken);

            if (response != null && response.Ok && response.Channel != null)
            {
                _logger.LogInformation("✅ Channel created successfully: {ChannelName} ({ChannelId})",
                    response.Channel.Name, response.Channel.Id);
                return response.Channel;
            }
            else
            {
                _logger.LogError("❌ Failed to create channel {ChannelName}: {Error}",
                    name, response?.Error ?? "Unknown error");
                throw new InvalidOperationException($"Failed to create channel: {response?.Error ?? "Unknown error"}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception creating channel {ChannelName}", name);
            throw;
        }
    }

    public async Task<bool> JoinChannelAsync(string channelId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("👋 Joining channel {ChannelId}", channelId);

            var request = new
            {
                channel = channelId
            };

            var response = await _apiClient.PostAsync<SlackApiResponse>("conversations.join", request, cancellationToken);

            if (response != null && response.Ok)
            {
                _logger.LogInformation("✅ Successfully joined channel {ChannelId}", channelId);
                return true;
            }
            else
            {
                _logger.LogError("❌ Failed to join channel {ChannelId}: {Error}",
                    channelId, response?.Error ?? "Unknown error");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception joining channel {ChannelId}", channelId);
            return false;
        }
    }

    public async Task<bool> LeaveChannelAsync(string channelId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("👋 Leaving channel {ChannelId}", channelId);

            var request = new
            {
                channel = channelId
            };

            var response = await _apiClient.PostAsync<SlackApiResponse>("conversations.leave", request, cancellationToken);

            if (response != null && response.Ok)
            {
                _logger.LogInformation("✅ Successfully left channel {ChannelId}", channelId);
                return true;
            }
            else
            {
                _logger.LogError("❌ Failed to leave channel {ChannelId}: {Error}",
                    channelId, response?.Error ?? "Unknown error");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception leaving channel {ChannelId}", channelId);
            return false;
        }
    }

    public async Task<bool> InviteUserToChannelAsync(string channelId, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("👤 Inviting user {UserId} to channel {ChannelId}", userId, channelId);

            var request = new
            {
                channel = channelId,
                users = userId
            };

            var response = await _apiClient.PostAsync<SlackApiResponse>("conversations.invite", request, cancellationToken);

            if (response != null && response.Ok)
            {
                _logger.LogInformation("✅ Successfully invited user {UserId} to channel {ChannelId}", userId, channelId);
                return true;
            }
            else
            {
                _logger.LogError("❌ Failed to invite user {UserId} to channel {ChannelId}: {Error}",
                    userId, channelId, response?.Error ?? "Unknown error");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception inviting user {UserId} to channel {ChannelId}", userId, channelId);
            return false;
        }
    }
}