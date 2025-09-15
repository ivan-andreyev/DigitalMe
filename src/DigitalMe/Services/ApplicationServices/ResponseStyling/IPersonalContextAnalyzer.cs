using DigitalMe.Data.Entities;

namespace DigitalMe.Services.ApplicationServices.ResponseStyling;

/// <summary>
/// Interface for personal context analysis.
/// Analyzes situational context to determine appropriate communication style based on personality.
/// </summary>
public interface IPersonalContextAnalyzer
{
    /// <summary>
    /// Gets communication style parameters for given context
    /// </summary>
    /// <param name="context">Situational context</param>
    /// <returns>Personal contextual communication style</returns>
    Task<ContextualCommunicationStyle> GetContextualStyleAsync(SituationalContext context);
}