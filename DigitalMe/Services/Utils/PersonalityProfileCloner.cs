using DigitalMe.Data.Entities;

namespace DigitalMe.Services.Utils;

/// <summary>
/// Утилитный класс для клонирования профилей личности.
/// Централизует логику клонирования, избегая дублирования кода.
/// </summary>
public static class PersonalityProfileCloner
{
    /// <summary>
    /// Выполняет полное клонирование профиля личности со всеми связанными данными.
    /// Включает клонирование трейтов и временных паттернов поведения.
    /// </summary>
    /// <param name="original">Оригинальный профиль для клонирования</param>
    /// <returns>Полная копия профиля личности</returns>
    public static PersonalityProfile Clone(PersonalityProfile original)
    {
        if (original == null)
        {
            throw new ArgumentNullException(nameof(original));
        }

        // Create shallow copy of main properties
        var cloned = new PersonalityProfile
        {
            Id = original.Id,
            Name = original.Name,
            Description = original.Description,
            Age = original.Age,
            Profession = original.Profession,
            Location = original.Location,
            CoreValues = original.CoreValues,
            CommunicationStyle = original.CommunicationStyle,
            TechnicalPreferences = original.TechnicalPreferences,
            IsActive = original.IsActive,
            AccuracyScore = original.AccuracyScore,
            LastProfileUpdate = original.LastProfileUpdate,
            CreatedAt = original.CreatedAt,
            UpdatedAt = original.UpdatedAt
        };

        // Deep clone traits collection
        if (original.Traits != null)
        {
            cloned.Traits = original.Traits.Select(trait => new PersonalityTrait
            {
                Id = trait.Id,
                Name = trait.Name,
                Description = trait.Description,
                Category = trait.Category,
                Weight = trait.Weight,
                PersonalityProfileId = trait.PersonalityProfileId,
                CreatedAt = trait.CreatedAt,
                UpdatedAt = trait.UpdatedAt
            }).ToList();
        }

        // Deep clone temporal patterns if they exist
        if (original.TemporalPatterns != null)
        {
            cloned.TemporalPatterns = original.TemporalPatterns.Select(pattern => new TemporalBehaviorPattern
            {
                Id = pattern.Id,
                PatternName = pattern.PatternName,
                ContextTrigger = pattern.ContextTrigger,
                TriggerConditions = pattern.TriggerConditions,
                BehaviorModifications = pattern.BehaviorModifications,
                Priority = pattern.Priority,
                IsActive = pattern.IsActive,
                PersonalityProfileId = pattern.PersonalityProfileId,
                CreatedAt = pattern.CreatedAt,
                UpdatedAt = pattern.UpdatedAt
            }).ToList();
        }

        return cloned;
    }

    /// <summary>
    /// Выполняет простое клонирование профиля личности только с трейтами.
    /// Используется в случаях, когда временные паттерны не нужны.
    /// </summary>
    /// <param name="original">Оригинальный профиль для клонирования</param>
    /// <returns>Упрощенная копия профиля личности без временных паттернов</returns>
    public static PersonalityProfile CloneSimple(PersonalityProfile original)
    {
        if (original == null)
        {
            throw new ArgumentNullException(nameof(original));
        }

        // Create shallow copy of main properties
        var cloned = new PersonalityProfile
        {
            Id = original.Id,
            Name = original.Name,
            Description = original.Description,
            Age = original.Age,
            Profession = original.Profession,
            Location = original.Location,
            CoreValues = original.CoreValues,
            CommunicationStyle = original.CommunicationStyle,
            TechnicalPreferences = original.TechnicalPreferences,
            IsActive = original.IsActive,
            AccuracyScore = original.AccuracyScore,
            LastProfileUpdate = original.LastProfileUpdate,
            CreatedAt = original.CreatedAt,
            UpdatedAt = original.UpdatedAt
        };

        // Clone traits only
        if (original.Traits != null)
        {
            cloned.Traits = original.Traits.Select(trait => new PersonalityTrait
            {
                Id = trait.Id,
                Name = trait.Name,
                Description = trait.Description,
                Category = trait.Category,
                Weight = trait.Weight,
                PersonalityProfileId = trait.PersonalityProfileId,
                CreatedAt = trait.CreatedAt,
                UpdatedAt = trait.UpdatedAt
            }).ToList();
        }

        return cloned;
    }
}