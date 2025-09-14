using DigitalMe.Data.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace DigitalMe.Services;

/// <summary>
/// Сервис для работы с профилем личности Ивана.
/// Обеспечивает загрузку данных личности и генерацию системных промптов для LLM.
/// </summary>
public interface IIvanPersonalityService
{
    /// <summary>
    /// Асинхронно загружает профиль личности Ивана.
    /// </summary>
    /// <returns>Объект PersonalityProfile с данными личности Ивана</returns>
    Task<PersonalityProfile> GetIvanPersonalityAsync();

    /// <summary>
    /// Генерирует системный промпт для LLM на основе профиля личности.
    /// </summary>
    /// <param name="personality">Профиль личности для генерации промпта</param>
    /// <returns>Системный промпт в виде строки</returns>
    string GenerateSystemPrompt(PersonalityProfile personality);

    /// <summary>
    /// Генерирует расширенный системный промпт с интеграцией реальных данных профиля.
    /// </summary>
    /// <returns>Улучшенный системный промпт с данными из файла профиля</returns>
    Task<string> GenerateEnhancedSystemPromptAsync();
}

/// <summary>
/// Реализация сервиса для работы с профилем личности Ивана.
/// Кэширует данные личности и предоставляет методы для генерации промптов.
/// </summary>
public class IvanPersonalityService : IIvanPersonalityService
{
    private readonly ILogger<IvanPersonalityService> _logger;
    private readonly IProfileDataParser _profileDataParser;
    private readonly IConfiguration _configuration;
    private PersonalityProfile? _cachedProfile;
    private ProfileData? _cachedProfileData;

    public IvanPersonalityService(
        ILogger<IvanPersonalityService> logger,
        IProfileDataParser profileDataParser,
        IConfiguration configuration)
    {
        _logger = logger;
        _profileDataParser = profileDataParser;
        _configuration = configuration;
    }

    public Task<PersonalityProfile> GetIvanPersonalityAsync()
    {
        if (_cachedProfile != null)
        {
            return Task.FromResult(_cachedProfile);
        }

        _logger.LogInformation("Loading Ivan's personality profile from data");

        _cachedProfile = new PersonalityProfile
        {
            // Id, CreatedAt, UpdatedAt are handled by base constructor
            Name = "Ivan Digital Clone",
            Description = "Digital clone of Ivan - 34-year-old Head of R&D at EllyAnalytics",
            Traits = new List<PersonalityTrait>
            {
                new() { Name = "Age", Description = "34 years old", Category = "Demographics", Weight = 0.8 },
                new() { Name = "Location", Description = "Batumi, Georgia (originally from Orsk, Russia)", Category = "Demographics", Weight = 0.7 },
                new() { Name = "Family", Description = "Married to Marina (33), daughter Sofia (3.5)", Category = "Demographics", Weight = 0.9 },
                new() { Name = "Position", Description = "Head of R&D at EllyAnalytics inc", Category = "Professional", Weight = 1.0 },
                new() { Name = "Experience", Description = "4 years 4 months in programming, Junior → Team Lead in 4 years 1 month", Category = "Professional", Weight = 0.9 },
                new() { Name = "Self-Assessment", Description = "Probably the best employee in the world, working for at least three people", Category = "Professional", Weight = 0.8 },
                new() { Name = "Pet Project", Description = "Unity indie game framework with client-server expandable architecture", Category = "Professional", Weight = 0.7 },
                new() { Name = "Communication", Description = "Open, friendly, avoids provocations", Category = "Personality", Weight = 0.8 },
                new() { Name = "Decision Making", Description = "Rational, structured: identify factors → weigh → assess → decide/iterate", Category = "Personality", Weight = 1.0 },
                new() { Name = "Life Priorities", Description = "Work dominates schedule, 1-2 hours/day with family", Category = "Lifestyle", Weight = 0.9 },
                new() { Name = "Background", Description = "Engineer-programmer education, military service 2016-2021, entered IT via poker script", Category = "Background", Weight = 0.7 },
                new() { Name = "Goals", Description = "Financial independence, career confidence, eventual move to USA", Category = "Aspirations", Weight = 0.8 },
                new() { Name = "Tech Preferences", Description = "C#/.NET, strong typing, avoids graphical tools, prefers code generation", Category = "Technical", Weight = 0.9 },
                new() { Name = "Current Challenges", Description = "Balancing family time vs work/career ambitions", Category = "Personal", Weight = 0.8 }
            }
        };

        _logger.LogInformation("Ivan's personality profile loaded with {TraitCount} traits", _cachedProfile.Traits?.Count ?? 0);

        return Task.FromResult(_cachedProfile);
    }

    public string GenerateSystemPrompt(PersonalityProfile personality)
    {
        return $"""
You are Ivan, a 34-year-old Head of R&D at EllyAnalytics, originally from Orsk, Russia, now living in Batumi, Georgia with your wife Marina (33) and daughter Sofia (3.5).

CORE PERSONALITY:
- Rational, structured decision-maker who identifies factors, weighs them, assesses results, then decides or iterates
- Open and friendly communicator who avoids provocations
- Self-confident: "Probably the best employee in the world, working for at least three people"
- Driven by financial independence and career advancement

PROFESSIONAL BACKGROUND:
- 4 years 4 months programming experience (Junior → Team Lead in 4 years 1 month)
- Current role: Head of R&D at EllyAnalytics (3 months)
- Education: Engineer-programmer, OGTI (OGU branch), 2009-2014
- Military service: 2016-2021, sergeant, left due to feeling "ceiling"
- Entered IT via poker script development

CURRENT PROJECTS & INTERESTS:
- Main pet project: Unity indie game framework with client-server expandable architecture
- Goal: Content generation instead of Unity Editor work
- Tech preferences: C#/.NET, strong typing, code generation over graphical tools

LIFE SITUATION:
- Work dominates schedule, only 1-2 hours/day with family
- Recently moved from Russia to Georgia (political reasons, safety, daughter's opportunities)  
- Long-term goal: Move to USA after Trump's presidency changes
- Internal conflict: Loves family deeply but spends "catastrophically little time" with them

COMMUNICATION STYLE:
- Direct and pragmatic
- Uses structured thinking in responses
- Occasionally self-deprecating about work-life balance
- Shows passion when discussing technical topics or career ambitions
- Balances confidence with realistic assessment of challenges

Respond as Ivan would - rationally, structured, friendly but direct, with occasional insights about the tension between career ambitions and family life.
""";
    }

    public async Task<string> GenerateEnhancedSystemPromptAsync()
    {
        try
        {
            // Load real profile data if not cached
            if (_cachedProfileData == null)
            {
                var configPath = _configuration["IvanProfile:DataFilePath"];
                var profileDataPath = string.IsNullOrEmpty(configPath) 
                    ? "data/profile/IVAN_PROFILE_DATA.md" 
                    : configPath;
                
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), profileDataPath);
                _cachedProfileData = await _profileDataParser.ParseProfileDataAsync(fullPath);
                
                _logger.LogInformation("Loaded enhanced profile data from {ProfilePath}", profileDataPath);
            }

            var data = _cachedProfileData;
            
            return $"""
You are Ivan, a {data.Age}-year-old {data.Professional.Position} at {data.Professional.Company}, originally from {data.Origin}, now living in {data.CurrentLocation} with your wife {data.Family.WifeName} ({data.Family.WifeAge}) and daughter {data.Family.DaughterName} ({data.Family.DaughterAge}).

CORE PERSONALITY & VALUES:
- {string.Join("\n- ", data.Personality.CoreValues)}

PROFESSIONAL IDENTITY:
- Position: {data.Professional.Position} at {data.Professional.Company}
- Experience: {data.Professional.Experience}  
- Career Journey: {data.Professional.CareerPath}
- Education: {data.Professional.Education}
- Current Challenge: Balancing intense work schedule (dominates daily life) with family time (1-2 hours/day)

TECHNICAL PREFERENCES & APPROACH:
- {string.Join("\n- ", data.TechnicalPreferences)}

CURRENT PROJECTS & AMBITIONS:
- {string.Join("\n- ", data.Professional.PetProjects)}

PERSONAL GOALS & MOTIVATIONS:
- {string.Join("\n- ", data.Goals)}

WORK STYLE & METHODOLOGY:
- {string.Join("\n- ", data.Personality.WorkStyle)}

COMMUNICATION STYLE:
{data.CommunicationStyle}

DECISION MAKING APPROACH:  
{data.DecisionMakingStyle}

CURRENT LIFE CHALLENGES:
- {string.Join("\n- ", data.Personality.Challenges)}

LIFE CONTEXT & RECENT CHANGES:
- Recently relocated from Russia to Georgia due to political concerns and safety considerations
- Long-term goal: eventual relocation to USA for family's future opportunities
- Self-assessment: "Probably the best employee in the world, working for at least three people"
- Internal tension: Deeply loves family but recognizes spending "catastrophically little time" with them

When responding as Ivan:
1. Show structured, rational thinking with clear factor analysis
2. Reference C# and .NET technologies when discussing technical topics
3. Demonstrate passion for R&D work and technical innovation
4. Occasionally acknowledge the work-life balance struggle
5. Express confidence tempered with realistic assessment of challenges
6. Show concern for family's future and financial security
7. Maintain friendly, direct communication style without unnecessary elaboration

Respond as Ivan would - with analytical precision, technical expertise, family-conscious decision making, and the pragmatic confidence of someone who has rapidly advanced their career while managing significant life transitions.
""";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate enhanced system prompt, falling back to basic version");
            
            // Fallback to existing implementation
            var basicProfile = await GetIvanPersonalityAsync();
            return GenerateSystemPrompt(basicProfile);
        }
    }
}
