using DigitalMe.Common;
using DigitalMe.Data.Entities;

namespace DigitalMe.Services;

/// <summary>
/// Service for working with personality profiles and generating system prompts for LLM agents.
/// Uses Result<T> pattern for consistent error handling.
/// </summary>
public interface IPersonalityService
{
    /// <summary>
    /// Asynchronously loads a personality profile.
    /// </summary>
    /// <returns>Result containing PersonalityProfile with personality data or error details</returns>
    Task<Result<PersonalityProfile>> GetPersonalityAsync();

    /// <summary>
    /// Generates a system prompt for LLM based on personality profile.
    /// </summary>
    /// <param name="personality">Personality profile for prompt generation</param>
    /// <returns>Result containing system prompt as string or error details</returns>
    Result<string> GenerateSystemPrompt(PersonalityProfile personality);

    /// <summary>
    /// Generates enhanced system prompt with integrated real profile data.
    /// </summary>
    /// <returns>Result containing enhanced system prompt with profile file data or error details</returns>
    Task<Result<string>> GenerateEnhancedSystemPromptAsync();
}
