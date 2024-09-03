using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using DigitalMe.DTOs;
using DigitalMe.Models;
using DigitalMe.Data;
using DigitalMe.Tests.Unit.Builders;

namespace DigitalMe.Tests.Unit.Controllers;

public class PersonalityControllerTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public PersonalityControllerTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetPersonality_WithExistingPersonality_ShouldReturnPersonalityWithTraits()
    {
        // Arrange
        var personality = PersonalityProfileBuilder.ForIvan();
        var trait1 = PersonalityTraitBuilder.Create()
            .WithCategory("Core")
            .WithName("Analytical")
            .WithDescription("Strong analytical thinking")
            .WithWeight(0.9)
            .Build();
        var trait2 = PersonalityTraitBuilder.Create()
            .WithCategory("Behavior")
            .WithName("Pragmatic")
            .WithDescription("Practical approach to problems")
            .WithWeight(0.8)
            .Build();

        await SeedTestData(personality, new[] { trait1, trait2 });

        // Act
        var response = await _client.GetAsync($"/api/personality/{personality.Name}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var personalityDto = JsonSerializer.Deserialize<PersonalityProfileDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        personalityDto.Should().NotBeNull();
        personalityDto!.Name.Should().Be(personality.Name);
        personalityDto.Description.Should().Be(personality.Description);
        personalityDto.Traits.Should().HaveCount(2);
        personalityDto.Traits.Should().Contain(t => t.Name == "Analytical");
        personalityDto.Traits.Should().Contain(t => t.Name == "Pragmatic");
    }

    [Fact]
    public async Task GetPersonality_WithNonExistentPersonality_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/personality/NonExistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Personality 'NonExistent' not found");
    }

    [Fact]
    public async Task CreatePersonality_WithValidData_ShouldCreateAndReturnPersonality()
    {
        // Arrange
        var createDto = new CreatePersonalityProfileDto
        {
            Name = "TestPersonality",
            Description = "A test personality profile"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/personality", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var content = await response.Content.ReadAsStringAsync();
        var personalityDto = JsonSerializer.Deserialize<PersonalityProfileDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        personalityDto.Should().NotBeNull();
        personalityDto!.Name.Should().Be(createDto.Name);
        personalityDto.Description.Should().Be(createDto.Description);
        personalityDto.Id.Should().NotBeEmpty();
        personalityDto.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        personalityDto.Traits.Should().BeEmpty();

        // Verify location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain($"/api/Personality/{createDto.Name}");
    }

    [Fact]
    public async Task CreatePersonality_WithEmptyName_ShouldReturnBadRequest()
    {
        // Arrange
        var createDto = new CreatePersonalityProfileDto
        {
            Name = "",
            Description = "Description without name"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/personality", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdatePersonality_WithValidData_ShouldUpdateAndReturnPersonality()
    {
        // Arrange
        var personality = PersonalityProfileBuilder.Create()
            .WithName("UpdateTest")
            .WithDescription("Original description")
            .Build();

        await SeedTestData(personality);

        var updateDto = new UpdatePersonalityProfileDto
        {
            Description = "Updated description with new information"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/personality/{personality.Id}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var personalityDto = JsonSerializer.Deserialize<PersonalityProfileDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        personalityDto.Should().NotBeNull();
        personalityDto!.Description.Should().Be(updateDto.Description);
        personalityDto.UpdatedAt.Should().BeAfter(personalityDto.CreatedAt);
    }

    [Fact]
    public async Task UpdatePersonality_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var updateDto = new UpdatePersonalityProfileDto
        {
            Description = "This won't work"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/personality/{nonExistentId}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddTrait_WithValidData_ShouldAddTraitToPersonality()
    {
        // Arrange
        var personality = PersonalityProfileBuilder.Create()
            .WithName("TraitTest")
            .Build();

        await SeedTestData(personality);

        var createTraitDto = new CreatePersonalityTraitDto
        {
            Category = "Cognitive",
            Name = "Creative",
            Description = "High creative thinking ability",
            Weight = 0.85
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/personality/{personality.Id}/traits", createTraitDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var traitDto = JsonSerializer.Deserialize<PersonalityTraitDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        traitDto.Should().NotBeNull();
        traitDto!.Category.Should().Be(createTraitDto.Category);
        traitDto.Name.Should().Be(createTraitDto.Name);
        traitDto.Description.Should().Be(createTraitDto.Description);
        traitDto.Weight.Should().Be(createTraitDto.Weight);
    }

    [Fact]
    public async Task GetSystemPrompt_WithValidPersonalityId_ShouldReturnSystemPrompt()
    {
        // Arrange
        var personality = PersonalityProfileBuilder.ForIvan();
        var traits = new[]
        {
            PersonalityTraitBuilder.Create()
                .WithCategory("Core")
                .WithName("Analytical")
                .WithDescription("Strong analytical thinking")
                .WithWeight(0.9)
                .Build(),
            PersonalityTraitBuilder.Create()
                .WithCategory("Communication")
                .WithName("Direct")
                .WithDescription("Direct communication style")
                .WithWeight(0.8)
                .Build()
        };

        await SeedTestData(personality, traits);

        // Act
        var response = await _client.GetAsync($"/api/personality/{personality.Id}/system-prompt");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var systemPrompt = await response.Content.ReadAsStringAsync();
        systemPrompt.Should().NotBeNullOrEmpty();
        systemPrompt.Should().Contain(personality.Name);
        systemPrompt.Should().Contain("Analytical");
        systemPrompt.Should().Contain("Direct");
    }

    [Fact]
    public async Task GetSystemPrompt_WithNonExistentPersonalityId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/personality/{nonExistentId}/system-prompt");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePersonality_WithValidId_ShouldDeletePersonality()
    {
        // Arrange
        var personality = PersonalityProfileBuilder.Create()
            .WithName("DeleteTest")
            .Build();

        await SeedTestData(personality);

        // Act
        var response = await _client.DeleteAsync($"/api/personality/{personality.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion
        var getResponse = await _client.GetAsync($"/api/personality/{personality.Name}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePersonality_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/personality/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddTrait_WithInvalidPersonalityId_ShouldReturnNotFoundOrBadRequest()
    {
        // Arrange
        var nonExistentPersonalityId = Guid.NewGuid();
        var createTraitDto = new CreatePersonalityTraitDto
        {
            Category = "Test",
            Name = "TestTrait",
            Description = "This should fail",
            Weight = 0.5
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/personality/{nonExistentPersonalityId}/traits", createTraitDto);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    private async Task SeedTestData(PersonalityProfile personality, PersonalityTrait[]? traits = null)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DigitalMeDbContext>();
        
        // Ensure database is clean
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        personality.Id = Guid.NewGuid();
        personality.CreatedAt = DateTime.UtcNow;
        personality.UpdatedAt = DateTime.UtcNow;

        context.PersonalityProfiles.Add(personality);

        if (traits != null)
        {
            foreach (var trait in traits)
            {
                trait.Id = Guid.NewGuid();
                trait.PersonalityProfileId = personality.Id;
                trait.CreatedAt = DateTime.UtcNow;
                context.PersonalityTraits.Add(trait);
            }
        }

        await context.SaveChangesAsync();
    }
}

public record CreatePersonalityTraitDto
{
    public string Category { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double Weight { get; init; }
}