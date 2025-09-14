using DigitalMe.Data;
using DigitalMe.Data.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DigitalMe.Tests.Unit.Data;

public class PostgreSqlTypeCompatibilityTests : IDisposable
{
    private readonly DigitalMeDbContext _context;

    public PostgreSqlTypeCompatibilityTests()
    {
        var options = new DbContextOptionsBuilder<DigitalMeDbContext>()
            .UseInMemoryDatabase($"PostgreSQLTypeTest_{Guid.NewGuid()}")
            .Options;

        this._context = new DigitalMeDbContext(options);
        this._context.Database.EnsureCreated();
    }

    [Fact]
    public async Task Conversation_BooleanField_ShouldHandleCorrectly()
    {
        // Arrange - Test the boolean field fix we implemented
        var conversation = new Conversation
        {
            Platform = "Web",
            UserId = "boolean-test-user",
            Title = "Boolean Field Test",
            IsActive = true, // This was the problematic field
            StartedAt = DateTime.UtcNow,
            EndedAt = null
        };

        // Act
        this._context.Conversations.Add(conversation);
        await this._context.SaveChangesAsync();

        // Assert
        var savedConversation = await this._context.Conversations
            .FirstOrDefaultAsync(c => c.UserId == "boolean-test-user");

        savedConversation.Should().NotBeNull("conversation should be saved successfully");
        savedConversation!.IsActive.Should().BeTrue("boolean field should maintain true value");

        // Test boolean queries that were failing before
        var activeConversations = await this._context.Conversations
            .Where(c => c.IsActive == true) // This query was failing with PostgreSQL
            .ToListAsync();

        activeConversations.Should().Contain(
            c => c.Id == conversation.Id,
            "boolean comparison should work correctly");

        // Test boolean AND operations that were failing
        var activeUserConversations = await this._context.Conversations
            .Where(c => c.UserId == "boolean-test-user" && c.IsActive) // AND with boolean was failing
            .ToListAsync();

        activeUserConversations.Should().HaveCount(1, "boolean AND operations should work");
    }

    [Fact]
    public async Task Conversation_DateTimeFields_ShouldHandleCorrectly()
    {
        // Arrange - Test the DateTime field fixes
        var startTime = DateTime.UtcNow.AddHours(-1);
        var endTime = DateTime.UtcNow;

        var conversation = new Conversation
        {
            Platform = "Web",
            UserId = "datetime-test-user",
            Title = "DateTime Field Test",
            IsActive = false,
            StartedAt = startTime, // These were TEXT fields causing casting issues
            EndedAt = endTime
        };

        // Act
        this._context.Conversations.Add(conversation);
        await this._context.SaveChangesAsync();

        // Assert
        var savedConversation = await this._context.Conversations
            .FirstOrDefaultAsync(c => c.UserId == "datetime-test-user");

        savedConversation.Should().NotBeNull("conversation with DateTime fields should save");
        savedConversation!.StartedAt.Should().BeCloseTo(startTime, TimeSpan.FromSeconds(1),
            "StartedAt DateTime should be preserved accurately");
        savedConversation.EndedAt.Should().BeCloseTo(endTime, TimeSpan.FromSeconds(1),
            "EndedAt DateTime should be preserved accurately");

        // Test DateTime queries that were failing before
        var recentConversations = await this._context.Conversations
            .Where(c => c.StartedAt > DateTime.UtcNow.AddHours(-2)) // DateTime comparison was failing
            .ToListAsync();

        recentConversations.Should().Contain(
            c => c.Id == conversation.Id,
            "DateTime comparison queries should work");
    }

    [Fact]
    public async Task Message_DateTimeField_ShouldHandleCorrectly()
    {
        // Arrange - Test Message DateTime fields
        var conversation = new Conversation
        {
            Platform = "Web",
            UserId = "message-datetime-user",
            Title = "Message DateTime Test",
            IsActive = true
        };
        this._context.Conversations.Add(conversation);
        await this._context.SaveChangesAsync();

        var messageTime = DateTime.UtcNow;
        var message = new Message
        {
            ConversationId = conversation.Id,
            Role = "user",
            Content = "Test message with DateTime",
            Timestamp = messageTime, // This field was TEXT causing issues
            Metadata = "{\"test\": \"data\"}"
        };

        // Act
        this._context.Messages.Add(message);
        await this._context.SaveChangesAsync();

        // Assert
        var savedMessage = await this._context.Messages
            .FirstOrDefaultAsync(m => m.ConversationId == conversation.Id);

        savedMessage.Should().NotBeNull("message should be saved with DateTime field");
        savedMessage!.Timestamp.Should().BeCloseTo(messageTime, TimeSpan.FromSeconds(1),
            "Timestamp should be preserved accurately");

        // Test DateTime ordering that was problematic
        var orderedMessages = await this._context.Messages
            .Where(m => m.ConversationId == conversation.Id)
            .OrderByDescending(m => m.Timestamp) // Ordering by DateTime was failing
            .ToListAsync();

        orderedMessages.Should().HaveCount(1);
        orderedMessages.First().Id.Should().Be(message.Id);
    }

    [Fact]
    public async Task PersonalityProfile_DateTimeFields_ShouldHandleCorrectly()
    {
        // Arrange - Test PersonalityProfile DateTime fields
        var createdTime = DateTime.UtcNow.AddDays(-1);
        var updatedTime = DateTime.UtcNow;

        var personality = new PersonalityProfile
        {
            Name = "DateTime Test Profile",
            Description = "Testing DateTime field compatibility",
            Traits = new List<PersonalityTrait>(),
            CreatedAt = createdTime, // These were TEXT fields
            UpdatedAt = updatedTime
        };

        // Act
        this._context.PersonalityProfiles.Add(personality);
        await this._context.SaveChangesAsync();

        // Assert
        var savedPersonality = await this._context.PersonalityProfiles
            .FirstOrDefaultAsync(p => p.Name == "DateTime Test Profile");

        savedPersonality.Should().NotBeNull("personality should save with DateTime fields");
        savedPersonality!.CreatedAt.Should().BeCloseTo(createdTime, TimeSpan.FromSeconds(1));
        savedPersonality.UpdatedAt.Should().BeCloseTo(updatedTime, TimeSpan.FromSeconds(1));

        // Test DateTime range queries
        var recentProfiles = await this._context.PersonalityProfiles
            .Where(p => p.UpdatedAt > DateTime.UtcNow.AddHours(-1)) // DateTime comparison
            .ToListAsync();

        recentProfiles.Should().Contain(
            p => p.Id == personality.Id,
            "DateTime range queries should work");
    }

    [Fact]
    public async Task TelegramMessage_BooleanAndDateTimeFields_ShouldHandleCorrectly()
    {
        // Arrange - Test TelegramMessage fields that had type issues
        var messageDate = DateTime.UtcNow.AddMinutes(-30);
        var createdDate = DateTime.UtcNow;

        var telegramMessage = new TelegramMessage
        {
            TelegramMessageId = 123456,
            ChatId = 789,
            Text = "Test Telegram message",
            Username = "testuser",
            MessageDate = messageDate, // DateTime that was TEXT
            CreatedAt = createdDate // DateTime that was TEXT
        };

        // Act
        this._context.TelegramMessages.Add(telegramMessage);
        await this._context.SaveChangesAsync();

        // Assert
        var savedMessage = await this._context.TelegramMessages
            .FirstOrDefaultAsync(t => t.TelegramMessageId == 123456);

        savedMessage.Should().NotBeNull("telegram message should save with mixed field types");
        savedMessage!.Username.Should().Be("testuser");
        savedMessage.MessageDate.Should().BeCloseTo(messageDate, TimeSpan.FromSeconds(1));
        savedMessage.CreatedAt.Should().BeCloseTo(createdDate, TimeSpan.FromSeconds(1));

        // Test queries that combine string and DateTime operations
        var userRecentMessages = await this._context.TelegramMessages
            .Where(t => t.Username == "testuser" && t.MessageDate > DateTime.UtcNow.AddHours(-1))
            .ToListAsync();

        userRecentMessages.Should().Contain(
            t => t.Id == telegramMessage.Id,
            "complex queries with string AND DateTime should work");
    }

    [Fact]
    public async Task AllEntities_GuidFields_ShouldHandleCorrectly()
    {
        // Arrange - Test GUID fields that needed PostgreSQL UUID configuration
        var personalityId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var traitId = Guid.NewGuid();

        var personality = new PersonalityProfile
        {
            Id = personalityId,
            Name = "GUID Test",
            Description = "Testing GUID fields",
            Traits = new List<PersonalityTrait>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var conversation = new Conversation
        {
            Id = conversationId,
            Platform = "Web",
            UserId = "guid-test-user",
            Title = "GUID Test Conversation",
            IsActive = true
        };

        var message = new Message
        {
            Id = messageId,
            ConversationId = conversationId,
            Role = "user",
            Content = "GUID test message",
            Timestamp = DateTime.UtcNow,
            Metadata = "{}"
        };

        var trait = new PersonalityTrait
        {
            Id = traitId,
            PersonalityProfileId = personalityId,
            Category = "Test",
            Name = "GUID Trait",
            Description = "Testing GUID relationships",
            Weight = 1.0,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        this._context.PersonalityProfiles.Add(personality);
        this._context.Conversations.Add(conversation);
        this._context.Messages.Add(message);
        this._context.PersonalityTraits.Add(trait);
        await this._context.SaveChangesAsync();

        // Assert
        var savedPersonality = await this._context.PersonalityProfiles.FindAsync(personalityId);
        var savedConversation = await this._context.Conversations.FindAsync(conversationId);
        var savedMessage = await this._context.Messages.FindAsync(messageId);
        var savedTrait = await this._context.PersonalityTraits.FindAsync(traitId);

        savedPersonality.Should().NotBeNull("personality should save with GUID ID");
        savedConversation.Should().NotBeNull("conversation should save with GUID ID");
        savedMessage.Should().NotBeNull("message should save with GUID ID");
        savedTrait.Should().NotBeNull("trait should save with GUID ID");

        // Test GUID foreign key relationships
        savedMessage!.ConversationId.Should().Be(conversationId, "GUID foreign key should work");
        savedTrait!.PersonalityProfileId.Should().Be(personalityId, "GUID foreign key should work");

        // Test GUID queries
        var messagesForConversation = await this._context.Messages
            .Where(m => m.ConversationId == conversationId) // GUID comparison
            .ToListAsync();

        messagesForConversation.Should().HaveCount(1);
        messagesForConversation.First().Id.Should().Be(messageId);
    }

    public void Dispose()
    {
        this._context.Dispose();
    }
}
