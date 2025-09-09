using DigitalMe.Data.Entities;

namespace DigitalMe.Services;

public interface IPersonalityService
{
    Task<PersonalityProfile?> GetPersonalityAsync(string name);
    Task<PersonalityProfile> CreatePersonalityAsync(string name, string description);
    Task<PersonalityProfile> UpdatePersonalityAsync(Guid id, string description);
    Task<string> GenerateSystemPromptAsync(Guid personalityId);
    Task<string> GenerateIvanSystemPromptAsync();
    Task<PersonalityTrait> AddTraitAsync(Guid personalityId, string category, string name, string description, double weight = 1.0);
    Task<IEnumerable<PersonalityTrait>> GetPersonalityTraitsAsync(Guid personalityId);
    Task<bool> DeletePersonalityAsync(Guid id);
}
