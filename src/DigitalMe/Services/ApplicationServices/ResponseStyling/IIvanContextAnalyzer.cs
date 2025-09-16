using DigitalMe.Common;
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
    /// <returns>Result containing Ivan's contextual communication style or error details</returns>
    Task<Result<ContextualCommunicationStyle>> GetContextualStyleAsync(SituationalContext context);
}