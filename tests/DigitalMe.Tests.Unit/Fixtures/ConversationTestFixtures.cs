using DigitalMe.Data.Entities;
using DigitalMe.Tests.Unit.Builders;

namespace DigitalMe.Tests.Unit.Fixtures;

public static class ConversationTestFixtures
{
    public static Conversation CreateCompleteConversationWithMessages()
    {
        var conversationId = Guid.NewGuid();
        
        var conversation = ConversationBuilder.Create()
            .WithId(conversationId)
            .WithTitle("Technical Discussion - SOLID Principles")
            .WithPlatform("web")
            .WithUserId("developer-123")
            .WithStartedAt(DateTime.UtcNow.AddHours(-2))
            .Build();

        var messages = new List<Message>
        {
            MessageBuilder.Create()
                .WithConversationId(conversationId)
                .WithRole("system")
                .WithContent("Conversation started with Ivan's digital clone")
                .WithMetadata("""{"event": "session_start", "personality_profile": "ivan_technical"}""")
                .WithTimestamp(DateTime.UtcNow.AddHours(-2))
                .Build(),

            MessageBuilder.Create()
                .WithConversationId(conversationId)
                .WithRole("user")
                .WithContent("Can you explain SOLID principles and how they apply in C# development?")
                .WithMetadata("""{"platform": "web", "user_agent": "Mozilla/5.0", "ip": "192.168.1.100"}""")
                .WithTimestamp(DateTime.UtcNow.AddHours(-2).AddMinutes(1))
                .Build(),

            MessageBuilder.Create()
                .WithConversationId(conversationId)
                .WithRole("assistant")
                .WithContent("SOLID principles are fundamental design principles that make software more maintainable and extensible. In C#, they're particularly powerful because of the language's strong typing and OOP features. Let me break them down...")
                .WithMetadata("""{"response_time": 1200, "tokens_used": 180, "personality_traits_applied": ["structured_direct", "technical_expertise"]}""")
                .WithTimestamp(DateTime.UtcNow.AddHours(-2).AddMinutes(2))
                .Build(),

            MessageBuilder.Create()
                .WithConversationId(conversationId)
                .WithRole("user")
                .WithContent("That's very clear! Can you show a practical example of Dependency Inversion in ASP.NET Core?")
                .WithMetadata("""{"platform": "web", "follow_up": true}""")
                .WithTimestamp(DateTime.UtcNow.AddHours(-1).AddMinutes(30))
                .Build(),

            MessageBuilder.Create()
                .WithConversationId(conversationId)
                .WithRole("assistant")
                .WithContent("Absolutely! In ASP.NET Core, Dependency Inversion is everywhere. Here's a practical example with a service and repository pattern...")
                .WithMetadata("""{"response_time": 900, "tokens_used": 220, "code_examples": true}""")
                .WithTimestamp(DateTime.UtcNow.AddHours(-1).AddMinutes(31))
                .Build()
        };

        conversation.Messages = messages;
        return conversation;
    }

    public static Conversation CreateSimpleConversation()
    {
        return ConversationBuilder.Create()
            .WithTitle("Quick Question")
            .WithPlatform("telegram")
            .WithUserId("quick-user-456")
            .WithMessage(MessageBuilder.UserMessage())
            .WithMessage(MessageBuilder.AssistantMessage())
            .Build();
    }

    public static Conversation CreateLongRunningConversation()
    {
        var conversationId = Guid.NewGuid();
        var conversation = ConversationBuilder.Create()
            .WithId(conversationId)
            .WithTitle("Extended Architecture Discussion")
            .WithPlatform("web")
            .WithUserId("architect-789")
            .WithStartedAt(DateTime.UtcNow.AddDays(-1))
            .Build();

        var messages = new List<Message>();
        
        for (int i = 0; i < 20; i++)
        {
            messages.Add(MessageBuilder.Create()
                .WithConversationId(conversationId)
                .WithRole(i % 2 == 0 ? "user" : "assistant")
                .WithContent($"Message {i + 1}: This is part of a long conversation about software architecture...")
                .WithMetadata($"{{\"message_number\": {i + 1}, \"conversation_length\": \"extended\"}}")
                .WithTimestamp(DateTime.UtcNow.AddDays(-1).AddMinutes(i * 5))
                .Build());
        }

        conversation.Messages = messages;
        return conversation;
    }

    public static IEnumerable<Conversation> CreateMultipleConversations()
    {
        yield return CreateCompleteConversationWithMessages();
        yield return CreateSimpleConversation();
        yield return CreateLongRunningConversation();
        
        yield return ConversationBuilder.CompletedConversation();
        yield return ConversationBuilder.WebChat();
        yield return ConversationBuilder.TelegramChat();
    }

    public static (Conversation conversation, ICollection<Message> messages) CreateConversationWithSeparateMessages()
    {
        var conversation = ConversationBuilder.WebChat();
        var messages = new List<Message>
        {
            MessageBuilder.Create().WithConversationId(conversation.Id).WithRole("system").WithContent("Conversation started").Build(),
            MessageBuilder.Create().WithConversationId(conversation.Id).WithRole("user").WithContent("Can you help me understand SOLID principles?").Build(),
            MessageBuilder.Create().WithConversationId(conversation.Id).WithRole("assistant").WithContent("SOLID principles are five design principles...").Build()
        };

        return (conversation, messages);
    }

    public static Conversation CreateConversationForTesting()
    {
        return ConversationBuilder.Create()
            .WithTitle("Test Conversation")
            .WithPlatform("test")
            .WithUserId("test-user")
            .WithStartedAt(DateTime.UtcNow.AddMinutes(-30))
            .WithIsActive(true)
            .Build();
    }
}