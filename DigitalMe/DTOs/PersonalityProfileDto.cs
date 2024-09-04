namespace DigitalMe.DTOs;

public record PersonalityProfileDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public List<PersonalityTraitDto> Traits { get; init; } = new();
}

public record CreatePersonalityProfileDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

public record UpdatePersonalityProfileDto
{
    public string Description { get; init; } = string.Empty;
}