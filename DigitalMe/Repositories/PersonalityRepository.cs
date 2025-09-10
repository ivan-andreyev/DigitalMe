using Microsoft.EntityFrameworkCore;
using DigitalMe.Data;
using DigitalMe.Models;

namespace DigitalMe.Repositories;

public class PersonalityRepository : IPersonalityRepository
{
    private readonly DigitalMeDbContext _context;

    public PersonalityRepository(DigitalMeDbContext context)
    {
        _context = context;
    }

    public async Task<PersonalityProfile?> GetProfileAsync(string name)
    {
        return await _context.PersonalityProfiles
            .Include(p => p.Traits)
            .FirstOrDefaultAsync(p => p.Name == name);
    }

    public async Task<PersonalityProfile?> GetProfileByIdAsync(Guid id)
    {
        return await _context.PersonalityProfiles
            .Include(p => p.Traits)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PersonalityProfile> CreateProfileAsync(PersonalityProfile profile)
    {
        _context.PersonalityProfiles.Add(profile);
        await _context.SaveChangesAsync();
        return profile;
    }

    public async Task<PersonalityProfile> UpdateProfileAsync(PersonalityProfile profile)
    {
        profile.UpdatedAt = DateTime.UtcNow;
        _context.PersonalityProfiles.Update(profile);
        await _context.SaveChangesAsync();
        return profile;
    }

    public async Task<bool> DeleteProfileAsync(Guid id)
    {
        var profile = await _context.PersonalityProfiles.FindAsync(id);
        if (profile == null) return false;

        _context.PersonalityProfiles.Remove(profile);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<PersonalityTrait>> GetTraitsAsync(Guid profileId)
    {
        return await _context.PersonalityTraits
            .Where(t => t.PersonalityProfileId == profileId)
            .OrderBy(t => t.Category)
            .ThenBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<PersonalityTrait> AddTraitAsync(PersonalityTrait trait)
    {
        _context.PersonalityTraits.Add(trait);
        await _context.SaveChangesAsync();
        return trait;
    }

    public async Task<PersonalityTrait> UpdateTraitAsync(PersonalityTrait trait)
    {
        _context.PersonalityTraits.Update(trait);
        await _context.SaveChangesAsync();
        return trait;
    }

    public async Task<bool> DeleteTraitAsync(Guid traitId)
    {
        var trait = await _context.PersonalityTraits.FindAsync(traitId);
        if (trait == null) return false;

        _context.PersonalityTraits.Remove(trait);
        await _context.SaveChangesAsync();
        return true;
    }
}
