using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using DigitalMe.Configuration;
using DigitalMe.Integrations.External.Slack.Models;

namespace DigitalMe.Integrations.External.Slack;

/// <summary>
/// Slack integration service implementing comprehensive Slack API functionality.
/// Provides messaging, file operations, channel management, user management, and interactive elements.
/// </summary>
public class SlackService : ISlackService, IDisposable
{
    private readonly ILogger<SlackService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SlackSettings _settings;
    private readonly IOptionsMonitor<IntegrationSettings> _integrationSettings;
    
    private string _botToken = string.Empty;
    private bool _isConnected = false;
    private const string SlackApiBaseUrl = "https://slack.com/api/";
    
    // Rate limiting - Slack allows 1+ requests per second per method
    private readonly SemaphoreSlim _rateLimitSemaphore;
    private DateTime _lastRequestTime = DateTime.MinValue;
    private readonly TimeSpan _rateLimitDelay = TimeSpan.FromMilliseconds(1100); // 1.1 seconds between requests

    public SlackService(
        ILogger<SlackService> logger,
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<IntegrationSettings> integrationSettings)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _integrationSettings = integrationSettings;
        _settings = integrationSettings.CurrentValue.Slack;
        _rateLimitSemaphore = new SemaphoreSlim(1, 1);
    }

    #region Connection and Initialization

    public async Task<bool> InitializeAsync(string botToken)
    {
        if (string.IsNullOrWhiteSpace(botToken))
        {
            _logger.LogError("Slack bot token is required for initialization");
            return false;
        }

        try
        {
            _logger.LogInformation("Initializing Slack Bot connection...");
            _botToken = botToken;

            // Test the connection by getting bot information
            var botInfo = await GetBotInfoAsync();
            _isConnected = botInfo != null;

            if (_isConnected)
            {
                _logger.LogInformation("Slack Bot connection established: {BotName} ({BotId})", 
                    botInfo?.Name ?? "Unknown", botInfo?.Id ?? "unknown");
            }

            return _isConnected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Slack Bot connection");
            _isConnected = false;
            return false;
        }
    }

    public Task<bool> IsConnectedAsync()
    {
        return Task.FromResult(_isConnected && !string.IsNullOrEmpty(_botToken));
    }

    public async Task DisconnectAsync()
    {
        _logger.LogInformation("Disconnecting from Slack API...");
        
        _isConnected = false;
        _botToken = string.Empty;
        
        await Task.CompletedTask;
    }

    #endregion

    #region Basic Messaging

    public async Task<SlackMessageResponse> SendMessageAsync(string channel, string message)
    {
        var messageRequest = new SlackMessageRequest
        {
            Channel = channel,
            Text = message
        };

        return await SendMessageAsync(channel, messageRequest);
    }

    public async Task<SlackMessageResponse> SendMessageAsync(string channel, SlackMessageRequest message)
    {
        EnsureConnected();

        try
        {
            _logger.LogInformation("Sending Slack message to channel {Channel}: {Text}", channel, message.Text);

            var payload = new
            {
                channel = message.Channel ?? channel,
                text = message.Text,
                blocks = message.Blocks,
                attachments = message.Attachments,
                thread_ts = message.ThreadTimestamp,
                reply_broadcast = message.ReplyBroadcast,
                unfurl_links = message.UnfurlLinks,
                unfurl_media = message.UnfurlMedia
            };

            var response = await MakeSlackApiCallAsync<SlackMessageResponse>("chat.postMessage", payload);
            
            _logger.LogInformation("Message sent successfully to {Channel}, timestamp: {Timestamp}", 
                channel, response?.Timestamp);
            
            return response ?? new SlackMessageResponse { Ok = false, Error = "Unknown error" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to Slack channel {Channel}", channel);
            throw;
        }
    }

    public async Task<SlackMessageResponse> UpdateMessageAsync(string channel, string timestamp, string message)
    {
        EnsureConnected();

        try
        {
            _logger.LogInformation("Updating Slack message in channel {Channel}, timestamp {Timestamp}", 
                channel, timestamp);

            var payload = new
            {
                channel,
                ts = timestamp,
                text = message
            };

            var response = await MakeSlackApiCallAsync<SlackMessageResponse>("chat.update", payload);
            
            _logger.LogInformation("Message updated successfully in {Channel}, timestamp: {Timestamp}", 
                channel, timestamp);
            
            return response ?? new SlackMessageResponse { Ok = false, Error = "Unknown error" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update message in Slack channel {Channel}, timestamp {Timestamp}", 
                channel, timestamp);
            throw;
        }
    }

    public async Task<bool> DeleteMessageAsync(string channel, string timestamp)
    {
        EnsureConnected();

        try
        {
            _logger.LogInformation("Deleting Slack message from channel {Channel}, timestamp {Timestamp}", 
                channel, timestamp);

            var payload = new
            {
                channel,
                ts = timestamp
            };

            var response = await MakeSlackApiCallAsync<SlackApiResponse>("chat.delete", payload);
            
            var success = response?.Ok ?? false;
            _logger.LogInformation("Message deletion {Result} for channel {Channel}, timestamp: {Timestamp}", 
                success ? "successful" : "failed", channel, timestamp);
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete message from Slack channel {Channel}, timestamp {Timestamp}", 
                channel, timestamp);
            return false;
        }
    }

    #endregion

    #region File Operations

    public async Task<SlackFileResponse> UploadFileAsync(string channel, Stream file, string filename, string? title = null)
    {
        EnsureConnected();

        try
        {
            _logger.LogInformation("Uploading file {Filename} to Slack channel {Channel}", filename, channel);

            using var httpClient = CreateHttpClient();
            using var formContent = new MultipartFormDataContent();
            
            formContent.Add(new StringContent(channel), "channels");
            if (!string.IsNullOrEmpty(title))
                formContent.Add(new StringContent(title), "title");
            
            formContent.Add(new StreamContent(file), "file", filename);

            var response = await httpClient.PostAsync($"{SlackApiBaseUrl}files.upload", formContent);
            var jsonResponse = await response.Content.ReadAsStringAsync();
            
            var fileResponse = JsonSerializer.Deserialize<SlackFileResponse>(jsonResponse, GetJsonOptions());
            
            _logger.LogInformation("File upload {Result} for {Filename} to channel {Channel}", 
                fileResponse?.Ok == true ? "successful" : "failed", filename, channel);
            
            return fileResponse ?? new SlackFileResponse { Ok = false, Error = "Unknown error" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file {Filename} to Slack channel {Channel}", filename, channel);
            throw;
        }
    }

    public async Task<SlackFileResponse> UploadFileAsync(string channel, byte[] fileContent, string filename, string? title = null)
    {
        using var stream = new MemoryStream(fileContent);
        return await UploadFileAsync(channel, stream, filename, title);
    }

    public async Task<bool> DeleteFileAsync(string fileId)
    {
        EnsureConnected();

        try
        {
            _logger.LogInformation("Deleting Slack file {FileId}", fileId);

            var payload = new { file = fileId };
            var response = await MakeSlackApiCallAsync<SlackApiResponse>("files.delete", payload);
            
            var success = response?.Ok ?? false;
            _logger.LogInformation("File deletion {Result} for file {FileId}", 
                success ? "successful" : "failed", fileId);
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete Slack file {FileId}", fileId);
            return false;
        }
    }

    #endregion

    #region Channel Management

    public async Task<IEnumerable<SlackChannel>> GetChannelsAsync()
    {
        EnsureConnected();

        try
        {
            _logger.LogInformation("Retrieving Slack channels");

            var payload = new { types = "public_channel,private_channel" };
            var response = await MakeSlackApiCallAsync<SlackChannelsResponse>("conversations.list", payload);
            
            var channels = response?.Channels ?? Enumerable.Empty<SlackChannel>();
            _logger.LogInformation("Retrieved {Count} Slack channels", channels.Count());
            
            return channels;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve Slack channels");
            throw;
        }
    }

    public async Task<SlackChannel> GetChannelInfoAsync(string channelId)
    {
        EnsureConnected();

        try
        {
            _logger.LogInformation("Retrieving Slack channel info for {ChannelId}", channelId);

            var payload = new { channel = channelId };
            var response = await MakeSlackApiCallAsync<SlackChannelResponse>("conversations.info", payload);
            
            var channel = response?.Channel ?? new SlackChannel { Id = channelId, Name = "Unknown" };
            _logger.LogInformation("Retrieved channel info for {ChannelId}: {ChannelName}", 
                channelId, channel.Name);
            
            return channel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve Slack channel info for {ChannelId}", channelId);
            throw;
        }
    }

    public async Task<SlackChannel> CreateChannelAsync(string name, bool isPrivate = false)
    {
        EnsureConnected();

        try
        {
            _logger.LogInformation("Creating {Type} Slack channel: {Name}", 
                isPrivate ? "private" : "public", name);

            var payload = new { name, is_private = isPrivate };
            var apiMethod = isPrivate ? "conversations.create" : "conversations.create";
            var response = await MakeSlackApiCallAsync<SlackChannelResponse>(apiMethod, payload);
            
            var channel = response?.Channel ?? new SlackChannel { Name = name };
            _logger.LogInformation("Channel creation {Result} for {Name}: {ChannelId}", 
                response?.Ok == true ? "successful" : "failed", name, channel.Id);
            
            return channel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Slack channel {Name}", name);
            throw;
        }
    }

    public async Task<bool> JoinChannelAsync(string channelId)
    {
        EnsureConnected();

        try
        {
            _logger.LogInformation("Joining Slack channel {ChannelId}", channelId);

            var payload = new { channel = channelId };
            var response = await MakeSlackApiCallAsync<SlackApiResponse>("conversations.join", payload);
            
            var success = response?.Ok ?? false;
            _logger.LogInformation("Channel join {Result} for {ChannelId}", 
                success ? "successful" : "failed", channelId);
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join Slack channel {ChannelId}", channelId);
            return false;
        }
    }

    public async Task<bool> LeaveChannelAsync(string channelId)
    {
        EnsureConnected();

        try
        {
            _logger.LogInformation("Leaving Slack channel {ChannelId}", channelId);

            var payload = new { channel = channelId };
            var response = await MakeSlackApiCallAsync<SlackApiResponse>("conversations.leave", payload);
            
            var success = response?.Ok ?? false;
            _logger.LogInformation("Channel leave {Result} for {ChannelId}", 
                success ? "successful" : "failed", channelId);
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to leave Slack channel {ChannelId}", channelId);
            return false;
        }
    }

    #endregion

    #region User Management

    public async Task<IEnumerable<SlackUser>> GetUsersAsync()
    {
        EnsureConnected();

        try
        {
            _logger.LogInformation("Retrieving Slack users");

            var response = await MakeSlackApiCallAsync<SlackUsersResponse>("users.list", null);
            
            var users = response?.Members ?? Enumerable.Empty<SlackUser>();
            _logger.LogInformation("Retrieved {Count} Slack users", users.Count());
            
            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve Slack users");
            throw;
        }
    }

    public async Task<SlackUser> GetUserInfoAsync(string userId)
    {
        EnsureConnected();

        try
        {
            _logger.LogInformation("Retrieving Slack user info for {UserId}", userId);

            var payload = new { user = userId };
            var response = await MakeSlackApiCallAsync<SlackUserResponse>("users.info", payload);
            
            var user = response?.User ?? new SlackUser { Id = userId, Name = "Unknown" };
            _logger.LogInformation("Retrieved user info for {UserId}: {UserName}", 
                userId, user.Name);
            
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve Slack user info for {UserId}", userId);
            throw;
        }
    }

    public async Task<SlackUser> GetBotInfoAsync()
    {
        EnsureConnected();

        try
        {
            _logger.LogInformation("Retrieving Slack bot info");

            var response = await MakeSlackApiCallAsync<SlackAuthResponse>("auth.test", null);
            
            var botUser = new SlackUser
            {
                Id = response?.UserId ?? "unknown",
                Name = response?.User ?? "DigitalMe Bot",
                IsBot = true
            };
            
            _logger.LogInformation("Retrieved bot info: {BotName} ({BotId})", botUser.Name, botUser.Id);
            
            return botUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve Slack bot info");
            throw;
        }
    }

    #endregion

    #region Interactive Elements

    public async Task<SlackMessageResponse> SendMessageWithButtonsAsync(string channel, string message, IEnumerable<SlackButton> buttons)
    {
        var blocks = new object[]
        {
            new
            {
                type = "section",
                text = new { type = "mrkdwn", text = message }
            },
            new
            {
                type = "actions",
                elements = buttons.Select(b => new
                {
                    type = "button",
                    text = new { type = "plain_text", text = b.Text },
                    value = b.Value,
                    action_id = b.ActionId
                })
            }
        };

        var messageRequest = new SlackMessageRequest
        {
            Channel = channel,
            Text = message,
            Blocks = blocks
        };

        return await SendMessageAsync(channel, messageRequest);
    }

    public async Task<SlackMessageResponse> SendMessageWithAttachmentsAsync(string channel, string message, IEnumerable<SlackAttachment> attachments)
    {
        var messageRequest = new SlackMessageRequest
        {
            Channel = channel,
            Text = message,
            Attachments = attachments
        };

        return await SendMessageAsync(channel, messageRequest);
    }

    #endregion

    #region Message History

    public async Task<IEnumerable<SlackMessage>> GetMessagesAsync(string channel, int limit = 50)
    {
        EnsureConnected();

        try
        {
            _logger.LogInformation("Retrieving {Limit} messages from Slack channel {Channel}", limit, channel);

            var payload = new { channel, limit };
            var response = await MakeSlackApiCallAsync<SlackMessagesResponse>("conversations.history", payload);
            
            var messages = response?.Messages ?? Enumerable.Empty<SlackMessage>();
            _logger.LogInformation("Retrieved {Count} messages from channel {Channel}", messages.Count(), channel);
            
            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve messages from Slack channel {Channel}", channel);
            throw;
        }
    }

    public async Task<IEnumerable<SlackMessage>> GetMessagesAsync(string channel, DateTime since, int limit = 50)
    {
        EnsureConnected();

        try
        {
            var timestamp = ((DateTimeOffset)since).ToUnixTimeSeconds().ToString();
            _logger.LogInformation("Retrieving {Limit} messages from Slack channel {Channel} since {Since}", 
                limit, channel, since);

            var payload = new { channel, oldest = timestamp, limit };
            var response = await MakeSlackApiCallAsync<SlackMessagesResponse>("conversations.history", payload);
            
            var messages = response?.Messages ?? Enumerable.Empty<SlackMessage>();
            _logger.LogInformation("Retrieved {Count} messages from channel {Channel} since {Since}", 
                messages.Count(), channel, since);
            
            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve messages from Slack channel {Channel} since {Since}", 
                channel, since);
            throw;
        }
    }

    public async Task<SlackMessage> GetMessageAsync(string channel, string timestamp)
    {
        var messages = await GetMessagesAsync(channel, 1);
        return messages.FirstOrDefault(m => m.Timestamp == timestamp) ?? 
               new SlackMessage { Channel = channel, Timestamp = timestamp, Text = "Message not found" };
    }

    #endregion

    #region Reactions

    public async Task<bool> AddReactionAsync(string channel, string timestamp, string reaction)
    {
        EnsureConnected();

        try
        {
            _logger.LogInformation("Adding reaction {Reaction} to message {Timestamp} in channel {Channel}", 
                reaction, timestamp, channel);

            var payload = new { channel, timestamp, name = reaction.TrimStart(':').TrimEnd(':') };
            var response = await MakeSlackApiCallAsync<SlackApiResponse>("reactions.add", payload);
            
            var success = response?.Ok ?? false;
            _logger.LogInformation("Reaction add {Result} for {Reaction} on message {Timestamp}", 
                success ? "successful" : "failed", reaction, timestamp);
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add reaction {Reaction} to message {Timestamp} in channel {Channel}", 
                reaction, timestamp, channel);
            return false;
        }
    }

    public async Task<bool> RemoveReactionAsync(string channel, string timestamp, string reaction)
    {
        EnsureConnected();

        try
        {
            _logger.LogInformation("Removing reaction {Reaction} from message {Timestamp} in channel {Channel}", 
                reaction, timestamp, channel);

            var payload = new { channel, timestamp, name = reaction.TrimStart(':').TrimEnd(':') };
            var response = await MakeSlackApiCallAsync<SlackApiResponse>("reactions.remove", payload);
            
            var success = response?.Ok ?? false;
            _logger.LogInformation("Reaction remove {Result} for {Reaction} on message {Timestamp}", 
                success ? "successful" : "failed", reaction, timestamp);
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove reaction {Reaction} from message {Timestamp} in channel {Channel}", 
                reaction, timestamp, channel);
            return false;
        }
    }

    public async Task<IEnumerable<SlackReaction>> GetReactionsAsync(string channel, string timestamp)
    {
        EnsureConnected();

        try
        {
            _logger.LogInformation("Retrieving reactions for message {Timestamp} in channel {Channel}", 
                timestamp, channel);

            var payload = new { channel, timestamp };
            var response = await MakeSlackApiCallAsync<SlackReactionsResponse>("reactions.get", payload);
            
            var reactions = response?.Message?.Reactions ?? Enumerable.Empty<SlackReaction>();
            _logger.LogInformation("Retrieved {Count} reactions for message {Timestamp}", 
                reactions.Count(), timestamp);
            
            return reactions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve reactions for message {Timestamp} in channel {Channel}", 
                timestamp, channel);
            return Enumerable.Empty<SlackReaction>();
        }
    }

    #endregion

    #region Private Helper Methods

    private void EnsureConnected()
    {
        if (!_isConnected || string.IsNullOrEmpty(_botToken))
        {
            throw new InvalidOperationException("Slack service not initialized. Call InitializeAsync first.");
        }
    }

    private HttpClient CreateHttpClient()
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri(SlackApiBaseUrl);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _botToken);
        httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        
        return httpClient;
    }

    private async Task<T?> MakeSlackApiCallAsync<T>(string method, object? payload) where T : class
    {
        await ApplyRateLimitAsync();

        var retryCount = 0;
        var maxRetries = _settings.MaxRetries;

        while (retryCount <= maxRetries)
        {
            try
            {
                using var httpClient = CreateHttpClient();
                
                HttpResponseMessage response;
                
                if (payload == null)
                {
                    response = await httpClient.GetAsync(method);
                }
                else
                {
                    var json = JsonSerializer.Serialize(payload, GetJsonOptions());
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    response = await httpClient.PostAsync(method, content);
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Slack API call failed: {Method}, Status: {StatusCode}, Response: {Response}", 
                        method, response.StatusCode, jsonResponse);
                    
                    if (retryCount < maxRetries)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount + 1)); // Exponential backoff
                        await Task.Delay(delay);
                        retryCount++;
                        continue;
                    }
                }

                var result = JsonSerializer.Deserialize<T>(jsonResponse, GetJsonOptions());
                return result;
            }
            catch (HttpRequestException ex) when (retryCount < maxRetries)
            {
                _logger.LogWarning(ex, "HTTP request failed for Slack API method {Method}, retry {RetryCount}/{MaxRetries}", 
                    method, retryCount + 1, maxRetries);
                
                var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount + 1));
                await Task.Delay(delay);
                retryCount++;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException && retryCount < maxRetries)
            {
                _logger.LogWarning(ex, "Request timeout for Slack API method {Method}, retry {RetryCount}/{MaxRetries}", 
                    method, retryCount + 1, maxRetries);
                
                var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount + 1));
                await Task.Delay(delay);
                retryCount++;
            }
        }

        throw new HttpRequestException($"Slack API call failed after {maxRetries} retries: {method}");
    }

    private async Task ApplyRateLimitAsync()
    {
        await _rateLimitSemaphore.WaitAsync();
        try
        {
            var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
            if (timeSinceLastRequest < _rateLimitDelay)
            {
                var delay = _rateLimitDelay - timeSinceLastRequest;
                await Task.Delay(delay);
            }
            _lastRequestTime = DateTime.UtcNow;
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        _rateLimitSemaphore?.Dispose();
    }

    #endregion
}