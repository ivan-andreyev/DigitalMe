using DigitalMe.Data.Entities;

namespace DigitalMe.Tests.Unit.Builders;

public class PersonalityTraitBuilder
{
    private PersonalityTrait _trait;

    public PersonalityTraitBuilder()
    {
        _trait = new PersonalityTrait();
    }

    public static PersonalityTraitBuilder Create() => new();

    public PersonalityTraitBuilder WithId(Guid id)
    {
        _trait.Id = id;
        return this;
    }

    public PersonalityTraitBuilder WithPersonalityProfileId(Guid personalityProfileId)
    {
        _trait.PersonalityProfileId = personalityProfileId;
        return this;
    }

    public PersonalityTraitBuilder WithCategory(string category)
    {
        _trait.Category = category;
        return this;
    }

    public PersonalityTraitBuilder WithName(string name)
    {
        _trait.Name = name;
        return this;
    }

    public PersonalityTraitBuilder WithDescription(string description)
    {
        _trait.Description = description;
        return this;
    }

    public PersonalityTraitBuilder WithWeight(double weight)
    {
        _trait.Weight = weight;
        return this;
    }

    public PersonalityTraitBuilder WithCreatedAt(DateTime createdAt)
    {
        _trait.CreatedAt = createdAt;
        return this;
    }

    public PersonalityTraitBuilder WithPersonalityProfile(PersonalityProfile profile)
    {
        _trait.PersonalityProfile = profile;
        _trait.PersonalityProfileId = profile.Id;
        return this;
    }

    public PersonalityTrait Build() => _trait;

    public static PersonalityTrait Default() => Create()
        .WithCategory("Communication")
        .WithName("Direct")
        .WithDescription("Prefers straightforward, honest communication")
        .WithWeight(0.8)
        .Build();

    public static PersonalityTrait TechnicalTrait() => Create()
        .WithCategory("Technical")
        .WithName("C# Expert")
        .WithDescription("Deep expertise in C# and .NET ecosystem")
        .WithWeight(0.9)
        .Build();

    public static PersonalityTrait LeadershipTrait() => Create()
        .WithCategory("Leadership")
        .WithName("Mentoring")
        .WithDescription("Enjoys teaching and developing team members")
        .WithWeight(0.7)
        .Build();
}
