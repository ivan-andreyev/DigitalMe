using DigitalMe.Data.Entities;
using DigitalMe.Services.Utils;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Strategies;

/// <summary>
/// Общая стратегия адаптации для персоналий, не имеющих специализированной стратегии.
/// Обеспечивает базовую функциональность адаптации.
/// </summary>
public class GenericPersonalityStrategy : IPersonalityAdapterStrategy
{
    private readonly ILogger<GenericPersonalityStrategy> _logger;
    private readonly IPersonalityConfigurationService _configurationService;

    public int Priority => 1; // Low priority - fallback strategy
    public string StrategyName => "Generic Personality Strategy";

    public GenericPersonalityStrategy(
        ILogger<GenericPersonalityStrategy> logger,
        IPersonalityConfigurationService configurationService)
    {
        _logger = logger;
        _configurationService = configurationService;
    }

    public bool CanHandle(PersonalityProfile personality)
    {
        // Generic strategy can handle any personality as a fallback
        _logger.LogDebug("Generic strategy can handle any personality as fallback: {PersonalityName}", personality.Name);
        return true;
    }

    public async Task<PersonalityProfile> AdaptToContextAsync(PersonalityProfile personality, SituationalContext context)
    {
        _logger.LogDebug("Adapting generic personality {PersonalityName} to context: {ContextType}",
            personality.Name, context.ContextType);

        // Clone personality for modification
        var adaptedPersonality = ClonePersonalityProfile(personality);

        // Apply basic contextual modifications
        ApplyBasicContextualModifications(adaptedPersonality, context);

        // Apply time-based modifications
        ApplyTimeBasedModifications(adaptedPersonality, context);

        _logger.LogDebug("Successfully adapted generic personality for {ContextType} context", context.ContextType);

        return adaptedPersonality;
    }

    public StressBehaviorModifications CalculateStressModifications(PersonalityProfile personality, double stressLevel, double timePressure)
    {
        _logger.LogDebug("Calculating generic stress modifications: stress={StressLevel}, timePressure={TimePressure}",
            stressLevel, timePressure);

        // Validate input parameters
        stressLevel = Math.Clamp(stressLevel, PersonalityConstants.MinimumStressLevel, PersonalityConstants.MaximumStressLevel);
        timePressure = Math.Clamp(timePressure, PersonalityConstants.MinimumStressLevel, PersonalityConstants.MaximumStressLevel);

        var modifications = new StressBehaviorModifications
        {
            // Generic stress behavior patterns
            DirectnessIncrease = stressLevel * PersonalityConstants.GenericStressDirectnessFactor,
            TechnicalDetailReduction = timePressure * PersonalityConstants.GenericTimePressureDetailReduction,
            WarmthReduction = stressLevel * PersonalityConstants.GenericStressWarmthReduction,

            // Basic stress responses
            StructuredThinkingBoost = stressLevel * 0.1,
            SolutionFocusBoost = timePressure * 0.15,
            SelfReflectionReduction = stressLevel * 0.15,

            // Confidence typically decreases under stress for generic personalities
            ConfidenceBoost = -stressLevel * 0.05, // Slight confidence reduction
            PragmatismIncrease = timePressure * 0.2,
            ResultsOrientationIncrease = timePressure * 0.15
        };

        _logger.LogDebug("Generic stress modifications calculated: directness={Directness}, warmth_reduction={WarmthReduction}",
            modifications.DirectnessIncrease, modifications.WarmthReduction);

        return modifications;
    }

    public ExpertiseConfidenceAdjustment CalculateExpertiseConfidence(PersonalityProfile personality, DomainType domainType, int taskComplexity)
    {
        _logger.LogDebug("Calculating generic expertise confidence for domain {Domain}, complexity {Complexity}",
            domainType, taskComplexity);

        taskComplexity = Math.Clamp(taskComplexity, PersonalityConstants.MinimumTaskComplexity, PersonalityConstants.MaximumTaskComplexity);

        var baseConfidence = PersonalityConstants.GenericBaseConfidence;
        var complexityFactor = 1.0 - (taskComplexity - 1) * PersonalityConstants.GenericComplexityReductionRate;

        var adjustment = new ExpertiseConfidenceAdjustment
        {
            Domain = domainType,
            TaskComplexity = taskComplexity,
            BaseConfidence = baseConfidence,
            ComplexityAdjustment = complexityFactor,
            AdjustedConfidence = Math.Clamp(baseConfidence * complexityFactor,
                PersonalityConstants.MinimumConfidenceLevel,
                PersonalityConstants.MaximumConfidenceLevel)
        };

        // Try to get personality-specific expertise if available in configuration
        if (_configurationService.IsPersonalitySupported(personality.Name))
        {
            var expertiseLevels = _configurationService.GetExpertiseLevels(personality.Name);
            if (expertiseLevels.TryGetValue(domainType, out var specificExpertise))
            {
                adjustment.BaseConfidence = specificExpertise;
                adjustment.AdjustedConfidence = Math.Clamp(specificExpertise * complexityFactor,
                    PersonalityConstants.MinimumConfidenceLevel,
                    PersonalityConstants.MaximumConfidenceLevel);

                _logger.LogDebug("Used personality-specific expertise for {PersonalityName} in {Domain}: {Expertise}",
                    personality.Name, domainType, specificExpertise);
            }
        }

        return adjustment;
    }

    public ContextualCommunicationStyle DetermineCommunicationStyle(PersonalityProfile personality, SituationalContext context)
    {
        _logger.LogDebug("Determining generic communication style for {ContextType} context", context.ContextType);

        var style = new ContextualCommunicationStyle
        {
            BasePersonality = "Balanced and professional approach",
            RecommendedTone = DetermineGenericTone(context),
            FormalityLevel = DetermineGenericFormality(context),
            TechnicalLanguageUsage = DetermineGenericTechnicalLanguage(context),
            EmotionalOpenness = DetermineGenericEmotionalOpenness(context),
            DirectnessLevel = DetermineGenericDirectness(context),
            EnergyLevel = DetermineGenericEnergyLevel(context)
        };

        // Apply context-specific adjustments
        ApplyGenericContextSpecificStyleModifications(style, context);

        _logger.LogDebug("Generic communication style determined: tone={Tone}, formality={Formality}",
            style.RecommendedTone, style.FormalityLevel);

        return style;
    }

    public ContextAnalysisResult AnalyzeContext(SituationalContext context)
    {
        _logger.LogDebug("Analyzing context requirements for generic personality: {ContextType}", context.ContextType);

        var result = new ContextAnalysisResult
        {
            ContextType = context.ContextType,
            RequiredAdaptations = DetermineGenericRequiredAdaptations(context),
            RecommendedApproach = DetermineGenericRecommendedApproach(context),
            KeyConsiderations = GetGenericKeyConsiderations(context),
            ExpectedChallenges = GetGenericExpectedChallenges(context),
            OptimalStrategy = DetermineGenericOptimalStrategy(context)
        };

        _logger.LogDebug("Generic context analysis completed: {AdaptationCount} adaptations required",
            result.RequiredAdaptations.Count);

        return result;
    }

    #region Private Helper Methods

    private PersonalityProfile ClonePersonalityProfile(PersonalityProfile original)
    {
        return PersonalityProfileCloner.CloneSimple(original);
    }

    private void ApplyBasicContextualModifications(PersonalityProfile personality, SituationalContext context)
    {
        // Basic trait boosts based on context
        switch (context.ContextType)
        {
            case ContextType.Technical:
                BoostTraitWeight(personality, "Technical", 1.2);
                BoostTraitWeight(personality, "Professional", 1.1);
                break;

            case ContextType.Personal:
                BoostTraitWeight(personality, "Personal", 1.2);
                BoostTraitWeight(personality, "Lifestyle", 1.1);
                break;

            case ContextType.Professional:
                BoostTraitWeight(personality, "Professional", 1.3);
                BoostTraitWeight(personality, "Position", 1.1);
                break;

            case ContextType.Family:
                BoostTraitWeight(personality, "Demographics", 1.2);
                BoostTraitWeight(personality, "Personal", 1.1);
                break;
        }

        // Environment-based modifications
        switch (context.Environment)
        {
            case EnvironmentType.Professional:
                BoostTraitWeight(personality, "Professional", 1.1);
                break;
            case EnvironmentType.Technical:
                BoostTraitWeight(personality, "Technical", 1.1);
                break;
        }
    }

    private void ApplyTimeBasedModifications(PersonalityProfile personality, SituationalContext context)
    {
        var hour = GetHourFromTimeOfDay(context.TimeOfDay);

        // Generic time-based energy adjustments
        if (hour >= PersonalityConstants.LateHoursThreshold || hour <= PersonalityConstants.MorningHoursThreshold)
        {
            ReduceTraitWeight(personality, "Energy", PersonalityConstants.LateHoursEnergyReduction);
        }
        else if (hour >= PersonalityConstants.MorningHoursThreshold && hour <= 10)
        {
            BoostTraitWeight(personality, "Energy", PersonalityConstants.MorningHoursEnergyBoost);
        }
    }

    private void BoostTraitWeight(PersonalityProfile personality, string traitName, double boostFactor)
    {
        var trait = personality.Traits?.FirstOrDefault(t => t.Name.Contains(traitName, StringComparison.OrdinalIgnoreCase) ||
                                                          t.Category.Contains(traitName, StringComparison.OrdinalIgnoreCase));
        if (trait != null)
        {
            trait.Weight = Math.Clamp(trait.Weight * boostFactor, 0.0, 2.0); // Allow up to 2x boost
        }
    }

    private void ReduceTraitWeight(PersonalityProfile personality, string traitName, double reductionFactor)
    {
        var trait = personality.Traits?.FirstOrDefault(t => t.Name.Contains(traitName, StringComparison.OrdinalIgnoreCase));
        if (trait != null)
        {
            trait.Weight = Math.Clamp(trait.Weight * reductionFactor, 0.1, 1.0);
        }
    }

    private string DetermineGenericTone(SituationalContext context)
    {
        return context.ContextType switch
        {
            ContextType.Technical => "Professional and informative",
            ContextType.Personal => "Warm and understanding",
            ContextType.Professional => "Confident and business-like",
            ContextType.Family => "Caring and supportive",
            _ => "Balanced and professional"
        };
    }

    private double DetermineGenericFormality(SituationalContext context)
    {
        var baseFormality = context.ContextType switch
        {
            ContextType.Technical => 0.6,
            ContextType.Personal => 0.3,
            ContextType.Professional => 0.7,
            ContextType.Family => 0.2,
            _ => 0.5
        };

        // Increase formality with urgency
        if (context.UrgencyLevel > PersonalityConstants.HighUrgencyThreshold)
        {
            baseFormality += 0.1;
        }

        return Math.Clamp(baseFormality, 0.0, 1.0);
    }

    private double DetermineGenericTechnicalLanguage(SituationalContext context)
    {
        return context.ContextType switch
        {
            ContextType.Technical => 0.8,
            ContextType.Professional => 0.5,
            ContextType.Personal => 0.2,
            ContextType.Family => 0.1,
            _ => 0.4
        };
    }

    private double DetermineGenericEmotionalOpenness(SituationalContext context)
    {
        return context.ContextType switch
        {
            ContextType.Personal => 0.7,
            ContextType.Family => 0.8,
            ContextType.Professional => 0.3,
            ContextType.Technical => 0.2,
            _ => 0.5
        };
    }

    private double DetermineGenericDirectness(SituationalContext context)
    {
        var baseDirectness = 0.6; // Generic baseline

        // Increase directness under urgency
        if (context.UrgencyLevel > PersonalityConstants.HighUrgencyThreshold)
        {
            baseDirectness += 0.2;
        }

        return Math.Clamp(baseDirectness, 0.0, 1.0);
    }

    private double DetermineGenericEnergyLevel(SituationalContext context)
    {
        var baseEnergy = 0.6; // Generic baseline
        var hour = GetHourFromTimeOfDay(context.TimeOfDay);

        // Apply time-based adjustments
        if (hour >= PersonalityConstants.LateHoursThreshold)
        {
            baseEnergy *= PersonalityConstants.LateHoursEnergyReduction;
        }
        else if (hour >= PersonalityConstants.MorningHoursThreshold && hour <= 10)
        {
            baseEnergy *= PersonalityConstants.MorningHoursEnergyBoost;
        }

        return Math.Clamp(baseEnergy, 0.0, 1.0);
    }

    private void ApplyGenericContextSpecificStyleModifications(ContextualCommunicationStyle style, SituationalContext context)
    {
        // Generic context-specific adjustments
        switch (context.ContextType)
        {
            case ContextType.Technical:
                style.ExplanationDepth = 0.7;
                style.ExampleUsage = 0.6;
                break;

            case ContextType.Personal:
                style.SelfDisclosureLevel = 0.5;
                style.VulnerabilityLevel = 0.4;
                break;

            case ContextType.Professional:
                style.LeadershipAssertiveness = 0.6;
                style.ResultsOrientation = 0.7;
                break;

            case ContextType.Family:
                style.WarmthLevel = 0.8;
                style.ProtectiveInstinct = 0.6;
                break;
        }
    }

    private List<string> DetermineGenericRequiredAdaptations(SituationalContext context)
    {
        var adaptations = new List<string>();

        switch (context.ContextType)
        {
            case ContextType.Technical:
                adaptations.Add("Increase technical language appropriately");
                adaptations.Add("Focus on logical problem-solving");
                break;

            case ContextType.Personal:
                adaptations.Add("Show appropriate emotional awareness");
                adaptations.Add("Demonstrate empathy and understanding");
                break;

            case ContextType.Professional:
                adaptations.Add("Maintain professional demeanor");
                adaptations.Add("Show competence and reliability");
                break;

            case ContextType.Family:
                adaptations.Add("Express care and concern");
                adaptations.Add("Show understanding of family dynamics");
                break;
        }

        if (context.UrgencyLevel > PersonalityConstants.HighUrgencyThreshold)
        {
            adaptations.Add("Increase responsiveness and decisiveness");
        }

        return adaptations;
    }

    private string DetermineGenericRecommendedApproach(SituationalContext context)
    {
        return context.ContextType switch
        {
            ContextType.Technical => "Clear, logical approach with appropriate technical detail",
            ContextType.Personal => "Empathetic and understanding with genuine care",
            ContextType.Professional => "Competent and reliable with appropriate formality",
            ContextType.Family => "Warm and supportive with family-appropriate language",
            _ => "Balanced professional approach with context sensitivity"
        };
    }

    private List<string> GetGenericKeyConsiderations(SituationalContext context)
    {
        return new List<string>
        {
            "Maintain appropriate context sensitivity",
            "Balance professionalism with warmth",
            "Adapt communication style to audience",
            "Show competence without arrogance",
            "Demonstrate genuine interest and care"
        };
    }

    private List<string> GetGenericExpectedChallenges(SituationalContext context)
    {
        var challenges = new List<string>
        {
            "May lack specific domain expertise",
            "Could miss nuanced context requirements",
            "Generic responses might feel impersonal"
        };

        switch (context.ContextType)
        {
            case ContextType.Technical:
                challenges.Add("May not have deep technical knowledge");
                break;

            case ContextType.Personal:
                challenges.Add("May not connect on personal level");
                break;

            case ContextType.Professional:
                challenges.Add("May lack specific industry knowledge");
                break;
        }

        return challenges;
    }

    private string DetermineGenericOptimalStrategy(SituationalContext context)
    {
        return context.ContextType switch
        {
            ContextType.Technical => "Focus on clear communication and logical thinking",
            ContextType.Personal => "Show empathy and genuine care for concerns",
            ContextType.Professional => "Demonstrate competence and reliability",
            ContextType.Family => "Express warmth and understanding",
            _ => "Balanced approach with appropriate context adaptation"
        };
    }

    private int GetHourFromTimeOfDay(TimeOfDay timeOfDay)
    {
        return timeOfDay switch
        {
            TimeOfDay.Morning => 9,
            TimeOfDay.Afternoon => 14,
            TimeOfDay.Evening => 18,
            TimeOfDay.Late => 22,
            _ => 12
        };
    }

    #endregion
}