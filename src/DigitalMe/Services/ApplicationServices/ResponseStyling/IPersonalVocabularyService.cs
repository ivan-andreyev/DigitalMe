using DigitalMe.Common;
using DigitalMe.Data.Entities;

namespace DigitalMe.Services.ApplicationServices.ResponseStyling;

/// <summary>
/// Interface for personal vocabulary management.
/// Handles vocabulary preferences based on context and personality.
/// </summary>
public interface IPersonalVocabularyService
{
    /// <summary>
    /// Gets personal vocabulary preferences for given context
    /// </summary>
    /// <param name="context">Situational context</param>
    /// <returns>Result containing personal vocabulary preferences or error details</returns>
    Task<Result<PersonalVocabularyPreferences>> GetVocabularyPreferencesAsync(SituationalContext context);
}

/// <summary>
/// Represents personal context-specific vocabulary preferences.
/// Contains preferred terms, phrases, and expressions.
/// </summary>
public class PersonalVocabularyPreferences
{
    public List<string> PreferredTechnicalTerms { get; set; } = new();
    public List<string> PreferredCasualPhrases { get; set; } = new();
    public List<string> PreferredProfessionalPhrases { get; set; } = new();
    public List<string> SignatureExpressions { get; set; } = new();
    public List<string> AvoidedPhrases { get; set; } = new();
    public string DecisionMakingLanguage { get; set; } = string.Empty;
    public string SelfReferenceStyle { get; set; } = string.Empty;
}