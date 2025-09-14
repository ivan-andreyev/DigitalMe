using DigitalMe.Data;
using DigitalMe.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services;

/// <summary>
/// Simple MVP personality service with direct DbContext access
/// No repository pattern - direct database operations for simplicity
/// Implements both IPersonalityService (for compatibility) and IMVPPersonalityService (ISP compliance)
/// </summary>
public class MVPPersonalityService : IPersonalityService, IMVPPersonalityService
{
    private readonly DigitalMeDbContext _context;
    private readonly ILogger<MVPPersonalityService> _logger;

    public MVPPersonalityService(
        DigitalMeDbContext context,
        ILogger<MVPPersonalityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get Ivan's personality profile - hardcoded for MVP
    /// </summary>
    public async Task<PersonalityProfile?> GetIvanProfileAsync()
    {
        try
        {
            var profile = await _context.PersonalityProfiles
                .Include(p => p.Traits)
                .FirstOrDefaultAsync(p => p.Name == "Ivan");

            if (profile == null)
            {
                _logger.LogWarning("Ivan's personality profile not found in database");
                return null;
            }

            _logger.LogInformation("Retrieved Ivan's profile with {TraitCount} traits", profile.Traits?.Count ?? 0);
            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Ivan's personality profile");
            return null;
        }
    }

    /// <summary>
    /// Generate system prompt based on Ivan's personality
    /// </summary>
    public async Task<string> GenerateIvanSystemPromptAsync()
    {
        var profile = await GetIvanProfileAsync();
        if (profile == null)
        {
            return GetFallbackSystemPrompt();
        }

        var systemPrompt = $@"
Ты - цифровая копия Ивана, максимально точно воспроизводящая его личность, стиль мышления и общения.

БИОГРАФИЯ И КОНТЕКСТ:
{profile.Description}

КЛЮЧЕВЫЕ ЧЕРТЫ ЛИЧНОСТИ:
{string.Join("\n", profile.Traits.OrderByDescending(t => t.Weight).Take(5).Select(t => $"- {t.Name} ({t.Category}): {t.Description}"))}

ОСНОВНЫЕ ПРИНЦИПЫ ИВАНА:
- Финансовая безопасность - основной драйвер решений
- Избегание потолка - постоянное развитие и рост 
- Рациональный подход к принятию решений
- Прямота и открытость в общении

СТИЛЬ ОБЩЕНИЯ:
- Технически компетентный, фокус на C#/.NET
- Структурированное мышление
- Прагматичный подход к проблемам
- Избегает провокаций, но честен в оценках
- Может говорить о семье и балансе работа/жизнь

ТЕХНИЧЕСКИЕ ПРЕДПОЧТЕНИЯ:
- C# и .NET экосистема
- Unity для разработки игр  
- Строгая типизация и архитектурные паттерны
- Backend архитектура и системное проектирование

ЖИЗНЕННЫЙ КОНТЕКСТ:
- 34 года, Head of R&D в EllyAnalytics
- Переехал из России в Грузию по политическим мотивам
- Жена Марина, дочь София (3.5 года)
- Работает интенсively, но переживает о балансе с семьёй

Отвечай как Иван - прямо, технически грамотно, с учётом его жизненного опыта и приоритетов.
";

        _logger.LogInformation("Generated system prompt for Ivan with {TraitCount} traits", profile.Traits?.Count ?? 0);
        return systemPrompt.Trim();
    }

    /// <summary>
    /// Legacy interface compatibility - maps to Ivan for MVP
    /// </summary>
    public async Task<PersonalityProfile?> GetPersonalityAsync(string name)
    {
        // For MVP, we only support Ivan
        if (name.Equals("Ivan", StringComparison.OrdinalIgnoreCase))
        {
            return await GetIvanProfileAsync();
        }

        _logger.LogWarning("Requested personality '{Name}' not supported in MVP (only Ivan)", name);
        return null;
    }

    /// <summary>
    /// Legacy interface compatibility - not implemented in MVP
    /// </summary>
    public async Task<PersonalityProfile> CreatePersonalityAsync(string name, string description)
    {
        throw new NotImplementedException("Creating personalities not supported in MVP - use data seeder");
    }

    /// <summary>
    /// Legacy interface compatibility - not implemented in MVP
    /// </summary>
    public async Task<PersonalityProfile> UpdatePersonalityAsync(Guid id, string description)
    {
        throw new NotImplementedException("Updating personalities not supported in MVP - use data seeder");
    }

    /// <summary>
    /// Legacy interface compatibility - maps to Ivan system prompt
    /// </summary>
    public async Task<string> GenerateSystemPromptAsync(Guid personalityId)
    {
        // For MVP, always generate Ivan's prompt regardless of ID
        return await GenerateIvanSystemPromptAsync();
    }

    /// <summary>
    /// Get all personality traits for Ivan - useful for debugging
    /// </summary>
    public async Task<List<PersonalityTrait>> GetIvanTraitsAsync()
    {
        try
        {
            var traits = await _context.PersonalityTraits
                .Where(t => t.PersonalityProfile.Name == "Ivan")
                .OrderByDescending(t => t.Weight)
                .ToListAsync();

            _logger.LogInformation("Retrieved {TraitCount} traits for Ivan", traits.Count);
            return traits;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Ivan's personality traits");
            return new List<PersonalityTrait>();
        }
    }

    /// <summary>
    /// Legacy interface compatibility - not implemented in MVP
    /// </summary>
    public async Task<PersonalityTrait> AddTraitAsync(Guid personalityId, string category, string name, string description, double weight = 1.0)
    {
        throw new NotImplementedException("Adding traits not supported in MVP - use data seeder");
    }

    /// <summary>
    /// Legacy interface compatibility - maps to Ivan traits
    /// </summary>
    public async Task<IEnumerable<PersonalityTrait>> GetPersonalityTraitsAsync(Guid personalityId)
    {
        // For MVP, always return Ivan's traits regardless of ID
        return await GetIvanTraitsAsync();
    }

    /// <summary>
    /// Legacy interface compatibility - not implemented in MVP
    /// </summary>
    public async Task<bool> DeletePersonalityAsync(Guid id)
    {
        throw new NotImplementedException("Deleting personalities not supported in MVP");
    }

    /// <summary>
    /// Fallback system prompt if database is unavailable
    /// </summary>
    private static string GetFallbackSystemPrompt()
    {
        return @"
Ты - цифровая копия Ивана, 34-летнего Head of R&D в EllyAnalytics.

ОСНОВНЫЕ ХАРАКТЕРИСТИКИ:
- Программист с фокусом на C#/.NET
- Рациональный подход к решениям
- Прямота в общении
- Стремление к финансовой безопасности
- Избегание карьерного потолка

Отвечай технически грамотно, прямо и по существу.
";
    }
}
