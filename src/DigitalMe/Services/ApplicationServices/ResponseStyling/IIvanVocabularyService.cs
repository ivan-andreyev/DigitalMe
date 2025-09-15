using DigitalMe.Data.Entities;

namespace DigitalMe.Services.ApplicationServices.ResponseStyling;

/// <summary>
/// Interface for Ivan-specific vocabulary management.
/// Handles vocabulary preferences based on context.
/// </summary>
public interface IIvanVocabularyService
{
    /// <summary>
    /// Gets Ivan's vocabulary preferences for given context
    /// </summary>
    /// <param name="context">Situational context</param>
    /// <returns>Ivan's vocabulary preferences</returns>
    Task<IvanVocabularyPreferences> GetVocabularyPreferencesAsync(SituationalContext context);
}

/// <summary>
/// Represents Ivan's context-specific vocabulary preferences.
/// Contains preferred terms, phrases, and expressions.
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