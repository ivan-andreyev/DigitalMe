using DigitalMe.Integrations.External.Telegram;
using DigitalMe.Models;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Tools.Strategies;

/// <summary>
/// Tool Strategy для отправки сообщений в Telegram.
/// Срабатывает на упоминания Telegram или команды отправки сообщений.
/// </summary>
public class TelegramToolStrategy : BaseToolStrategy
{
    private readonly ITelegramService _telegramService;

    public TelegramToolStrategy(ITelegramService telegramService, ILogger<TelegramToolStrategy> logger)
        : base(logger)
    {
        _telegramService = telegramService;
    }

    public override string ToolName => "send_telegram_message";
    public override string Description => "Отправить сообщение в Telegram";
    public override int Priority => 2; // Высокий приоритет для коммуникационных инструментов

    public override Task<bool> ShouldTriggerAsync(string message, PersonalityContext context)
    {
        var messageLower = message.ToLower();

        // Триггеры для Telegram
        var shouldTrigger = ContainsWords(messageLower,
            "отправь", "пошли", "напиши в телеграм", "telegram",
            "телега", "тг", "отправь сообщение", "пошли в тг",
            "напиши в тг", "send telegram");

        Logger.LogDebug("Telegram trigger check for message '{Message}': {Result}",
            message.Length > 50 ? message[..50] + "..." : message, shouldTrigger);

        return Task.FromResult(shouldTrigger);
    }

    public override async Task<object> ExecuteAsync(Dictionary<string, object> parameters, PersonalityContext context)
    {
        Logger.LogInformation("Executing Telegram message send");

        try
        {
            ValidateRequiredParameters(parameters, "chat_id", "message");

            var chatId = GetParameter<long>(parameters, "chat_id");
            var message = GetParameter<string>(parameters, "message");

            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message cannot be empty");

            // Проверяем подключение к Telegram
            if (!await _telegramService.IsConnectedAsync())
            {
                Logger.LogInformation("Telegram not connected, attempting initialization");
                // Try to initialize with empty token (development mode)
                await _telegramService.InitializeAsync("");
            }

            var telegramMessage = await _telegramService.SendMessageAsync(chatId, message);

            var result = new
            {
                success = true,
                message_id = telegramMessage.MessageId,
                chat_id = telegramMessage.ChatId,
                sent_at = telegramMessage.MessageDate,
                tool_name = ToolName
            };

            Logger.LogInformation("Successfully sent Telegram message to chat {ChatId}", chatId);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send Telegram message");
            return new
            {
                success = false,
                error = ex.Message,
                tool_name = ToolName
            };
        }
    }

    public override object GetParameterSchema()
    {
        return new
        {
            type = "object",
            properties = new
            {
                chat_id = new
                {
                    type = "integer",
                    description = "ID чата в Telegram"
                },
                message = new
                {
                    type = "string",
                    description = "Текст сообщения для отправки"
                }
            },
            required = new[] { "chat_id", "message" }
        };
    }
}
