namespace DigitalMe.Integrations.External.Telegram;

public interface ITelegramService
{
    Task<bool> InitializeAsync(string botToken);
    Task<TelegramMessage> SendMessageAsync(long chatId, string message);
    Task<IEnumerable<TelegramMessage>> GetRecentMessagesAsync(long chatId, int limit = 10);
    Task<bool> IsConnectedAsync();
    Task DisconnectAsync();
}

public class TelegramMessage
{
    public long MessageId { get; set; }
    public long ChatId { get; set; }
    public string FromUsername { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime MessageDate { get; set; }
    public bool IsFromBot { get; set; }
}
