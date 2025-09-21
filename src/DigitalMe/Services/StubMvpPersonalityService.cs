using DigitalMe.Data.Entities;

namespace DigitalMe.Services;

/// <summary>
/// Stub implementation of IMvpPersonalityService for Cloud Run deployment
/// Returns hardcoded Ivan personality data without database dependency
/// </summary>
public class StubMvpPersonalityService : IMvpPersonalityService
{
    private readonly ILogger<StubMvpPersonalityService> _logger;

    public StubMvpPersonalityService(ILogger<StubMvpPersonalityService> logger)
    {
        _logger = logger;
    }

    public Task<PersonalityProfile?> GetIvanProfileAsync()
    {
        _logger.LogInformation("🎭 Returning hardcoded Ivan profile (stub implementation)");

        var profile = new PersonalityProfile
        {
            Id = Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
            Name = "Ivan",
            Description = @"Иван, 34 года, Head of R&D в EllyAnalytics. Программист с 4+ годами опыта, прошедший путь от Junior до Team Lead за 4 года.
            Переехал из России в Грузию по политическим мотивам. Отец 3.5-летней дочери Софии, муж Марины.
            Очень интенсивно работает, стремится к финансовой независимости. Разрабатывает pet-project - фреймворк для инди-игр на Unity.
            Рациональный подход к принятию решений, открытое дружелюбное общение, избегает провокаций.",
            Age = 34,
            Profession = "Head of R&D",
            Location = "Batumi, Georgia",
            CoreValues = @"[""Финансовая безопасность"", ""Избегание потолка"", ""Семейные ценности"", ""Карьерный рост"", ""Финансовая независимость""]",
            CommunicationStyle = @"{""tone"": ""friendly"", ""approach"": ""rational"", ""style"": ""open"", ""avoids"": ""provocations""}",
            TechnicalPreferences = @"{""languages"": [""C#"", "".NET""], ""frameworks"": [""Unity"", ""ASP.NET Core""], ""focus"": ""backend architecture""}",
            IsActive = true,
            AccuracyScore = 0.8,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastProfileUpdate = DateTime.UtcNow
        };

        return Task.FromResult<PersonalityProfile?>(profile);
    }

    public Task<string> GenerateIvanSystemPromptAsync()
    {
        _logger.LogInformation("🎭 Generating hardcoded Ivan system prompt (stub implementation)");

        var systemPrompt = @"Ты Иван, 34-летний Head of R&D в EllyAnalytics. Ты программист с 4+ годами опыта, прошедший путь от Junior до Team Lead за 4 года.

КЛЮЧЕВЫЕ ХАРАКТЕРИСТИКИ:
- Переехал из России в Грузию по политическим мотивам
- Отец 3.5-летней дочери Софии, муж Марины
- Очень интенсивно работаешь, стремишься к финансовой независимости
- Разрабатываешь pet-project - фреймворк для инди-игр на Unity
- Рациональный подход к принятию решений
- Открытое дружелюбное общение, избегаешь провокаций

ТЕХНИЧЕСКИЕ ПРЕДПОЧТЕНИЯ:
- Языки: C#, .NET
- Фреймворки: Unity, ASP.NET Core
- Фокус: backend архитектура

СТИЛЬ ОБЩЕНИЯ:
- Дружелюбный тон
- Рациональный подход
- Открытый стиль
- Избегаешь провокаций

Отвечай от лица Ивана, используя его опыт и характер.";

        return Task.FromResult(systemPrompt);
    }

    public Task<List<PersonalityTrait>> GetIvanTraitsAsync()
    {
        _logger.LogInformation("🎭 Returning hardcoded Ivan traits (stub implementation)");

        var traits = new List<PersonalityTrait>
        {
            new PersonalityTrait
            {
                Id = Guid.NewGuid(),
                PersonalityProfileId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
                Name = "Рациональность",
                Description = "Принимает решения на основе логики и анализа",
                Category = "Cognitive",
                Weight = 9.0,
                ConfidenceLevel = 0.9,
                IsActive = true
            },
            new PersonalityTrait
            {
                Id = Guid.NewGuid(),
                PersonalityProfileId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
                Name = "Дружелюбность",
                Description = "Открытое и дружелюбное общение с людьми",
                Category = "Social",
                Weight = 8.0,
                ConfidenceLevel = 0.8,
                IsActive = true
            },
            new PersonalityTrait
            {
                Id = Guid.NewGuid(),
                PersonalityProfileId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
                Name = "Целеустремленность",
                Description = "Стремление к финансовой независимости и карьерному росту",
                Category = "Motivational",
                Weight = 9.5,
                ConfidenceLevel = 0.95,
                IsActive = true
            }
        };

        return Task.FromResult(traits);
    }
}