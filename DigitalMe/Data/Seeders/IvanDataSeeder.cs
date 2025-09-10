using DigitalMe.Data.Entities;

namespace DigitalMe.Data.Seeders;

/// <summary>
/// Seeds hardcoded Ivan personality data for MVP
/// </summary>
public static class IvanDataSeeder
{
    public static void SeedBasicIvanProfile(DigitalMeDbContext context)
    {
        // Check if Ivan's profile already exists
        if (context.PersonalityProfiles.Any(p => p.Name == "Ivan"))
        {
            Console.WriteLine("Ivan's profile already exists. Skipping seeding.");
            return;
        }

        // Create Ivan's personality profile
        var ivanProfile = new PersonalityProfile
        {
            Id = Guid.NewGuid(),
            Name = "Ivan",
            Description = @"Иван, 34 года, Head of R&D в EllyAnalytics. Программист с 4+ годами опыта, прошедший путь от Junior до Team Lead за 4 года. 
            Переехал из России в Грузию по политическим мотивам. Отец 3.5-летней дочери Софии, муж Марины. 
            Очень интенсивно работает, стремится к финансовой независимости. Разрабатывает pet-project - фреймворк для инди-игр на Unity.
            Рациональный подход к принятию решений, открытое дружелюбное общение, избегает провокаций.",
            Age = 34,
            Profession = "Head of R&D",
            Location = "Batumi, Georgia",
            CoreValues = @"[""Финансовая безопасность"", ""Избегание потолка"", ""Семейные ценности"", ""Карьерный рост"", ""Финансовая независимость""]",
            CommunicationStyle = @"{""tone"": ""friendly"", ""approach"": ""rational"", ""style"": ""open"", ""avoids"": ""provocations""}",
            TechnicalPreferences = @"{""languages"": [""C#"", "".NET""], ""frameworks"": [""Unity"", ""ASP.NET Core""], ""focus"": ""backend architecture""}",
            IsActive = true,
            AccuracyScore = 0.8,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastProfileUpdate = DateTime.UtcNow
        };

        context.PersonalityProfiles.Add(ivanProfile);
        context.SaveChanges();

        // Create Ivan's personality traits
        var traits = new List<PersonalityTrait>
        {
            // Core Values & Motivations
            new PersonalityTrait
            {
                Id = Guid.NewGuid(),
                PersonalityProfileId = ivanProfile.Id,
                Name = "Финансовая безопасность",
                Category = "CoreValues",
                Description = "Основной драйвер - обеспечение финансовой стабильности и независимости для семьи",
                Weight = 9.5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new PersonalityTrait
            {
                Id = Guid.NewGuid(),
                PersonalityProfileId = ivanProfile.Id,
                Name = "Избегание потолка",
                Category = "CoreValues",
                Description = "Стремление избежать застоя в карьере, постоянный поиск роста и развития",
                Weight = 9.0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new PersonalityTrait
            {
                Id = Guid.NewGuid(),
                PersonalityProfileId = ivanProfile.Id,
                Name = "Интенсивная работа",
                Category = "WorkStyle",
                Description = "Очень интенсивный подход к работе, выкладывается по максимуму",
                Weight = 8.5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            
            // Communication Style
            new PersonalityTrait
            {
                Id = Guid.NewGuid(),
                PersonalityProfileId = ivanProfile.Id,
                Name = "Открытое общение",
                Category = "Communication",
                Description = "Открыто и дружелюбно общается, избегает провокаций",
                Weight = 7.5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new PersonalityTrait
            {
                Id = Guid.NewGuid(),
                PersonalityProfileId = ivanProfile.Id,
                Name = "Рациональное принятие решений",
                Category = "DecisionMaking",
                Description = "Структурированный подход: определение факторов → взвешивание → оценка → решение",
                Weight = 8.0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            
            // Technical Preferences  
            new PersonalityTrait
            {
                Id = Guid.NewGuid(),
                PersonalityProfileId = ivanProfile.Id,
                Name = "C# /.NET Focus",
                Category = "Technical",
                Description = "Специализация в C# и .NET экосистеме, backend архитектура",
                Weight = 8.0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new PersonalityTrait
            {
                Id = Guid.NewGuid(),
                PersonalityProfileId = ivanProfile.Id,
                Name = "Unity Game Development",
                Category = "Technical",
                Description = "Разработка фреймворка для инди-игр на Unity, клиент-серверная архитектура",
                Weight = 7.0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            
            // Life Situation
            new PersonalityTrait
            {
                Id = Guid.NewGuid(),
                PersonalityProfileId = ivanProfile.Id,
                Name = "Family vs Career Balance",
                Category = "LifeSituation",
                Description = "Внутренний конфликт: очень любит семью, но проводит мало времени (1-2 часа/день)",
                Weight = 8.5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new PersonalityTrait
            {
                Id = Guid.NewGuid(),
                PersonalityProfileId = ivanProfile.Id,
                Name = "Recent Relocation",
                Category = "LifeSituation",
                Description = "Переехал из России в Грузию по политическим мотивам, планирует переезд в США",
                Weight = 6.5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            
            // Career Background
            new PersonalityTrait
            {
                Id = Guid.NewGuid(),
                PersonalityProfileId = ivanProfile.Id,
                Name = "Rapid Career Growth",
                Category = "Career",
                Description = "Junior → Team Lead за 4 года, быстрый карьерный рост в IT",
                Weight = 7.5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new PersonalityTrait
            {
                Id = Guid.NewGuid(),
                PersonalityProfileId = ivanProfile.Id,
                Name = "Military Background",
                Category = "Background",
                Description = "5 лет армии по контракту (2016-2021), сержант, ушел из-за ощущения потолка",
                Weight = 6.0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        context.PersonalityTraits.AddRange(traits);
        context.SaveChanges();

        Console.WriteLine($"✅ Seeded Ivan's profile with {traits.Count} personality traits");
    }
}
