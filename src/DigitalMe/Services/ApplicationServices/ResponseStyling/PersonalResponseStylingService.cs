using DigitalMe.Data.Entities;
using DigitalMe.Services.Optimization;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.ApplicationServices.ResponseStyling;

/// <summary>
/// Personal response styling service implementation.
/// Orchestrates specialized services following Single Responsibility Principle.
/// </summary>
public class PersonalResponseStylingService : IPersonalResponseStylingService
{
    private readonly IPersonalVocabularyService _vocabularyService;
    private readonly IPersonalLinguisticPatternService _linguisticPatternService;
    private readonly IPersonalContextAnalyzer _contextAnalyzer;
    private readonly IPerformanceOptimizationService _performanceOptimizationService;
    private readonly ILogger<PersonalResponseStylingService> _logger;

    public PersonalResponseStylingService(
        IPersonalVocabularyService vocabularyService,
        IPersonalLinguisticPatternService linguisticPatternService,
        IPersonalContextAnalyzer contextAnalyzer,
        IPerformanceOptimizationService performanceOptimizationService,
        ILogger<PersonalResponseStylingService> logger)
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

        try
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            // Get contextual communication style through specialized analyzer
            var communicationStyle = await _contextAnalyzer.GetContextualStyleAsync(context);

            // Apply linguistic patterns through specialized service
            var enhancedText = _linguisticPatternService.ApplyPersonalLinguisticPatterns(input, communicationStyle);

            // Get vocabulary preferences for context enrichment
            var vocabularyPrefs = await _vocabularyService.GetVocabularyPreferencesAsync(context);

            // Apply vocabulary-based enhancements
            enhancedText = ApplyVocabularyEnhancements(enhancedText, vocabularyPrefs);

            _logger.LogDebug("Response styling completed: {Original} -> {Enhanced}",
                input.Length, enhancedText.Length);

            return enhancedText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error styling response for context {ContextType}", context.ContextType);
            return input; // Return original on error
        }
    }

    public async Task<ContextualCommunicationStyle> GetContextualStyleAsync(SituationalContext context)
    {
        var cacheKey = $"communication_style_{context.ContextType}_{context.UrgencyLevel:F1}";
        return await _performanceOptimizationService.GetOrSetAsync(cacheKey, async () =>
        {
            return await _contextAnalyzer.GetContextualStyleAsync(context);
        }, TimeSpan.FromMinutes(30));
    }

    public string ApplyPersonalLinguisticPatterns(string text, ContextualCommunicationStyle style)
    {
        return _linguisticPatternService.ApplyPersonalLinguisticPatterns(text, style);
    }

    public async Task<PersonalVocabularyPreferences> GetVocabularyPreferencesAsync(SituationalContext context)
    {
        var cacheKey = $"vocabulary_prefs_{context.ContextType}";
        return await _performanceOptimizationService.GetOrSetAsync(cacheKey, async () =>
        {
            return await _vocabularyService.GetVocabularyPreferencesAsync(context);
        }, TimeSpan.FromMinutes(15));
    }

    private static string ApplyVocabularyEnhancements(string text, PersonalVocabularyPreferences vocabulary)
    {
        var enhancedText = text;

        // Apply signature expressions contextually
        if (vocabulary.SignatureExpressions.Any() && !ContainsAnySignature(text, vocabulary.SignatureExpressions))
        {
            var signature = vocabulary.SignatureExpressions.First();
            enhancedText = $"{signature}. {enhancedText}";
        }

        // Add personal touches for family context
        if (enhancedText.Contains("family") && !enhancedText.Contains("Marina") &&
            vocabulary.SignatureExpressions.Any(s => s.Contains("Marina")))
        {
            enhancedText = enhancedText.Replace("family", "Marina and Sofia");
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