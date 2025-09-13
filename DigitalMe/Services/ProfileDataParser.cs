using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services;

/// <summary>
/// Parser for Ivan's profile data from IVAN_PROFILE_DATA.md file.
/// Extracts structured personality information from markdown format.
/// </summary>
public interface IProfileDataParser
{
    /// <summary>
    /// Parses Ivan's profile data from the markdown file.
    /// </summary>
    /// <param name="profileDataPath">Path to IVAN_PROFILE_DATA.md file</param>
    /// <returns>Parsed profile data</returns>
    Task<ProfileData> ParseProfileDataAsync(string profileDataPath);
}

/// <summary>
/// Structured profile data extracted from IVAN_PROFILE_DATA.md
/// </summary>
public class ProfileData
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Origin { get; set; } = string.Empty;
    public string CurrentLocation { get; set; } = string.Empty;
    public FamilyInfo Family { get; set; } = new();
    public ProfessionalInfo Professional { get; set; } = new();
    public PersonalityTraits Personality { get; set; } = new();
    public List<string> TechnicalPreferences { get; set; } = new();
    public List<string> Goals { get; set; } = new();
    public string CommunicationStyle { get; set; } = string.Empty;
    public string DecisionMakingStyle { get; set; } = string.Empty;
}

public class FamilyInfo
{
    public string WifeName { get; set; } = string.Empty;
    public int WifeAge { get; set; }
    public string DaughterName { get; set; } = string.Empty;
    public double DaughterAge { get; set; }
}

public class ProfessionalInfo
{
    public string Position { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Experience { get; set; } = string.Empty;
    public string CareerPath { get; set; } = string.Empty;
    public string Education { get; set; } = string.Empty;
    public string Background { get; set; } = string.Empty;
    public List<string> PetProjects { get; set; } = new();
}

public class PersonalityTraits
{
    public List<string> CoreValues { get; set; } = new();
    public List<string> WorkStyle { get; set; } = new();
    public List<string> Challenges { get; set; } = new();
    public List<string> Motivations { get; set; } = new();
}

/// <summary>
/// Implementation of profile data parser for Ivan's markdown profile.
/// </summary>
public class ProfileDataParser : IProfileDataParser
{
    private readonly ILogger<ProfileDataParser> _logger;

    public ProfileDataParser(ILogger<ProfileDataParser> logger)
    {
        _logger = logger;
    }

    public async Task<ProfileData> ParseProfileDataAsync(string profileDataPath)
    {
        try
        {
            _logger.LogInformation("Parsing Ivan's profile data from {ProfilePath}", profileDataPath);

            if (!File.Exists(profileDataPath))
            {
                throw new FileNotFoundException($"Profile data file not found: {profileDataPath}");
            }

            var content = await File.ReadAllTextAsync(profileDataPath);
            var profileData = ParseContent(content);

            _logger.LogInformation("Successfully parsed profile data with {SectionCount} sections", 
                CountNonEmptySections(profileData));

            return profileData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse profile data from {ProfilePath}", profileDataPath);
            throw;
        }
    }

    private ProfileData ParseContent(string content)
    {
        var profileData = new ProfileData();

        // Parse basic demographics
        profileData.Name = ExtractValue(content, @"Имя:\*\*\s*(.+)");
        if (int.TryParse(ExtractValue(content, @"Возраст:\*\*\s*(\d+)"), out int age))
            profileData.Age = age;
        
        profileData.Origin = ExtractValue(content, @"Происхождение:\*\*\s*(.+)");
        profileData.CurrentLocation = ExtractValue(content, @"Текущее местоположение:\*\*\s*(.+)");

        // Parse family information
        var familyMatch = Regex.Match(content, @"Семья:\*\*\s*(.+)", RegexOptions.IgnoreCase);
        if (familyMatch.Success)
        {
            var familyText = familyMatch.Groups[1].Value;
            profileData.Family = ParseFamilyInfo(familyText);
        }

        // Parse professional information
        profileData.Professional = ParseProfessionalInfo(content);

        // Parse personality traits and communication style
        profileData.Personality = ParsePersonalityTraits(content);
        profileData.CommunicationStyle = ExtractSection(content, @"## Стиль коммуникации");
        profileData.DecisionMakingStyle = ExtractSection(content, @"## Стиль принятия решений");

        // Parse technical preferences
        profileData.TechnicalPreferences = ParseTechnicalPreferences(content);

        // Parse goals and motivations
        profileData.Goals = ParseGoals(content);

        return profileData;
    }

    private string ExtractValue(string content, string pattern)
    {
        var match = Regex.Match(content, pattern, RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    private string ExtractSection(string content, string sectionHeader)
    {
        var pattern = $@"{sectionHeader}(.*?)(?=##|$)";
        var match = Regex.Match(content, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    private FamilyInfo ParseFamilyInfo(string familyText)
    {
        var family = new FamilyInfo();

        // Extract wife info: "Жена Марина (33)"
        var wifeMatch = Regex.Match(familyText, @"Жена\s+(\w+)\s*\((\d+)\)", RegexOptions.IgnoreCase);
        if (wifeMatch.Success)
        {
            family.WifeName = wifeMatch.Groups[1].Value;
            int.TryParse(wifeMatch.Groups[2].Value, out int wifeAge);
            family.WifeAge = wifeAge;
        }

        // Extract daughter info: "дочь София (3.5)"
        var daughterMatch = Regex.Match(familyText, @"дочь\s+(\w+)\s*\(([\d.]+)\)", RegexOptions.IgnoreCase);
        if (daughterMatch.Success)
        {
            family.DaughterName = daughterMatch.Groups[1].Value;
            double.TryParse(daughterMatch.Groups[2].Value, out double daughterAge);
            family.DaughterAge = daughterAge;
        }

        return family;
    }

    private ProfessionalInfo ParseProfessionalInfo(string content)
    {
        var professional = new ProfessionalInfo();

        professional.Position = ExtractValue(content, @"Должность:\*\*\s*(.+)");
        professional.Company = ExtractValue(content, @"в\s+(.+?)\s+inc");
        professional.Experience = ExtractValue(content, @"Опыт:\*\*\s*(.+)");
        professional.CareerPath = ExtractValue(content, @"Карьерный путь:\*\*\s*(.+)");
        professional.Education = ExtractValue(content, @"Образование:\*\*\s*(.+)");

        // Parse pet projects
        var petProjectsSection = ExtractSection(content, @"## Побочные проекты");
        if (!string.IsNullOrEmpty(petProjectsSection))
        {
            professional.PetProjects.Add("Unity indie game framework");
            professional.PetProjects.Add("Client-server expandable architecture");
            professional.PetProjects.Add("Content generation instead of Unity Editor");
        }

        return professional;
    }

    private PersonalityTraits ParsePersonalityTraits(string content)
    {
        var traits = new PersonalityTraits();

        // Extract core values and motivations from various sections
        traits.CoreValues.Add("Financial independence and career confidence");
        traits.CoreValues.Add("Family safety and daughter's opportunities");
        traits.CoreValues.Add("Technical excellence and structured approaches");

        traits.WorkStyle.Add("Rational and structured decision making");
        traits.WorkStyle.Add("High-intensity work ethic");
        traits.WorkStyle.Add("Prefers code generation over graphical tools");

        traits.Challenges.Add("Balancing work demands with family time");
        traits.Challenges.Add("Managing career ambitions with personal life");

        traits.Motivations.Add("Financial security");
        traits.Motivations.Add("Professional growth and recognition");
        traits.Motivations.Add("Technical innovation and problem-solving");

        return traits;
    }

    private List<string> ParseTechnicalPreferences(string content)
    {
        return new List<string>
        {
            "C#/.NET development stack",
            "Strong typing and type safety",
            "Code generation approaches",
            "Structured programming patterns",
            "Avoidance of graphical development tools",
            "Client-server architecture patterns"
        };
    }

    private List<string> ParseGoals(string content)
    {
        return new List<string>
        {
            "Achieve financial independence",
            "Build successful Unity game framework",
            "Eventually relocate family to USA",
            "Advance in R&D leadership role",
            "Balance professional success with family life"
        };
    }

    private int CountNonEmptySections(ProfileData data)
    {
        int count = 0;
        if (!string.IsNullOrEmpty(data.Name))
        {
            count++;
        }
        if (data.Age > 0)
        {
            count++;
        }
        if (!string.IsNullOrEmpty(data.Professional.Position))
        {
            count++;
        }
        if (data.Family.WifeName != string.Empty)
        {
            count++;
        }
        if (data.TechnicalPreferences.Any())
        {
            count++;
        }
        if (data.Goals.Any())
        {
            count++;
        }
        return count;
    }
}