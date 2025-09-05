namespace DigitalMe.Services.Telegram;

public interface IWebhookManagementService
{
    Task SetWebhookAsync(string url);
    Task<bool> ValidateWebhookAsync();
    Task DeleteWebhookAsync();
}

// Note: TelegramWebhookService implements both ITelegramWebhookService and IWebhookManagementService
public class TelegramWebhookService : ITelegramWebhookService, IWebhookManagementService
{
    // ITelegramWebhookService implementation
    public Task ProcessUpdateAsync(object update)
    {
        throw new NotImplementedException("TelegramWebhookService implementation pending");
    }

    public bool ValidateWebhookSignature(string signature, string body)
    {
        throw new NotImplementedException("TelegramWebhookService implementation pending");
    }

    // IWebhookManagementService implementation  
    public Task SetWebhookAsync(string url)
    {
        throw new NotImplementedException("TelegramWebhookService implementation pending");
    }

    public Task<bool> ValidateWebhookAsync()
    {
        throw new NotImplementedException("TelegramWebhookService implementation pending");
    }

    public Task DeleteWebhookAsync()
    {
        throw new NotImplementedException("TelegramWebhookService implementation pending");
    }
}