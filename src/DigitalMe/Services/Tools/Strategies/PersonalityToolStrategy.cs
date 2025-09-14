using DigitalMe.Models;
using DigitalMe.Services;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Tools.Strategies;

/// <summary>
/// Tool Strategy для получения информации о чертах личности Ивана.
/// Срабатывает на вопросы о личности, характере или самопрезентации.
/// </summary>
public class PersonalityToolStrategy : BaseToolStrategy
{
    private readonly IPersonalityService _personalityService;

    public PersonalityToolStrategy(IPersonalityService personalityService, ILogger<PersonalityToolStrategy> logger)
        : base(logger)
    {
        _personalityService = personalityService;
    }

    public override string ToolName => "get_personality_traits";
    public override string Description => "Получить черты личности Ивана";
    public override int Priority => 2; // Высокий приоритет для самопознания

    public override Task<bool> ShouldTriggerAsync(string message, PersonalityContext context)
    {
        var messageLower = message.ToLower();

        // Триггеры для получения информации о личности
        var shouldTrigger = ContainsWords(messageLower,
            "расскажи о себе", "твои черты", "какой ты", "твоя личность",
            "что ты за человек", "твой характер", "опиши себя",
            "кто ты такой", "твои качества", "твои особенности",
            "personality", "character", "traits", "about you",
            "расскажи про себя", "познакомься", "представься",
            "твои сильные стороны", "твои слабости");

        // Дополнительные триггеры для профессиональных качеств
        if (!shouldTrigger)
        {
            shouldTrigger = ContainsWords(messageLower, "опыт", "experience", "навыки", "skills") &&
                          ContainsWords(messageLower, "твой", "твои", "your", "расскажи");
        }

        Logger.LogDebug("Personality trigger check for message '{Message}': {Result}",
            message.Length > 50 ? message[..50] + "..." : message, shouldTrigger);

        return Task.FromResult(shouldTrigger);
    }

    public override async Task<object> ExecuteAsync(Dictionary<string, object> parameters, PersonalityContext context)
    {
        Logger.LogInformation("Executing personality traits retrieval");

        try
        {
            var category = GetParameter<string>(parameters, "category", "");

            var personality = await _personalityService.GetPersonalityAsync("Ivan");
            if (personality == null)
            {
                Logger.LogWarning("Ivan's personality not found in database");
                return new
                {
                    success = false,
                    error = "Ivan's personality not found",
                    tool_name = ToolName
                };
            }

            var traits = await _personalityService.GetPersonalityTraitsAsync(personality.Id);

            // Фильтруем по категории если указана
            if (!string.IsNullOrWhiteSpace(category))
            {
                traits = traits.Where(t => t.Category.Contains(category, StringComparison.OrdinalIgnoreCase));
                Logger.LogDebug("Filtering personality traits by category: {Category}", category);
            }

            var traitsList = traits.ToList();

            var result = new
            {
                success = true,
                personality_id = personality.Id,
                name = personality.Name,
                description = personality.Description,
                category_filter = string.IsNullOrWhiteSpace(category) ? "all" : category,
                traits_count = traitsList.Count,
                traits = traitsList.Select(t => new
                {
                    category = t.Category,
                    name = t.Name,
                    description = t.Description,
                    weight = t.Weight
                }).ToList(),
                summary = GeneratePersonalitySummary(personality, traitsList),
                tool_name = ToolName
            };

            Logger.LogInformation("Successfully retrieved {Count} personality traits for Ivan (category: {Category})",
                traitsList.Count, string.IsNullOrWhiteSpace(category) ? "all" : category);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to retrieve personality traits");
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
                category = new
                {
                    type = "string",
                    description = "Категория черт личности для фильтрации (опционально). Например: 'professional', 'personal', 'technical'"
                },
                detail_level = new
                {
                    type = "string",
                    description = "Уровень детализации: 'brief', 'detailed', 'full' (по умолчанию 'detailed')",
                    @enum = new[] { "brief", "detailed", "full" },
                    @default = "detailed"
                }
            },
            required = new string[] { } // Все параметры опциональны
        };
    }

    private static string GeneratePersonalitySummary(PersonalityProfile personality, List<PersonalityTrait> traits)
    {
        if (!traits.Any())
            return "Черты личности не найдены.";

        var topTraits = traits
            .OrderByDescending(t => t.Weight)
            .Take(3)
            .Select(t => t.Name)
            .ToList();

        var categories = traits
            .GroupBy(t => t.Category)
            .Select(g => $"{g.Key} ({g.Count()})")
            .ToList();

        return $"{personality.Name} - {personality.Description}. " +
               $"Основные черты: {string.Join(", ", topTraits)}. " +
               $"Категории: {string.Join(", ", categories)}.";
    }
}
