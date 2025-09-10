using DigitalMe.Data.Entities;

namespace DigitalMe.Tests.Unit.Builders;

public class ConversationBuilder
{
    private Conversation _conversation;

    public ConversationBuilder()
    {
        _conversation = new Conversation();
    }

    public static ConversationBuilder Create() => new();

    public ConversationBuilder WithId(Guid id)
    {
        _conversation.Id = id;
        return this;
    }

    public ConversationBuilder WithTitle(string title)
    {
        _conversation.Title = title;
        return this;
    }

    public ConversationBuilder WithPlatform(string platform)
    {
        _conversation.Platform = platform;
        return this;
    }

    public ConversationBuilder WithUserId(string userId)
    {
        _conversation.UserId = userId;
        return this;
    }

    public ConversationBuilder WithStartedAt(DateTime startedAt)
    {
        _conversation.StartedAt = startedAt;
        return this;
    }

    public ConversationBuilder WithEndedAt(DateTime? endedAt)
    {
        _conversation.EndedAt = endedAt;
        return this;
    }

    public ConversationBuilder WithIsActive(bool isActive)
    {
        _conversation.IsActive = isActive;
        return this;
    }

    public ConversationBuilder WithMessage(Message message)
    {
        _conversation.Messages.Add(message);
        return this;
    }

    public ConversationBuilder WithMessages(ICollection<Message> messages)
    {
        _conversation.Messages = messages;
        return this;
    }

    public ConversationBuilder IsActive(bool isActive)
    {
        _conversation.IsActive = isActive;
        return this;
    }

    public ConversationBuilder ForTelegram(string userId)
    {
        _conversation.Platform = "Telegram";
        _conversation.UserId = userId;
        return this;
    }

    public Conversation Build() => _conversation;

    public static Conversation Default() => Create()
        .WithTitle("Default Chat Session")
        .WithPlatform("web")
        .WithUserId("user-123")
        .Build();

    public static Conversation WebChat() => Create()
        .WithTitle("Web Chat with Ivan")
        .WithPlatform("web")
        .WithUserId("ivan-user")
        .Build();

    public static Conversation TelegramChat() => Create()
        .WithTitle("Telegram Conversation")
        .WithPlatform("telegram")
        .WithUserId("telegram-456")
        .Build();

    public static Conversation CompletedConversation() => Create()
        .WithTitle("Completed Support Session")
        .WithPlatform("web")
        .WithUserId("support-789")
        .WithIsActive(false)
        .WithEndedAt(DateTime.UtcNow.AddHours(-1))
        .Build();
}
