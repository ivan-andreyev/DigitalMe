using DigitalMe.Data.Entities;
using DigitalMe.Services.PersonalityEngine;
using DigitalMe.Services.Optimization;
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
/// Ivan-specific response styling service implementation.
/// Uses existing personality engine but applies Ivan's specific patterns.
/// </summary>
public class IvanResponseStylingService : IIvanResponseStylingService
{
    private readonly IIvanVocabularyService _vocabularyService;
    private readonly IIvanLinguisticPatternService _linguisticPatternService;
    private readonly IIvanContextAnalyzer _contextAnalyzer;
    private readonly IPerformanceOptimizationService _performanceOptimizationService;
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
        IIvanVocabularyService vocabularyService,
        IIvanLinguisticPatternService linguisticPatternService,
        IIvanContextAnalyzer contextAnalyzer,
        IPerformanceOptimizationService performanceOptimizationService,
        ILogger<IvanResponseStylingService> logger)
    {
        _vocabularyService = vocabularyService;
        _linguisticPatternService = linguisticPatternService;
        _contextAnalyzer = contextAnalyzer;
        _performanceOptimizationService = performanceOptimizationService;
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

            // Get vocabulary preferences for context enrichment
            var vocabularyPrefs = await GetVocabularyPreferencesAsync(context);

            // Apply vocabulary-based enhancements
            var finalText = ApplyVocabularyEnhancements(styledText, vocabularyPrefs);

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

        // Use caching for communication styles to improve performance
        var cacheKey = $"communication_style_{context.ContextType}_{context.UrgencyLevel:F1}";

        return await _performanceOptimizationService.GetOrSetAsync(cacheKey, async () =>
        {
            var result = await _contextAnalyzer.GetContextualStyleAsync(context);
            if (!result.IsSuccess)
            {
                _logger.LogError("Failed to get contextual style: {Error}, using default", result.Error);
                return new ContextualCommunicationStyle
                {
                    Context = context,
                    BasePersonalityName = "Ivan",
                    DirectnessLevel = 0.75,
                    SelfReflection = 0.8,
                    ExplanationDepth = 0.7,
                    TechnicalDepth = 0.6,
                    RecommendedTone = "Direct, rational, structured with occasional self-awareness"
                };
            }

            return result.Value;
        }, TimeSpan.FromMinutes(30)); // Cache for 30 minutes
    }

    public string ApplyIvanLinguisticPatterns(string text, ContextualCommunicationStyle style)
    {
        return _linguisticPatternService.ApplyIvanLinguisticPatterns(text, style);
    }

    public async Task<IvanVocabularyPreferences> GetVocabularyPreferencesAsync(SituationalContext context)
    {
        _logger.LogDebug("Getting vocabulary preferences for {ContextType} context", context.ContextType);

        // Use caching for vocabulary preferences since they are relatively static
        var cacheKey = $"vocabulary_preferences_{context.ContextType}";

        return await _performanceOptimizationService.GetOrSetAsync(cacheKey, async () =>
        {
            var result = await _vocabularyService.GetVocabularyPreferencesAsync(context);
            if (!result.IsSuccess)
            {
                _logger.LogError("Failed to get vocabulary preferences: {Error}, using fallback", result.Error);
                return new IvanVocabularyPreferences
                {
                    PreferredTechnicalTerms = new List<string> { "structured", "systematic", "pragmatic" },
                    PreferredCasualPhrases = new List<string> { "honestly", "frankly", "here's the thing" },
                    SignatureExpressions = new List<string> { "Ivan's perspective", "from experience" }
                };
            }

            return result.Value;
        }, TimeSpan.FromHours(2)); // Cache for 2 hours - vocabulary preferences are very stable
    }

    private static string ApplyVocabularyEnhancements(string text, IvanVocabularyPreferences vocabulary)
    {
        var enhancedText = text;

        // Apply signature expressions contextually
        if (vocabulary.SignatureExpressions.Any() && !ContainsAnySignature(text, vocabulary.SignatureExpressions))
        {
            var signature = vocabulary.SignatureExpressions.First();
            enhancedText = $"{signature}. {enhancedText}";
        }

        // Replace generic terms with preferred technical terms
        foreach (var techTerm in vocabulary.PreferredTechnicalTerms)
        {
            if (techTerm.Contains(" "))
            {
                var genericTerm = techTerm.Split(' ').Last();
                enhancedText = enhancedText.Replace(genericTerm, techTerm);
            }
        }

        return enhancedText;
    }

    private static bool ContainsAnySignature(string text, List<string> signatures)
    {
        return signatures.Any(sig => text.Contains(sig, StringComparison.OrdinalIgnoreCase));
    }
}