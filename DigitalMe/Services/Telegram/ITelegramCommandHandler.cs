namespace DigitalMe.Services.Telegram;

public interface ITelegramCommandHandler
{
    Task HandleCommandAsync(string command, long chatId, CancellationToken cancellationToken = default);
}

public class TelegramCommandHandler : ITelegramCommandHandler
{
    public Task HandleCommandAsync(string command, long chatId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("TelegramCommandHandler implementation pending");
    }
}