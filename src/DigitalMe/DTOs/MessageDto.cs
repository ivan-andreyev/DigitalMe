namespace DigitalMe.DTOs;

public record MessageDto
{
    public Guid Id { get; init; }
    public Guid ConversationId { get; init; }
    public string Role { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}

public record CreateMessageDto
{
    public string Role { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public Dictionary<string, object>? Metadata { get; init; }
}

public record ChatRequestDto
{
    public string Message { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string ConversationId { get; init; } = string.Empty;
    public string Platform { get; init; } = "Web";
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
