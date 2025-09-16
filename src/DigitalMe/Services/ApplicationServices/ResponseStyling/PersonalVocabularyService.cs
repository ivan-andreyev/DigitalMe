using DigitalMe.Common;
using DigitalMe.Data.Entities;
using DigitalMe.Services.PersonalityEngine;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.ApplicationServices.ResponseStyling;

/// <summary>
/// Implementation of personal vocabulary management.
/// Extracts vocabulary preferences based on personality profile and context.
/// </summary>
public class PersonalVocabularyService : IPersonalVocabularyService
{
    private readonly IPersonalityService _personalityService;
    private readonly ILogger<PersonalVocabularyService> _logger;

    public PersonalVocabularyService(
        IPersonalityService personalityService,
        ILogger<PersonalVocabularyService> logger)
    {
        _personalityService = personalityService;
        _logger = logger;
    }

    public async Task<Result<PersonalVocabularyPreferences>> GetVocabularyPreferencesAsync(SituationalContext context)
    {
        return await ResultExtensions.TryAsync(async () =>
        {
            var personalityResult = await _personalityService.GetPersonalityAsync();

            if (!personalityResult.IsSuccess)
                throw new InvalidOperationException($"Failed to load personality profile: {personalityResult.Error}");

            var personality = personalityResult.Value!;

            return context.ContextType switch
            {
                ContextType.Technical => GetTechnicalVocabulary(personality),
                ContextType.Professional => GetProfessionalVocabulary(personality),
                ContextType.Personal => GetPersonalVocabulary(personality),
                _ => GetDefaultVocabulary(personality)
            };
        }, $"Error getting vocabulary preferences for context {context.ContextType}");
    }

    private static PersonalVocabularyPreferences GetTechnicalVocabulary(PersonalityProfile personality)
    {
        return new PersonalVocabularyPreferences
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

    private static PersonalVocabularyPreferences GetProfessionalVocabulary(PersonalityProfile personality)
    {
        return new PersonalVocabularyPreferences
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

    private static PersonalVocabularyPreferences GetPersonalVocabulary(PersonalityProfile personality)
    {
        return new PersonalVocabularyPreferences
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

    private static PersonalVocabularyPreferences GetDefaultVocabulary(PersonalityProfile personality)
    {
        return new PersonalVocabularyPreferences
        {
            PreferredCasualPhrases = new() { "в принципе", "по сути" },
            DecisionMakingLanguage = "сбалансированный",
            SelfReferenceStyle = "я"
        };
    }

    private static PersonalVocabularyPreferences GetFallbackVocabulary()
    {
        return new PersonalVocabularyPreferences
        {
            DecisionMakingLanguage = "нейтральный",
            SelfReferenceStyle = "я"
        };
    }
}