using DigitalMe.Models;

namespace DigitalMe.Tests.Unit.Builders;

public class PersonalityProfileBuilder
{
    private PersonalityProfile _profile;

    public PersonalityProfileBuilder()
    {
        _profile = new PersonalityProfile();
    }

    public static PersonalityProfileBuilder Create() => new();

    public PersonalityProfileBuilder WithId(Guid id)
    {
        _profile.Id = id;
        return this;
    }

    public PersonalityProfileBuilder WithName(string name)
    {
        _profile.Name = name;
        return this;
    }

    public PersonalityProfileBuilder WithDescription(string description)
    {
        _profile.Description = description;
        return this;
    }

    public PersonalityProfileBuilder WithTraits(string traits)
    {
        _profile.Traits = traits;
        return this;
    }

    public PersonalityProfileBuilder WithCreatedAt(DateTime createdAt)
    {
        _profile.CreatedAt = createdAt;
        return this;
    }

    public PersonalityProfileBuilder WithUpdatedAt(DateTime updatedAt)
    {
        _profile.UpdatedAt = updatedAt;
        return this;
    }

    public PersonalityProfileBuilder WithPersonalityTrait(PersonalityTrait trait)
    {
        _profile.PersonalityTraits.Add(trait);
        return this;
    }

    public PersonalityProfileBuilder WithPersonalityTraits(ICollection<PersonalityTrait> traits)
    {
        _profile.PersonalityTraits = traits;
        return this;
    }

    public PersonalityProfile Build() => _profile;

    public static PersonalityProfile Default() => Create()
        .WithName("Ivan Petrov")
        .WithDescription("Head of R&D, pragmatic programmer, focus on results")
        .WithTraits("""
        {
            "personality": "analytical",
            "communication_style": "direct",
            "technical_preferences": "C#, .NET, strongly typed",
            "decision_making": "data-driven"
        }
        """)
        .Build();

    public static PersonalityProfile ForIvan() => Create()
        .WithName("Ivan Digital Clone")
        .WithDescription("Digital representation of Ivan's personality and thinking patterns")
        .WithTraits("""
        {
            "core_values": ["efficiency", "quality", "family"],
            "communication_style": "structured_direct",
            "technical_stack": ["csharp", "dotnet", "clean_architecture"],
            "work_philosophy": "pragmatic_perfectionist",
            "leadership_style": "mentoring_technical_lead"
        }
        """)
        .Build();
}