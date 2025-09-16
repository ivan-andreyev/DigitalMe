using DigitalMe.Common;
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
    /// <returns>Result containing personality-styled response or error details</returns>
    Task<Result<string>> StyleResponseAsync(string input, SituationalContext context);

    /// <summary>
    /// Gets communication style parameters for given context
    /// </summary>
    /// <param name="context">Situational context</param>
    /// <returns>Result containing personal contextual communication style or error details</returns>
    Task<Result<ContextualCommunicationStyle>> GetContextualStyleAsync(SituationalContext context);

    /// <summary>
    /// Applies personal linguistic patterns to text
    /// </summary>
    /// <param name="text">Original text</param>
    /// <param name="style">Communication style to apply</param>
    /// <returns>Result containing text with personal linguistic patterns or error details</returns>
    Result<string> ApplyPersonalLinguisticPatterns(string text, ContextualCommunicationStyle style);

    /// <summary>
    /// Gets personal vocabulary preferences for context
    /// </summary>
    /// <param name="context">Situational context</param>
    /// <returns>Result containing vocabulary recommendations or error details</returns>
    Task<Result<PersonalVocabularyPreferences>> GetVocabularyPreferencesAsync(SituationalContext context);
}