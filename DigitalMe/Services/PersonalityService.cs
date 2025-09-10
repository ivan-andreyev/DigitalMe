using DigitalMe.Models;
using DigitalMe.Repositories;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services;

public class PersonalityService : IPersonalityService
{
    private readonly IPersonalityRepository _personalityRepository;
    private readonly ILogger<PersonalityService> _logger;

    public PersonalityService(
        IPersonalityRepository personalityRepository,
        ILogger<PersonalityService> logger)
    {
        _personalityRepository = personalityRepository;
        _logger = logger;
    }

    public async Task<PersonalityProfile?> GetPersonalityAsync(string name)
    {
        return await _personalityRepository.GetProfileAsync(name);
    }

    public async Task<PersonalityProfile> CreatePersonalityAsync(string name, string description)
    {
        var personality = new PersonalityProfile
        {
            Name = name,
            Description = description
        };

        return await _personalityRepository.CreateProfileAsync(personality);
    }

    public async Task<PersonalityProfile> UpdatePersonalityAsync(Guid id, string description)
    {
        var personality = await _personalityRepository.GetProfileByIdAsync(id);
        if (personality == null)
            throw new ArgumentException($"Personality with ID {id} not found");

        personality.Description = description;
        return await _personalityRepository.UpdateProfileAsync(personality);
    }

    public async Task<string> GenerateSystemPromptAsync(Guid personalityId)
    {
        var personality = await _personalityRepository.GetProfileByIdAsync(personalityId);
        if (personality == null)
            throw new ArgumentException($"Personality with ID {personalityId} not found");

        var traits = await _personalityRepository.GetTraitsAsync(personalityId);

        var systemPrompt = $@"
Ты - цифровая копия {personality.Name}, максимально точно воспроизводящая его личность, стиль мышления и общения.

БИОГРАФИЯ И КОНТЕКСТ:
{personality.Description}

ОСНОВНЫЕ ПРИНЦИПЫ ИВАНА:
- Всем похуй (философия независимости от мнений окружающих)
- Сила в правде (честность и прямолинейность превыше всего)
- Живи и дай жить другим (уважение к личному выбору)

СТИЛЬ ОБЩЕНИЯ:
- Прямолинейный, без лишних слов и воды
- Технически компетентный, оперирует фактами
- Может быть резким, но всегда справедливым
- Использует профессиональный сленг в IT контексте
- Не терпит бюрократии и формализма

ТЕХНИЧЕСКИЕ ПРЕДПОЧТЕНИЯ:
- C#/.NET экосистема
- Строгая типизация
- Избегает графических инструментов, предпочитает код
- Архитектурное мышление";

        if (traits.Any())
        {
            systemPrompt += "\n\nИНДИВИДУАЛЬНЫЕ ЧЕРТЫ ЛИЧНОСТИ:\n";
            foreach (var trait in traits.OrderByDescending(t => t.Weight))
            {
                systemPrompt += $"- {trait.Category}: {trait.Name} - {trait.Description} (важность: {trait.Weight})\n";
            }
        }

        systemPrompt += @"

ИНСТРУКЦИИ ПО ПОВЕДЕНИЮ:
- Отвечай КАК Иван, используя его мышление и стиль
- Сохраняй консистентность с его принципами и ценностями
- При технических вопросах демонстрируй экспертизу в C#/.NET
- Будь прямолинейным, но не хамским
- Используй ""я"" от лица Ивана, не упоминай что ты ""цифровая копия""

Действуй естественно, как если бы ты и есть Иван.";

        return systemPrompt.Trim();
    }

    public async Task<PersonalityTrait> AddTraitAsync(Guid personalityId, string category, string name, string description, double weight = 1.0)
    {
        var trait = new PersonalityTrait
        {
            PersonalityProfileId = personalityId,
            Category = category,
            Name = name,
            Description = description,
            Weight = weight
        };

        return await _personalityRepository.AddTraitAsync(trait);
    }

    public async Task<IEnumerable<PersonalityTrait>> GetPersonalityTraitsAsync(Guid personalityId)
    {
        return await _personalityRepository.GetTraitsAsync(personalityId);
    }

    public async Task<bool> DeletePersonalityAsync(Guid id)
    {
        return await _personalityRepository.DeleteProfileAsync(id);
    }

    public async Task<string> GenerateIvanSystemPromptAsync()
    {
        var ivanPersonality = await GetPersonalityAsync("Ivan");
        if (ivanPersonality != null)
        {
            return await GenerateSystemPromptAsync(ivanPersonality.Id);
        }

        _logger.LogWarning("Ivan personality profile not found, using default system prompt");
        return "You are Ivan, a digital assistant. Respond professionally and helpfully.";
    }
}
