using DigitalMe.Data.Entities;

namespace DigitalMe.Services.ApplicationServices.ResponseStyling;

/// <summary>
/// Interface for Ivan-specific linguistic pattern application.
/// Handles application of Ivan's communication patterns to text.
/// </summary>
public interface IIvanLinguisticPatternService
{
    /// <summary>
    /// Applies Ivan's linguistic patterns to text
    /// </summary>
    /// <param name="text">Text to enhance with patterns</param>
    /// <param name="style">Communication style context</param>
    /// <returns>Text enhanced with Ivan's linguistic patterns</returns>
    string ApplyIvanLinguisticPatterns(string text, ContextualCommunicationStyle style);
}