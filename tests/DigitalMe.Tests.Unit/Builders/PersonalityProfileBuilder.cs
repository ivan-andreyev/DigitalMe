namespace DigitalMe.Tests.Unit.Builders;

using System.Collections.Generic;
using DigitalMe.Data.Entities;

public class PersonalityProfileBuilder
{
    private readonly PersonalityProfile _profile;

    public PersonalityProfileBuilder()
    {
        this._profile = new PersonalityProfile();
    }

    public static PersonalityProfileBuilder Create()
    {
        return new PersonalityProfileBuilder();
    }

    public PersonalityProfileBuilder WithId(Guid id)
    {
        this._profile.Id = id;
        return this;
    }

    public PersonalityProfileBuilder WithName(string name)
    {
        this._profile.Name = name;
        return this;
    }

    public PersonalityProfileBuilder WithDescription(string description)
    {
        this._profile.Description = description;
        return this;
    }

    public PersonalityProfileBuilder WithTraits(ICollection<PersonalityTrait> traits)
    {
        this._profile.Traits = traits;
        return this;
    }

    public PersonalityProfileBuilder WithCreatedAt(DateTime createdAt)
    {
        this._profile.CreatedAt = createdAt;
        return this;
    }

    public PersonalityProfileBuilder WithAge(int age)
    {
        this._profile.Age = age;
        return this;
    }

    public PersonalityProfileBuilder WithTags(List<PersonalityTrait> traits)
    {
        this._profile.Traits = traits;
        return this;
    }

    public static PersonalityProfileBuilder ForIvan()
    {
        var traits = new List<PersonalityTrait>
        {
            new PersonalityTrait(Guid.NewGuid(), "Communication", "Direct", "Прямолинейное общение без лишних слов", 8.0),
            new PersonalityTrait(Guid.NewGuid(), "Technical", "Expert", "Глубокие технические знания в .NET/C#", 9.0),
            new PersonalityTrait(Guid.NewGuid(), "Philosophy", "Independent", "Всем похуй - независимая позиция", 7.0)
        };

        return Create()
            .WithName("Ivan")
            .WithAge(34)
            .WithDescription("Программист, Head of R&D, прямолинейный и честный")
            .WithTraits(traits);
    }

    public static PersonalityProfileBuilder Default()
    {
        return Create()
            .WithName("Default Profile")
            .WithAge(30)
            .WithDescription("Default test personality profile");
    }

    public PersonalityProfile Build()
    {
        return this._profile;
    }
}
