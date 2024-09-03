using DigitalMe.Models;

namespace DigitalMe.Tests.Unit.Builders;

public class MessageBuilder
{
    private Message _message;

    public MessageBuilder()
    {
        _message = new Message();
    }

    public static MessageBuilder Create() => new();

    public MessageBuilder WithId(Guid id)
    {
        _message.Id = id;
        return this;
    }

    public MessageBuilder WithConversationId(Guid conversationId)
    {
        _message.ConversationId = conversationId;
        return this;
    }

    public MessageBuilder WithRole(string role)
    {
        _message.Role = role;
        return this;
    }

    public MessageBuilder WithContent(string content)
    {
        _message.Content = content;
        return this;
    }

    public MessageBuilder WithMetadata(string metadata)
    {
        _message.Metadata = metadata;
        return this;
    }

    public MessageBuilder WithTimestamp(DateTime timestamp)
    {
        _message.Timestamp = timestamp;
        return this;
    }

    public MessageBuilder WithConversation(Conversation conversation)
    {
        _message.Conversation = conversation;
        _message.ConversationId = conversation.Id;
        return this;
    }

    public Message Build() => _message;

    public static Message Default() => Create()
        .WithRole("user")
        .WithContent("Hello, how are you?")
        .WithMetadata("{}")
        .Build();

    public static Message UserMessage() => Create()
        .WithRole("user")
        .WithContent("Can you help me understand SOLID principles?")
        .WithMetadata("""{"platform": "web", "ip": "192.168.1.1"}""")
        .Build();

    public static Message AssistantMessage() => Create()
        .WithRole("assistant")
        .WithContent("SOLID principles are five design principles that make software more understandable, flexible and maintainable...")
        .WithMetadata("""{"response_time": 1200, "tokens_used": 150}""")
        .Build();

    public static Message SystemMessage() => Create()
        .WithRole("system")
        .WithContent("Conversation started")
        .WithMetadata("""{"event": "session_start"}""")
        .Build();
}