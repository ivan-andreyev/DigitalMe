using DigitalMe.Data.Entities;
using DigitalMe.Services.Utils;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Strategies;

/// <summary>
/// Стратегия адаптации для персоналии Ивана.
/// Содержит всю Ivan-specific логику поведенческих модификаций.
/// </summary>
public class IvanPersonalityStrategy : IPersonalityAdapterStrategy
{
    private readonly ILogger<IvanPersonalityStrategy> _logger;
    private readonly IPersonalityConfigurationService _configurationService;

    public int Priority => 100; // High priority for Ivan-specific strategy
    public string StrategyName => "Ivan Personality Strategy";

    public IvanPersonalityStrategy(
        ILogger<IvanPersonalityStrategy> logger,
        IPersonalityConfigurationService configurationService)
    {
        _logger = logger;
        _configurationService = configurationService;
    }

    public bool CanHandle(PersonalityProfile personality)
    {
        var canHandle = personality.Name.Contains("Ivan", StringComparison.OrdinalIgnoreCase) ||
                       personality.Description.Contains("Ivan", StringComparison.OrdinalIgnoreCase);

        _logger.LogDebug("Ivan strategy can handle personality {PersonalityName}: {CanHandle}",
            personality.Name, canHandle);

        return canHandle;
    }

    public async Task<PersonalityProfile> AdaptToContextAsync(PersonalityProfile personality, SituationalContext context)
    {
        _logger.LogDebug("Adapting Ivan personality to context: {ContextType}, urgency: {Urgency}",
            context.ContextType, context.UrgencyLevel);

        // Clone personality for modification
        var adaptedPersonality = ClonePersonalityProfile(personality);

        // Apply general contextual modifications
        ApplyContextualTraitModifications(adaptedPersonality, context);

        // Apply Ivan-specific contextual modifications
        ApplyIvanSpecificContextualModifications(adaptedPersonality, context);

        // Apply time-based modifications
        ApplyTimeBasedModifications(adaptedPersonality, context);

        _logger.LogDebug("Successfully adapted Ivan personality for {ContextType} context", context.ContextType);

        return adaptedPersonality;
    }

    public StressBehaviorModifications CalculateStressModifications(PersonalityProfile personality, double stressLevel, double timePressure)
    {
        _logger.LogDebug("Calculating Ivan stress modifications: stress={StressLevel}, timePressure={TimePressure}",
            stressLevel, timePressure);

        // Validate input parameters
        stressLevel = Math.Clamp(stressLevel, PersonalityConstants.MinimumStressLevel, PersonalityConstants.MaximumStressLevel);
        timePressure = Math.Clamp(timePressure, PersonalityConstants.MinimumStressLevel, PersonalityConstants.MaximumStressLevel);

        var stressRules = _configurationService.GetStressModificationRules("Ivan");

        var modifications = new StressBehaviorModifications
        {
            // Ivan gets more direct and structured under stress
            DirectnessIncrease = stressLevel * stressRules.DirectnessFactor,
            StructuredThinkingBoost = stressLevel * stressRules.StructuredThinkingBoost,
            TechnicalDetailReduction = timePressure * stressRules.TechnicalDetailReduction,
            WarmthReduction = (stressLevel + timePressure) * stressRules.WarmthReduction,

            // Ivan specific: becomes more solution-focused under pressure
            SolutionFocusBoost = timePressure * stressRules.SolutionFocusBoost,
            SelfReflectionReduction = stressLevel * stressRules.SelfReflectionReduction,

            // Additional Ivan patterns
            PragmatismIncrease = timePressure * 0.25, // More pragmatic when time-pressed
            ConfidenceBoost = stressLevel * 0.1, // Slightly more confident under stress (leadership mode)
            ResultsOrientationIncrease = (stressLevel + timePressure) * 0.2 // More results-focused
        };

        _logger.LogDebug("Ivan stress modifications calculated: directness={Directness}, structure={Structure}",
            modifications.DirectnessIncrease, modifications.StructuredThinkingBoost);

        return modifications;
    }

    public ExpertiseConfidenceAdjustment CalculateExpertiseConfidence(PersonalityProfile personality, DomainType domainType, int taskComplexity)
    {
        _logger.LogDebug("Calculating Ivan expertise confidence for domain {Domain}, complexity {Complexity}",
            domainType, taskComplexity);

        taskComplexity = Math.Clamp(taskComplexity, PersonalityConstants.MinimumTaskComplexity, PersonalityConstants.MaximumTaskComplexity);

        var expertiseLevels = _configurationService.GetExpertiseLevels("Ivan");
        var behaviorRules = _configurationService.GetBehaviorRules("Ivan");

        var expertiseLevel = expertiseLevels.GetValueOrDefault(domainType, PersonalityConstants.GenericBaseConfidence);
        var complexityFactor = 1.0 - (taskComplexity - 1) * PersonalityConstants.ComplexityConfidenceReductionRate;

        var adjustment = new ExpertiseConfidenceAdjustment
        {
            Domain = domainType,
            TaskComplexity = taskComplexity,
            BaseConfidence = expertiseLevel,
            ComplexityAdjustment = complexityFactor,
            AdjustedConfidence = Math.Clamp(expertiseLevel * complexityFactor,
                PersonalityConstants.MinimumConfidenceLevel,
                PersonalityConstants.MaximumConfidenceLevel)
        };

        // Ivan specific: Extra confidence boost in his core domains
        if (IsIvanCoreDomain(domainType))
        {
            adjustment.DomainExpertiseBonus = behaviorRules.CoreDomainConfidenceBonus;
            adjustment.AdjustedConfidence = Math.Clamp(
                adjustment.AdjustedConfidence + adjustment.DomainExpertiseBonus,
                PersonalityConstants.MinimumConfidenceLevel,
                PersonalityConstants.MaximumConfidenceLevel);

            _logger.LogDebug("Applied Ivan core domain bonus for {Domain}: {Bonus}",
                domainType, adjustment.DomainExpertiseBonus);
        }

        // Ivan specific: Known weakness adjustment
        if (IsIvanWeaknessDomain(domainType))
        {
            adjustment.KnownWeaknessReduction = behaviorRules.WeaknessReduction;
            adjustment.AdjustedConfidence = Math.Clamp(
                adjustment.AdjustedConfidence - adjustment.KnownWeaknessReduction,
                PersonalityConstants.MinimumConfidenceLevel,
                PersonalityConstants.WeaknessDomainConfidenceCap);

            _logger.LogDebug("Applied Ivan weakness reduction for {Domain}: {Reduction}",
                domainType, adjustment.KnownWeaknessReduction);
        }

        return adjustment;
    }

    public ContextualCommunicationStyle DetermineCommunicationStyle(PersonalityProfile personality, SituationalContext context)
    {
        _logger.LogDebug("Determining Ivan communication style for {ContextType} context", context.ContextType);

        var communicationRules = _configurationService.GetCommunicationStyleRules("Ivan");
        var style = new ContextualCommunicationStyle
        {
            BasePersonality = "Direct, structured, and pragmatic with occasional self-awareness about work-life balance",
            RecommendedTone = DetermineIvanTone(context, communicationRules),
            FormalityLevel = DetermineIvanFormality(context, communicationRules),
            TechnicalLanguageUsage = DetermineIvanTechnicalLanguage(context),
            EmotionalOpenness = DetermineIvanEmotionalOpenness(context),
            DirectnessLevel = DetermineIvanDirectness(context, communicationRules),
            EnergyLevel = DetermineIvanEnergyLevel(context, communicationRules)
        };

        // Context-specific adjustments
        ApplyIvanContextSpecificStyleModifications(style, context);

        _logger.LogDebug("Ivan communication style determined: tone={Tone}, formality={Formality}",
            style.RecommendedTone, style.FormalityLevel);

        return style;
    }

    public ContextAnalysisResult AnalyzeContext(SituationalContext context)
    {
        _logger.LogDebug("Analyzing context requirements for Ivan: {ContextType}", context.ContextType);

        var result = new ContextAnalysisResult
        {
            ContextType = context.ContextType,
            RequiredAdaptations = DetermineRequiredAdaptations(context),
            RecommendedApproach = DetermineRecommendedApproach(context),
            KeyConsiderations = GetIvanKeyConsiderations(context),
            ExpectedChallenges = GetIvanExpectedChallenges(context),
            OptimalStrategy = DetermineOptimalStrategy(context)
        };

        _logger.LogDebug("Context analysis completed for Ivan: {AdaptationCount} adaptations required",
            result.RequiredAdaptations.Count);

        return result;
    }

    #region Private Helper Methods

    private PersonalityProfile ClonePersonalityProfile(PersonalityProfile original)
    {
        return PersonalityProfileCloner.CloneSimple(original);
    }

    private void ApplyContextualTraitModifications(PersonalityProfile personality, SituationalContext context)
    {
        var behaviorRules = _configurationService.GetBehaviorRules("Ivan");

        switch (context.ContextType)
        {
            case ContextType.Technical:
                BoostTraitWeight(personality, "Technical", behaviorRules.TechnicalContextBoost);
                BoostTraitWeight(personality, "Tech Preferences", behaviorRules.TechnicalContextBoost * PersonalityConstants.IvanTechnicalTraitBoost);
                break;

            case ContextType.Personal:
                BoostTraitWeight(personality, "Personal", behaviorRules.PersonalContextBoost);
                BoostTraitWeight(personality, "Lifestyle", behaviorRules.PersonalContextBoost * PersonalityConstants.IvanPersonalTraitBoost);
                break;

            case ContextType.Professional:
                BoostTraitWeight(personality, "Professional", behaviorRules.ProfessionalContextBoost);
                BoostTraitWeight(personality, "Position", behaviorRules.ProfessionalContextBoost * PersonalityConstants.IvanProfessionalTraitBoost);
                break;

            case ContextType.Family:
                BoostTraitWeight(personality, "Family", behaviorRules.FamilyContextBoost);
                BoostTraitWeight(personality, "Demographics", behaviorRules.FamilyContextBoost * PersonalityConstants.IvanFamilyTraitBoost);
                break;
        }
    }

    private void ApplyIvanSpecificContextualModifications(PersonalityProfile personality, SituationalContext context)
    {
        // High stress about work-life balance when discussing family or personal time
        if (context.ContextType == ContextType.Personal &&
            (context.Topic.Contains("family", StringComparison.OrdinalIgnoreCase) ||
             context.Topic.Contains("time", StringComparison.OrdinalIgnoreCase)))
        {
            BoostTraitWeight(personality, "Current Challenges", PersonalityConstants.IvanFamilyChallengeBoost);
            BoostTraitWeight(personality, "Life Priorities", PersonalityConstants.IvanLifePrioritiesBoost);
        }

        // Extra confidence in technical leadership contexts
        if (context.ContextType == ContextType.Technical && context.Environment == EnvironmentType.Professional)
        {
            BoostTraitWeight(personality, "Self-Assessment", PersonalityConstants.IvanTechnicalLeadershipSelfAssessmentBoost);
            BoostTraitWeight(personality, "Position", PersonalityConstants.IvanTechnicalProfessionalPositionBoost);
        }

        // Strategic thinking for R&D and business contexts
        if (context.Topic.Contains("strategy", StringComparison.OrdinalIgnoreCase) ||
            context.Topic.Contains("R&D", StringComparison.OrdinalIgnoreCase))
        {
            BoostTraitWeight(personality, "Decision Making", PersonalityConstants.IvanStrategicDecisionMakingBoost);
            BoostTraitWeight(personality, "Goals", PersonalityConstants.IvanStrategicGoalsBoost);
        }
    }

    private void ApplyTimeBasedModifications(PersonalityProfile personality, SituationalContext context)
    {
        var hour = GetHourFromTimeOfDay(context.TimeOfDay);

        if (hour >= PersonalityConstants.LateHoursThreshold || hour <= PersonalityConstants.MorningHoursThreshold)
        {
            // Less energetic during late hours
            ReduceTraitWeight(personality, "Energy", PersonalityConstants.LateHoursEnergyReduction);
        }
        else if (hour >= PersonalityConstants.MorningHoursThreshold && hour <= 10)
        {
            // More energetic during morning hours
            BoostTraitWeight(personality, "Energy", PersonalityConstants.MorningHoursEnergyBoost);
        }
    }

    private void BoostTraitWeight(PersonalityProfile personality, string traitName, double boostFactor)
    {
        var trait = personality.Traits?.FirstOrDefault(t => t.Name.Contains(traitName, StringComparison.OrdinalIgnoreCase) ||
                                                          t.Category.Contains(traitName, StringComparison.OrdinalIgnoreCase));
        if (trait != null)
        {
            trait.Weight = Math.Clamp(trait.Weight * boostFactor, 0.0, PersonalityConstants.MaxTraitWeightBoost); // Allow up to 2x boost
        }
    }

    private void ReduceTraitWeight(PersonalityProfile personality, string traitName, double reductionFactor)
    {
        var trait = personality.Traits?.FirstOrDefault(t => t.Name.Contains(traitName, StringComparison.OrdinalIgnoreCase));
        if (trait != null)
        {
            trait.Weight = Math.Clamp(trait.Weight * reductionFactor, PersonalityConstants.MinTraitWeight, PersonalityConstants.MaxTraitWeight);
        }
    }

    private bool IsIvanCoreDomain(DomainType domainType)
    {
        return domainType == DomainType.CSharpDotNet || domainType == DomainType.GameDevelopment || domainType == DomainType.SoftwareArchitecture;
    }

    private bool IsIvanWeaknessDomain(DomainType domainType)
    {
        return domainType == DomainType.WorkLifeBalance || domainType == DomainType.PersonalRelations;
    }

    private string DetermineIvanTone(SituationalContext context, CommunicationStyleRules rules)
    {
        return context.ContextType switch
        {
            ContextType.Technical => "Confident and detailed",
            ContextType.Personal => "Reflective and honest",
            ContextType.Professional => "Assertive and results-focused",
            ContextType.Family => "Caring but concerned about balance",
            _ => "Professional but friendly"
        };
    }

    private double DetermineIvanFormality(SituationalContext context, CommunicationStyleRules rules)
    {
        var baseFormality = context.ContextType switch
        {
            ContextType.Technical => PersonalityConstants.TechnicalContextFormality,
            ContextType.Personal => PersonalityConstants.PersonalContextFormality,
            ContextType.Professional => PersonalityConstants.ProfessionalContextFormality,
            ContextType.Family => PersonalityConstants.FamilyContextFormality,
            _ => PersonalityConstants.DefaultContextFormality
        };

        // Increase formality with urgency and complexity
        if (context.UrgencyLevel > rules.HighUrgencyThreshold)
        {
            baseFormality += PersonalityConstants.UrgencyFormalityBoost;
        }

        return Math.Clamp(baseFormality, 0.0, 1.0);
    }

    private double DetermineIvanTechnicalLanguage(SituationalContext context)
    {
        return context.ContextType switch
        {
            ContextType.Technical => 0.95,
            ContextType.Professional => 0.70,
            ContextType.Personal => 0.30,
            ContextType.Family => 0.20,
            _ => 0.50
        };
    }

    private double DetermineIvanEmotionalOpenness(SituationalContext context)
    {
        return context.ContextType switch
        {
            ContextType.Personal => 0.65,
            ContextType.Family => 0.90,
            ContextType.Professional => 0.40,
            ContextType.Technical => 0.30,
            _ => 0.50
        };
    }

    private double DetermineIvanDirectness(SituationalContext context, CommunicationStyleRules rules)
    {
        var baseDirectness = 0.80; // Ivan is generally direct

        // Increase directness under stress or urgency
        if (context.UrgencyLevel > rules.HighUrgencyThreshold)
        {
            baseDirectness += 0.15;
        }

        return Math.Clamp(baseDirectness, 0.0, 1.0);
    }

    private double DetermineIvanEnergyLevel(SituationalContext context, CommunicationStyleRules rules)
    {
        var baseEnergy = 0.75; // Ivan generally has good energy
        var hour = GetHourFromTimeOfDay(context.TimeOfDay);

        // Apply time-based adjustments
        if (hour >= PersonalityConstants.LateHoursThreshold)
        {
            baseEnergy *= rules.EnergyReductionLateHours;
        }
        else if (hour >= PersonalityConstants.MorningHoursThreshold && hour <= 10)
        {
            baseEnergy *= rules.EnergyBoostMorningHours;
        }

        return Math.Clamp(baseEnergy, 0.0, 1.0);
    }

    private void ApplyIvanContextSpecificStyleModifications(ContextualCommunicationStyle style, SituationalContext context)
    {
        // Ivan becomes more detailed when explaining technical concepts
        if (context.ContextType == ContextType.Technical)
        {
            style.TechnicalDepth = 0.85;
            style.ExampleUsage = 0.80;
        }

        // Ivan shows vulnerability when discussing personal challenges
        if (context.ContextType == ContextType.Personal || context.ContextType == ContextType.Family)
        {
            style.VulnerabilityLevel = 0.70;
            style.EmotionalOpenness = 0.55;
        }

        // Ivan shows leadership assertiveness in professional contexts
        if (context.ContextType == ContextType.Professional)
        {
            style.LeadershipTone = 0.75;
            style.ResultsFocus = 0.90;
        }
    }

    private List<string> DetermineRequiredAdaptations(SituationalContext context)
    {
        var adaptations = new List<string>();

        switch (context.ContextType)
        {
            case ContextType.Technical:
                adaptations.Add("Boost technical expertise confidence");
                adaptations.Add("Increase technical language usage");
                adaptations.Add("Emphasize structured problem-solving");
                break;

            case ContextType.Personal:
                adaptations.Add("Increase emotional openness");
                adaptations.Add("Show work-life balance awareness");
                adaptations.Add("Demonstrate self-reflection");
                break;

            case ContextType.Professional:
                adaptations.Add("Emphasize leadership experience");
                adaptations.Add("Show results-oriented thinking");
                adaptations.Add("Display confident decision-making");
                break;

            case ContextType.Family:
                adaptations.Add("Show protective instincts");
                adaptations.Add("Acknowledge time constraints");
                adaptations.Add("Express care and concern");
                break;
        }

        if (context.UrgencyLevel > PersonalityConstants.HighUrgencyThreshold)
        {
            adaptations.Add("Increase directness and decisiveness");
            adaptations.Add("Focus on immediate solutions");
        }

        return adaptations;
    }

    private string DetermineRecommendedApproach(SituationalContext context)
    {
        return context.ContextType switch
        {
            ContextType.Technical => "Structured analysis with detailed explanations and code examples",
            ContextType.Personal => "Honest reflection with acknowledgment of challenges and growth",
            ContextType.Professional => "Results-focused leadership with clear decision rationale",
            ContextType.Family => "Caring concern balanced with realistic constraints",
            _ => "Balanced professional approach with appropriate adaptation"
        };
    }

    private List<string> GetIvanKeyConsiderations(SituationalContext context)
    {
        var considerations = new List<string>
        {
            "Ivan's strong technical background and confidence",
            "His acknowledged work-life balance challenges",
            "Results-oriented and structured thinking patterns"
        };

        switch (context.ContextType)
        {
            case ContextType.Technical:
                considerations.Add("Deep C#/.NET and game development expertise");
                considerations.Add("Preference for code generation over graphical tools");
                break;

            case ContextType.Personal:
                considerations.Add("Tension between career ambitions and family time");
                considerations.Add("Self-awareness about spending limited time with family");
                break;

            case ContextType.Professional:
                considerations.Add("Rapid career progression (Junior to Head of R&D in 4 years)");
                considerations.Add("Strong self-assessment: 'best employee in the world'");
                break;

            case ContextType.Family:
                considerations.Add("Deep love for family despite time constraints");
                considerations.Add("Concern about daughter's opportunities and safety");
                break;
        }

        return considerations;
    }

    private List<string> GetIvanExpectedChallenges(SituationalContext context)
    {
        var challenges = new List<string>();

        switch (context.ContextType)
        {
            case ContextType.Personal:
                challenges.Add("May become defensive about work-life balance criticism");
                challenges.Add("Could show vulnerability about family time limitations");
                break;

            case ContextType.Professional:
                challenges.Add("High confidence might come across as arrogant");
                challenges.Add("May over-emphasize technical solutions");
                break;

            case ContextType.Family:
                challenges.Add("Guilt about limited family time may affect responses");
                challenges.Add("Protective instincts may override practical considerations");
                break;

            case ContextType.Technical:
                challenges.Add("May provide overly detailed technical explanations");
                challenges.Add("Could dismiss non-technical approaches too quickly");
                break;
        }

        return challenges;
    }

    private string DetermineOptimalStrategy(SituationalContext context)
    {
        return context.ContextType switch
        {
            ContextType.Technical => "Lead with expertise, provide concrete examples, show structured thinking",
            ContextType.Personal => "Show authentic vulnerability, acknowledge challenges, demonstrate growth mindset",
            ContextType.Professional => "Display confident leadership, focus on results, show decision-making process",
            ContextType.Family => "Express genuine care, acknowledge constraints, show future-oriented thinking",
            _ => "Balanced approach with context-appropriate adaptations"
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