using DigitalMe.Data.Entities;
using DigitalMe.Services.Performance;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.ApplicationServices.ResponseStyling;

/// <summary>
/// Refactored implementation of Ivan-specific response styling service.
/// Orchestrates specialized services following Single Responsibility Principle.
/// </summary>
public class IvanResponseStylingServiceRefactored : IIvanResponseStylingService
{
    private readonly IIvanVocabularyService _vocabularyService;
    private readonly IIvanLinguisticPatternService _linguisticPatternService;
    private readonly IIvanContextAnalyzer _contextAnalyzer;
    private readonly ICachingService _cachingService;
    private readonly ILogger<IvanResponseStylingServiceRefactored> _logger;

    public IvanResponseStylingServiceRefactored(
        IIvanVocabularyService vocabularyService,
        IIvanLinguisticPatternService linguisticPatternService,
        IIvanContextAnalyzer contextAnalyzer,
        ICachingService cachingService,
        ILogger<IvanResponseStylingServiceRefactored> logger)
    {
        _vocabularyService = vocabularyService;
        _linguisticPatternService = linguisticPatternService;
        _contextAnalyzer = contextAnalyzer;
        _cachingService = cachingService;
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
            var styleResult = await _contextAnalyzer.GetContextualStyleAsync(context);
            if (!styleResult.IsSuccess)
            {
                _logger.LogError("Failed to get communication style: {Error}", styleResult.Error);
                return input; // Return original on style error
            }

            // Apply linguistic patterns through specialized service
            var enhancedText = styleResult.Value != null
                ? _linguisticPatternService.ApplyIvanLinguisticPatterns(input, styleResult.Value)
                : input;

            // Get vocabulary preferences for context enrichment
            var vocabularyResult = await _vocabularyService.GetVocabularyPreferencesAsync(context);
            if (!vocabularyResult.IsSuccess)
            {
                _logger.LogError("Failed to get vocabulary preferences: {Error}", vocabularyResult.Error);
                return input; // Return original on vocabulary error
            }

            // Apply vocabulary-based enhancements
            enhancedText = vocabularyResult.Value != null
                ? ApplyVocabularyEnhancements(enhancedText, vocabularyResult.Value)
                : enhancedText;

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
        return await _cachingService.GetOrSetAsync(cacheKey, async () =>
        {
            var result = await _contextAnalyzer.GetContextualStyleAsync(context);
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException($"Failed to get contextual style: {result.Error}");
            }
            return result.Value!;
        }, TimeSpan.FromMinutes(30));
    }

    public string ApplyIvanLinguisticPatterns(string text, ContextualCommunicationStyle style)
    {
        return _linguisticPatternService.ApplyIvanLinguisticPatterns(text, style);
    }

    public async Task<IvanVocabularyPreferences> GetVocabularyPreferencesAsync(SituationalContext context)
    {
        var cacheKey = $"vocabulary_prefs_{context.ContextType}";
        return await _cachingService.GetOrSetAsync(cacheKey, async () =>
        {
            var result = await _vocabularyService.GetVocabularyPreferencesAsync(context);
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException($"Failed to get vocabulary preferences: {result.Error}");
            }
            return result.Value!;
        }, TimeSpan.FromMinutes(15));
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