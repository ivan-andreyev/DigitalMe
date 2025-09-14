using DigitalMe.Data.Entities;

namespace DigitalMe.Tests.Unit.Builders;

public class PersonalityTraitBuilder
{
    private PersonalityTrait _trait;

    public PersonalityTraitBuilder()
    {
        this._trait = new PersonalityTrait();
    }

    public static PersonalityTraitBuilder Create() => new();

    public PersonalityTraitBuilder WithId(Guid id)
    {
        this._trait.Id = id;
        return this;
    }

    public PersonalityTraitBuilder WithPersonalityProfileId(Guid personalityProfileId)
    {
        this._trait.PersonalityProfileId = personalityProfileId;
        return this;
    }

    public PersonalityTraitBuilder WithCategory(string category)
    {
        this._trait.Category = category;
        return this;
    }

    public PersonalityTraitBuilder WithName(string name)
    {
        this._trait.Name = name;
        return this;
    }

    public PersonalityTraitBuilder WithDescription(string description)
    {
        this._trait.Description = description;
        return this;
    }

    public PersonalityTraitBuilder WithWeight(double weight)
    {
        this._trait.Weight = weight;
        return this;
    }

    public PersonalityTraitBuilder WithCreatedAt(DateTime createdAt)
    {
        this._trait.CreatedAt = createdAt;
        return this;
    }

    public PersonalityTraitBuilder WithPersonalityProfile(PersonalityProfile profile)
    {
        this._trait.PersonalityProfile = profile;
        this._trait.PersonalityProfileId = profile.Id;
        return this;
    }

    public PersonalityTrait Build() => this._trait;

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
