using DigitalMe.Data.Entities;

namespace DigitalMe.Services.ApplicationServices.ResponseStyling;

/// <summary>
/// Interface for Ivan-specific context analysis.
/// Analyzes situational context to determine appropriate communication style.
/// </summary>
public interface IIvanContextAnalyzer
{
    /// <summary>
    /// Gets communication style parameters for given context
    /// </summary>
    /// <param name="context">Situational context</param>
    /// <returns>Ivan's contextual communication style</returns>
    Task<ContextualCommunicationStyle> GetContextualStyleAsync(SituationalContext context);
}