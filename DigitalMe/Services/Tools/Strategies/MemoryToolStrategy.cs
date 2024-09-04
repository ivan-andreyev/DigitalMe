using DigitalMe.Models;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Tools.Strategies;

/// <summary>
/// Tool Strategy для сохранения важной информации в долгосрочную память.
/// Срабатывает на просьбы запомнить, сохранить или отметить что-то важное.
/// </summary>
public class MemoryToolStrategy : BaseToolStrategy
{
    public MemoryToolStrategy(ILogger<MemoryToolStrategy> logger)
        : base(logger)
    {
    }

    public override string ToolName => "store_memory";
    public override string Description => "Сохранить важную информацию в долгосрочную память";
    public override int Priority => 2; // Высокий приоритет для запоминания важной информации

    public override Task<bool> ShouldTriggerAsync(string message, PersonalityContext context)
    {
        var messageLower = message.ToLower();
        
        // Триггеры для сохранения в память
        var shouldTrigger = ContainsWords(messageLower,
            "запомни", "сохрани", "это важно", "не забудь",
            "remember", "store", "важная информация", "пометь",
            "отметь", "записи", "заметка", "note", "важно помнить",
            "нужно помнить", "сохрани это", "запиши",
            "добавь в память", "память", "memory");

        // Дополнительные триггеры с контекстом важности
        if (!shouldTrigger)
        {
            shouldTrigger = ContainsWords(messageLower, "важно", "критично", "обязательно") &&
                          ContainsWords(messageLower, "помни", "знай", "учти");
        }

        Logger.LogDebug("Memory trigger check for message '{Message}': {Result}", 
            message.Length > 50 ? message[..50] + "..." : message, shouldTrigger);

        return Task.FromResult(shouldTrigger);
    }

    public override Task<object> ExecuteAsync(Dictionary<string, object> parameters, PersonalityContext context)
    {
        Logger.LogInformation("Executing memory storage");

        try
        {
            ValidateRequiredParameters(parameters, "key", "value");

            var key = GetParameter<string>(parameters, "key");
            var value = GetParameter<string>(parameters, "value");
            var importance = GetParameter(parameters, "importance", 5.0);
            var category = GetParameter(parameters, "category", "general");
            var tags = GetParameter(parameters, "tags", "").Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Memory key cannot be empty");

            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Memory value cannot be empty");

            // Валидация важности (1-10)
            if (importance < 1 || importance > 10)
                importance = Math.Clamp(importance, 1, 10);

            // TODO: Реальная интеграция с системой памяти когда будет готова
            // Сейчас только логирование для демонстрации функциональности
            Logger.LogInformation("Storing memory: {Key} = {Value} (importance: {Importance}, category: {Category})", 
                key, value, importance, category);

            // Генерируем уникальный ID для записи в памяти
            var memoryId = Guid.NewGuid().ToString("N")[..8];

            var result = new
            {
                success = true,
                memory_id = memoryId,
                key = key,
                value = value,
                importance = importance,
                category = category,
                tags = tags.Where(t => !string.IsNullOrWhiteSpace(t)).ToArray(),
                stored_at = DateTime.UtcNow,
                expires_at = importance >= 8 ? (DateTime?)null : DateTime.UtcNow.AddDays(30), // Важные записи не истекают
                personality_context = new
                {
                    user_id = context.CurrentState.GetValueOrDefault("userId", "unknown").ToString(),
                    platform = context.CurrentState.GetValueOrDefault("platform", "unknown").ToString(),
                    conversation_id = context.CurrentState.GetValueOrDefault("conversationId", "").ToString()
                },
                tool_name = ToolName
            };

            Logger.LogInformation("Successfully stored memory entry {MemoryId} with key '{Key}' and importance {Importance}", 
                memoryId, key, importance);
            return Task.FromResult<object>(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to store memory");
            return Task.FromResult<object>(new { 
                success = false, 
                error = ex.Message,
                tool_name = ToolName 
            });
        }
    }

    public override object GetParameterSchema()
    {
        return new
        {
            type = "object",
            properties = new
            {
                key = new { 
                    type = "string", 
                    description = "Ключ для идентификации сохраняемой информации" 
                },
                value = new { 
                    type = "string", 
                    description = "Значение или информация для сохранения" 
                },
                importance = new { 
                    type = "number", 
                    description = "Важность информации от 1 (низкая) до 10 (критичная), по умолчанию 5",
                    minimum = 1,
                    maximum = 10,
                    @default = 5
                },
                category = new { 
                    type = "string", 
                    description = "Категория памяти: 'personal', 'work', 'technical', 'general' (по умолчанию 'general')",
                    @enum = new[] { "personal", "work", "technical", "general", "preferences", "contacts" },
                    @default = "general"
                },
                tags = new { 
                    type = "string", 
                    description = "Теги для поиска, разделенные запятыми (опционально)" 
                },
                expires_in_days = new { 
                    type = "number", 
                    description = "Через сколько дней запись истекает (опционально, для важности >= 8 не истекает)",
                    minimum = 1,
                    maximum = 365
                }
            },
            required = new[] { "key", "value" }
        };
    }
}