using DigitalMe.Common;
using DigitalMe.Data.Entities;
using DigitalMe.Services.Performance;
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
    private readonly ICachingService _cachingService;
    private readonly ILogger<PersonalResponseStylingService> _logger;

    public PersonalResponseStylingService(
        IPersonalVocabularyService vocabularyService,
        IPersonalLinguisticPatternService linguisticPatternService,
        IPersonalContextAnalyzer contextAnalyzer,
        ICachingService cachingService,
        ILogger<PersonalResponseStylingService> logger)
    {
        _vocabularyService = vocabularyService;
        _linguisticPatternService = linguisticPatternService;
        _contextAnalyzer = contextAnalyzer;
        _cachingService = cachingService;
        _logger = logger;
    }

    public async Task<Result<string>> StyleResponseAsync(string input, SituationalContext context)
    {
        _logger.LogInformation("Styling response for {ContextType} context ({InputLength} chars)",
            context.ContextType, input.Length);

        return await ResultExtensions.TryAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            // Get contextual communication style through specialized analyzer
            var communicationStyleResult = await GetContextualStyleAsync(context);
            if (communicationStyleResult.IsFailure)
                throw new InvalidOperationException($"Failed to get communication style: {communicationStyleResult.Error}");

            // Apply linguistic patterns through specialized service
            var linguisticResult = ApplyPersonalLinguisticPatterns(input, communicationStyleResult.Value!);
            if (linguisticResult.IsFailure)
                throw new InvalidOperationException($"Failed to apply linguistic patterns: {linguisticResult.Error}");

            // Get vocabulary preferences for context enrichment
            var vocabularyResult = await GetVocabularyPreferencesAsync(context);
            if (vocabularyResult.IsFailure)
                throw new InvalidOperationException($"Failed to get vocabulary preferences: {vocabularyResult.Error}");

            // Apply vocabulary-based enhancements
            var enhancedText = ApplyVocabularyEnhancements(linguisticResult.Value!, vocabularyResult.Value!);

            _logger.LogDebug("Response styling completed: {Original} -> {Enhanced}",
                input.Length, enhancedText.Length);

            return enhancedText;
        }, $"Error styling response for context {context.ContextType}");
    }

    public async Task<Result<ContextualCommunicationStyle>> GetContextualStyleAsync(SituationalContext context)
    {
        var cacheKey = $"communication_style_{context.ContextType}_{context.UrgencyLevel:F1}";
        return await _cachingService.GetOrSetAsync(cacheKey, async () =>
        {
            var result = await _contextAnalyzer.GetContextualStyleAsync(context);
            return result.ValueOrThrow(); // Unwrap Result<T> for caching
        }, TimeSpan.FromMinutes(30));
    }

    public Result<string> ApplyPersonalLinguisticPatterns(string text, ContextualCommunicationStyle style)
    {
        return _linguisticPatternService.ApplyPersonalLinguisticPatterns(text, style);
    }

    public async Task<Result<PersonalVocabularyPreferences>> GetVocabularyPreferencesAsync(SituationalContext context)
    {
        var cacheKey = $"vocabulary_prefs_{context.ContextType}";
        return await _cachingService.GetOrSetAsync(cacheKey, async () =>
        {
            var result = await _vocabularyService.GetVocabularyPreferencesAsync(context);
            return result.ValueOrThrow(); // Unwrap Result<T> for caching
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