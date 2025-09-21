namespace DigitalMe.Services.Configuration;

public interface ITelegramConfigurationService
{
    Task<string> GetBotTokenAsync();
    Task<string> GetWebhookUrlAsync();
}

public class TelegramConfigurationService : ITelegramConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TelegramConfigurationService> _logger;

    public TelegramConfigurationService(
        IConfiguration configuration,
        ILogger<TelegramConfigurationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task<string> GetBotTokenAsync()
    {
        var token = _configuration["Integrations:Telegram:BotToken"]
                   ?? _configuration["Telegram:BotToken"]
                   ?? Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");

        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("Telegram bot token not configured - Telegram integration will not work");
            return Task.FromResult(string.Empty);
        }

        _logger.LogDebug("Telegram bot token retrieved from configuration");
        return Task.FromResult(token);
    }

    public Task<string> GetWebhookUrlAsync()
    {
        var webhookUrl = _configuration["Integrations:Telegram:WebhookUrl"]
                        ?? _configuration["Telegram:WebhookUrl"];

        if (string.IsNullOrEmpty(webhookUrl))
        {
            _logger.LogWarning("Telegram webhook URL not configured - using default");
            // Default for local development
            webhookUrl = "http://localhost:5000/api/telegram/webhook";
        }

        _logger.LogDebug("Telegram webhook URL: {WebhookUrl}", webhookUrl);
        return Task.FromResult(webhookUrl);
    }
}
