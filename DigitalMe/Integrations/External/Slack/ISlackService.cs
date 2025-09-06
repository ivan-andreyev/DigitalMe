using DigitalMe.Integrations.External.Slack.Models;

namespace DigitalMe.Integrations.External.Slack;

/// <summary>
/// Interface for Slack integration services
/// Provides core Slack functionality: messages, files, interactive elements
/// </summary>
public interface ISlackService
{
    // Connection and initialization
    Task<bool> InitializeAsync(string botToken);
    Task<bool> IsConnectedAsync();
    Task DisconnectAsync();
    
    // Basic messaging
    Task<SlackMessageResponse> SendMessageAsync(string channel, string message);
    Task<SlackMessageResponse> SendMessageAsync(string channel, SlackMessageRequest message);
    Task<SlackMessageResponse> UpdateMessageAsync(string channel, string timestamp, string message);
    Task<bool> DeleteMessageAsync(string channel, string timestamp);
    
    // File operations
    Task<SlackFileResponse> UploadFileAsync(string channel, Stream file, string filename, string? title = null);
    Task<SlackFileResponse> UploadFileAsync(string channel, byte[] fileContent, string filename, string? title = null);
    Task<bool> DeleteFileAsync(string fileId);
    
    // Channel management
    Task<IEnumerable<SlackChannel>> GetChannelsAsync();
    Task<SlackChannel> GetChannelInfoAsync(string channelId);
    Task<SlackChannel> CreateChannelAsync(string name, bool isPrivate = false);
    Task<bool> JoinChannelAsync(string channelId);
    Task<bool> LeaveChannelAsync(string channelId);
    
    // User management
    Task<IEnumerable<SlackUser>> GetUsersAsync();
    Task<SlackUser> GetUserInfoAsync(string userId);
    Task<SlackUser> GetBotInfoAsync();
    
    // Interactive elements
    Task<SlackMessageResponse> SendMessageWithButtonsAsync(string channel, string message, IEnumerable<SlackButton> buttons);
    Task<SlackMessageResponse> SendMessageWithAttachmentsAsync(string channel, string message, IEnumerable<SlackAttachment> attachments);
    
    // Message history
    Task<IEnumerable<SlackMessage>> GetMessagesAsync(string channel, int limit = 50);
    Task<IEnumerable<SlackMessage>> GetMessagesAsync(string channel, DateTime since, int limit = 50);
    Task<SlackMessage> GetMessageAsync(string channel, string timestamp);
    
    // Reactions
    Task<bool> AddReactionAsync(string channel, string timestamp, string reaction);
    Task<bool> RemoveReactionAsync(string channel, string timestamp, string reaction);
    Task<IEnumerable<SlackReaction>> GetReactionsAsync(string channel, string timestamp);
}