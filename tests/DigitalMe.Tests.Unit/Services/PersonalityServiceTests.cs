using DigitalMe.Data.Entities;
using DigitalMe.Repositories;
using DigitalMe.Services;
using DigitalMe.Tests.Unit.Builders;
using DigitalMe.Tests.Unit.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DigitalMe.Tests.Unit.Services;

public class PersonalityServiceTests : TestBase
{
    private readonly Mock<IPersonalityRepository> _mockRepository;
    private readonly Mock<ILogger<PersonalityService>> _mockLogger;
    private readonly PersonalityService _service;

    public PersonalityServiceTests()
    {
        _mockRepository = MockRepository.Create<IPersonalityRepository>();
        _mockLogger = MockRepository.Create<ILogger<PersonalityService>>();
        _service = new PersonalityService(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetPersonalityAsync_WithValidName_ReturnsPersonalityProfile()
    {
        // Arrange
        var expectedProfile = PersonalityProfileBuilder.ForIvan().Build();
        _mockRepository.Setup(r => r.GetProfileAsync("Ivan Digital Clone"))
            .ReturnsAsync(expectedProfile);

        // Act
        var result = await _service.GetPersonalityAsync("Ivan Digital Clone");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedProfile);
        _mockRepository.Verify(r => r.GetProfileAsync("Ivan Digital Clone"), Times.Once);
        VerifyAll();
    }

    [Fact]
    public async Task GetPersonalityAsync_WithInvalidName_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetProfileAsync("NonExistent"))
            .ReturnsAsync((PersonalityProfile?)null);

        // Act
        var result = await _service.GetPersonalityAsync("NonExistent");

        // Assert
        result.Should().BeNull();
        VerifyAll();
    }

    [Fact]
    public async Task CreatePersonalityAsync_WithValidData_ReturnsCreatedProfile()
    {
        // Arrange
        var name = "Test Profile";
        var description = "Test Description";
        var expectedProfile = PersonalityProfileBuilder.Create()
            .WithName(name)
            .WithDescription(description)
            .Build();

        _mockRepository.Setup(r => r.CreateProfileAsync(It.IsAny<PersonalityProfile>()))
            .ReturnsAsync((PersonalityProfile profile) => profile);

        // Act
        var result = await _service.CreatePersonalityAsync(name, description);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(name);
        result.Description.Should().Be(description);
        result.Id.Should().NotBeEmpty();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        
        _mockRepository.Verify(r => r.CreateProfileAsync(It.Is<PersonalityProfile>(p => 
            p.Name == name && p.Description == description)), Times.Once);
        VerifyAll();
    }

    [Fact]
    public async Task UpdatePersonalityAsync_WithValidId_ReturnsUpdatedProfile()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var newDescription = "Updated description";
        var existingProfile = PersonalityProfileBuilder.Create()
            .WithId(profileId)
            .WithName("Test Profile")
            .WithDescription("Old description")
            .Build();

        _mockRepository.Setup(r => r.GetProfileByIdAsync(profileId))
            .ReturnsAsync(existingProfile);
        _mockRepository.Setup(r => r.UpdateProfileAsync(It.IsAny<PersonalityProfile>()))
            .ReturnsAsync((PersonalityProfile profile) => profile);

        // Act
        var result = await _service.UpdatePersonalityAsync(profileId, newDescription);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(profileId);
        result.Description.Should().Be(newDescription);
        
        _mockRepository.Verify(r => r.GetProfileByIdAsync(profileId), Times.Once);
        _mockRepository.Verify(r => r.UpdateProfileAsync(It.Is<PersonalityProfile>(p => 
            p.Id == profileId && p.Description == newDescription)), Times.Once);
        VerifyAll();
    }

    [Fact]
    public async Task UpdatePersonalityAsync_WithInvalidId_ThrowsArgumentException()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        var description = "New description";
        
        _mockRepository.Setup(r => r.GetProfileByIdAsync(invalidId))
            .ReturnsAsync((PersonalityProfile?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdatePersonalityAsync(invalidId, description));
        
        exception.Message.Should().Contain($"Personality with ID {invalidId} not found");
        _mockRepository.Verify(r => r.GetProfileByIdAsync(invalidId), Times.Once);
        _mockRepository.Verify(r => r.UpdateProfileAsync(It.IsAny<PersonalityProfile>()), Times.Never);
        VerifyAll();
    }

    [Fact]
    public async Task GenerateSystemPromptAsync_WithValidProfileAndTraits_ReturnsSystemPrompt()
    {
        // Arrange
        var (profile, traits) = PersonalityTestFixtures.CreateProfileWithTraits();
        
        _mockRepository.Setup(r => r.GetProfileByIdAsync(profile.Id))
            .ReturnsAsync(profile);
        _mockRepository.Setup(r => r.GetTraitsAsync(profile.Id))
            .ReturnsAsync(traits);

        // Act
        var result = await _service.GenerateSystemPromptAsync(profile.Id);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain(profile.Name);
        result.Should().Contain(profile.Description);
        result.Should().Contain("цифровая копия");
        result.Should().Contain("БИОГРАФИЯ И КОНТЕКСТ");
        result.Should().Contain("СТИЛЬ ОБЩЕНИЯ");
        result.Should().Contain("ТЕХНИЧЕСКИЕ ПРЕДПОЧТЕНИЯ");
        result.Should().Contain("C#/.NET");
        result.Should().Contain("ИНДИВИДУАЛЬНЫЕ ЧЕРТЫ ЛИЧНОСТИ");
        
        foreach (var trait in traits)
        {
            result.Should().Contain(trait.Category);
            result.Should().Contain(trait.Name);
            result.Should().Contain(trait.Description);
        }
        
        VerifyAll();
    }

    [Fact]
    public async Task GenerateSystemPromptAsync_WithProfileButNoTraits_ReturnsBasicSystemPrompt()
    {
        // Arrange
        var profile = PersonalityProfileBuilder.ForIvan().Build();
        var emptyTraits = new List<PersonalityTrait>();
        
        _mockRepository.Setup(r => r.GetProfileByIdAsync(profile.Id))
            .ReturnsAsync(profile);
        _mockRepository.Setup(r => r.GetTraitsAsync(profile.Id))
            .ReturnsAsync(emptyTraits);

        // Act
        var result = await _service.GenerateSystemPromptAsync(profile.Id);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain(profile.Name);
        result.Should().Contain(profile.Description);
        result.Should().NotContain("ИНДИВИДУАЛЬНЫЕ ЧЕРТЫ ЛИЧНОСТИ");
        VerifyAll();
    }

    [Fact]
    public async Task GenerateSystemPromptAsync_WithInvalidId_ThrowsArgumentException()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        
        _mockRepository.Setup(r => r.GetProfileByIdAsync(invalidId))
            .ReturnsAsync((PersonalityProfile?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.GenerateSystemPromptAsync(invalidId));
        
        exception.Message.Should().Contain($"Personality with ID {invalidId} not found");
        VerifyAll();
    }

    [Fact]
    public async Task AddTraitAsync_WithValidData_ReturnsCreatedTrait()
    {
        // Arrange
        var personalityId = Guid.NewGuid();
        var category = "Technical";
        var name = "C# Expert";
        var description = "Deep expertise in C# development";
        var weight = 0.9;

        var expectedTrait = PersonalityTraitBuilder.Create()
            .WithPersonalityProfileId(personalityId)
            .WithCategory(category)
            .WithName(name)
            .WithDescription(description)
            .WithWeight(weight)
            .Build();

        _mockRepository.Setup(r => r.AddTraitAsync(It.IsAny<PersonalityTrait>()))
            .ReturnsAsync((PersonalityTrait trait) => trait);

        // Act
        var result = await _service.AddTraitAsync(personalityId, category, name, description, weight);

        // Assert
        result.Should().NotBeNull();
        result.PersonalityProfileId.Should().Be(personalityId);
        result.Category.Should().Be(category);
        result.Name.Should().Be(name);
        result.Description.Should().Be(description);
        result.Weight.Should().Be(weight);
        
        _mockRepository.Verify(r => r.AddTraitAsync(It.Is<PersonalityTrait>(t =>
            t.PersonalityProfileId == personalityId &&
            t.Category == category &&
            t.Name == name &&
            t.Description == description &&
            t.Weight == weight)), Times.Once);
        VerifyAll();
    }

    [Fact]
    public async Task AddTraitAsync_WithDefaultWeight_UsesDefaultValue()
    {
        // Arrange
        var personalityId = Guid.NewGuid();
        var category = "Technical";
        var name = "C# Expert";
        var description = "Deep expertise in C# development";

        _mockRepository.Setup(r => r.AddTraitAsync(It.IsAny<PersonalityTrait>()))
            .ReturnsAsync((PersonalityTrait trait) => trait);

        // Act
        var result = await _service.AddTraitAsync(personalityId, category, name, description);

        // Assert
        result.Weight.Should().Be(1.0);
        VerifyAll();
    }

    [Fact]
    public async Task GetPersonalityTraitsAsync_WithValidId_ReturnsTraits()
    {
        // Arrange
        var personalityId = Guid.NewGuid();
        var expectedTraits = new List<PersonalityTrait>
        {
            PersonalityTraitBuilder.Create().WithPersonalityProfileId(personalityId).WithCategory("Technical").WithName("C# Expert").WithDescription("Deep expertise in C# development").WithWeight(0.9).Build(),
            PersonalityTraitBuilder.Create().WithPersonalityProfileId(personalityId).WithCategory("Leadership").WithName("Mentoring").WithDescription("Enjoys teaching and developing team members").WithWeight(0.7).Build()
        };

        _mockRepository.Setup(r => r.GetTraitsAsync(personalityId))
            .ReturnsAsync(expectedTraits);

        // Act
        var result = await _service.GetPersonalityTraitsAsync(personalityId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedTraits);
        VerifyAll();
    }

    [Fact]
    public async Task DeletePersonalityAsync_WithValidId_ReturnsTrue()
    {
        // Arrange
        var personalityId = Guid.NewGuid();
        
        _mockRepository.Setup(r => r.DeleteProfileAsync(personalityId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeletePersonalityAsync(personalityId);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(r => r.DeleteProfileAsync(personalityId), Times.Once);
        VerifyAll();
    }

    [Fact]
    public async Task DeletePersonalityAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        
        _mockRepository.Setup(r => r.DeleteProfileAsync(invalidId))
            .ReturnsAsync(false);

        // Act
        var result = await _service.DeletePersonalityAsync(invalidId);

        // Assert
        result.Should().BeFalse();
        _mockRepository.Verify(r => r.DeleteProfileAsync(invalidId), Times.Once);
        VerifyAll();
    }
}