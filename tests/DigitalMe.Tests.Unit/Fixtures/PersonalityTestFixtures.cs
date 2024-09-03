using DigitalMe.Models;
using DigitalMe.Tests.Unit.Builders;

namespace DigitalMe.Tests.Unit.Fixtures;

public static class PersonalityTestFixtures
{
    public static PersonalityProfile CreateCompleteIvanProfile()
    {
        var profileId = Guid.NewGuid();
        
        var profile = PersonalityProfileBuilder.Create()
            .WithId(profileId)
            .WithName("Ivan Digital Clone")
            .WithDescription("Complete digital representation of Ivan's personality, values, and behavioral patterns")
            .WithTraits("""
            {
                "core_values": ["efficiency", "quality", "family_first", "continuous_learning"],
                "personality_type": "analytical_leader",
                "communication_style": {
                    "directness": 0.8,
                    "structure": 0.9,
                    "empathy": 0.7,
                    "technical_depth": 0.9
                },
                "technical_preferences": {
                    "languages": ["C#", "TypeScript"],
                    "frameworks": [".NET", "React"],
                    "principles": ["clean_architecture", "SOLID", "DRY"],
                    "avoids": ["graphical_tools", "unnecessary_complexity"]
                },
                "work_style": {
                    "planning": 0.9,
                    "execution": 0.8,
                    "mentoring": 0.8,
                    "innovation": 0.7
                },
                "decision_making": {
                    "data_driven": 0.9,
                    "pragmatic": 0.8,
                    "risk_assessment": 0.8
                }
            }
            """)
            .WithCreatedAt(DateTime.UtcNow.AddDays(-30))
            .WithUpdatedAt(DateTime.UtcNow)
            .Build();

        // Add detailed traits
        profile.PersonalityTraits = new List<PersonalityTrait>
        {
            PersonalityTraitBuilder.Create()
                .WithPersonalityProfileId(profileId)
                .WithCategory("Communication")
                .WithName("Structured Direct")
                .WithDescription("Communicates in well-organized, direct manner with clear outcomes")
                .WithWeight(0.9)
                .Build(),

            PersonalityTraitBuilder.Create()
                .WithPersonalityProfileId(profileId)
                .WithCategory("Technical")
                .WithName("C# Expert")
                .WithDescription("Deep expertise in C#/.NET ecosystem with strong architectural knowledge")
                .WithWeight(0.95)
                .Build(),

            PersonalityTraitBuilder.Create()
                .WithPersonalityProfileId(profileId)
                .WithCategory("Leadership")
                .WithName("Technical Mentoring")
                .WithDescription("Prefers teaching through practical examples and hands-on guidance")
                .WithWeight(0.8)
                .Build(),

            PersonalityTraitBuilder.Create()
                .WithPersonalityProfileId(profileId)
                .WithCategory("Values")
                .WithName("Family First")
                .WithDescription("Strong priority on family time and work-life balance")
                .WithWeight(0.95)
                .Build(),

            PersonalityTraitBuilder.Create()
                .WithPersonalityProfileId(profileId)
                .WithCategory("Work Style")
                .WithName("Pragmatic Perfectionist")
                .WithDescription("Seeks high quality while remaining practical about constraints")
                .WithWeight(0.8)
                .Build()
        };

        return profile;
    }

    public static PersonalityProfile CreateMinimalProfile()
    {
        return PersonalityProfileBuilder.Create()
            .WithName("Basic Profile")
            .WithDescription("Minimal profile for basic testing")
            .WithTraits("{}")
            .Build();
    }

    public static IEnumerable<PersonalityProfile> CreateMultipleProfiles()
    {
        yield return CreateCompleteIvanProfile();
        yield return CreateMinimalProfile();
        
        yield return PersonalityProfileBuilder.Create()
            .WithName("Test Manager Profile")
            .WithDescription("Profile focused on management and team coordination")
            .WithTraits("""
            {
                "role": "manager",
                "focus": ["team_building", "strategic_planning", "stakeholder_management"],
                "communication_style": "collaborative"
            }
            """)
            .Build();

        yield return PersonalityProfileBuilder.Create()
            .WithName("Developer Profile")
            .WithDescription("Profile focused on hands-on development")
            .WithTraits("""
            {
                "role": "developer",
                "focus": ["coding", "architecture", "problem_solving"],
                "communication_style": "technical"
            }
            """)
            .Build();
    }

    public static (PersonalityProfile profile, ICollection<PersonalityTrait> traits) CreateProfileWithTraits()
    {
        var profile = PersonalityProfileBuilder.ForIvan();
        var traits = new List<PersonalityTrait>
        {
            PersonalityTraitBuilder.Create().WithPersonalityProfileId(profile.Id).WithCategory("Technical").WithName("C# Expert").WithDescription("Deep expertise in C# development").WithWeight(0.9).Build(),
            PersonalityTraitBuilder.Create().WithPersonalityProfileId(profile.Id).WithCategory("Leadership").WithName("Mentoring").WithDescription("Enjoys teaching and developing team members").WithWeight(0.7).Build(),
            PersonalityTraitBuilder.Create().WithPersonalityProfileId(profile.Id).WithCategory("Communication").WithName("Direct").WithDescription("Prefers straightforward, honest communication").WithWeight(0.8).Build()
        };

        profile.PersonalityTraits = traits;
        return (profile, traits);
    }
}