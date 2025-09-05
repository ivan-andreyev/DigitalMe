namespace DigitalMe.Services.Telegram;

public interface ITelegramMessageDispatcher
{
    Task DispatchMessageAsync(long chatId, string message, CancellationToken cancellationToken = default);
}

public class TelegramMessageDispatcher : ITelegramMessageDispatcher
{
    public Task DispatchMessageAsync(long chatId, string message, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("TelegramMessageDispatcher implementation pending");
    }
}