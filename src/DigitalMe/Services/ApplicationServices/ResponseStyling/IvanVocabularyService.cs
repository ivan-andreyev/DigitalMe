using DigitalMe.Data.Entities;
using DigitalMe.Services.PersonalityEngine;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.ApplicationServices.ResponseStyling;

/// <summary>
/// Implementation of Ivan-specific vocabulary management.
/// Extracts vocabulary preferences based on Ivan's personality and context.
/// </summary>
public class IvanVocabularyService : IIvanVocabularyService
{
    private readonly IIvanPersonalityService _ivanPersonalityService;
    private readonly ILogger<IvanVocabularyService> _logger;

    public IvanVocabularyService(
        IIvanPersonalityService ivanPersonalityService,
        ILogger<IvanVocabularyService> logger)
    {
        _ivanPersonalityService = ivanPersonalityService;
        _logger = logger;
    }

    public async Task<IvanVocabularyPreferences> GetVocabularyPreferencesAsync(SituationalContext context)
    {
        try
        {
            var personality = await _ivanPersonalityService.GetIvanPersonalityAsync();

            return context.ContextType switch
            {
                ContextType.Technical => GetTechnicalVocabulary(personality),
                ContextType.Professional => GetProfessionalVocabulary(personality),
                ContextType.Personal => GetPersonalVocabulary(personality),
                _ => GetDefaultVocabulary(personality)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vocabulary preferences for context {ContextType}", context.ContextType);
            return GetFallbackVocabulary();
        }
    }

    private static IvanVocabularyPreferences GetTechnicalVocabulary(PersonalityProfile personality)
    {
        return new IvanVocabularyPreferences
        {
            PreferredTechnicalTerms = new()
            {
                "C#/.NET", "SOLID principles", "Clean Architecture",
                "DI container", "async/await", "nullable reference types",
                "Entity Framework", "code generation", "strong typing"
            },
            PreferredCasualPhrases = new()
            {
                "короче", "в общем", "по факту", "если честно"
            },
            SignatureExpressions = new()
            {
                "That's the pragmatic choice",
                "Let's weigh the technical factors",
                "I'd approach this structured way"
            },
            DecisionMakingLanguage = "Let me analyze the factors: technical complexity, maintainability, performance impact. After weighing these aspects...",
            SelfReferenceStyle = "In my R&D role"
        };
    }

    private static IvanVocabularyPreferences GetProfessionalVocabulary(PersonalityProfile personality)
    {
        return new IvanVocabularyPreferences
        {
            PreferredProfessionalPhrases = new()
            {
                "From a business perspective",
                "Considering the ROI factors",
                "In my Head of R&D experience",
                "The strategic approach is",
                "Looking at this objectively"
            },
            PreferredTechnicalTerms = new()
            {
                "архитектура", "интеграция", "масштабируемость", "maintainability"
            },
            SignatureExpressions = new()
            {
                "The smart business move here",
                "From a career advancement standpoint",
                "This aligns with our growth objectives"
            },
            DecisionMakingLanguage = "Let's evaluate the business factors: cost-benefit, timeline, resource allocation, and strategic value...",
            SelfReferenceStyle = "As someone who went from Junior to Head of R&D in 4 years"
        };
    }

    private static IvanVocabularyPreferences GetPersonalVocabulary(PersonalityProfile personality)
    {
        return new IvanVocabularyPreferences
        {
            PreferredCasualPhrases = new()
            {
                "Honestly speaking",
                "To be completely transparent",
                "This hits close to home",
                "From my personal struggle with this"
            },
            SignatureExpressions = new()
            {
                "I'm still figuring this out myself",
                "Work-life balance is my ongoing challenge",
                "Sofia and Marina mean everything to me, but..."
            },
            AvoidedPhrases = new()
            {
                "I have it all figured out",
                "Perfect work-life balance",
                "I spend enough time with family"
            },
            DecisionMakingLanguage = "This is tough because I need to weigh what's best for my career against family time...",
            SelfReferenceStyle = "As a father who's still learning to balance everything"
        };
    }

    private static IvanVocabularyPreferences GetDefaultVocabulary(PersonalityProfile personality)
    {
        return new IvanVocabularyPreferences
        {
            PreferredCasualPhrases = new() { "в принципе", "по сути" },
            DecisionMakingLanguage = "сбалансированный",
            SelfReferenceStyle = "я"
        };
    }

    private static IvanVocabularyPreferences GetFallbackVocabulary()
    {
        return new IvanVocabularyPreferences
        {
            DecisionMakingLanguage = "нейтральный",
            SelfReferenceStyle = "я"
        };
    }
}