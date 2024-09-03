using DigitalMe.Data;
using DigitalMe.Models;
using DigitalMe.Repositories;
using DigitalMe.Tests.Unit.Builders;
using DigitalMe.Tests.Unit.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DigitalMe.Tests.Unit.Repositories;

public class PersonalityRepositoryTests : TestBase
{
    private DigitalMeDbContext CreateContext()
    {
        var options = CreateInMemoryDbOptions<DigitalMeDbContext>();
        return new DigitalMeDbContext(options);
    }

    [Fact]
    public async Task GetProfileAsync_WithExistingName_ReturnsProfile()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new PersonalityRepository(context);
        
        var profile = PersonalityProfileBuilder.ForIvan();
        context.PersonalityProfiles.Add(profile);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetProfileAsync(profile.Name);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(profile.Name);
        result.Id.Should().Be(profile.Id);
        result.Description.Should().Be(profile.Description);
    }

    [Fact]
    public async Task GetProfileAsync_WithNonExistingName_ReturnsNull()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new PersonalityRepository(context);

        // Act
        var result = await repository.GetProfileAsync("Non-existent Profile");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProfileAsync_IncludesPersonalityTraits()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new PersonalityRepository(context);

        var (profile, traits) = PersonalityTestFixtures.CreateProfileWithTraits();
        context.PersonalityProfiles.Add(profile);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetProfileAsync(profile.Name);

        // Assert
        result.Should().NotBeNull();
        result!.PersonalityTraits.Should().HaveCount(traits.Count);
        result.PersonalityTraits.Should().BeEquivalentTo(traits, options => 
            options.Excluding(t => t.PersonalityProfile));
    }

    [Fact]
    public async Task GetProfileByIdAsync_WithExistingId_ReturnsProfile()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new PersonalityRepository(context);

        var profile = PersonalityProfileBuilder.Default();
        context.PersonalityProfiles.Add(profile);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetProfileByIdAsync(profile.Id);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(profile, options => 
            options.Excluding(p => p.PersonalityTraits));
    }

    [Fact]
    public async Task GetProfileByIdAsync_WithNonExistingId_ReturnsNull()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new PersonalityRepository(context);
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await repository.GetProfileByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateProfileAsync_WithValidProfile_SavesAndReturnsProfile()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new PersonalityRepository(context);

        var profile = PersonalityProfileBuilder.Create()
            .WithName("Test Profile")
            .WithDescription("Test Description")
            .Build();

        // Act
        var result = await repository.CreateProfileAsync(profile);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(profile.Id);
        result.Name.Should().Be(profile.Name);

        // Verify it's saved in database
        var savedProfile = await context.PersonalityProfiles.FindAsync(profile.Id);
        savedProfile.Should().NotBeNull();
        savedProfile.Should().BeEquivalentTo(profile, options => 
            options.Excluding(p => p.PersonalityTraits));
    }

    [Fact]
    public async Task UpdateProfileAsync_WithValidProfile_UpdatesAndReturnsProfile()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new PersonalityRepository(context);

        var profile = PersonalityProfileBuilder.Default();
        context.PersonalityProfiles.Add(profile);
        await context.SaveChangesAsync();

        var originalUpdatedAt = profile.UpdatedAt;
        
        // Simulate passage of time
        await Task.Delay(10);
        
        profile.Description = "Updated Description";

        // Act
        var result = await repository.UpdateProfileAsync(profile);

        // Assert
        result.Should().NotBeNull();
        result.Description.Should().Be("Updated Description");
        result.UpdatedAt.Should().BeAfter(originalUpdatedAt);

        // Verify changes are persisted
        var updatedProfile = await context.PersonalityProfiles.FindAsync(profile.Id);
        updatedProfile!.Description.Should().Be("Updated Description");
        updatedProfile.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public async Task DeleteProfileAsync_WithExistingId_DeletesAndReturnsTrue()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new PersonalityRepository(context);

        var profile = PersonalityProfileBuilder.Default();
        context.PersonalityProfiles.Add(profile);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.DeleteProfileAsync(profile.Id);

        // Assert
        result.Should().BeTrue();

        // Verify deletion
        var deletedProfile = await context.PersonalityProfiles.FindAsync(profile.Id);
        deletedProfile.Should().BeNull();
    }

    [Fact]
    public async Task DeleteProfileAsync_WithNonExistingId_ReturnsFalse()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new PersonalityRepository(context);
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await repository.DeleteProfileAsync(nonExistentId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetTraitsAsync_WithExistingProfileId_ReturnsOrderedTraits()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new PersonalityRepository(context);

        var profile = PersonalityProfileBuilder.Default();
        context.PersonalityProfiles.Add(profile);

        var traits = new List<PersonalityTrait>
        {
            PersonalityTraitBuilder.Create().WithPersonalityProfileId(profile.Id).WithCategory("Technical").WithName("C#").Build(),
            PersonalityTraitBuilder.Create().WithPersonalityProfileId(profile.Id).WithCategory("Leadership").WithName("Mentoring").Build(),
            PersonalityTraitBuilder.Create().WithPersonalityProfileId(profile.Id).WithCategory("Communication").WithName("Direct").Build(),
            PersonalityTraitBuilder.Create().WithPersonalityProfileId(profile.Id).WithCategory("Technical").WithName("Architecture").Build()
        };

        context.PersonalityTraits.AddRange(traits);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetTraitsAsync(profile.Id);

        // Assert
        var resultList = result.ToList();
        resultList.Should().HaveCount(4);
        
        // Verify ordering: first by Category, then by Name
        resultList[0].Category.Should().Be("Communication");
        resultList[1].Category.Should().Be("Leadership");
        resultList[2].Category.Should().Be("Technical");
        resultList[2].Name.Should().Be("Architecture");
        resultList[3].Category.Should().Be("Technical");
        resultList[3].Name.Should().Be("C#");
    }

    [Fact]
    public async Task GetTraitsAsync_WithNonExistingProfileId_ReturnsEmptyList()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new PersonalityRepository(context);
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await repository.GetTraitsAsync(nonExistentId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AddTraitAsync_WithValidTrait_SavesAndReturnsTrait()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new PersonalityRepository(context);

        var profile = PersonalityProfileBuilder.Default();
        context.PersonalityProfiles.Add(profile);
        await context.SaveChangesAsync();

        var trait = PersonalityTraitBuilder.Create()
            .WithPersonalityProfileId(profile.Id)
            .WithCategory("Test Category")
            .WithName("Test Trait")
            .WithDescription("Test Description")
            .WithWeight(0.8)
            .Build();

        // Act
        var result = await repository.AddTraitAsync(trait);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(trait);

        // Verify it's saved
        var savedTrait = await context.PersonalityTraits.FindAsync(trait.Id);
        savedTrait.Should().NotBeNull();
        savedTrait.Should().BeEquivalentTo(trait, options => 
            options.Excluding(t => t.PersonalityProfile));
    }

    [Fact]
    public async Task UpdateTraitAsync_WithValidTrait_UpdatesAndReturnsTrait()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new PersonalityRepository(context);

        var profile = PersonalityProfileBuilder.Default();
        context.PersonalityProfiles.Add(profile);

        var trait = PersonalityTraitBuilder.Default();
        trait.PersonalityProfileId = profile.Id;
        context.PersonalityTraits.Add(trait);
        await context.SaveChangesAsync();

        trait.Description = "Updated Description";
        trait.Weight = 0.9;

        // Act
        var result = await repository.UpdateTraitAsync(trait);

        // Assert
        result.Should().NotBeNull();
        result.Description.Should().Be("Updated Description");
        result.Weight.Should().Be(0.9);

        // Verify persistence
        var updatedTrait = await context.PersonalityTraits.FindAsync(trait.Id);
        updatedTrait!.Description.Should().Be("Updated Description");
        updatedTrait.Weight.Should().Be(0.9);
    }

    [Fact]
    public async Task DeleteTraitAsync_WithExistingId_DeletesAndReturnsTrue()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new PersonalityRepository(context);

        var profile = PersonalityProfileBuilder.Default();
        context.PersonalityProfiles.Add(profile);

        var trait = PersonalityTraitBuilder.Default();
        trait.PersonalityProfileId = profile.Id;
        context.PersonalityTraits.Add(trait);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.DeleteTraitAsync(trait.Id);

        // Assert
        result.Should().BeTrue();

        // Verify deletion
        var deletedTrait = await context.PersonalityTraits.FindAsync(trait.Id);
        deletedTrait.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTraitAsync_WithNonExistingId_ReturnsFalse()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new PersonalityRepository(context);
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await repository.DeleteTraitAsync(nonExistentId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteProfileAsync_WithTraits_DeletesProfileAndTraitsCascade()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new PersonalityRepository(context);

        var (profile, traits) = PersonalityTestFixtures.CreateProfileWithTraits();
        context.PersonalityProfiles.Add(profile);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.DeleteProfileAsync(profile.Id);

        // Assert
        result.Should().BeTrue();

        // Verify profile deletion
        var deletedProfile = await context.PersonalityProfiles.FindAsync(profile.Id);
        deletedProfile.Should().BeNull();

        // Verify traits are also deleted (cascade)
        var remainingTraits = await context.PersonalityTraits
            .Where(t => t.PersonalityProfileId == profile.Id)
            .ToListAsync();
        remainingTraits.Should().BeEmpty();
    }
}