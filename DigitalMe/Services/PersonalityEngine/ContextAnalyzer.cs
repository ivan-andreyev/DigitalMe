using DigitalMe.Data.Entities;
using DigitalMe.Services.Strategies;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.PersonalityEngine;

/// <summary>
/// Реализация анализатора контекста.
/// Предоставляет глубокий анализ ситуационных требований.
/// </summary>
public class ContextAnalyzer : IContextAnalyzer
{
    private readonly ILogger<ContextAnalyzer> _logger;
    private readonly IPersonalityStrategyFactory _strategyFactory;
    private readonly IPersonalityConfigurationService _configurationService;

    /// <summary>
    /// Инициализирует новый экземпляр анализатора контекста.
    /// </summary>
    /// <param name="logger">Логгер для записи диагностической информации</param>
    /// <param name="strategyFactory">Фабрика стратегий для получения подходящих стратегий анализа</param>
    /// <param name="configurationService">Сервис конфигурации персоналий</param>
    public ContextAnalyzer(
        ILogger<ContextAnalyzer> logger,
        IPersonalityStrategyFactory strategyFactory,
        IPersonalityConfigurationService configurationService)
    {
        _logger = logger;
        _strategyFactory = strategyFactory;
        _configurationService = configurationService;
    }

    public ContextAnalysisResult AnalyzeContextRequirements(SituationalContext context)
    {
        _logger.LogDebug("Analyzing context requirements: {ContextType}, urgency: {Urgency}, environment: {Environment}",
            context.ContextType, context.UrgencyLevel, context.Environment);

        var result = new ContextAnalysisResult
        {
            ContextType = context.ContextType,
            ComplexityLevel = DetermineContextComplexity(context),
            RequiredAdaptations = DetermineRequiredAdaptations(context),
            RecommendedApproach = DetermineRecommendedApproach(context),
            KeyConsiderations = GetKeyConsiderations(context),
            ExpectedChallenges = PredictPotentialChallenges(context),
            OptimalStrategy = DetermineOptimalStrategy(context),
            TemporalAnalysis = ConvertToTemporalAnalysisData(AnalyzeTemporalAspects(context)),
            KeyFactors = IdentifyKeyFactors(context).Select(f => f.Name).ToList(),
            SuccessMetrics = DefineSuccessMetrics(context)
        };

        _logger.LogDebug("Context analysis completed: complexity={Complexity}, {AdaptationCount} adaptations, {ChallengeCount} potential challenges",
            result.ComplexityLevel, result.RequiredAdaptations.Count, result.ExpectedChallenges.Count);

        return result;
    }

    public int DetermineContextComplexity(SituationalContext context)
    {
        var baseComplexity = context.ContextType switch
        {
            ContextType.Technical => 6,
            ContextType.Professional => 5,
            ContextType.Personal => 4,
            ContextType.Family => 3,
            _ => 4
        };

        // Adjust for urgency
        if (context.UrgencyLevel > PersonalityConstants.HighUrgencyThreshold)
        {
            baseComplexity += 2;
        }
        else if (context.UrgencyLevel < 0.3)
        {
            baseComplexity -= 1;
        }

        // Adjust for environment
        if (context.Environment == EnvironmentType.Professional)
        {
            baseComplexity += 1;
        }

        // Adjust for topic complexity (simple heuristic based on topic content)
        if (!string.IsNullOrEmpty(context.Topic))
        {
            var topicWords = context.Topic.Split(' ');
            if (topicWords.Length > 10) // Complex topic
            {
                baseComplexity += 1;
            }

            // Technical terms indicate higher complexity
            var technicalTerms = new[] { "architecture", "algorithm", "framework", "implementation", "refactoring", "optimization" };
            if (technicalTerms.Any(term => context.Topic.Contains(term, StringComparison.OrdinalIgnoreCase)))
            {
                baseComplexity += 1;
            }
        }

        var complexity = Math.Clamp(baseComplexity, PersonalityConstants.MinimumTaskComplexity, PersonalityConstants.MaximumTaskComplexity);

        _logger.LogTrace("Context complexity determined: {Complexity} (base: {BaseComplexity})", complexity, baseComplexity);

        return complexity;
    }

    public TemporalContextAnalysis AnalyzeTemporalAspects(SituationalContext context)
    {
        _logger.LogDebug("Analyzing temporal aspects of context at {TimeOfDay}", context.TimeOfDay);

        var hour = GetHourFromTimeOfDay(context.TimeOfDay);
        var dayOfWeek = DateTime.Now.DayOfWeek; // TimeOfDay enum doesn't have DayOfWeek

        var analysis = new TemporalContextAnalysis
        {
            TimeOfDay = DateTime.Now, // Используем текущее время, так как у нас только enum TimeOfDay
            TimeCategory = DetermineTimeCategory(hour),
            WorkingHours = IsWorkingHours(hour, dayOfWeek),
            EnergyLevel = DetermineExpectedEnergyLevel(hour),
            AttentionLevel = DetermineExpectedAttentionLevel(hour, dayOfWeek),
            TemporalAdaptations = DetermineTemporalAdaptations(hour, dayOfWeek),
            TimeBasedChallenges = DetermineTimeBasedChallenges(hour, dayOfWeek),
            RecommendedPacing = DetermineRecommendedPacing(context.UrgencyLevel, hour)
        };

        _logger.LogDebug("Temporal analysis completed: category={TimeCategory}, energy={EnergyLevel}, attention={AttentionLevel}",
            analysis.TimeCategory, analysis.EnergyLevel, analysis.AttentionLevel);

        return analysis;
    }

    public List<ContextFactor> IdentifyKeyFactors(SituationalContext context)
    {
        _logger.LogDebug("Identifying key context factors for {ContextType}", context.ContextType);

        var factors = new List<ContextFactor>();

        // Context type factor
        factors.Add(new ContextFactor
        {
            Name = "Context Type",
            Value = context.ContextType.ToString(),
            Impact = FactorImpact.High,
            Description = $"Primary interaction context: {context.ContextType}"
        });

        // Urgency factor
        var urgencyImpact = context.UrgencyLevel > PersonalityConstants.HighUrgencyThreshold ? FactorImpact.High :
                           context.UrgencyLevel > 0.4 ? FactorImpact.Medium : FactorImpact.Low;

        factors.Add(new ContextFactor
        {
            Name = "Urgency Level",
            Value = context.UrgencyLevel.ToString("F2"),
            Impact = urgencyImpact,
            Description = $"Time sensitivity: {GetUrgencyDescription(context.UrgencyLevel)}"
        });

        // Environment factor
        if (context.Environment != EnvironmentType.Unknown)
        {
            factors.Add(new ContextFactor
            {
                Name = "Environment",
                Value = context.Environment.ToString(),
                Impact = FactorImpact.Medium,
                Description = $"Physical/virtual environment: {context.Environment}"
            });
        }

        // Topic complexity factor
        if (!string.IsNullOrEmpty(context.Topic))
        {
            var topicComplexity = DetermineTopicComplexity(context.Topic);
            factors.Add(new ContextFactor
            {
                Name = "Topic Complexity",
                Value = topicComplexity.ToString(),
                Impact = topicComplexity > 6 ? FactorImpact.High : FactorImpact.Medium,
                Description = $"Subject matter complexity: {GetComplexityDescription(topicComplexity)}"
            });
        }

        // Temporal factor
        var timeCategory = DetermineTimeCategory(GetHourFromTimeOfDay(context.TimeOfDay));
        var temporalImpact = timeCategory == "Late Hours" || timeCategory == "Early Morning" ? FactorImpact.Medium : FactorImpact.Low;

        factors.Add(new ContextFactor
        {
            Name = "Time of Day",
            Value = timeCategory,
            Impact = temporalImpact,
            Description = $"Temporal context: {timeCategory} ({GetHourFromTimeOfDay(context.TimeOfDay):00}:00)"
        });

        _logger.LogDebug("Identified {FactorCount} key context factors", factors.Count);

        return factors;
    }

    public List<string> PredictPotentialChallenges(SituationalContext context)
    {
        _logger.LogDebug("Predicting potential challenges for {ContextType} context", context.ContextType);

        var challenges = new List<string>();

        // Context-specific challenges
        switch (context.ContextType)
        {
            case ContextType.Technical:
                challenges.AddRange(new[]
                {
                    "Over-technical explanations for non-technical audience",
                    "Assumption of technical knowledge level",
                    "Lack of practical examples or analogies"
                });
                break;

            case ContextType.Personal:
                challenges.AddRange(new[]
                {
                    "Inappropriate level of personal disclosure",
                    "Misreading emotional needs",
                    "Offering unsolicited advice"
                });
                break;

            case ContextType.Professional:
                challenges.AddRange(new[]
                {
                    "Too formal or too casual tone",
                    "Missing business context or priorities",
                    "Inadequate focus on actionable outcomes"
                });
                break;

            case ContextType.Family:
                challenges.AddRange(new[]
                {
                    "Work-life boundary confusion",
                    "Misunderstanding family dynamics",
                    "Inadequate emotional sensitivity"
                });
                break;
        }

        // Urgency-related challenges
        if (context.UrgencyLevel > PersonalityConstants.HighUrgencyThreshold)
        {
            challenges.AddRange(new[]
            {
                "Sacrificing quality for speed",
                "Missing important nuances due to time pressure",
                "Increased stress affecting communication quality"
            });
        }

        // Time-based challenges
        var hour = GetHourFromTimeOfDay(context.TimeOfDay);
        if (hour >= PersonalityConstants.LateHoursThreshold || hour <= PersonalityConstants.MorningHoursThreshold)
        {
            challenges.AddRange(new[]
            {
                "Reduced cognitive performance during off-peak hours",
                "Lower energy affecting response quality",
                "Potential for misunderstandings due to fatigue"
            });
        }

        _logger.LogDebug("Predicted {ChallengeCount} potential challenges", challenges.Count);

        return challenges;
    }

    #region Private Helper Methods

    private List<string> DetermineRequiredAdaptations(SituationalContext context)
    {
        var adaptations = new List<string>();

        // Base adaptations by context type
        switch (context.ContextType)
        {
            case ContextType.Technical:
                adaptations.AddRange(new[] { "Increase technical precision", "Use structured explanations", "Provide concrete examples" });
                break;

            case ContextType.Personal:
                adaptations.AddRange(new[] { "Show empathy and understanding", "Use warm communication", "Respect emotional boundaries" });
                break;

            case ContextType.Professional:
                adaptations.AddRange(new[] { "Maintain professional tone", "Focus on business outcomes", "Show competence and reliability" });
                break;

            case ContextType.Family:
                adaptations.AddRange(new[] { "Express care and concern", "Use family-appropriate language", "Show personal investment" });
                break;
        }

        // Urgency adaptations
        if (context.UrgencyLevel > PersonalityConstants.HighUrgencyThreshold)
        {
            adaptations.AddRange(new[] { "Increase directness", "Focus on immediate solutions", "Prioritize actionable items" });
        }

        // Temporal adaptations
        var hour = GetHourFromTimeOfDay(context.TimeOfDay);
        if (hour >= PersonalityConstants.LateHoursThreshold)
        {
            adaptations.Add("Account for reduced energy levels");
        }
        else if (hour >= PersonalityConstants.MorningHoursThreshold && hour <= 10)
        {
            adaptations.Add("Leverage higher morning energy");
        }

        return adaptations;
    }

    private string DetermineRecommendedApproach(SituationalContext context)
    {
        var baseApproach = context.ContextType switch
        {
            ContextType.Technical => "Structured, logical approach with technical depth and concrete examples",
            ContextType.Personal => "Empathetic, understanding approach with emotional awareness",
            ContextType.Professional => "Professional, results-focused approach with clear action orientation",
            ContextType.Family => "Warm, caring approach with family-sensitive communication",
            _ => "Balanced, context-appropriate approach"
        };

        if (context.UrgencyLevel > PersonalityConstants.HighUrgencyThreshold)
        {
            baseApproach = $"Urgent, direct approach: {baseApproach}";
        }

        return baseApproach;
    }

    private List<string> GetKeyConsiderations(SituationalContext context)
    {
        var considerations = new List<string>
        {
            "Maintain appropriate context sensitivity",
            "Balance expertise with accessibility",
            "Consider audience knowledge level",
            "Respect communication boundaries"
        };

        switch (context.ContextType)
        {
            case ContextType.Technical:
                considerations.AddRange(new[] { "Verify technical assumptions", "Provide scalable detail levels", "Include practical applications" });
                break;

            case ContextType.Personal:
                considerations.AddRange(new[] { "Respect privacy boundaries", "Show genuine care", "Avoid judgment" });
                break;

            case ContextType.Professional:
                considerations.AddRange(new[] { "Focus on business value", "Consider organizational context", "Maintain professional boundaries" });
                break;

            case ContextType.Family:
                considerations.AddRange(new[] { "Consider family dynamics", "Show emotional investment", "Balance care with practical advice" });
                break;
        }

        return considerations;
    }

    private string DetermineOptimalStrategy(SituationalContext context)
    {
        return context.ContextType switch
        {
            ContextType.Technical => "Lead with expertise, provide structured solutions, use concrete examples",
            ContextType.Personal => "Show genuine empathy, listen actively, provide supportive guidance",
            ContextType.Professional => "Demonstrate competence, focus on outcomes, maintain professional standards",
            ContextType.Family => "Express care, understand family needs, balance support with practical advice",
            _ => "Adapt flexibly to context while maintaining authenticity"
        };
    }

    private List<string> DefineSuccessMetrics(SituationalContext context)
    {
        var metrics = new List<string>();

        switch (context.ContextType)
        {
            case ContextType.Technical:
                metrics.AddRange(new[] { "Technical accuracy", "Solution clarity", "Implementation feasibility" });
                break;

            case ContextType.Personal:
                metrics.AddRange(new[] { "Emotional resonance", "Practical helpfulness", "Respectful boundaries" });
                break;

            case ContextType.Professional:
                metrics.AddRange(new[] { "Business value", "Actionable outcomes", "Professional competence" });
                break;

            case ContextType.Family:
                metrics.AddRange(new[] { "Emotional connection", "Family benefit", "Supportive guidance" });
                break;
        }

        // Universal metrics
        metrics.AddRange(new[] { "Communication clarity", "Contextual appropriateness", "Helpful outcomes" });

        return metrics;
    }

    private string DetermineTimeCategory(int hour)
    {
        return hour switch
        {
            >= 6 and <= 11 => "Morning",
            >= 12 and <= 17 => "Afternoon",
            >= 18 and <= 21 => "Evening",
            _ => "Late Hours"
        };
    }

    private bool IsWorkingHours(int hour, DayOfWeek dayOfWeek)
    {
        return dayOfWeek != DayOfWeek.Saturday && dayOfWeek != DayOfWeek.Sunday && hour >= 9 && hour <= 17;
    }

    private double DetermineExpectedEnergyLevel(int hour)
    {
        return hour switch
        {
            >= 6 and <= 10 => 0.8, // High morning energy
            >= 11 and <= 14 => 0.7, // Good mid-day energy
            >= 15 and <= 18 => 0.6, // Moderate afternoon energy
            >= 19 and <= 21 => 0.5, // Evening energy
            _ => 0.3 // Low late/early hours energy
        };
    }

    private double DetermineExpectedAttentionLevel(int hour, DayOfWeek dayOfWeek)
    {
        var baseAttention = hour switch
        {
            >= 9 and <= 11 => 0.9, // Peak morning attention
            >= 14 and <= 16 => 0.8, // Good afternoon attention
            >= 11 and <= 13 => 0.7, // Pre-lunch attention
            >= 17 and <= 19 => 0.6, // Evening attention
            _ => 0.4 // Off-peak attention
        };

        // Reduce attention on Mondays and Fridays
        if (dayOfWeek == DayOfWeek.Monday || dayOfWeek == DayOfWeek.Friday)
        {
            baseAttention *= 0.9;
        }

        return baseAttention;
    }

    private List<string> DetermineTemporalAdaptations(int hour, DayOfWeek dayOfWeek)
    {
        var adaptations = new List<string>();

        if (hour >= PersonalityConstants.LateHoursThreshold || hour <= PersonalityConstants.MorningHoursThreshold)
        {
            adaptations.AddRange(new[] { "Use simpler language", "Reduce complexity", "Be more concise" });
        }

        if (hour >= PersonalityConstants.MorningHoursThreshold && hour <= 10)
        {
            adaptations.AddRange(new[] { "Leverage high energy", "Handle complex topics", "Be thorough" });
        }

        if (dayOfWeek == DayOfWeek.Friday)
        {
            adaptations.Add("Consider end-of-week fatigue");
        }

        if (dayOfWeek == DayOfWeek.Monday)
        {
            adaptations.Add("Account for week-start adjustment");
        }

        return adaptations;
    }

    private List<string> DetermineTimeBasedChallenges(int hour, DayOfWeek dayOfWeek)
    {
        var challenges = new List<string>();

        if (hour >= PersonalityConstants.LateHoursThreshold)
        {
            challenges.AddRange(new[] { "Reduced cognitive performance", "Higher error rate", "Lower patience" });
        }

        if (dayOfWeek == DayOfWeek.Friday && hour >= 15)
        {
            challenges.Add("End-of-week mental fatigue");
        }

        if (dayOfWeek == DayOfWeek.Monday && hour <= 10)
        {
            challenges.Add("Week-start adjustment period");
        }

        return challenges;
    }

    private string DetermineRecommendedPacing(double urgencyLevel, int hour)
    {
        if (urgencyLevel > PersonalityConstants.HighUrgencyThreshold)
        {
            return "Fast, action-oriented pacing";
        }

        if (hour >= PersonalityConstants.LateHoursThreshold)
        {
            return "Slower, more deliberate pacing";
        }

        return "Moderate, adaptive pacing";
    }

    private string GetUrgencyDescription(double urgencyLevel)
    {
        return urgencyLevel switch
        {
            >= 0.8 => "Critical",
            >= 0.6 => "High",
            >= 0.4 => "Medium",
            >= 0.2 => "Low",
            _ => "Very Low"
        };
    }

    private int DetermineTopicComplexity(string topic)
    {
        var baseComplexity = 3;

        // Length-based complexity
        var wordCount = topic.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (wordCount > 20) baseComplexity += 2;
        else if (wordCount > 10) baseComplexity += 1;

        // Technical term detection
        var technicalTerms = new[] { "architecture", "algorithm", "framework", "implementation", "refactoring", "optimization", "integration", "microservices" };
        var technicalTermCount = technicalTerms.Count(term => topic.Contains(term, StringComparison.OrdinalIgnoreCase));
        baseComplexity += Math.Min(technicalTermCount, 3);

        return Math.Clamp(baseComplexity, 1, 10);
    }

    private string GetComplexityDescription(int complexity)
    {
        return complexity switch
        {
            >= 8 => "Very High",
            >= 6 => "High",
            >= 4 => "Medium",
            >= 2 => "Low",
            _ => "Very Low"
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

    private TemporalAnalysisData ConvertToTemporalAnalysisData(TemporalContextAnalysis analysis)
    {
        return new TemporalAnalysisData
        {
            TimeOfDay = TimeOfDay.Morning, // Можно улучшить логику конвертации DateTime в TimeOfDay enum
            EnergyLevel = analysis.EnergyLevel,
            ProductivityScore = analysis.AttentionLevel, // Используем AttentionLevel как ProductivityScore
            TimeBasedRecommendations = analysis.TemporalAdaptations, // Используем TemporalAdaptations вместо Recommendations
            Description = $"Time Category: {analysis.TimeCategory}, Working Hours: {analysis.WorkingHours}"
        };
    }

    #endregion
}