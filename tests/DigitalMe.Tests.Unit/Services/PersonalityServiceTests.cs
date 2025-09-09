using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using DigitalMe.Services;
using DigitalMe.DTOs;
using DigitalMe.Data.Entities;
using DigitalMe.Repositories;
using DigitalMe.Tests.Unit.Builders;
using Xunit;

namespace DigitalMe.Tests.Unit.Services;

public class PersonalityServiceTests : BaseTestWithDatabase, IAsyncLifetime
{
    private readonly Mock<ILogger<PersonalityService>> _mockLogger;
    private readonly PersonalityService _personalityService;
    private readonly IPersonalityRepository _personalityRepository;

    public PersonalityServiceTests()
    {
        _mockLogger = new Mock<ILogger<PersonalityService>>();
        _personalityRepository = new PersonalityRepository(Context);
        _personalityService = new PersonalityService(_personalityRepository, _mockLogger.Object);
    }

    public async Task InitializeAsync()
    {
        CleanupDatabase();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Helper method to create a test personality
    /// </summary>
    /// <param name="name">Personality name</param>
    /// <param name="description">Personality description</param>
    /// <returns>Created personality</returns>
    private async Task<PersonalityProfile> CreatePersonalityAsync(string name = "TestPersonality", string description = "Test description")
    {
        var personality = PersonalityProfileBuilder.Create()
            .WithName(name)
            .WithDescription(description)
            .Build();

        Context.PersonalityProfiles.Add(personality);
        await Context.SaveChangesAsync();
        return personality;
    }

    /// <summary>
    /// Helper method to create a test personality with traits
    /// </summary>
    /// <param name="name">Personality name</param>
    /// <param name="description">Personality description</param>
    /// <param name="traitCount">Number of traits to create</param>
    /// <returns>Created personality with traits</returns>
    private async Task<PersonalityProfile> CreatePersonalityWithTraitsAsync(string name = "TestPersonality", string description = "Test description", int traitCount = 2)
    {
        var personality = PersonalityProfileBuilder.Create()
            .WithName(name)
            .WithDescription(description)
            .Build();

        var traits = new List<PersonalityTrait>();
        for (int i = 0; i < traitCount; i++)
        {
            var trait = PersonalityTraitBuilder.Create()
                .WithCategory(i == 0 ? "Core" : "Behavior")
                .WithName(i == 0 ? "Analytical" : "Pragmatic")
                .WithDescription(i == 0 ? "Strong analytical thinking" : "Practical approach to problems")
                .WithWeight(i == 0 ? 0.9 : 0.8)
                .WithPersonalityProfileId(personality.Id)
                .Build();
            traits.Add(trait);
        }

        Context.PersonalityProfiles.Add(personality);
        Context.PersonalityTraits.AddRange(traits);
        await Context.SaveChangesAsync();
        return personality;
    }

    /// <summary>
    /// Helper method to create Ivan personality with traits for system prompt testing
    /// </summary>
    /// <returns>Created Ivan personality with traits</returns>
    private async Task<PersonalityProfile> CreateIvanPersonalityAsync()
    {
        var personality = PersonalityProfileBuilder.ForIvan().Build();
        var traits = new[]
        {
            PersonalityTraitBuilder.Create()
                .WithCategory("Core")
                .WithName("Analytical")
                .WithDescription("Strong analytical thinking")
                .WithWeight(0.9)
                .WithPersonalityProfileId(personality.Id)
                .Build(),
            PersonalityTraitBuilder.Create()
                .WithCategory("Communication")
                .WithName("Direct")
                .WithDescription("Direct communication style")
                .WithWeight(0.8)
                .WithPersonalityProfileId(personality.Id)
                .Build()
        };

        Context.PersonalityProfiles.Add(personality);
        Context.PersonalityTraits.AddRange(traits);
        await Context.SaveChangesAsync();
        return personality;
    }

    [Fact]
    public async Task GetPersonality_WithExistingPersonality_ShouldReturnPersonalityWithTraits()
    {
        // Arrange
        var personality = await CreatePersonalityWithTraitsAsync("TestPersonalityWithTraits", "Test personality for trait testing");

        // Act
        var result = await _personalityService.GetPersonalityAsync(personality.Name);
        var traits = await _personalityService.GetPersonalityTraitsAsync(personality.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(personality.Name);
        result.Description.Should().Be(personality.Description);
        traits.Should().HaveCount(2);
        traits.Should().Contain(t => t.Name == "Analytical");
        traits.Should().Contain(t => t.Name == "Pragmatic");
    }

    [Fact]
    public async Task GetPersonality_WithNonExistentPersonality_ShouldReturnNull()
    {
        // Arrange & Act
        var result = await _personalityService.GetPersonalityAsync("NonExistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreatePersonality_WithValidData_ShouldCreateAndReturnPersonality()
    {
        // Arrange
        var name = "TestPersonality";
        var description = "A test personality profile";

        // Act
        var result = await _personalityService.CreatePersonalityAsync(name, description);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(name);
        result.Description.Should().Be(description);
        result.Id.Should().NotBeEmpty();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));

        // Verify it was saved to database
        var savedPersonality = await Context.PersonalityProfiles.FindAsync(result.Id);
        savedPersonality.Should().NotBeNull();
        savedPersonality!.Name.Should().Be(name);
    }

    [Fact]
    public async Task CreatePersonality_WithEmptyName_ShouldCreatePersonalityWithEmptyName()
    {
        // Arrange
        var name = "";
        var description = "Description without name";

        // Act
        var result = await _personalityService.CreatePersonalityAsync(name, description);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(name);
        result.Description.Should().Be(description);
    }

    [Fact]
    public async Task UpdatePersonality_WithValidData_ShouldUpdateAndReturnPersonality()
    {
        // Arrange
        var personality = await CreatePersonalityAsync("UpdateTest", "Original description");

        var newDescription = "Updated description with new information";

        // Act
        var result = await _personalityService.UpdatePersonalityAsync(personality.Id, newDescription);

        // Assert
        result.Should().NotBeNull();
        result.Description.Should().Be(newDescription);
        result.UpdatedAt.Should().BeAfter(result.CreatedAt);

        // Verify in database
        var updatedPersonality = await Context.PersonalityProfiles.FindAsync(personality.Id);
        updatedPersonality!.Description.Should().Be(newDescription);
    }

    [Fact]
    public async Task UpdatePersonality_WithNonExistentId_ShouldThrowArgumentException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var newDescription = "This won't work";

        // Act & Assert
        await FluentActions.Invoking(() => _personalityService.UpdatePersonalityAsync(nonExistentId, newDescription))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Personality with ID {nonExistentId} not found");
    }

    [Fact]
    public async Task AddTrait_WithValidData_ShouldAddTraitToPersonality()
    {
        // Arrange
        var personality = await CreatePersonalityAsync("TraitTest");

        var category = "Cognitive";
        var name = "Creative";
        var description = "High creative thinking ability";
        var weight = 0.85;

        // Act
        var result = await _personalityService.AddTraitAsync(personality.Id, category, name, description, weight);

        // Assert
        result.Should().NotBeNull();
        result.Category.Should().Be(category);
        result.Name.Should().Be(name);
        result.Description.Should().Be(description);
        result.Weight.Should().Be(weight);
        result.PersonalityProfileId.Should().Be(personality.Id);

        // Verify in database
        var savedTrait = await Context.PersonalityTraits.FindAsync(result.Id);
        savedTrait.Should().NotBeNull();
        savedTrait!.Name.Should().Be(name);
    }

    [Fact]
    public async Task GetSystemPrompt_WithValidPersonalityId_ShouldReturnSystemPrompt()
    {
        // Arrange
        var personality = await CreateIvanPersonalityAsync();

        // Act
        var systemPrompt = await _personalityService.GenerateSystemPromptAsync(personality.Id);

        // Assert
        systemPrompt.Should().NotBeNullOrEmpty();
        systemPrompt.Should().Contain(personality.Name);
        systemPrompt.Should().Contain("Analytical");
        systemPrompt.Should().Contain("Direct");
    }

    [Fact]
    public async Task GetSystemPrompt_WithNonExistentPersonalityId_ShouldThrowArgumentException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await FluentActions.Invoking(() => _personalityService.GenerateSystemPromptAsync(nonExistentId))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Personality with ID {nonExistentId} not found");
    }

    [Fact]
    public async Task DeletePersonality_WithValidId_ShouldDeletePersonality()
    {
        // Arrange
        var personality = await CreatePersonalityAsync("DeleteTest");

        // Act
        var result = await _personalityService.DeletePersonalityAsync(personality.Id);

        // Assert
        result.Should().BeTrue();

        // Verify deletion
        var deletedPersonality = await Context.PersonalityProfiles.FindAsync(personality.Id);
        deletedPersonality.Should().BeNull();

        var getPersonality = await _personalityService.GetPersonalityAsync(personality.Name);
        getPersonality.Should().BeNull();
    }

    [Fact]
    public async Task DeletePersonality_WithNonExistentId_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _personalityService.DeletePersonalityAsync(nonExistentId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AddTrait_WithInvalidPersonalityId_ShouldCreateTraitAnyway()
    {
        // Arrange
        var nonExistentPersonalityId = Guid.NewGuid();
        var category = "Test";
        var name = "TestTrait";
        var description = "This should work anyway";
        var weight = 0.5;

        // Act
        var result = await _personalityService.AddTraitAsync(nonExistentPersonalityId, category, name, description, weight);

        // Assert
        result.Should().NotBeNull();
        result.PersonalityProfileId.Should().Be(nonExistentPersonalityId);
        result.Category.Should().Be(category);
        result.Name.Should().Be(name);
    }
}
