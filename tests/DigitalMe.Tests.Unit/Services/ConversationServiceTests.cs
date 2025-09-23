using DigitalMe.Data;
using DigitalMe.Data.Entities;
using DigitalMe.Repositories;
using DigitalMe.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DigitalMe.Tests.Unit.Services;

public class ConversationServiceTests : BaseTestWithDatabase
{
    private readonly Mock<ILogger<ConversationService>> _mockLogger;
    private readonly Mock<IMvpPersonalityService> _mockPersonalityService;
    private readonly ConversationService _service;
    private readonly Guid _testPersonalityProfileId;

    public ConversationServiceTests()
    {
        this._mockLogger = new Mock<ILogger<ConversationService>>();
        this._mockPersonalityService = new Mock<IMvpPersonalityService>();

        // Set up the mock to return a valid personality profile
        this._testPersonalityProfileId = Guid.NewGuid();
        var testPersonalityProfile = new PersonalityProfile
        {
            Id = this._testPersonalityProfileId,
            Name = "Ivan",
            Profession = "Test Developer"
        };
        this._mockPersonalityService
            .Setup(x => x.GetIvanProfileAsync())
            .ReturnsAsync(testPersonalityProfile);

        var conversationRepository = new ConversationRepository(this.Context);
        var messageRepository = new MessageRepository(this.Context);

        var mockServiceProvider = new Mock<IServiceProvider>();
        this._service = new ConversationService(conversationRepository, messageRepository, this._mockPersonalityService.Object, this._mockLogger.Object, mockServiceProvider.Object);
    }

    [Fact]
    public async Task StartConversationAsync_NewUser_ShouldCreateNewConversation()
    {
        // Arrange
        var platform = "Web";
        var userId = "new-user-123";
        var title = "Test Conversation";

        // Act
        var result = await this._service.StartConversationAsync(platform, userId, title);

        // Assert
        result.Should().NotBeNull("should create new conversation");
        result.Platform.Should().Be(platform);
        result.UserId.Should().Be(userId);
        result.Title.Should().Be(title);
        result.IsActive.Should().Be(true, "new conversation should be active");
        result.StartedAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(1), "should set recent start time");

        // Verify saved to database
        var savedConversation = await this.Context.Conversations.FindAsync(result.Id);
        savedConversation.Should().NotBeNull("should be saved to database");
    }

    [Fact]
    public async Task StartConversationAsync_ExistingActiveConversation_ShouldReturnExisting()
    {
        // Arrange
        var platform = "Web";
        var userId = "existing-user";
        var title = "First Conversation";

        // Create existing conversation
        var existingConversation = new Conversation
        {
            Platform = platform,
            UserId = userId,
            Title = "Existing Conversation",
            IsActive = true,
            StartedAt = DateTime.UtcNow.AddHours(-1),
            PersonalityProfileId = this._testPersonalityProfileId
        };

        this.Context.Conversations.Add(existingConversation);
        await this.Context.SaveChangesAsync();

        // Act
        var result = await this._service.StartConversationAsync(platform, userId, title);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(existingConversation.Id, "should return existing active conversation");
        result.Title.Should().Be("Existing Conversation", "should keep existing title");

        // Should not create duplicate
        var allUserConversations = await this.Context.Conversations
            .Where(c => c.UserId == userId && c.Platform == platform)
            .ToListAsync();
        allUserConversations.Should().HaveCount(1, "should not create duplicate conversations");
    }

    [Fact]
    public async Task AddMessageAsync_ValidConversation_ShouldAddMessage()
    {
        // Arrange
        var conversation = new Conversation
        {
            Platform = "Web",
            UserId = "test-user",
            Title = "Test Chat",
            IsActive = true,
            PersonalityProfileId = this._testPersonalityProfileId
        };
        this.Context.Conversations.Add(conversation);
        await this.Context.SaveChangesAsync();

        var role = "user";
        var content = "Hello Ivan!";
        var metadata = new Dictionary<string, object> { ["test"] = "value" };

        // Act
        var result = await this._service.AddMessageAsync(conversation.Id, role, content, metadata);

        // Assert
        result.Should().NotBeNull("should create message");
        result.ConversationId.Should().Be(conversation.Id);
        result.Role.Should().Be(role);
        result.Content.Should().Be(content);
        result.Timestamp.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));

        // Verify metadata serialization
        result.Metadata.Should().NotBeNullOrEmpty("should serialize metadata");

        // Verify saved to database
        var savedMessage = await this.Context.Messages.FindAsync(result.Id);
        savedMessage.Should().NotBeNull("should be saved to database");
        savedMessage!.Content.Should().Be(content);
    }

    [Fact]
    public async Task AddMessageAsync_NonExistentConversation_ShouldThrowException()
    {
        // Arrange
        var nonExistentConversationId = Guid.NewGuid();

        // Act & Assert - should throw exception for non-existent conversation
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            this._service.AddMessageAsync(nonExistentConversationId, "user", "test", null));

        exception.ParamName.Should().Be("conversationId");
        exception.Message.Should().Contain($"Conversation with ID {nonExistentConversationId} does not exist");
    }

    [Fact]
    public async Task GetConversationHistoryAsync_WithMessages_ShouldReturnOrderedHistory()
    {
        // Arrange
        var conversation = new Conversation
        {
            Platform = "Web",
            UserId = "history-user",
            Title = "History Test",
            IsActive = true,
            PersonalityProfileId = this._testPersonalityProfileId
        };
        this.Context.Conversations.Add(conversation);
        await this.Context.SaveChangesAsync();

        // Add messages in specific order
        var messages = new[]
        {
            new Message { ConversationId = conversation.Id, Role = "user", Content = "First message", Timestamp = DateTime.UtcNow.AddMinutes(-10) },
            new Message { ConversationId = conversation.Id, Role = "assistant", Content = "First response", Timestamp = DateTime.UtcNow.AddMinutes(-9) },
            new Message { ConversationId = conversation.Id, Role = "user", Content = "Second message", Timestamp = DateTime.UtcNow.AddMinutes(-5) },
            new Message { ConversationId = conversation.Id, Role = "assistant", Content = "Second response", Timestamp = DateTime.UtcNow.AddMinutes(-4) }
        };

        this.Context.Messages.AddRange(messages);
        await this.Context.SaveChangesAsync();

        // Act
        var result = await this._service.GetConversationHistoryAsync(conversation.Id, 10);

        // Assert
        result.Should().HaveCount(4, "should return all messages");
        result.Should().BeInDescendingOrder(m => m.Timestamp, "should be ordered by timestamp descending");
        result.First().Content.Should().Be("Second response", "most recent message should be first");
        result.Last().Content.Should().Be("First message", "oldest message should be last");
    }

    [Fact]
    public async Task GetConversationHistoryAsync_WithLimit_ShouldRespectLimit()
    {
        // Arrange
        var conversation = new Conversation
        {
            Platform = "Web",
            UserId = "limit-user",
            Title = "Limit Test",
            IsActive = true,
            PersonalityProfileId = this._testPersonalityProfileId
        };
        this.Context.Conversations.Add(conversation);
        await this.Context.SaveChangesAsync();

        // Add 5 messages
        for (int i = 0; i < 5; i++)
        {
            this.Context.Messages.Add(new Message
            {
                ConversationId = conversation.Id,
                Role = i % 2 == 0 ? "user" : "assistant",
                Content = $"Message {i}",
                Timestamp = DateTime.UtcNow.AddMinutes(-i),
                Metadata = "{}"
            });
        }
        await this.Context.SaveChangesAsync();

        // Act
        var result = await this._service.GetConversationHistoryAsync(conversation.Id, 3);

        // Assert
        result.Should().HaveCount(3, "should respect limit parameter");
        result.Should().BeInDescendingOrder(m => m.Timestamp, "should maintain chronological order");
    }

    [Fact]
    public async Task GetUserConversationsAsync_MultipleConversations_ShouldReturnUserSpecific()
    {
        // Arrange
        var targetUserId = "target-user";
        var otherUserId = "other-user";
        var platform = "Web";

        var conversations = new[]
        {
            new Conversation { Platform = platform, UserId = targetUserId, Title = "User Conv 1", IsActive = true, PersonalityProfileId = this._testPersonalityProfileId },
            new Conversation { Platform = platform, UserId = targetUserId, Title = "User Conv 2", IsActive = false, PersonalityProfileId = this._testPersonalityProfileId },
            new Conversation { Platform = platform, UserId = otherUserId, Title = "Other Conv", IsActive = true, PersonalityProfileId = this._testPersonalityProfileId },
            new Conversation { Platform = "Mobile", UserId = targetUserId, Title = "Wrong Platform", IsActive = true, PersonalityProfileId = this._testPersonalityProfileId }
        };

        this.Context.Conversations.AddRange(conversations);
        await this.Context.SaveChangesAsync();

        // Act
        var result = await this._service.GetUserConversationsAsync(platform, targetUserId);

        // Assert
        result.Should().HaveCount(2, "should return only target user's conversations for specified platform");
        result.Should().AllSatisfy(c => c.UserId.Should().Be(targetUserId));
        result.Should().AllSatisfy(c => c.Platform.Should().Be(platform));
        result.Select(c => c.Title).Should().Contain(new[] { "User Conv 1", "User Conv 2" });
    }

    [Fact]
    public async Task EndConversationAsync_ActiveConversation_ShouldMarkInactiveWithEndTime()
    {
        // Arrange
        var conversation = new Conversation
        {
            Platform = "Web",
            UserId = "end-user",
            Title = "End Test",
            IsActive = true,
            StartedAt = DateTime.UtcNow.AddHours(-1),
            PersonalityProfileId = this._testPersonalityProfileId
        };
        this.Context.Conversations.Add(conversation);
        await this.Context.SaveChangesAsync();

        // Act
        var result = await this._service.EndConversationAsync(conversation.Id);

        // Assert
        result.Should().NotBeNull("should return updated conversation");
        result.IsActive.Should().Be(false, "should mark conversation as inactive");

        var updatedConversation = await this.Context.Conversations.FindAsync(conversation.Id);
        updatedConversation.Should().NotBeNull();
        updatedConversation!.IsActive.Should().Be(false, "should mark as inactive");
        updatedConversation.EndedAt.Should().NotBeNull("should set end time");
        updatedConversation.EndedAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(1), "should set recent end time");
    }

    [Fact]
    public async Task EndConversationAsync_NonExistentConversation_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        await FluentActions.Invoking(() => this._service.EndConversationAsync(nonExistentId))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*not found*");
    }

}
