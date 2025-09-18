namespace DigitalMe.Tests.Unit.Builders;

using DigitalMe.Data.Entities;

public class ConversationBuilder
{
    private Conversation _conversation;

    public ConversationBuilder()
    {
        this._conversation = new Conversation();
    }

    public static ConversationBuilder Create() => new ();

    public ConversationBuilder WithId(Guid id)
    {
        this._conversation.Id = id;
        return this;
    }

    public ConversationBuilder WithTitle(string title)
    {
        this._conversation.Title = title;
        return this;
    }

    public ConversationBuilder WithPlatform(string platform)
    {
        this._conversation.Platform = platform;
        return this;
    }

    public ConversationBuilder WithUserId(string userId)
    {
        this._conversation.UserId = userId;
        return this;
    }

    public ConversationBuilder WithStartedAt(DateTime startedAt)
    {
        this._conversation.StartedAt = startedAt;
        return this;
    }

    public ConversationBuilder WithEndedAt(DateTime? endedAt)
    {
        this._conversation.EndedAt = endedAt;
        return this;
    }

    public ConversationBuilder WithIsActive(bool isActive)
    {
        this._conversation.IsActive = isActive;
        return this;
    }

    public ConversationBuilder WithMessage(Message message)
    {
        this._conversation.Messages.Add(message);
        return this;
    }

    public ConversationBuilder WithMessages(ICollection<Message> messages)
    {
        this._conversation.Messages = messages;
        return this;
    }

    public ConversationBuilder IsActive(bool isActive)
    {
        this._conversation.IsActive = isActive;
        return this;
    }

    public ConversationBuilder ForTelegram(string userId)
    {
        this._conversation.Platform = "Telegram";
        this._conversation.UserId = userId;
        return this;
    }

    public Conversation Build() => this._conversation;

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
