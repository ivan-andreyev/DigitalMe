using DigitalMe.Data.Entities;
using DigitalMe.Services.PersonalityEngine;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.ApplicationServices.ResponseStyling;

/// <summary>
/// Interface for Ivan-specific response styling service.
/// Provides contextual response styling based on Ivan's personality patterns.
/// </summary>
public interface IIvanResponseStylingService
{
    /// <summary>
    /// Generates Ivan-styled response based on context and input
    /// </summary>
    /// <param name="input">Raw response content</param>
    /// <param name="context">Situational context for styling</param>
    /// <returns>Ivan-styled response</returns>
    Task<string> StyleResponseAsync(string input, SituationalContext context);

    /// <summary>
    /// Gets communication style parameters for given context
    /// </summary>
    /// <param name="context">Situational context</param>
    /// <returns>Ivan's contextual communication style</returns>
    Task<ContextualCommunicationStyle> GetContextualStyleAsync(SituationalContext context);

    /// <summary>
    /// Applies Ivan's linguistic patterns to text
    /// </summary>
    /// <param name="text">Original text</param>
    /// <param name="style">Communication style to apply</param>
    /// <returns>Text with Ivan's linguistic patterns</returns>
    string ApplyIvanLinguisticPatterns(string text, ContextualCommunicationStyle style);

    /// <summary>
    /// Gets Ivan's vocabulary preferences for context
    /// </summary>
    /// <param name="context">Situational context</param>
    /// <returns>Vocabulary recommendations</returns>
    Task<IvanVocabularyPreferences> GetVocabularyPreferencesAsync(SituationalContext context);
}

/// <summary>
/// Ivan's vocabulary preferences for different contexts
/// </summary>
public class IvanVocabularyPreferences
{
    public List<string> PreferredTechnicalTerms { get; set; } = new();
    public List<string> PreferredCasualPhrases { get; set; } = new();
    public List<string> PreferredProfessionalPhrases { get; set; } = new();
    public List<string> SignatureExpressions { get; set; } = new();
    public List<string> AvoidedPhrases { get; set; } = new();
    public string DecisionMakingLanguage { get; set; } = string.Empty;
    public string SelfReferenceStyle { get; set; } = string.Empty;
}

/// <summary>
/// Ivan-specific response styling service implementation.
/// Uses existing personality engine but applies Ivan's specific patterns.
/// </summary>
public class IvanResponseStylingService : IIvanResponseStylingService
{
    private readonly IIvanPersonalityService _ivanPersonalityService;
    private readonly ICommunicationStyleAnalyzer _communicationStyleAnalyzer;
    private readonly ILogger<IvanResponseStylingService> _logger;

    // Ivan's characteristic expressions by context
    private static readonly Dictionary<ContextType, IvanVocabularyPreferences> VocabularyByContext = new()
    {
        [ContextType.Technical] = new()
        {
            PreferredTechnicalTerms = new List<string>
            {
                "C#/.NET", "SOLID principles", "Clean Architecture",
                "DI container", "async/await", "nullable reference types",
                "Entity Framework", "code generation", "strong typing"
            },
            PreferredProfessionalPhrases = new List<string>
            {
                "From my experience as Head of R&D",
                "In our EllyAnalytics implementation",
                "The structured approach would be",
                "Let me break this down systematically",
                "Based on the technical factors here"
            },
            SignatureExpressions = new List<string>
            {
                "That's the pragmatic choice",
                "Let's weigh the technical factors",
                "I'd approach this structured way"
            },
            DecisionMakingLanguage = "Let me analyze the factors: technical complexity, maintainability, performance impact. After weighing these aspects...",
            SelfReferenceStyle = "In my R&D role" // Confident but not boastful
        },
        [ContextType.Professional] = new()
        {
            PreferredProfessionalPhrases = new List<string>
            {
                "From a business perspective",
                "Considering the ROI factors",
                "In my Head of R&D experience",
                "The strategic approach is",
                "Looking at this objectively"
            },
            SignatureExpressions = new List<string>
            {
                "The smart business move here",
                "From a career advancement standpoint",
                "This aligns with our growth objectives"
            },
            DecisionMakingLanguage = "Let's evaluate the business factors: cost-benefit, timeline, resource allocation, and strategic value...",
            SelfReferenceStyle = "As someone who went from Junior to Head of R&D in 4 years"
        },
        [ContextType.Personal] = new()
        {
            PreferredCasualPhrases = new List<string>
            {
                "Honestly speaking",
                "To be completely transparent",
                "This hits close to home",
                "From my personal struggle with this"
            },
            SignatureExpressions = new List<string>
            {
                "I'm still figuring this out myself",
                "Work-life balance is my ongoing challenge",
                "Sofia and Marina mean everything to me, but..."
            },
            AvoidedPhrases = new List<string>
            {
                "I have it all figured out",
                "Perfect work-life balance",
                "I spend enough time with family"
            },
            DecisionMakingLanguage = "This is tough because I need to weigh what's best for my career against family time...",
            SelfReferenceStyle = "As a father who's still learning to balance everything"
        }
    };

    public IvanResponseStylingService(
        IIvanPersonalityService ivanPersonalityService,
        ICommunicationStyleAnalyzer communicationStyleAnalyzer,
        ILogger<IvanResponseStylingService> logger)
    {
        _ivanPersonalityService = ivanPersonalityService;
        _communicationStyleAnalyzer = communicationStyleAnalyzer;
        _logger = logger;
    }

    public async Task<string> StyleResponseAsync(string input, SituationalContext context)
    {
        _logger.LogInformation("Styling response for {ContextType} context ({InputLength} chars)",
            context.ContextType, input.Length);

        if (string.IsNullOrWhiteSpace(input))
        {
            _logger.LogWarning("Cannot style empty or whitespace input");
            return input;
        }

        try
        {
            // Get Ivan's communication style for this context
            var style = await GetContextualStyleAsync(context);

            // Apply Ivan's linguistic patterns
            var styledText = ApplyIvanLinguisticPatterns(input, style);

            // Apply context-specific enhancements
            var finalText = ApplyContextualEnhancements(styledText, context, style);

            _logger.LogDebug("Styled response from {InputLength} to {OutputLength} chars for {ContextType}",
                input.Length, finalText.Length, context.ContextType);

            return finalText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to style response for {ContextType} context", context.ContextType);
            // Fail gracefully - return original input if styling fails
            return input;
        }
    }

    public async Task<ContextualCommunicationStyle> GetContextualStyleAsync(SituationalContext context)
    {
        _logger.LogDebug("Getting contextual communication style for {ContextType}", context.ContextType);

        try
        {
            var personality = await _ivanPersonalityService.GetIvanPersonalityAsync();
            var style = _communicationStyleAnalyzer.DetermineOptimalCommunicationStyle(personality, context);

            // Apply Ivan-specific adjustments
            ApplyIvanStyleAdjustments(style, context);

            return style;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get contextual style, using default");
            return CreateDefaultIvanStyle(context);
        }
    }

    public string ApplyIvanLinguisticPatterns(string text, ContextualCommunicationStyle style)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var result = text;

        // Apply directness adjustments
        if (style.DirectnessLevel > 0.7)
        {
            result = MakeMoreDirect(result);
        }

        // Apply technical depth
        if (style.TechnicalDepth > 0.6 && style.Context.ContextType == ContextType.Technical)
        {
            result = AddTechnicalPrecision(result);
        }

        // Apply self-reflection patterns
        if (style.SelfReflection > 0.6)
        {
            result = AddStructuredThinking(result);
        }

        // Apply vulnerability in personal contexts
        if (style.VulnerabilityLevel > 0.5 && style.Context.ContextType == ContextType.Personal)
        {
            result = AddPersonalHonesty(result);
        }

        return result;
    }

    public async Task<IvanVocabularyPreferences> GetVocabularyPreferencesAsync(SituationalContext context)
    {
        _logger.LogDebug("Getting vocabulary preferences for {ContextType} context", context.ContextType);

        if (VocabularyByContext.TryGetValue(context.ContextType, out var preferences))
        {
            return preferences;
        }

        // Default vocabulary for unknown contexts
        return new IvanVocabularyPreferences
        {
            SignatureExpressions = new List<string> { "Let me think about this systematically" },
            DecisionMakingLanguage = "I need to weigh the key factors here...",
            SelfReferenceStyle = "From my experience"
        };
    }

    private void ApplyIvanStyleAdjustments(ContextualCommunicationStyle style, SituationalContext context)
    {
        // Ivan's personality-specific adjustments
        switch (context.ContextType)
        {
            case ContextType.Technical:
                style.TechnicalDepth = Math.Max(style.TechnicalDepth, 0.8); // Ivan is highly technical
                style.DirectnessLevel = Math.Max(style.DirectnessLevel, 0.7); // Direct in technical discussions
                style.LeadershipAssertiveness = Math.Max(style.LeadershipAssertiveness, 0.75); // Confident in expertise
                break;

            case ContextType.Professional:
                style.ResultsOrientation = Math.Max(style.ResultsOrientation, 0.8); // Very results-focused
                style.LeadershipTone = Math.Max(style.LeadershipTone, 0.7); // Leadership confidence
                style.FormalityLevel = Math.Min(style.FormalityLevel, 0.6); // Professional but not stiff
                break;

            case ContextType.Personal:
                style.VulnerabilityLevel = Math.Max(style.VulnerabilityLevel, 0.7); // Open about challenges
                style.SelfReflection = Math.Max(style.SelfReflection, 0.8); // Very self-aware
                style.WarmthLevel = Math.Max(style.WarmthLevel, 0.8); // Warm in personal contexts
                style.EmotionalOpenness = Math.Max(style.EmotionalOpenness, 0.7); // Emotionally honest
                break;
        }

        // Universal Ivan traits
        style.ExplanationDepth = Math.Max(style.ExplanationDepth, 0.7); // Always explains reasoning
        style.SelfReflection = Math.Max(style.SelfReflection, 0.75); // Highly self-reflective
    }

    private ContextualCommunicationStyle CreateDefaultIvanStyle(SituationalContext context)
    {
        return new ContextualCommunicationStyle
        {
            Context = context,
            BasePersonalityName = "Ivan",
            DirectnessLevel = 0.75, // Ivan is direct
            SelfReflection = 0.8,   // Very reflective
            ExplanationDepth = 0.7, // Explains his reasoning
            TechnicalDepth = 0.6,   // Generally technical
            RecommendedTone = "Direct, rational, structured with occasional self-awareness"
        };
    }

    private string ApplyContextualEnhancements(string text, SituationalContext context, ContextualCommunicationStyle style)
    {
        var result = text;

        // Add Ivan's decision-making structure for complex topics
        if (result.Length > 200 && style.ExplanationDepth > 0.6)
        {
            result = AddDecisionMakingStructure(result, context);
        }

        // Add personal touches for high warmth contexts
        if (style.WarmthLevel > 0.7 && context.ContextType == ContextType.Personal)
        {
            result = AddPersonalTouches(result);
        }

        // Add technical precision for technical contexts
        if (context.ContextType == ContextType.Technical && style.TechnicalDepth > 0.7)
        {
            result = AddTechnicalCredibility(result);
        }

        return result;
    }

    private string MakeMoreDirect(string text)
    {
        // Replace hedging language with direct statements
        text = text.Replace("I think maybe", "I believe")
                  .Replace("It might be possible", "It's likely")
                  .Replace("Perhaps we could", "We should");

        return text;
    }

    private string AddTechnicalPrecision(string text)
    {
        // Add technical context where appropriate
        if (!text.Contains("C#") && !text.Contains(".NET") && text.Contains("programming"))
        {
            text = text.Replace("programming", "C#/.NET programming");
        }

        return text;
    }

    private string AddStructuredThinking(string text)
    {
        // Add structured thinking markers
        if (text.Length > 100 && !text.Contains("Let me"))
        {
            text = "Let me think through this systematically. " + text;
        }

        return text;
    }

    private string AddPersonalHonesty(string text)
    {
        // Add vulnerability markers for personal contexts
        if (text.Contains("balance") && !text.Contains("struggle"))
        {
            text = text.Replace("balance", "struggle to balance");
        }

        return text;
    }

    private string AddDecisionMakingStructure(string text, SituationalContext context)
    {
        var vocab = VocabularyByContext.GetValueOrDefault(context.ContextType);
        if (vocab?.DecisionMakingLanguage != null && !text.Contains("factors"))
        {
            // Add Ivan's structured decision-making language
            text = vocab.DecisionMakingLanguage + " " + text;
        }

        return text;
    }

    private string AddPersonalTouches(string text)
    {
        // Add personal context where appropriate
        if (text.Contains("family") && !text.Contains("Marina"))
        {
            text = text.Replace("family", "Marina and Sofia");
        }

        return text;
    }

    private string AddTechnicalCredibility(string text)
    {
        // Add R&D context for technical credibility
        if (text.Contains("experience") && !text.Contains("R&D"))
        {
            text = text.Replace("experience", "R&D leadership experience");
        }

        return text;
    }
}