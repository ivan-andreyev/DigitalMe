namespace DigitalMe.DTOs;

public record PersonalityTraitDto
{
    public Guid Id { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double Weight { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreatePersonalityTraitDto
{
    public string Category { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double Weight { get; init; } = 1.0;
}