using DigitalMe.Data.Entities;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace DigitalMe.Integrations.External.Telegram;

/// <summary>
/// Сервис для интеграции с Telegram Bot API.
/// Обеспечивает отправку сообщений и получение данных через телеграм-бота.
/// </summary>
public class TelegramService : ITelegramService
{
    private readonly ILogger<TelegramService> _logger;
    private ITelegramBotClient? _botClient;
    private bool _isConnected;
    private string _botToken = string.Empty;

    public TelegramService(ILogger<TelegramService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> InitializeAsync(string botToken)
    {
        try
        {
            _logger.LogInformation("Initializing Telegram Bot connection...");
            _botToken = botToken;

            _botClient = new TelegramBotClient(_botToken);

            // Test the connection by getting bot information
            var me = await _botClient.GetMe();
            _isConnected = true;

            _logger.LogInformation("Telegram Bot connection established: @{Username} ({Name})", me.Username, me.FirstName);
            return _isConnected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Telegram Bot connection");
            _isConnected = false;
            return false;
        }
    }

    public async Task<TelegramMessage> SendMessageAsync(long chatId, string message)
    {
        if (!_isConnected || _botClient == null)
        {
            throw new InvalidOperationException("Telegram Bot not initialized. Call InitializeAsync first.");
        }

        _logger.LogInformation("Sending Telegram message to chat {ChatId}: {Message}", chatId, message);

        try
        {
            var sentMessage = await _botClient.SendMessage(
                chatId: chatId,
                text: message,
                parseMode: ParseMode.Html);

            return new TelegramMessage
            {
                MessageId = sentMessage.MessageId,
                ChatId = sentMessage.Chat.Id,
                FromUsername = sentMessage.From?.Username ?? "DigitalMeBot",
                Text = sentMessage.Text ?? message,
                MessageDate = sentMessage.Date,
                IsFromBot = sentMessage.From?.IsBot ?? true
            };
        }
        catch (ApiRequestException ex)
        {
            _logger.LogError(ex, "Telegram API error sending message to chat {ChatId}", chatId);
            throw;
        }
    }

    public Task<IEnumerable<TelegramMessage>> GetRecentMessagesAsync(long chatId, int limit = 10)
    {
        if (!_isConnected || _botClient == null)
        {
            throw new InvalidOperationException("Telegram Bot not initialized. Call InitializeAsync first.");
        }

        _logger.LogInformation("Getting recent Telegram messages from chat {ChatId}, limit: {Limit}", chatId, limit);

        try
        {
            // Note: Telegram Bot API doesn't provide a direct way to get chat history
            // This would typically be handled through webhook updates or polling for updates
            // For now, we'll return empty list and log a warning
            _logger.LogWarning("GetRecentMessagesAsync: Telegram Bot API doesn't support retrieving chat history directly. Use webhooks or polling instead.");

            return Task.FromResult(Enumerable.Empty<TelegramMessage>());
        }
        catch (ApiRequestException ex)
        {
            _logger.LogError(ex, "Telegram API error getting messages from chat {ChatId}", chatId);
            throw;
        }
    }

    public async Task<bool> IsConnectedAsync()
    {
        if (!_isConnected || _botClient == null || string.IsNullOrEmpty(_botToken))
        {
            return false;
        }

        try
        {
            // Test connection with a simple API call
            await _botClient.GetMe();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        _logger.LogInformation("Disconnecting from Telegram Bot API...");

        _isConnected = false;
        _botToken = string.Empty;
        _botClient = null;

        await Task.CompletedTask;
    }

    public async Task SetWebhookAsync(string url, UpdateType[]? allowedUpdates = null, int maxConnections = 10, bool dropPendingUpdates = false)
    {
        if (!_isConnected || _botClient == null)
        {
            throw new InvalidOperationException("Telegram Bot not initialized. Call InitializeAsync first.");
        }

        try
        {
            await _botClient.SetWebhook(
                url: url,
                allowedUpdates: allowedUpdates ?? new[] { UpdateType.Message, UpdateType.CallbackQuery, UpdateType.InlineQuery },
                maxConnections: maxConnections,
                dropPendingUpdates: dropPendingUpdates
            );

            _logger.LogInformation("Webhook set successfully: {Url}", url);
        }
        catch (ApiRequestException ex)
        {
            _logger.LogError(ex, "Telegram API error setting webhook: {Url}. Error: {ErrorCode} - {Description}",
                url, ex.ErrorCode, ex.Message);
            throw;
        }
    }

    public async Task DeleteWebhookAsync()
    {
        if (!_isConnected || _botClient == null)
        {
            throw new InvalidOperationException("Telegram Bot not initialized. Call InitializeAsync first.");
        }

        try
        {
            await _botClient.DeleteWebhook();
            _logger.LogInformation("Webhook deleted successfully");
        }
        catch (ApiRequestException ex)
        {
            _logger.LogError(ex, "Telegram API error deleting webhook. Error: {ErrorCode} - {Description}",
                ex.ErrorCode, ex.Message);
            throw;
        }
    }

    public async Task<WebhookInfo> GetWebhookInfoAsync()
    {
        if (!_isConnected || _botClient == null)
        {
            throw new InvalidOperationException("Telegram Bot not initialized. Call InitializeAsync first.");
        }

        try
        {
            var webhookInfo = await _botClient.GetWebhookInfo();
            _logger.LogInformation("Retrieved webhook info - URL: {Url}, HasCustomCert: {HasCustomCert}, PendingUpdates: {PendingUpdates}",
                webhookInfo.Url, webhookInfo.HasCustomCertificate, webhookInfo.PendingUpdateCount);
            return webhookInfo;
        }
        catch (ApiRequestException ex)
        {
            _logger.LogError(ex, "Telegram API error getting webhook info. Error: {ErrorCode} - {Description}",
                ex.ErrorCode, ex.Message);
            throw;
        }
    }
}
