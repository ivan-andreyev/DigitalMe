namespace DigitalMe.Services.Telegram.Commands;
using DigitalMe.Services.Telegram;

public interface ITelegramCommand
{
    string CommandName { get; }
    Task ExecuteAsync(long chatId, string[] args, CancellationToken cancellationToken = default);
}

public class StartCommand : ITelegramCommand
{
    public string CommandName => "start";
    
    public Task ExecuteAsync(long chatId, string[] args, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("StartCommand implementation pending");
    }
}

public class HelpCommand : ITelegramCommand
{
    public string CommandName => "help";
    
    public Task ExecuteAsync(long chatId, string[] args, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("HelpCommand implementation pending");
    }
}

public class PersonalityCommand : ITelegramCommand
{
    public string CommandName => "personality";
    
    public Task ExecuteAsync(long chatId, string[] args, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("PersonalityCommand implementation pending");
    }
}

public class SettingsCommand : ITelegramCommand
{
    public string CommandName => "settings";
    
    public Task ExecuteAsync(long chatId, string[] args, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("SettingsCommand implementation pending");
    }
}