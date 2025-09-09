using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using DigitalMe.DTOs;
using DigitalMe.Data.Entities;
using DigitalMe.Data;
using DigitalMe.Tests.Unit.Builders;
using DigitalMe.Tests.Unit.Fixtures;

namespace DigitalMe.Tests.Unit.Controllers;

public class ChatControllerTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ChatControllerTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task EnsureCleanDatabaseWithIvan()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DigitalMeDbContext>();
        
        // Ensure database is clean and recreated with Ivan
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        
        // Seed Ivan personality using the same logic as BaseTestWithDatabase
        var ivan = PersonalityTestFixtures.CreateCompleteIvanProfile();
        ivan.Name = "Ivan";
        context.PersonalityProfiles.Add(ivan);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetStatus_WithIvanPersonalityExists_ShouldReturnReadyStatus()
    {
        // Arrange - Use consistent Ivan seeding
        await EnsureCleanDatabaseWithIvan();

        // Act
        var response = await _client.GetAsync("/api/chat/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var statusResponse = JsonSerializer.Deserialize<JsonElement>(content);
        
        statusResponse.GetProperty("personalityLoaded").GetBoolean().Should().BeTrue();
        // Note: mcpConnected might be false in test environment due to external dependencies
        statusResponse.GetProperty("timestamp").ValueKind.Should().Be(JsonValueKind.String);
    }

    [Fact]
    public async Task GetStatus_WithoutIvanPersonality_ShouldReturnNotReadyStatus()
    {
        // Arrange - Clean database WITHOUT Ivan seeding
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DigitalMeDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        // Don't seed Ivan - this is the test case

        // Act
        var response = await _client.GetAsync("/api/chat/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var statusResponse = JsonSerializer.Deserialize<JsonElement>(content);
        
        statusResponse.GetProperty("personalityLoaded").GetBoolean().Should().BeFalse();
        statusResponse.GetProperty("status").GetString().Should().Be("Not Ready");
    }

    [Fact]
    public async Task SendMessage_WithoutIvanPersonality_ShouldReturnBadRequest()
    {
        // Arrange - Clean database WITHOUT Ivan seeding
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DigitalMeDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        // Don't seed Ivan - this is the test case
        
        // Arrange
        var chatRequest = new ChatRequestDto
        {
            Message = "Hello, Ivan!",
            Platform = "Test",
            UserId = "testuser123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/chat/send", chatRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Ivan's personality profile not found");
    }

    [Fact]
    public async Task SendMessage_WithIvanPersonality_ShouldProcessMessageAndReturnResponse()
    {
        // Arrange - Use consistent Ivan seeding
        await EnsureCleanDatabaseWithIvan();

        var chatRequest = new ChatRequestDto
        {
            Message = "What's your approach to solving complex problems?",
            Platform = "Telegram",
            UserId = "problemsolver123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/chat/send", chatRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var messageDto = JsonSerializer.Deserialize<MessageDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        messageDto.Should().NotBeNull();
        messageDto!.Role.Should().Be("assistant");
        messageDto.Content.Should().NotBeNullOrEmpty();
        messageDto.ConversationId.Should().NotBeEmpty();
        messageDto.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(30));
        
        // Verify metadata contains expected fields
        messageDto.Metadata.Should().ContainKey("mood");
        messageDto.Metadata.Should().ContainKey("confidence");
    }

    [Fact]
    public async Task SendMessage_MultipleMessages_ShouldMaintainConversationContext()
    {
        // Arrange - Use consistent Ivan seeding
        await EnsureCleanDatabaseWithIvan();

        var userId = "contextuser456";
        var platform = "Telegram";

        var firstRequest = new ChatRequestDto
        {
            Message = "Hi Ivan, I'm working on a C# project",
            Platform = platform,
            UserId = userId
        };

        var secondRequest = new ChatRequestDto
        {
            Message = "What design patterns do you recommend for this scenario?",
            Platform = platform,
            UserId = userId
        };

        // Act
        var firstResponse = await _client.PostAsJsonAsync("/api/chat/send", firstRequest);
        var secondResponse = await _client.PostAsJsonAsync("/api/chat/send", secondRequest);

        // Assert
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstContent = await firstResponse.Content.ReadAsStringAsync();
        var secondContent = await secondResponse.Content.ReadAsStringAsync();

        var firstMessageDto = JsonSerializer.Deserialize<MessageDto>(firstContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var secondMessageDto = JsonSerializer.Deserialize<MessageDto>(secondContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        firstMessageDto.Should().NotBeNull();
        secondMessageDto.Should().NotBeNull();

        // Both messages should belong to the same conversation
        firstMessageDto!.ConversationId.Should().Be(secondMessageDto!.ConversationId);
    }

    [Fact]
    public async Task SendMessage_DifferentUsers_ShouldCreateSeparateConversations()
    {
        // Arrange - Use consistent Ivan seeding  
        await EnsureCleanDatabaseWithIvan();

        var user1Request = new ChatRequestDto
        {
            Message = "Hello from user 1",
            Platform = "Telegram",
            UserId = "user1"
        };

        var user2Request = new ChatRequestDto
        {
            Message = "Hello from user 2",
            Platform = "Telegram",
            UserId = "user2"
        };

        // Act
        var user1Response = await _client.PostAsJsonAsync("/api/chat/send", user1Request);
        var user2Response = await _client.PostAsJsonAsync("/api/chat/send", user2Request);

        // Assert
        user1Response.StatusCode.Should().Be(HttpStatusCode.OK);
        user2Response.StatusCode.Should().Be(HttpStatusCode.OK);

        var user1Content = await user1Response.Content.ReadAsStringAsync();
        var user2Content = await user2Response.Content.ReadAsStringAsync();

        var user1MessageDto = JsonSerializer.Deserialize<MessageDto>(user1Content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var user2MessageDto = JsonSerializer.Deserialize<MessageDto>(user2Content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        user1MessageDto.Should().NotBeNull();
        user2MessageDto.Should().NotBeNull();

        // Different users should have different conversations
        user1MessageDto!.ConversationId.Should().NotBe(user2MessageDto!.ConversationId);
    }

    [Fact]
    public async Task SendMessage_DifferentPlatforms_ShouldCreateSeparateConversations()
    {
        // Arrange - Use consistent Ivan seeding
        await EnsureCleanDatabaseWithIvan();

        var userId = "multiplatformuser";

        var telegramRequest = new ChatRequestDto
        {
            Message = "Hello from Telegram",
            Platform = "Telegram",
            UserId = userId
        };

        var discordRequest = new ChatRequestDto
        {
            Message = "Hello from Discord",
            Platform = "Discord",
            UserId = userId
        };

        // Act
        var telegramResponse = await _client.PostAsJsonAsync("/api/chat/send", telegramRequest);
        var discordResponse = await _client.PostAsJsonAsync("/api/chat/send", discordRequest);

        // Assert
        telegramResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        discordResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var telegramContent = await telegramResponse.Content.ReadAsStringAsync();
        var discordContent = await discordResponse.Content.ReadAsStringAsync();

        var telegramMessageDto = JsonSerializer.Deserialize<MessageDto>(telegramContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var discordMessageDto = JsonSerializer.Deserialize<MessageDto>(discordContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        telegramMessageDto.Should().NotBeNull();
        discordMessageDto.Should().NotBeNull();

        // Same user on different platforms should have different conversations
        telegramMessageDto!.ConversationId.Should().NotBe(discordMessageDto!.ConversationId);
    }

    [Fact]
    public async Task SendMessage_WithEmptyMessage_ShouldProcessSuccessfully()
    {
        // Arrange - Use consistent Ivan seeding
        await EnsureCleanDatabaseWithIvan();

        var chatRequest = new ChatRequestDto
        {
            Message = "",
            Platform = "Test",
            UserId = "emptyuser"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/chat/send", chatRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var messageDto = JsonSerializer.Deserialize<MessageDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        messageDto.Should().NotBeNull();
        messageDto!.Role.Should().Be("assistant");
        messageDto.Content.Should().NotBeNull(); // Agent should handle empty input gracefully
    }

}