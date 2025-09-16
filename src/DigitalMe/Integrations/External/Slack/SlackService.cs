using DigitalMe.Configuration;
using DigitalMe.Integrations.External.Slack.Models;
using DigitalMe.Integrations.External.Slack.Services;
using Microsoft.Extensions.Options;

namespace DigitalMe.Integrations.External.Slack;

/// <summary>
/// Facade for Slack integration services implementing comprehensive Slack API functionality.
/// Delegates to specialized services following Single Responsibility Principle.
/// </summary>
public class SlackService : ISlackService, IDisposable
{
    private readonly ISlackApiClient _apiClient;
    private readonly SlackConnectionService _connectionService;
    private readonly ISlackMessageService _messageService;
    private readonly ISlackChannelService _channelService;
    private readonly ISlackFileService _fileService;
    private readonly ISlackUserService _userService;
    private readonly ISlackReactionService _reactionService;
    private readonly ILogger<SlackService> _logger;

    public SlackService(
        ISlackApiClient apiClient,
        SlackConnectionService connectionService,
        ISlackMessageService messageService,
        ISlackChannelService channelService,
        ISlackFileService fileService,
        ISlackUserService userService,
        ISlackReactionService reactionService,
        ILogger<SlackService> logger)
    {
        _apiClient = apiClient;
        _connectionService = connectionService;
        _messageService = messageService;
        _channelService = channelService;
        _fileService = fileService;
        _userService = userService;
        _reactionService = reactionService;
        _logger = logger;
    }

    #region Connection and Initialization - Delegate to SlackConnectionService

    public async Task<bool> InitializeAsync(string botToken)
    {
        var result = await _connectionService.InitializeAsync(botToken);
        if (result)
        {
            _apiClient.SetBotToken(botToken);
        }
        return result;
    }

    public async Task<bool> IsConnectedAsync()
    {
        return await _connectionService.IsConnectedAsync();
    }

    public async Task DisconnectAsync()
    {
        await _connectionService.DisconnectAsync();
    }

    #endregion

    #region Basic Messaging - Delegate to ISlackMessageService

    public async Task<SlackMessageResponse> SendMessageAsync(string channel, string message)
    {
        return await _messageService.SendMessageAsync(channel, message);
    }

    public async Task<SlackMessageResponse> SendMessageAsync(string channel, SlackMessageRequest message)
    {
        return await _messageService.SendMessageAsync(message.Channel, message.Text, message.Attachments?.ToArray());
    }

    public async Task<SlackMessageResponse> UpdateMessageAsync(string channel, string timestamp, string message)
    {
        return await _messageService.UpdateMessageAsync(channel, timestamp, message);
    }

    public async Task<bool> DeleteMessageAsync(string channel, string timestamp)
    {
        return await _messageService.DeleteMessageAsync(channel, timestamp);
    }

    #endregion

    #region File Operations - Delegate to ISlackFileService

    public async Task<SlackFileResponse> UploadFileAsync(string channel, Stream file, string filename, string? title = null)
    {
        return await _fileService.UploadFileAsync(channel, file, filename, title);
    }

    public async Task<SlackFileResponse> UploadFileAsync(string channel, byte[] fileContent, string filename, string? title = null)
    {
        using var stream = new MemoryStream(fileContent);
        return await _fileService.UploadFileAsync(channel, stream, filename, title);
    }

    public async Task<bool> DeleteFileAsync(string fileId)
    {
        return await _fileService.DeleteFileAsync(fileId);
    }

    #endregion

    #region Channel Management - Delegate to ISlackChannelService

    public async Task<IEnumerable<SlackChannel>> GetChannelsAsync()
    {
        return await _channelService.GetChannelsAsync();
    }

    public async Task<SlackChannel> GetChannelInfoAsync(string channelId)
    {
        var channel = await _channelService.GetChannelInfoAsync(channelId);
        return channel ?? throw new InvalidOperationException($"Channel {channelId} not found");
    }

    public async Task<SlackChannel> CreateChannelAsync(string name, bool isPrivate = false)
    {
        return await _channelService.CreateChannelAsync(name, isPrivate);
    }

    public async Task<bool> JoinChannelAsync(string channelId)
    {
        return await _channelService.JoinChannelAsync(channelId);
    }

    public async Task<bool> LeaveChannelAsync(string channelId)
    {
        return await _channelService.LeaveChannelAsync(channelId);
    }

    #endregion

    #region User Management - Delegate to ISlackUserService

    public async Task<IEnumerable<SlackUser>> GetUsersAsync()
    {
        return await _userService.GetUsersAsync();
    }

    public async Task<SlackUser> GetUserInfoAsync(string userId)
    {
        var user = await _userService.GetUserInfoAsync(userId);
        return user ?? throw new InvalidOperationException($"User {userId} not found");
    }

    public async Task<SlackUser> GetBotInfoAsync()
    {
        return await _connectionService.GetBotInfoAsync();
    }

    #endregion

    #region Interactive Elements - Enhanced messaging functionality

    public async Task<SlackMessageResponse> SendMessageWithButtonsAsync(string channel, string message, IEnumerable<SlackButton> buttons)
    {
        try
        {
            _logger.LogInformation("📤 Sending message with buttons to channel {Channel}", channel);

            var attachments = new[]
            {
                new SlackAttachment
                {
                    Text = message,
                    Color = "good"
                }
            };

            return await _messageService.SendMessageAsync(channel, message, attachments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to send message with buttons to channel {Channel}", channel);
            return new SlackMessageResponse { Ok = false, Error = $"Exception: {ex.Message}" };
        }
    }

    public async Task<SlackMessageResponse> SendMessageWithAttachmentsAsync(string channel, string message, IEnumerable<SlackAttachment> attachments)
    {
        return await _messageService.SendMessageAsync(channel, message, attachments.ToArray());
    }

    #endregion

    #region Message History - Enhanced message operations

    public async Task<IEnumerable<SlackMessage>> GetMessagesAsync(string channel, int limit = 50)
    {
        var response = await _messageService.GetMessageHistoryAsync(channel, limit);
        return response.Messages ?? Enumerable.Empty<SlackMessage>();
    }

    public async Task<IEnumerable<SlackMessage>> GetMessagesAsync(string channel, DateTime since, int limit = 50)
    {
        var sinceTimestamp = ((DateTimeOffset)since).ToUnixTimeSeconds().ToString();
        var response = await _messageService.GetMessageHistoryAsync(channel, limit, sinceTimestamp);
        return response.Messages ?? Enumerable.Empty<SlackMessage>();
    }

    public async Task<SlackMessage> GetMessageAsync(string channel, string timestamp)
    {
        var response = await _messageService.GetMessageHistoryAsync(channel, 1, timestamp, timestamp);
        var message = response.Messages?.FirstOrDefault();
        return message ?? throw new InvalidOperationException($"Message {timestamp} not found in channel {channel}");
    }

    #endregion

    #region Reactions - Delegate to ISlackReactionService

    public async Task<bool> AddReactionAsync(string channel, string timestamp, string reaction)
    {
        return await _reactionService.AddReactionAsync(channel, timestamp, reaction);
    }

    public async Task<bool> RemoveReactionAsync(string channel, string timestamp, string reaction)
    {
        return await _reactionService.RemoveReactionAsync(channel, timestamp, reaction);
    }

    public async Task<IEnumerable<SlackReaction>> GetReactionsAsync(string channel, string timestamp)
    {
        return await _reactionService.GetReactionsAsync(channel, timestamp);
    }

    #endregion

    public void Dispose()
    {
        _apiClient?.Dispose();
    }
}