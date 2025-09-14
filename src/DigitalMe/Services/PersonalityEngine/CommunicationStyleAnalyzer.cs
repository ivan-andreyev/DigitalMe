using DigitalMe.Data.Entities;
using DigitalMe.Services.Strategies;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.PersonalityEngine;

/// <summary>
/// Реализация анализатора стиля коммуникации.
/// Использует Strategy Pattern для делегирования анализа подходящим стратегиям.
/// </summary>
public class CommunicationStyleAnalyzer : ICommunicationStyleAnalyzer
{
    private readonly ILogger<CommunicationStyleAnalyzer> _logger;
    private readonly IPersonalityStrategyFactory _strategyFactory;
    private readonly IPersonalityConfigurationService _configurationService;

    /// <summary>
    /// Инициализирует новый экземпляр анализатора стиля коммуникации.
    /// </summary>
    /// <param name="logger">Логгер для записи диагностической информации</param>
    /// <param name="strategyFactory">Фабрика стратегий для получения подходящих стратегий анализа</param>
    /// <param name="configurationService">Сервис конфигурации персоналий</param>
    public CommunicationStyleAnalyzer(
        ILogger<CommunicationStyleAnalyzer> logger,
        IPersonalityStrategyFactory strategyFactory,
        IPersonalityConfigurationService configurationService)
    {
        _logger = logger;
        _strategyFactory = strategyFactory;
        _configurationService = configurationService;
    }

    public ContextualCommunicationStyle DetermineOptimalCommunicationStyle(PersonalityProfile personality, SituationalContext context)
    {
        _logger.LogDebug("Determining optimal communication style for {PersonalityName} in context {ContextType}",
            personality.Name, context.ContextType);

        // Get appropriate strategy for this personality
        var strategy = _strategyFactory.GetStrategy(personality);
        if (strategy == null)
        {
            _logger.LogWarning("No strategy found for personality {PersonalityName}, using generic communication style",
                personality.Name);
            return CreateGenericCommunicationStyle(personality, context);
        }

        _logger.LogDebug("Using strategy {StrategyName} for communication style analysis",
            strategy.StrategyName);

        // Delegate to the strategy
        var style = strategy.DetermineCommunicationStyle(personality, context);

        _logger.LogDebug("Communication style determined for {PersonalityName}: formality={Formality}, directness={Directness}, warmth={Warmth}",
            personality.Name, style.FormalityLevel, style.DirectnessLevel, style.WarmthLevel);

        return style;
    }

    public CommunicationContextAnalysis AnalyzeCommunicationContext(SituationalContext context)
    {
        _logger.LogDebug("Analyzing communication context: type={ContextType}, urgency={Urgency}",
            context.ContextType, context.UrgencyLevel);

        var analysis = new CommunicationContextAnalysis
        {
            Context = context,
            RecommendedFormalityLevel = DetermineBaseFormalityLevel(context),
            RecommendedDirectnessLevel = DetermineBaseDirectnessLevel(context),
            RecommendedTechnicalDepth = DetermineBaseTechnicalDepth(context),
            RecommendedEmotionalOpenness = DetermineBaseEmotionalOpenness(context),
            RecommendedWarmthLevel = DetermineBaseWarmthLevel(context),
            CommunicationRequirements = GetCommunicationRequirements(context),
            CommunicationChallenges = GetCommunicationChallenges(context),
            RecommendedTone = DetermineRecommendedTone(context),
            StyleRecommendations = GetStyleRecommendations(context),
            PriorityCommunicationAspects = GetPriorityCommunicationAspects(context),
            AnalysisTimestamp = DateTime.UtcNow
        };

        _logger.LogDebug("Communication context analysis completed: formality={Formality}, directness={Directness}, tone={Tone}",
            analysis.RecommendedFormalityLevel, analysis.RecommendedDirectnessLevel, analysis.RecommendedTone);

        return analysis;
    }

    public double DetermineFormalityLevel(PersonalityProfile personality, SituationalContext context)
    {
        _logger.LogDebug("Determining formality level for {PersonalityName} in {ContextType}",
            personality.Name, context.ContextType);

        var baseFormalityLevel = DetermineBaseFormalityLevel(context);

        // Get strategy for personality-specific adjustments
        var strategy = _strategyFactory.GetStrategy(personality);
        if (strategy != null)
        {
            // Delegate to strategy for full communication style, then extract formality
            var communicationStyle = strategy.DetermineCommunicationStyle(personality, context);
            return communicationStyle.FormalityLevel;
        }

        return baseFormalityLevel;
    }

    public string DetermineRecommendedTone(PersonalityProfile personality, SituationalContext context)
    {
        _logger.LogDebug("Determining recommended tone for {PersonalityName} in {ContextType}",
            personality.Name, context.ContextType);

        // Get strategy for personality-specific tone adjustments
        var strategy = _strategyFactory.GetStrategy(personality);
        if (strategy != null)
        {
            // Delegate to strategy for full communication style, then extract recommended tone
            var communicationStyle = strategy.DetermineCommunicationStyle(personality, context);
            return communicationStyle.RecommendedTone;
        }

        // Fallback to generic tone determination
        return DetermineRecommendedTone(context);
    }

    #region Private Helper Methods

    private ContextualCommunicationStyle CreateGenericCommunicationStyle(PersonalityProfile personality, SituationalContext context)
    {
        return new ContextualCommunicationStyle
        {
            Context = context,
            BasePersonalityName = personality.Name,
            FormalityLevel = DetermineBaseFormalityLevel(context),
            DirectnessLevel = DetermineBaseDirectnessLevel(context),
            TechnicalDepth = DetermineBaseTechnicalDepth(context),
            EmotionalOpenness = DetermineBaseEmotionalOpenness(context),
            WarmthLevel = DetermineBaseWarmthLevel(context),
            RecommendedTone = DetermineRecommendedTone(context),
            TechnicalLanguageUsage = DetermineBaseTechnicalDepth(context),
            LeadershipTone = 0.5,
            SelfReflection = 0.5,
            VulnerabilityLevel = 0.3,
            ProtectiveInstinct = 0.5,
            ExampleUsage = 0.5,
            ResultsFocus = context.UrgencyLevel > PersonalityConstants.HighUrgencyThreshold ? 0.8 : 0.5,
            ExplanationDepth = DetermineBaseTechnicalDepth(context),
            SelfDisclosureLevel = context.ContextType == ContextType.Personal ? 0.7 : 0.3,
            LeadershipAssertiveness = 0.5,
            ResultsOrientation = context.UrgencyLevel > PersonalityConstants.HighUrgencyThreshold ? 0.8 : 0.5
        };
    }

    private double DetermineBaseFormalityLevel(SituationalContext context)
    {
        return context.ContextType switch
        {
            ContextType.Technical => PersonalityConstants.TechnicalContextFormality,
            ContextType.Professional => PersonalityConstants.ProfessionalContextFormality,
            ContextType.Personal => PersonalityConstants.PersonalContextFormality,
            ContextType.Family => PersonalityConstants.FamilyContextFormality,
            _ => PersonalityConstants.DefaultContextFormality
        };
    }

    private double DetermineBaseDirectnessLevel(SituationalContext context)
    {
        var baseDirectness = context.ContextType switch
        {
            ContextType.Technical => 0.8,
            ContextType.Professional => 0.7,
            ContextType.Personal => 0.5,
            ContextType.Family => 0.4,
            _ => 0.6
        };

        // Adjust for urgency
        if (context.UrgencyLevel > PersonalityConstants.HighUrgencyThreshold)
        {
            baseDirectness = Math.Min(baseDirectness + 0.2, 1.0);
        }

        return baseDirectness;
    }

    private double DetermineBaseTechnicalDepth(SituationalContext context)
    {
        return context.ContextType switch
        {
            ContextType.Technical => 0.9,
            ContextType.Professional => 0.6,
            ContextType.Personal => 0.3,
            ContextType.Family => 0.2,
            _ => 0.5
        };
    }

    private double DetermineBaseEmotionalOpenness(SituationalContext context)
    {
        return context.ContextType switch
        {
            ContextType.Personal => 0.8,
            ContextType.Family => 0.9,
            ContextType.Professional => 0.4,
            ContextType.Technical => 0.3,
            _ => 0.5
        };
    }

    private double DetermineBaseWarmthLevel(SituationalContext context)
    {
        return context.ContextType switch
        {
            ContextType.Family => 0.9,
            ContextType.Personal => 0.7,
            ContextType.Professional => 0.5,
            ContextType.Technical => 0.4,
            _ => 0.5
        };
    }

    private List<string> GetCommunicationRequirements(SituationalContext context)
    {
        var requirements = new List<string>();

        switch (context.ContextType)
        {
            case ContextType.Technical:
                requirements.AddRange(new[] { "Technical precision", "Clear explanations", "Concrete examples", "Structured approach" });
                break;

            case ContextType.Professional:
                requirements.AddRange(new[] { "Professional tone", "Business focus", "Action orientation", "Clear outcomes" });
                break;

            case ContextType.Personal:
                requirements.AddRange(new[] { "Emotional awareness", "Personal boundaries", "Empathy", "Authenticity" });
                break;

            case ContextType.Family:
                requirements.AddRange(new[] { "Warmth", "Care", "Personal investment", "Family sensitivity" });
                break;
        }

        if (context.UrgencyLevel > PersonalityConstants.HighUrgencyThreshold)
        {
            requirements.AddRange(new[] { "Quick responses", "Focus on priorities", "Direct communication" });
        }

        return requirements;
    }

    private List<string> GetCommunicationChallenges(SituationalContext context)
    {
        var challenges = new List<string>();

        switch (context.ContextType)
        {
            case ContextType.Technical:
                challenges.AddRange(new[] { "Over-technical language", "Assumption of knowledge", "Lack of practical examples" });
                break;

            case ContextType.Professional:
                challenges.AddRange(new[] { "Too formal or informal", "Missing business context", "Unclear priorities" });
                break;

            case ContextType.Personal:
                challenges.AddRange(new[] { "Boundary violations", "Inappropriate disclosure", "Misreading emotions" });
                break;

            case ContextType.Family:
                challenges.AddRange(new[] { "Work-life confusion", "Missing family dynamics", "Insufficient emotional sensitivity" });
                break;
        }

        return challenges;
    }

    private string DetermineRecommendedTone(SituationalContext context)
    {
        var baseTone = context.ContextType switch
        {
            ContextType.Technical => "Professional and precise",
            ContextType.Professional => "Business-focused and competent",
            ContextType.Personal => "Warm and understanding",
            ContextType.Family => "Caring and supportive",
            _ => "Balanced and appropriate"
        };

        if (context.UrgencyLevel > PersonalityConstants.HighUrgencyThreshold)
        {
            baseTone = $"Urgent and direct: {baseTone}";
        }

        return baseTone;
    }

    private List<string> GetStyleRecommendations(SituationalContext context)
    {
        var recommendations = new List<string>
        {
            "Match communication to context",
            "Consider audience needs",
            "Maintain appropriate boundaries"
        };

        switch (context.ContextType)
        {
            case ContextType.Technical:
                recommendations.AddRange(new[] { "Use structured explanations", "Provide concrete examples", "Verify understanding" });
                break;

            case ContextType.Professional:
                recommendations.AddRange(new[] { "Focus on business value", "Be action-oriented", "Show competence" });
                break;

            case ContextType.Personal:
                recommendations.AddRange(new[] { "Show empathy", "Respect boundaries", "Be authentic" });
                break;

            case ContextType.Family:
                recommendations.AddRange(new[] { "Express care", "Consider family dynamics", "Balance support with advice" });
                break;
        }

        return recommendations;
    }

    private List<string> GetPriorityCommunicationAspects(SituationalContext context)
    {
        return context.ContextType switch
        {
            ContextType.Technical => new List<string> { "Technical accuracy", "Clarity", "Practicality" },
            ContextType.Professional => new List<string> { "Business value", "Competence", "Results" },
            ContextType.Personal => new List<string> { "Empathy", "Authenticity", "Boundaries" },
            ContextType.Family => new List<string> { "Care", "Support", "Connection" },
            _ => new List<string> { "Appropriateness", "Clarity", "Helpfulness" }
        };
    }

    #endregion
}