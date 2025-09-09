using DigitalMe.Models;

namespace DigitalMe.Repositories;

public interface IPersonalityRepository
{
    Task<PersonalityProfile?> GetProfileAsync(string name);
    Task<PersonalityProfile?> GetProfileByIdAsync(Guid id);
    Task<PersonalityProfile> CreateProfileAsync(PersonalityProfile profile);
    Task<PersonalityProfile> UpdateProfileAsync(PersonalityProfile profile);
    Task<bool> DeleteProfileAsync(Guid id);
    Task<IEnumerable<PersonalityTrait>> GetTraitsAsync(Guid profileId);
    Task<PersonalityTrait> AddTraitAsync(PersonalityTrait trait);
    Task<PersonalityTrait> UpdateTraitAsync(PersonalityTrait trait);
    Task<bool> DeleteTraitAsync(Guid traitId);
}
