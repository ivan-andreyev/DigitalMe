namespace DigitalMe.Services.Telegram;

/// <summary>
/// Telegram Bot integration service interface.
/// TODO: Implement for production Telegram bot functionality.
/// </summary>
public interface ITelegramBotService
{
    /// <summary>
    /// Sends a message to a Telegram chat.
    /// TODO: Implement Telegram Bot API integration.
    /// </summary>
    Task SendMessageAsync(string chatId, string message);

    /// <summary>
    /// Starts the Telegram bot webhook listener.
    /// TODO: Implement webhook handling for incoming messages.
    /// </summary>
    Task StartAsync();

    /// <summary>
    /// Stops the Telegram bot webhook listener.
    /// TODO: Implement graceful webhook shutdown.
    /// </summary>
    Task StopAsync();
}

/// <summary>
/// Stub implementation of Telegram Bot service for MVP.
/// Throws NotImplementedException for all methods.
/// TODO: Replace with actual Telegram Bot API integration.
/// </summary>
public class TelegramBotServiceStub : ITelegramBotService
{
    public Task SendMessageAsync(string chatId, string message)
    {
        throw new NotImplementedException("TelegramBotService requires implementation for production use");
    }

    public Task StartAsync()
    {
        throw new NotImplementedException("TelegramBotService requires implementation for production use");
    }

    public Task StopAsync()
    {
        throw new NotImplementedException("TelegramBotService requires implementation for production use");
    }
}