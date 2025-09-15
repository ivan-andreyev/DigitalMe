using DigitalMe.Data.Entities;

namespace DigitalMe.Services.ApplicationServices.ResponseStyling;

/// <summary>
/// Interface for personal linguistic pattern application.
/// Handles application of personality-specific communication patterns to text.
/// </summary>
public interface IPersonalLinguisticPatternService
{
    /// <summary>
    /// Applies personal linguistic patterns to text
    /// </summary>
    /// <param name="text">Text to enhance with patterns</param>
    /// <param name="style">Communication style context</param>
    /// <returns>Text enhanced with personal linguistic patterns</returns>
    string ApplyPersonalLinguisticPatterns(string text, ContextualCommunicationStyle style);
}