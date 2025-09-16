using DigitalMe.Common;
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
    /// <returns>Result containing personal contextual communication style or error details</returns>
    Task<Result<ContextualCommunicationStyle>> GetContextualStyleAsync(SituationalContext context);
}