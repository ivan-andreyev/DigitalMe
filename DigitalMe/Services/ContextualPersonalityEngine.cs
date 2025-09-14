using DigitalMe.Data.Entities;
using DigitalMe.Services.PersonalityEngine;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DigitalMe.Services;

/// <summary>
/// Интерфейс для контекстно-зависимого управления личностью.
/// Адаптирует поведение агента под текущую ситуацию и контекст взаимодействия.
/// </summary>
public interface IContextualPersonalityEngine
{
    /// <summary>
    /// Адаптирует профиль личности под текущий ситуационный контекст.
    /// </summary>
    /// <param name="basePersonality">Базовый профиль личности</param>
    /// <param name="context">Ситуационный контекст</param>
    /// <returns>Адаптированный профиль личности</returns>
    Task<PersonalityProfile> AdaptPersonalityToContextAsync(PersonalityProfile basePersonality, SituationalContext context);

    /// <summary>
    /// Модифицирует поведенческие паттерны на основе уровня стресса и временного давления.
    /// </summary>
    /// <param name="personality">Профиль личности для модификации</param>
    /// <param name="stressLevel">Уровень стресса (0.0-1.0)</param>
    /// <param name="timePressure">Уровень временного давления (0.0-1.0)</param>
    /// <returns>Модифицированные паттерны поведения</returns>
    StressBehaviorModifications ModifyBehaviorForStressAndTime(PersonalityProfile personality, double stressLevel, double timePressure);

    /// <summary>
    /// Настраивает уверенность в ответах на основе экспертизы в предметной области.
    /// </summary>
    /// <param name="personality">Профиль личности</param>
    /// <param name="domainType">Тип предметной области</param>
    /// <param name="taskComplexity">Сложность задачи (1-10)</param>
    /// <returns>Настройки уверенности для данной области</returns>
    ExpertiseConfidenceAdjustment AdjustConfidenceByExpertise(PersonalityProfile personality, DomainType domainType, int taskComplexity);

    /// <summary>
    /// Определяет оптимальный стиль коммуникации для текущего контекста.
    /// </summary>
    /// <param name="personality">Профиль личности</param>
    /// <param name="context">Контекст взаимодействия</param>
    /// <returns>Рекомендуемый стиль коммуникации</returns>
    ContextualCommunicationStyle DetermineOptimalCommunicationStyle(PersonalityProfile personality, SituationalContext context);

    /// <summary>
    /// Анализирует контекст и предоставляет рекомендации по адаптации поведения.
    /// </summary>
    /// <param name="context">Ситуационный контекст</param>
    /// <returns>Рекомендации по адаптации</returns>
    ContextAnalysisResult AnalyzeContextRequirements(SituationalContext context);
}

/// <summary>
/// Реализация движка контекстно-зависимой адаптации личности.
/// Выступает как оркестратор, делегирующий работу специализированным сервисам для соблюдения принципа SRP.
/// </summary>
public class ContextualPersonalityEngine : IContextualPersonalityEngine
{
    private readonly ILogger<ContextualPersonalityEngine> _logger;
    private readonly IPersonalityContextAdapter _contextAdapter;
    private readonly IStressBehaviorAnalyzer _stressBehaviorAnalyzer;
    private readonly IExpertiseConfidenceAnalyzer _expertiseConfidenceAnalyzer;
    private readonly ICommunicationStyleAnalyzer _communicationStyleAnalyzer;
    private readonly IContextAnalyzer _contextAnalyzer;

    public ContextualPersonalityEngine(
        ILogger<ContextualPersonalityEngine> logger,
        IPersonalityContextAdapter contextAdapter,
        IStressBehaviorAnalyzer stressBehaviorAnalyzer,
        IExpertiseConfidenceAnalyzer expertiseConfidenceAnalyzer,
        ICommunicationStyleAnalyzer communicationStyleAnalyzer,
        IContextAnalyzer contextAnalyzer)
    {
        _logger = logger;
        _contextAdapter = contextAdapter;
        _stressBehaviorAnalyzer = stressBehaviorAnalyzer;
        _expertiseConfidenceAnalyzer = expertiseConfidenceAnalyzer;
        _communicationStyleAnalyzer = communicationStyleAnalyzer;
        _contextAnalyzer = contextAnalyzer;
    }

    public async Task<PersonalityProfile> AdaptPersonalityToContextAsync(PersonalityProfile basePersonality, SituationalContext context)
    {
        _logger.LogDebug("Orchestrating personality adaptation for {PersonalityName} to context: {ContextType}, urgency: {Urgency}, time: {TimeOfDay}",
            basePersonality.Name, context.ContextType, context.UrgencyLevel, context.TimeOfDay);

        // Delegate to specialized context adapter
        var adaptedPersonality = await _contextAdapter.AdaptToContextAsync(basePersonality, context);

        _logger.LogDebug("Personality adaptation orchestrated successfully for context {ContextType}", context.ContextType);
        return adaptedPersonality;
    }

    public StressBehaviorModifications ModifyBehaviorForStressAndTime(PersonalityProfile personality, double stressLevel, double timePressure)
    {
        _logger.LogDebug("Orchestrating stress behavior analysis for {PersonalityName}: stress={StressLevel:F2}, timePressure={TimePressure:F2}",
            personality.Name, stressLevel, timePressure);

        // Delegate to specialized stress behavior analyzer
        var modifications = _stressBehaviorAnalyzer.AnalyzeStressModifications(personality, stressLevel, timePressure);

        _logger.LogDebug("Stress behavior analysis orchestrated: Directness={Directness:F2}, TechnicalDetail={TechnicalDetail:F2}",
            modifications.DirectnessIncrease, modifications.TechnicalDetailReduction);

        return modifications;
    }

    public ExpertiseConfidenceAdjustment AdjustConfidenceByExpertise(PersonalityProfile personality, DomainType domainType, int taskComplexity)
    {
        _logger.LogDebug("Orchestrating expertise confidence analysis for {PersonalityName} in domain {Domain} with complexity {Complexity}",
            personality.Name, domainType, taskComplexity);

        // Delegate to specialized expertise confidence analyzer
        var adjustment = _expertiseConfidenceAnalyzer.AnalyzeExpertiseConfidence(personality, domainType, taskComplexity);

        _logger.LogDebug("Expertise confidence analysis orchestrated: Base={BaseConfidence:F2}, Final={FinalConfidence:F2}",
            adjustment.BaseConfidence, adjustment.AdjustedConfidence);

        return adjustment;
    }

    public ContextualCommunicationStyle DetermineOptimalCommunicationStyle(PersonalityProfile personality, SituationalContext context)
    {
        _logger.LogDebug("Orchestrating communication style analysis for {PersonalityName} in {ContextType} context",
            personality.Name, context.ContextType);

        // Delegate to specialized communication style analyzer
        var style = _communicationStyleAnalyzer.DetermineOptimalCommunicationStyle(personality, context);

        _logger.LogDebug("Communication style analysis orchestrated: {StyleSummary}", style.StyleSummary);
        return style;
    }

    public ContextAnalysisResult AnalyzeContextRequirements(SituationalContext context)
    {
        _logger.LogDebug("Orchestrating context requirements analysis for {ContextType} with urgency {Urgency}",
            context.ContextType, context.UrgencyLevel);

        // Delegate to specialized context analyzer
        var result = _contextAnalyzer.AnalyzeContextRequirements(context);

        _logger.LogDebug("Context requirements analysis orchestrated with {RecommendationCount} recommendations",
            result.AdaptationRecommendations.Count);

        return result;
    }

    // Orchestrator no longer contains private helper methods - all logic delegated to specialized services
}

#region Supporting Data Classes

/// <summary>
/// Ситуационный контекст для адаптации личности.
/// </summary>
public class SituationalContext
{
    public ContextType ContextType { get; set; }
    public EnvironmentType Environment { get; set; }
    public string Topic { get; set; } = "";
    public double UrgencyLevel { get; set; } = 0.5; // 0.0-1.0
    public TimeOfDay TimeOfDay { get; set; }
    public string UserRole { get; set; } = ""; // colleague, family, friend, client
    public Dictionary<string, object> AdditionalContext { get; set; } = new();
}

/// <summary>
/// Модификации поведения под стрессом и временным давлением.
/// </summary>
public class StressBehaviorModifications
{
    public double StressLevel { get; set; }
    public double TimePressure { get; set; }
    public double DirectnessIncrease { get; set; }
    public double StructuredThinkingBoost { get; set; }
    public double TechnicalDetailReduction { get; set; }
    public double WarmthReduction { get; set; }
    public double SolutionFocusBoost { get; set; }
    public double SelfReflectionReduction { get; set; }
    public double ConfidenceBoost { get; set; }
    public double PragmatismIncrease { get; set; }
    public double ResultsOrientationIncrease { get; set; }
}

/// <summary>
/// Настройка уверенности на основе экспертизы.
/// </summary>
public class ExpertiseConfidenceAdjustment
{
    public DomainType Domain { get; set; }
    public int TaskComplexity { get; set; }
    public double BaseConfidence { get; set; }
    public double ComplexityAdjustment { get; set; }
    public double DomainExpertiseBonus { get; set; }
    public double KnownWeaknessReduction { get; set; }
    public double AdjustedConfidence { get; set; }
    public string ConfidenceExplanation => GenerateConfidenceExplanation();

    private string GenerateConfidenceExplanation()
    {
        var parts = new List<string>();

        if (BaseConfidence > 0.8)
            parts.Add($"High expertise in {Domain}");
        else if (BaseConfidence < 0.4)
            parts.Add($"Limited experience in {Domain}");

        if (TaskComplexity > 7)
            parts.Add("Complex task reduces confidence");

        if (DomainExpertiseBonus > 0)
            parts.Add("Core competency area");

        if (KnownWeaknessReduction > 0)
            parts.Add("Acknowledged weakness area");

        return string.Join(", ", parts);
    }
}

/// <summary>
/// Контекстно-зависимый стиль коммуникации.
/// </summary>
public class ContextualCommunicationStyle
{
    public SituationalContext Context { get; set; } = null!;
    public string BasePersonalityName { get; set; } = "";
    public double FormalityLevel { get; set; } = 0.5;
    public double DirectnessLevel { get; set; } = 0.5;
    public double TechnicalDepth { get; set; } = 0.5;
    public double EmotionalOpenness { get; set; } = 0.5;
    public double LeadershipTone { get; set; } = 0.5;
    public double WarmthLevel { get; set; } = 0.5;
    public double SelfReflection { get; set; } = 0.5;
    public double VulnerabilityLevel { get; set; } = 0.3;
    public double ProtectiveInstinct { get; set; } = 0.5;
    public double ExampleUsage { get; set; } = 0.5;
    public double ResultsFocus { get; set; } = 0.5;
    public double ExplanationDepth { get; set; } = 0.5;
    public double SelfDisclosureLevel { get; set; } = 0.5;
    public double LeadershipAssertiveness { get; set; } = 0.5;
    public double ResultsOrientation { get; set; } = 0.5;
    public string RecommendedTone { get; set; } = "";
    public double TechnicalLanguageUsage { get; set; } = 0.5;
    public double EnergyLevel { get; set; } = 0.5;
    public string BasePersonality { get; set; } = ""; // Унификация с BasePersonalityName
    public string StyleSummary { get; set; } = "";
}

/// <summary>
/// Данные временного анализа для контекста.
/// </summary>
public class TemporalAnalysisData
{
    public TimeOfDay TimeOfDay { get; set; }
    public double EnergyLevel { get; set; }
    public double ProductivityScore { get; set; }
    public List<string> TimeBasedRecommendations { get; set; } = new();
    public string Description { get; set; } = "";
}

/// <summary>
/// Результат анализа контекстных требований.
/// </summary>
public class ContextAnalysisResult
{
    public SituationalContext Context { get; set; } = null!;
    public DateTime AnalysisTimestamp { get; set; }
    public ResponseSpeed RequiredResponseSpeed { get; set; }
    public DetailLevel RecommendedDetailLevel { get; set; }
    public double RecommendedFormalityLevel { get; set; }
    public EmotionalTone RecommendedEmotionalTone { get; set; }
    public List<string> AdaptationRecommendations { get; set; } = new();

    // Расширенные свойства для полного анализа контекста
    public ContextType ContextType { get; set; }
    public int ComplexityLevel { get; set; }
    public List<string> RequiredAdaptations { get; set; } = new();
    public string RecommendedApproach { get; set; } = "";
    public List<string> KeyConsiderations { get; set; } = new();
    public List<string> ExpectedChallenges { get; set; } = new();
    public string OptimalStrategy { get; set; } = "";
    public TemporalAnalysisData? TemporalAnalysis { get; set; }
    public List<string> KeyFactors { get; set; } = new();
    public List<string> SuccessMetrics { get; set; } = new();
}

/// <summary>
/// Типы контекста взаимодействия.
/// </summary>
public enum ContextType
{
    Technical,
    Professional,
    Personal,
    Family,
    Creative,
    Strategic,
    Crisis,
    Celebration
}

/// <summary>
/// Типы среды взаимодействия.
/// </summary>
public enum EnvironmentType
{
    Unknown,
    Professional,
    Personal,
    Technical,
    Social,
    Educational,
    Family
}

/// <summary>
/// Время дня для контекстной адаптации.
/// </summary>
public enum TimeOfDay
{
    Morning,
    Afternoon,
    Evening,
    Late
}

/// <summary>
/// Предметные области для оценки экспертизы.
/// </summary>
public enum DomainType
{
    CSharpDotNet,
    SoftwareArchitecture,
    DatabaseDesign,
    GameDevelopment,
    TeamLeadership,
    RnDManagement,
    BusinessStrategy,
    PersonalRelations,
    WorkLifeBalance,
    Politics,
    Finance,
    Education,
    Psychology,
    Marketing
}

/// <summary>
/// Скорость реакции.
/// </summary>
public enum ResponseSpeed
{
    Immediate,
    Fast,
    Normal,
    Thoughtful
}

/// <summary>
/// Уровень детализации.
/// </summary>
public enum DetailLevel
{
    Low,
    Medium,
    High,
    Comprehensive
}

/// <summary>
/// Эмоциональный тон.
/// </summary>
public enum EmotionalTone
{
    Professional,
    Warm,
    Analytical,
    Focused,
    Reflective,
    Supportive,
    Confident
}

#endregion