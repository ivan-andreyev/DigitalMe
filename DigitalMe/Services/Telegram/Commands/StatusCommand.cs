using DigitalMe.Services.Telegram;

namespace DigitalMe.Services.Telegram.Commands;

public class StatusCommand : ITelegramCommand
{
    public string CommandName => "status";

    public Task ExecuteAsync(long chatId, string[] args, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("StatusCommand implementation pending");
    }
}
