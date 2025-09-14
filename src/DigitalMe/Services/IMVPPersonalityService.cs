using DigitalMe.Data.Entities;

namespace DigitalMe.Services;

/// <summary>
/// MVP-specific personality service interface focused on Ivan's profile
/// Follows Interface Segregation Principle by exposing only MVP-required methods
/// </summary>
public interface IMvpPersonalityService
{
    /// <summary>
    /// Gets Ivan's personality profile from database
    /// </summary>
    Task<PersonalityProfile?> GetIvanProfileAsync();

    /// <summary>
    /// Generates system prompt specifically for Ivan's personality
    /// </summary>
    Task<string> GenerateIvanSystemPromptAsync();

    /// <summary>
    /// Gets all personality traits for Ivan
    /// </summary>
    Task<List<PersonalityTrait>> GetIvanTraitsAsync();
}
