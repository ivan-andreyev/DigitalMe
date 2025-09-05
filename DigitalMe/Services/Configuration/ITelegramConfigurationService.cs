namespace DigitalMe.Services.Configuration;

public interface ITelegramConfigurationService
{
    Task<string> GetBotTokenAsync();
    Task<string> GetWebhookUrlAsync();
}

public class TelegramConfigurationService : ITelegramConfigurationService
{
    public Task<string> GetBotTokenAsync()
    {
        throw new NotImplementedException("TelegramConfigurationService implementation pending");
    }

    public Task<string> GetWebhookUrlAsync()
    {
        throw new NotImplementedException("TelegramConfigurationService implementation pending");
    }
}