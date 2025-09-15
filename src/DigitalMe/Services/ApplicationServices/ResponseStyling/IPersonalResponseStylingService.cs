using DigitalMe.Data.Entities;

namespace DigitalMe.Services.ApplicationServices.ResponseStyling;

/// <summary>
/// Interface for personal response styling service.
/// Provides contextual response styling based on personality patterns.
/// </summary>
public interface IPersonalResponseStylingService
{
    /// <summary>
    /// Generates personality-styled response based on context and input
    /// </summary>
    /// <param name="input">Raw response content</param>
    /// <param name="context">Situational context for styling</param>
    /// <returns>Personality-styled response</returns>
    Task<string> StyleResponseAsync(string input, SituationalContext context);

    /// <summary>
    /// Gets communication style parameters for given context
    /// </summary>
    /// <param name="context">Situational context</param>
    /// <returns>Personal contextual communication style</returns>
    Task<ContextualCommunicationStyle> GetContextualStyleAsync(SituationalContext context);

    /// <summary>
    /// Applies personal linguistic patterns to text
    /// </summary>
    /// <param name="text">Original text</param>
    /// <param name="style">Communication style to apply</param>
    /// <returns>Text with personal linguistic patterns</returns>
    string ApplyPersonalLinguisticPatterns(string text, ContextualCommunicationStyle style);

    /// <summary>
    /// Gets personal vocabulary preferences for context
    /// </summary>
    /// <param name="context">Situational context</param>
    /// <returns>Vocabulary recommendations</returns>
    Task<PersonalVocabularyPreferences> GetVocabularyPreferencesAsync(SituationalContext context);
}