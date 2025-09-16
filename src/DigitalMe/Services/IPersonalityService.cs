using DigitalMe.Common;
using DigitalMe.Data.Entities;

namespace DigitalMe.Services;

/// <summary>
/// Service for working with personality profiles and generating system prompts for LLM agents.
/// Provides methods for loading personality data and generating personalized prompts.
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

    // Legacy database-oriented methods for backward compatibility
    Task<PersonalityProfile?> GetPersonalityAsync(string name);
    Task<PersonalityProfile> CreatePersonalityAsync(string name, string description);
    Task<PersonalityProfile> UpdatePersonalityAsync(Guid id, string description);
    Task<string> GenerateSystemPromptAsync(Guid personalityId);
    Task<string> GenerateIvanSystemPromptAsync();
    Task<PersonalityTrait> AddTraitAsync(Guid personalityId, string category, string name, string description, double weight = 1.0);
    Task<IEnumerable<PersonalityTrait>> GetPersonalityTraitsAsync(Guid personalityId);
    Task<bool> DeletePersonalityAsync(Guid id);
}

/// <summary>
/// Legacy alias for IPersonalityService for backward compatibility.
/// </summary>
[Obsolete("Use IPersonalityService instead", false)]
public interface IIvanPersonalityService : IPersonalityService
{
    /// <summary>
    /// Legacy method name - use GetPersonalityAsync() instead.
    /// </summary>
    [Obsolete("Use GetPersonalityAsync() instead", false)]
    Task<Result<PersonalityProfile>> GetIvanPersonalityAsync() => GetPersonalityAsync();
}
