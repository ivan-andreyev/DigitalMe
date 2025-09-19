using System.Text.Json;
using DigitalMe.Data.Entities;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services;

/// <summary>
/// Интерфейс для маппинга черт личности в конкретные паттерны поведения и ответов.
/// Обеспечивает трансляцию PersonalityProfile в конкретные поведенческие модели.
/// </summary>
public interface IPersonalityBehaviorMapper
{
    /// <summary>
    /// Генерирует модификаторы ответа на основе черт личности и контекста.
    /// </summary>
    /// <param name="personality">Профиль личности</param>
    /// <param name="context">Контекст взаимодействия</param>
    /// <returns>Модификаторы поведения для применения к ответу</returns>
    BehaviorModifiers GetBehaviorModifiers(PersonalityProfile personality, InteractionContext context);

    /// <summary>
    /// Определяет стиль коммуникации на основе личности и ситуации.
    /// </summary>
    /// <param name="personality">Профиль личности</param>
    /// <param name="situationType">Тип ситуации (техническая, личная, профессиональная)</param>
    /// <returns>Стиль коммуникации для применения</returns>
    CommunicationStyle MapCommunicationStyle(PersonalityProfile personality, SituationType situationType);

    /// <summary>
    /// Модулирует тон ответа на основе черт личности и эмоционального контекста.
    /// </summary>
    /// <param name="personality">Профиль личности</param>
    /// <param name="emotionalContext">Эмоциональный контекст взаимодействия</param>
    /// <returns>Настройки тона для ответа</returns>
    ToneModulation ModulateTone(PersonalityProfile personality, EmotionalContext emotionalContext);

    /// <summary>
    /// Извлекает релевантные черты личности для конкретного типа задачи.
    /// </summary>
    /// <param name="personality">Профиль личности</param>
    /// <param name="taskType">Тип задачи</param>
    /// <returns>Список релевантных черт с весовыми коэффициентами</returns>
    List<WeightedTrait> GetRelevantTraits(PersonalityProfile personality, TaskType taskType);
}

/// <summary>
/// Реализация сервиса маппинга черт личности в поведенческие паттерны.
/// Специализирована для модели личности Ивана с его техническими предпочтениями и стилем работы.
/// </summary>
public class PersonalityBehaviorMapper : IPersonalityBehaviorMapper
{
    private readonly ILogger<PersonalityBehaviorMapper> _logger;
    private readonly IPersonalityService _personalityService;

    public PersonalityBehaviorMapper(
        ILogger<PersonalityBehaviorMapper> logger,
        IPersonalityService personalityService)
    {
        _logger = logger;
        _personalityService = personalityService;
    }

    public BehaviorModifiers GetBehaviorModifiers(PersonalityProfile personality, InteractionContext context)
    {
        _logger.LogDebug("Generating behavior modifiers for personality {PersonalityName} in context {ContextType}",
            personality.Name, context.Type);

        var modifiers = new BehaviorModifiers();

        // Ivan's core behavioral patterns
        if (IsIvanPersonality(personality))
        {
            modifiers = ApplyIvanBehaviorPatterns(modifiers, context);
        }

        // Apply trait-based modifications
        foreach (var trait in personality.Traits ?? new List<PersonalityTrait>())
        {
            ApplyTraitModification(modifiers, trait, context);
        }

        _logger.LogDebug("Generated behavior modifiers: Confidence={Confidence}, Directness={Directness}, TechnicalDetail={TechnicalDetail}",
            modifiers.ConfidenceLevel, modifiers.DirectnessLevel, modifiers.TechnicalDetailLevel);

        return modifiers;
    }

    public CommunicationStyle MapCommunicationStyle(PersonalityProfile personality, SituationType situationType)
    {
        _logger.LogDebug("Mapping communication style for {PersonalityName} in {SituationType} situation",
            personality.Name, situationType);

        var style = new CommunicationStyle();

        if (IsIvanPersonality(personality))
        {
            style = MapIvanCommunicationStyle(situationType);
        }
        else
        {
            style = MapGenericCommunicationStyle(personality, situationType);
        }

        _logger.LogDebug("Mapped communication style: Formality={Formality}, TechnicalLanguage={TechnicalLanguage}",
            style.FormalityLevel, style.TechnicalLanguageUsage);

        return style;
    }

    public ToneModulation ModulateTone(PersonalityProfile personality, EmotionalContext emotionalContext)
    {
        _logger.LogDebug("Modulating tone for {PersonalityName} with emotional context {EmotionalState}",
            personality.Name, emotionalContext.PrimaryEmotion);

        var modulation = new ToneModulation();

        if (IsIvanPersonality(personality))
        {
            modulation = ModulateIvanTone(emotionalContext);
        }
        else
        {
            modulation = ModulateGenericTone(personality, emotionalContext);
        }

        _logger.LogDebug("Tone modulation: Warmth={Warmth}, Analytical={Analytical}, Supportive={Supportive}",
            modulation.WarmthLevel, modulation.AnalyticalLevel, modulation.SupportivenessLevel);

        return modulation;
    }

    public List<WeightedTrait> GetRelevantTraits(PersonalityProfile personality, TaskType taskType)
    {
        _logger.LogDebug("Extracting relevant traits for {PersonalityName} and task type {TaskType}",
            personality.Name, taskType);

        var relevantTraits = new List<WeightedTrait>();

        if (personality.Traits == null || !personality.Traits.Any())
        {
            _logger.LogWarning("No traits found for personality {PersonalityName}", personality.Name);
            return relevantTraits;
        }

        // Task-specific trait relevance mapping
        var traitRelevanceWeights = GetTraitRelevanceForTaskType(taskType);

        foreach (var trait in personality.Traits)
        {
            if (traitRelevanceWeights.TryGetValue(trait.Category, out var relevanceMultiplier))
            {
                var weightedTrait = new WeightedTrait
                {
                    Trait = trait,
                    RelevanceWeight = trait.Weight * relevanceMultiplier,
                    TaskSpecificModifier = GetTaskSpecificModifier(trait, taskType)
                };

                relevantTraits.Add(weightedTrait);
            }
        }

        // Sort by relevance weight, most relevant first
        relevantTraits = relevantTraits
            .OrderByDescending(wt => wt.RelevanceWeight)
            .Take(10) // Limit to top 10 most relevant traits
            .ToList();

        _logger.LogDebug("Found {RelevantTraitCount} relevant traits for task type {TaskType}",
            relevantTraits.Count, taskType);

        return relevantTraits;
    }

    #region Private Helper Methods

    private bool IsIvanPersonality(PersonalityProfile personality)
    {
        return personality.Name.Contains("Ivan", StringComparison.OrdinalIgnoreCase) ||
               personality.Description.Contains("Ivan", StringComparison.OrdinalIgnoreCase);
    }

    private BehaviorModifiers ApplyIvanBehaviorPatterns(BehaviorModifiers modifiers, InteractionContext context)
    {
        // Ivan's signature behavioral patterns
        modifiers.ConfidenceLevel = 0.85; // High confidence but not arrogant
        modifiers.DirectnessLevel = 0.80; // Direct communication
        modifiers.StructuredThinking = 0.95; // Highly structured approach
        modifiers.SelfReflectionLevel = 0.70; // Moderate self-reflection

        // Context-specific adjustments
        switch (context.Type)
        {
            case InteractionType.Technical:
                modifiers.TechnicalDetailLevel = 0.90;
                modifiers.PragmatismLevel = 0.95;
                modifiers.ConfidenceLevel = 0.95; // Very confident in technical matters
                break;

            case InteractionType.Personal:
                modifiers.WarmthLevel = 0.75;
                modifiers.SelfReflectionLevel = 0.85; // More reflective about personal matters
                modifiers.VulnerabilityLevel = 0.40; // Moderate openness about challenges
                break;

            case InteractionType.Professional:
                modifiers.FormalityLevel = 0.60;
                modifiers.LeadershipTone = 0.80;
                modifiers.ResultsOrientation = 0.90;
                break;

            case InteractionType.Family:
                modifiers.WarmthLevel = 0.90;
                modifiers.ProtectiveLevel = 0.85;
                modifiers.SelfReflectionLevel = 0.90; // High awareness of work-family tension
                break;
        }

        return modifiers;
    }

    private void ApplyTraitModification(BehaviorModifiers modifiers, PersonalityTrait trait, InteractionContext context)
    {
        // Apply trait-specific behavioral modifications
        switch (trait.Category.ToLowerInvariant())
        {
            case "professional":
                modifiers.CompetenceLevel += trait.Weight * 0.1;
                modifiers.TechnicalDetailLevel += trait.Weight * 0.05;
                break;

            case "personality":
                if (trait.Name.Contains("Decision Making", StringComparison.OrdinalIgnoreCase))
                {
                    modifiers.StructuredThinking += trait.Weight * 0.1;
                    modifiers.AnalyticalDepth += trait.Weight * 0.1;
                }
                break;

            case "technical":
                modifiers.TechnicalDetailLevel += trait.Weight * 0.15;
                modifiers.PragmatismLevel += trait.Weight * 0.1;
                break;
        }

        // Normalize values to stay within 0-1 range
        NormalizeBehaviorModifiers(modifiers);
    }

    private CommunicationStyle MapIvanCommunicationStyle(SituationType situationType)
    {
        var style = new CommunicationStyle
        {
            BasePersonality = "Direct, structured, and pragmatic with occasional self-awareness about work-life balance",
            PreferredTone = "Professional but friendly"
        };

        switch (situationType)
        {
            case SituationType.Technical:
                style.FormalityLevel = 0.4; // Informal but precise
                style.TechnicalLanguageUsage = 0.95; // Heavy technical language
                style.ExplanationDepth = 0.85; // Deep technical explanations
                style.ExampleUsage = 0.80; // Frequent code examples
                style.PreferredTone = "Confident and detailed";
                break;

            case SituationType.Personal:
                style.FormalityLevel = 0.2; // Very informal
                style.TechnicalLanguageUsage = 0.3; // Minimal technical jargon
                style.EmotionalOpenness = 0.65; // Moderate emotional sharing
                style.SelfDisclosureLevel = 0.70; // Open about personal struggles
                style.PreferredTone = "Reflective and honest";
                break;

            case SituationType.Professional:
                style.FormalityLevel = 0.65; // More formal
                style.TechnicalLanguageUsage = 0.70; // Balanced technical usage
                style.LeadershipAssertiveness = 0.75; // Clear leadership voice
                style.ResultsOrientation = 0.90; // Focus on outcomes
                style.PreferredTone = "Assertive and results-focused";
                break;

            case SituationType.Family:
                style.FormalityLevel = 0.1; // Very informal
                style.WarmthLevel = 0.90; // High warmth
                style.ProtectiveInstinct = 0.85; // Strong protective language
                style.FutureOrientation = 0.80; // Focus on family's future
                style.PreferredTone = "Caring but concerned about balance";
                break;
        }

        return style;
    }

    private CommunicationStyle MapGenericCommunicationStyle(PersonalityProfile personality, SituationType situationType)
    {
        // Generic mapping based on available traits
        var style = new CommunicationStyle
        {
            FormalityLevel = 0.5,
            TechnicalLanguageUsage = 0.5,
            PreferredTone = "Balanced and professional"
        };

        // Adjust based on personality traits if available
        if (personality.Traits != null)
        {
            foreach (var trait in personality.Traits)
            {
                if (trait.Category.Equals("Professional", StringComparison.OrdinalIgnoreCase))
                {
                    style.TechnicalLanguageUsage += trait.Weight * 0.1;
                    style.CompetenceProjection += trait.Weight * 0.1;
                }
            }
        }

        NormalizeCommunicationStyle(style);
        return style;
    }

    private ToneModulation ModulateIvanTone(EmotionalContext emotionalContext)
    {
        var modulation = new ToneModulation
        {
            BaselineConfidence = 0.80,
            AnalyticalLevel = 0.85,
            WarmthLevel = 0.60
        };

        switch (emotionalContext.PrimaryEmotion)
        {
            case EmotionalState.Confident:
                modulation.ConfidenceBoost = 0.15;
                modulation.AnalyticalLevel = 0.90;
                modulation.DirectnessLevel = 0.85;
                break;

            case EmotionalState.Concerned:
                modulation.CautiousLevel = 0.75;
                modulation.AnalyticalLevel = 0.95; // More analytical when concerned
                modulation.SupportivenessLevel = 0.70;
                break;

            case EmotionalState.Excited:
                modulation.EnthusiasmLevel = 0.85;
                modulation.TechnicalDetailLevel = 0.90; // More details when excited about tech
                modulation.WarmthLevel = 0.75;
                break;

            case EmotionalState.Reflective:
                modulation.SelfAwarenessLevel = 0.90;
                modulation.VulnerabilityLevel = 0.55;
                modulation.AnalyticalLevel = 0.95;
                break;

            case EmotionalState.Frustrated:
                modulation.DirectnessLevel = 0.90;
                modulation.PragmatismLevel = 0.95;
                modulation.WarmthLevel = 0.45; // Less warm when frustrated
                break;
        }

        return modulation;
    }

    private ToneModulation ModulateGenericTone(PersonalityProfile personality, EmotionalContext emotionalContext)
    {
        // Generic tone modulation
        return new ToneModulation
        {
            BaselineConfidence = 0.60,
            AnalyticalLevel = 0.50,
            WarmthLevel = 0.50,
            SupportivenessLevel = 0.60
        };
    }

    private Dictionary<string, double> GetTraitRelevanceForTaskType(TaskType taskType)
    {
        return taskType switch
        {
            TaskType.Technical => new Dictionary<string, double>
            {
                { "Technical", 1.0 },
                { "Professional", 0.8 },
                { "Personality", 0.6 },
                { "Background", 0.4 }
            },
            TaskType.Personal => new Dictionary<string, double>
            {
                { "Personality", 1.0 },
                { "Lifestyle", 0.9 },
                { "Aspirations", 0.8 },
                { "Demographics", 0.7 },
                { "Professional", 0.3 }
            },
            TaskType.Professional => new Dictionary<string, double>
            {
                { "Professional", 1.0 },
                { "Technical", 0.8 },
                { "Aspirations", 0.7 },
                { "Personality", 0.6 }
            },
            _ => new Dictionary<string, double>
            {
                { "Personality", 0.8 },
                { "Professional", 0.6 },
                { "Technical", 0.5 }
            }
        };
    }

    private double GetTaskSpecificModifier(PersonalityTrait trait, TaskType taskType)
    {
        // Additional task-specific modifiers based on trait content
        if (taskType == TaskType.Technical &&
            (trait.Name.Contains("Tech", StringComparison.OrdinalIgnoreCase) ||
             trait.Description.Contains("C#", StringComparison.OrdinalIgnoreCase)))
        {
            return 1.2; // Boost technical traits for technical tasks
        }

        if (taskType == TaskType.Personal &&
            (trait.Name.Contains("Family", StringComparison.OrdinalIgnoreCase) ||
             trait.Name.Contains("Life", StringComparison.OrdinalIgnoreCase)))
        {
            return 1.15; // Boost personal traits for personal tasks
        }

        return 1.0; // No modifier
    }

    private void NormalizeBehaviorModifiers(BehaviorModifiers modifiers)
    {
        // Ensure all values stay within reasonable bounds
        modifiers.ConfidenceLevel = Math.Clamp(modifiers.ConfidenceLevel, 0.0, 1.0);
        modifiers.DirectnessLevel = Math.Clamp(modifiers.DirectnessLevel, 0.0, 1.0);
        modifiers.TechnicalDetailLevel = Math.Clamp(modifiers.TechnicalDetailLevel, 0.0, 1.0);
        modifiers.StructuredThinking = Math.Clamp(modifiers.StructuredThinking, 0.0, 1.0);
        modifiers.WarmthLevel = Math.Clamp(modifiers.WarmthLevel, 0.0, 1.0);
        modifiers.PragmatismLevel = Math.Clamp(modifiers.PragmatismLevel, 0.0, 1.0);
    }

    private void NormalizeCommunicationStyle(CommunicationStyle style)
    {
        style.FormalityLevel = Math.Clamp(style.FormalityLevel, 0.0, 1.0);
        style.TechnicalLanguageUsage = Math.Clamp(style.TechnicalLanguageUsage, 0.0, 1.0);
        style.WarmthLevel = Math.Clamp(style.WarmthLevel, 0.0, 1.0);
        style.EmotionalOpenness = Math.Clamp(style.EmotionalOpenness, 0.0, 1.0);
    }

    #endregion
}

#region Supporting Data Classes

/// <summary>
/// Модификаторы поведения для настройки ответов агента.
/// </summary>
public class BehaviorModifiers
{
    public double ConfidenceLevel { get; set; } = 0.5;
    public double DirectnessLevel { get; set; } = 0.5;
    public double TechnicalDetailLevel { get; set; } = 0.5;
    public double StructuredThinking { get; set; } = 0.5;
    public double WarmthLevel { get; set; } = 0.5;
    public double PragmatismLevel { get; set; } = 0.5;
    public double SelfReflectionLevel { get; set; } = 0.5;
    public double VulnerabilityLevel { get; set; } = 0.3;
    public double FormalityLevel { get; set; } = 0.5;
    public double LeadershipTone { get; set; } = 0.5;
    public double ResultsOrientation { get; set; } = 0.5;
    public double ProtectiveLevel { get; set; } = 0.5;
    public double CompetenceLevel { get; set; } = 0.5;
    public double AnalyticalDepth { get; set; } = 0.5;
}

/// <summary>
/// Стиль коммуникации для адаптации под ситуацию.
/// </summary>
public class CommunicationStyle
{
    public string BasePersonality { get; set; } = string.Empty;
    public string PreferredTone { get; set; } = string.Empty;
    public double FormalityLevel { get; set; } = 0.5;
    public double TechnicalLanguageUsage { get; set; } = 0.5;
    public double ExplanationDepth { get; set; } = 0.5;
    public double ExampleUsage { get; set; } = 0.5;
    public double EmotionalOpenness { get; set; } = 0.5;
    public double SelfDisclosureLevel { get; set; } = 0.5;
    public double LeadershipAssertiveness { get; set; } = 0.5;
    public double ResultsOrientation { get; set; } = 0.5;
    public double WarmthLevel { get; set; } = 0.5;
    public double ProtectiveInstinct { get; set; } = 0.5;
    public double FutureOrientation { get; set; } = 0.5;
    public double CompetenceProjection { get; set; } = 0.5;
}

/// <summary>
/// Модуляция тона на основе эмоционального контекста.
/// </summary>
public class ToneModulation
{
    public double BaselineConfidence { get; set; } = 0.5;
    public double AnalyticalLevel { get; set; } = 0.5;
    public double WarmthLevel { get; set; } = 0.5;
    public double ConfidenceBoost { get; set; } = 0.0;
    public double CautiousLevel { get; set; } = 0.5;
    public double SupportivenessLevel { get; set; } = 0.5;
    public double EnthusiasmLevel { get; set; } = 0.5;
    public double TechnicalDetailLevel { get; set; } = 0.5;
    public double SelfAwarenessLevel { get; set; } = 0.5;
    public double VulnerabilityLevel { get; set; } = 0.3;
    public double DirectnessLevel { get; set; } = 0.5;
    public double PragmatismLevel { get; set; } = 0.5;
}

/// <summary>
/// Черта личности с весовым коэффициентом релевантности.
/// </summary>
public class WeightedTrait
{
    public PersonalityTrait Trait { get; set; } = null!;
    public double RelevanceWeight { get; set; }
    public double TaskSpecificModifier { get; set; } = 1.0;
}

/// <summary>
/// Контекст взаимодействия для адаптации поведения.
/// </summary>
public class InteractionContext
{
    public InteractionType Type { get; set; }
    public string Topic { get; set; } = string.Empty;
    public int ComplexityLevel { get; set; } = 1; // 1-5 scale
    public bool IsUrgent { get; set; } = false;
    public string UserRole { get; set; } = string.Empty; // colleague, family, friend, etc.
}

/// <summary>
/// Эмоциональный контекст для модуляции тона.
/// </summary>
public class EmotionalContext
{
    public EmotionalState PrimaryEmotion { get; set; }
    public double Intensity { get; set; } = 0.5; // 0.0-1.0
    public string Trigger { get; set; } = string.Empty;
}

/// <summary>
/// Типы взаимодействий.
/// </summary>
public enum InteractionType
{
    Technical,
    Personal,
    Professional,
    Family,
    Creative,
    ProblemSolving
}

/// <summary>
/// Типы ситуаций для адаптации коммуникации.
/// </summary>
public enum SituationType
{
    Technical,
    Personal,
    Professional,
    Family,
    Crisis,
    Celebration
}

/// <summary>
/// Типы задач для извлечения релевантных черт.
/// </summary>
public enum TaskType
{
    Technical,
    Personal,
    Professional,
    Creative,
    Analytical,
    Communication
}

/// <summary>
/// Эмоциональные состояния.
/// </summary>
public enum EmotionalState
{
    Confident,
    Concerned,
    Excited,
    Reflective,
    Frustrated,
    Pleased,
    Analytical,
    Protective
}

#endregion