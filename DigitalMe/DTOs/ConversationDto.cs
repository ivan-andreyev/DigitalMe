namespace DigitalMe.DTOs;

public record ConversationDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Platform { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public DateTime StartedAt { get; init; }
    public DateTime? EndedAt { get; init; }
    public bool IsActive { get; init; }
    public List<MessageDto> Messages { get; init; } = new();
}

public record CreateConversationDto
{
    public string Title { get; init; } = string.Empty;
    public string Platform { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
}