using DigitalMe.Data.Entities;
using System.Collections.Generic;

namespace DigitalMe.Tests.Unit.Builders;

public class PersonalityProfileBuilder
{
    private readonly PersonalityProfile _profile;

    public PersonalityProfileBuilder()
    {
        _profile = new PersonalityProfile();
    }

    public static PersonalityProfileBuilder Create()
    {
        return new PersonalityProfileBuilder();
    }

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

    public PersonalityProfileBuilder WithTraits(ICollection<PersonalityTrait> traits)
    {
        _profile.Traits = traits;
        return this;
    }

    public PersonalityProfileBuilder WithCreatedAt(DateTime createdAt)
    {
        _profile.CreatedAt = createdAt;
        return this;
    }

    public PersonalityProfileBuilder WithAge(int age)
    {
        _profile.Age = age;
        return this;
    }

    public PersonalityProfileBuilder WithTags(List<PersonalityTrait> traits)
    {
        _profile.Traits = traits;
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
        return _profile;
    }
}