namespace DigitalMe.Web.Models;

public enum MessageRole
{
    User,
    Assistant,
    System
}

public class ChatMessage
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public MessageRole Role { get; set; }
    public DateTime Timestamp { get; set; }
    public string ConversationId { get; set; } = string.Empty;
    public Dictionary<string, object>? Metadata { get; set; }
}

public class ChatResponseDto
{
    public string? MessageId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class ChatMessageDto
{
    public string Id { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string ConversationId { get; set; } = string.Empty;
}

public class PersonalityState
{
    public bool IsOnline { get; set; } = true;
    public string? CurrentMood { get; set; } = "Focused";
    public string? CurrentActivity { get; set; } = "Available for chat";
    public string? MoodEmoji { get; set; } = "🎯";
    public Dictionary<string, int> Traits { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.Now;
}