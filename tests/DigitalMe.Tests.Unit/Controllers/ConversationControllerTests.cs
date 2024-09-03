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

public class ConversationControllerTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ConversationControllerTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task StartConversation_WithValidData_ShouldCreateAndReturnConversation()
    {
        // Arrange
        var createDto = new CreateConversationDto
        {
            Title = "Test Conversation",
            Platform = "Telegram",
            UserId = "user123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/conversation", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var conversationDto = JsonSerializer.Deserialize<ConversationDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        conversationDto.Should().NotBeNull();
        conversationDto!.Title.Should().Be(createDto.Title);
        conversationDto.Platform.Should().Be(createDto.Platform);
        conversationDto.UserId.Should().Be(createDto.UserId);
        conversationDto.IsActive.Should().BeTrue();
        conversationDto.EndedAt.Should().BeNull();
        conversationDto.Messages.Should().BeEmpty();
        conversationDto.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task GetActiveConversation_WithExistingActiveConversation_ShouldReturnConversationWithMessages()
    {
        // Arrange
        var conversation = ConversationBuilder.Create()
            .ForTelegram("user456")
            .WithTitle("Active Chat")
            .IsActive(true)
            .Build();

        var messages = new[]
        {
            MessageBuilder.Create()
                .WithRole("user")
                .WithContent("Hello")
                .WithTimestamp(DateTime.UtcNow.AddMinutes(-10))
                .Build(),
            MessageBuilder.Create()
                .WithRole("assistant")
                .WithContent("Hi there!")
                .WithTimestamp(DateTime.UtcNow.AddMinutes(-9))
                .Build()
        };

        await SeedTestData(conversation, messages);

        // Act
        var response = await _client.GetAsync($"/api/conversation/active?platform={conversation.Platform}&userId={conversation.UserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var conversationDto = JsonSerializer.Deserialize<ConversationDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        conversationDto.Should().NotBeNull();
        conversationDto!.Id.Should().Be(conversation.Id);
        conversationDto.IsActive.Should().BeTrue();
        conversationDto.Messages.Should().HaveCount(2);
        conversationDto.Messages.Should().Contain(m => m.Role == "user" && m.Content == "Hello");
        conversationDto.Messages.Should().Contain(m => m.Role == "assistant" && m.Content == "Hi there!");
    }

    [Fact]
    public async Task GetActiveConversation_WithNoActiveConversation_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/conversation/active?platform=Discord&userId=nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("No active conversation found");
    }

    [Fact]
    public async Task GetMessages_WithExistingConversation_ShouldReturnMessages()
    {
        // Arrange
        var conversation = ConversationBuilder.Create()
            .ForTelegram("user789")
            .Build();

        var messages = new[]
        {
            MessageBuilder.Create()
                .WithRole("user")
                .WithContent("First message")
                .WithTimestamp(DateTime.UtcNow.AddMinutes(-20))
                .Build(),
            MessageBuilder.Create()
                .WithRole("assistant")
                .WithContent("First response")
                .WithTimestamp(DateTime.UtcNow.AddMinutes(-19))
                .Build(),
            MessageBuilder.Create()
                .WithRole("user")
                .WithContent("Second message")
                .WithTimestamp(DateTime.UtcNow.AddMinutes(-10))
                .Build()
        };

        await SeedTestData(conversation, messages);

        // Act
        var response = await _client.GetAsync($"/api/conversation/{conversation.Id}/messages");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var messageDtos = JsonSerializer.Deserialize<MessageDto[]>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        messageDtos.Should().NotBeNull();
        messageDtos!.Should().HaveCount(3);
        messageDtos.Should().Contain(m => m.Content == "First message");
        messageDtos.Should().Contain(m => m.Content == "First response");
        messageDtos.Should().Contain(m => m.Content == "Second message");
    }

    [Fact]
    public async Task GetMessages_WithLimitParameter_ShouldReturnLimitedMessages()
    {
        // Arrange
        var conversation = ConversationBuilder.Create()
            .ForTelegram("user_limit_test")
            .Build();

        var messages = Enumerable.Range(1, 10)
            .Select(i => MessageBuilder.Create()
                .WithRole(i % 2 == 1 ? "user" : "assistant")
                .WithContent($"Message {i}")
                .WithTimestamp(DateTime.UtcNow.AddMinutes(-i))
                .Build())
            .ToArray();

        await SeedTestData(conversation, messages);

        // Act
        var response = await _client.GetAsync($"/api/conversation/{conversation.Id}/messages?limit=3");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var messageDtos = JsonSerializer.Deserialize<MessageDto[]>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        messageDtos.Should().NotBeNull();
        messageDtos!.Should().HaveCount(3);
    }

    [Fact]
    public async Task AddMessage_WithValidData_ShouldAddMessageToConversation()
    {
        // Arrange
        var conversation = ConversationBuilder.Create()
            .ForTelegram("user_add_message")
            .Build();

        await SeedTestData(conversation);

        var createMessageDto = new CreateMessageDto
        {
            Role = "user",
            Content = "New message content",
            Metadata = new Dictionary<string, object> { { "source", "test" } }
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/conversation/{conversation.Id}/messages", createMessageDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var messageDto = JsonSerializer.Deserialize<MessageDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        messageDto.Should().NotBeNull();
        messageDto!.ConversationId.Should().Be(conversation.Id);
        messageDto.Role.Should().Be(createMessageDto.Role);
        messageDto.Content.Should().Be(createMessageDto.Content);
        messageDto.Metadata.Should().ContainKey("source");
        messageDto.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task AddMessage_WithNonExistentConversation_ShouldReturnBadRequestOrNotFound()
    {
        // Arrange
        var nonExistentConversationId = Guid.NewGuid();
        var createMessageDto = new CreateMessageDto
        {
            Role = "user",
            Content = "This should fail"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/conversation/{nonExistentConversationId}/messages", createMessageDto);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task EndConversation_WithActiveConversation_ShouldEndConversation()
    {
        // Arrange
        var conversation = ConversationBuilder.Create()
            .ForTelegram("user_end_test")
            .IsActive(true)
            .Build();

        await SeedTestData(conversation);

        // Act
        var response = await _client.PostAsync($"/api/conversation/{conversation.Id}/end", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var conversationDto = JsonSerializer.Deserialize<ConversationDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        conversationDto.Should().NotBeNull();
        conversationDto!.Id.Should().Be(conversation.Id);
        conversationDto.IsActive.Should().BeFalse();
        conversationDto.EndedAt.Should().NotBeNull();
        conversationDto.EndedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task EndConversation_WithNonExistentConversation_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentConversationId = Guid.NewGuid();

        // Act
        var response = await _client.PostAsync($"/api/conversation/{nonExistentConversationId}/end", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUserConversations_WithExistingUserConversations_ShouldReturnAllUserConversations()
    {
        // Arrange
        var userId = "multi_conversation_user";
        var platform = "Telegram";

        var conversations = new[]
        {
            ConversationBuilder.Create()
                .WithPlatform(platform)
                .WithUserId(userId)
                .WithTitle("Conversation 1")
                .IsActive(true)
                .Build(),
            ConversationBuilder.Create()
                .WithPlatform(platform)
                .WithUserId(userId)
                .WithTitle("Conversation 2")
                .IsActive(false)
                .WithEndedAt(DateTime.UtcNow.AddHours(-1))
                .Build(),
            // Different user - should not be included
            ConversationBuilder.Create()
                .WithPlatform(platform)
                .WithUserId("different_user")
                .WithTitle("Other User Conversation")
                .Build()
        };

        await SeedTestData(conversations, null);

        // Act
        var response = await _client.GetAsync($"/api/conversation/user?platform={platform}&userId={userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var conversationDtos = JsonSerializer.Deserialize<ConversationDto[]>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        conversationDtos.Should().NotBeNull();
        conversationDtos!.Should().HaveCount(2);
        conversationDtos.Should().Contain(c => c.Title == "Conversation 1" && c.IsActive);
        conversationDtos.Should().Contain(c => c.Title == "Conversation 2" && !c.IsActive);
        conversationDtos.Should().NotContain(c => c.Title == "Other User Conversation");
    }

    [Fact]
    public async Task GetUserConversations_WithNoUserConversations_ShouldReturnEmptyArray()
    {
        // Act
        var response = await _client.GetAsync("/api/conversation/user?platform=Discord&userId=no_conversations_user");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var conversationDtos = JsonSerializer.Deserialize<ConversationDto[]>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        conversationDtos.Should().NotBeNull();
        conversationDtos!.Should().BeEmpty();
    }

    private async Task SeedTestData(Conversation conversation, Message[]? messages = null)
    {
        await SeedTestData(new[] { conversation }, messages);
    }

    private async Task SeedTestData(Conversation[] conversations, Message[]? messages = null)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DigitalMeDbContext>();
        
        // Ensure database is clean
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        foreach (var conversation in conversations)
        {
            conversation.Id = Guid.NewGuid();
            conversation.StartedAt = DateTime.UtcNow.AddMinutes(-30);
            context.Conversations.Add(conversation);
        }

        if (messages != null && conversations.Length == 1)
        {
            foreach (var message in messages)
            {
                message.Id = Guid.NewGuid();
                message.ConversationId = conversations[0].Id;
                if (message.Timestamp == default)
                    message.Timestamp = DateTime.UtcNow.AddMinutes(-15);
                context.Messages.Add(message);
            }
        }

        await context.SaveChangesAsync();
    }
}