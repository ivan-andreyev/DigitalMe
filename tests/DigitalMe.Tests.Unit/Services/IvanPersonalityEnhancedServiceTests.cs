using DigitalMe.Data.Entities;
using DigitalMe.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DigitalMe.Tests.Unit.Services;

/// <summary>
/// Enhanced unit tests for IvanPersonalityService with profile data integration.
/// Tests personality accuracy, profile parsing, and system prompt generation.
/// </summary>
public class IvanPersonalityEnhancedServiceTests
{
    private readonly Mock<ILogger<IvanPersonalityService>> _mockLogger;
    private readonly Mock<IProfileDataParser> _mockProfileParser;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly IvanPersonalityService _service;

    public IvanPersonalityEnhancedServiceTests()
    {
        this._mockLogger = new Mock<ILogger<IvanPersonalityService>>();
        this._mockProfileParser = new Mock<IProfileDataParser>();
        this._mockConfiguration = new Mock<IConfiguration>();

        this._service = new IvanPersonalityService(
            this._mockLogger.Object,
            this._mockProfileParser.Object,
            this._mockConfiguration.Object);
    }

    [Fact]
    public async Task GetIvanPersonalityAsync_ShouldReturnCachedProfile_WhenCalledMultipleTimes()
    {
        // Act
        var profile1 = await this._service.GetIvanPersonalityAsync();
        var profile2 = await this._service.GetIvanPersonalityAsync();

        // Assert
        Assert.Same(profile1, profile2);
        Assert.Equal("Ivan Digital Clone", profile1.Name);
        Assert.Equal("Digital clone of Ivan - 34-year-old Head of R&D at EllyAnalytics", profile1.Description);
    }

    [Fact]
    public async Task GetIvanPersonalityAsync_ShouldContainEssentialPersonalityTraits()
    {
        // Act
        var profile = await this._service.GetIvanPersonalityAsync();

        // Assert
        Assert.NotNull(profile.Traits);
        Assert.True(profile.Traits.Count >= 10, "Profile should contain at least 10 personality traits");

        // Check for essential traits
        var traitNames = profile.Traits.Select(t => t.Name).ToList();
        Assert.Contains("Age", traitNames);
        Assert.Contains("Position", traitNames);
        Assert.Contains("Family", traitNames);
        Assert.Contains("Tech Preferences", traitNames);
        Assert.Contains("Decision Making", traitNames);
    }

    [Fact]
    public void GenerateSystemPrompt_ShouldIncludeIvanKeyCharacteristics()
    {
        // Arrange
        var personalityProfile = new PersonalityProfile
        {
            Name = "Test Ivan",
            Description = "Test description",
            Traits = new List<PersonalityTrait>
            {
                new() { Name = "Position", Description = "Head of R&D", Category = "Professional" }
            }
        };

        // Act
        var systemPrompt = this._service.GenerateSystemPrompt(personalityProfile);

        // Assert
        Assert.Contains("Ivan", systemPrompt);
        Assert.Contains("34-year-old", systemPrompt);
        Assert.Contains("Head of R&D", systemPrompt);
        Assert.Contains("EllyAnalytics", systemPrompt);
        Assert.Contains("Marina", systemPrompt);
        Assert.Contains("Sofia", systemPrompt);
        Assert.Contains("Batumi, Georgia", systemPrompt);
        Assert.Contains("C#/.NET", systemPrompt);
    }

    [Fact]
    public async Task GenerateEnhancedSystemPromptAsync_WithValidProfileData_ShouldIncludeRealData()
    {
        // Arrange
        var mockProfileData = this.CreateMockProfileData();
        this._mockProfileParser
            .Setup(x => x.ParseProfileDataAsync(It.IsAny<string>()))
            .ReturnsAsync(mockProfileData);

        this._mockConfiguration
            .Setup(x => x["IvanProfile:DataFilePath"])
            .Returns("test/path/IVAN_PROFILE_DATA.md");

        // Act
        var enhancedPrompt = await this._service.GenerateEnhancedSystemPromptAsync();

        // Assert
        Assert.Contains("34-year-old", enhancedPrompt);
        Assert.Contains("Head of R&D at EllyAnalytics", enhancedPrompt);
        Assert.Contains("Marina (33)", enhancedPrompt);
        Assert.Contains("Sofia (3.5)", enhancedPrompt);
        Assert.Contains("Financial independence", enhancedPrompt);
        Assert.Contains("C#/.NET development", enhancedPrompt);
        Assert.Contains("Unity indie game framework", enhancedPrompt);

        // Verify parser was called
        this._mockProfileParser.Verify(x => x.ParseProfileDataAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GenerateEnhancedSystemPromptAsync_WithCachedData_ShouldNotReparseFile()
    {
        // Arrange
        var mockProfileData = this.CreateMockProfileData();
        this._mockProfileParser
            .Setup(x => x.ParseProfileDataAsync(It.IsAny<string>()))
            .ReturnsAsync(mockProfileData);

        // Act - Call twice
        await this._service.GenerateEnhancedSystemPromptAsync();
        await this._service.GenerateEnhancedSystemPromptAsync();

        // Assert - Parser should only be called once due to caching
        this._mockProfileParser.Verify(x => x.ParseProfileDataAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GenerateEnhancedSystemPromptAsync_WithParsingError_ShouldFallbackToBasicPrompt()
    {
        // Arrange
        this._mockProfileParser
            .Setup(x => x.ParseProfileDataAsync(It.IsAny<string>()))
            .ThrowsAsync(new FileNotFoundException("Profile file not found"));

        // Act
        var result = await this._service.GenerateEnhancedSystemPromptAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);

        // Should contain basic Ivan characteristics from fallback method
        Assert.Contains("Ivan", result);
        Assert.Contains("Head of R&D", result);

        // Verify error was logged
        this.VerifyLogWasCalled(LogLevel.Error, "Failed to generate enhanced system prompt");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task GenerateEnhancedSystemPromptAsync_WithMissingConfigPath_ShouldUseDefaultPath(string configPath)
    {
        // Arrange
        var mockProfileData = this.CreateMockProfileData();
        this._mockProfileParser
            .Setup(x => x.ParseProfileDataAsync(It.IsAny<string>()))
            .ReturnsAsync(mockProfileData);

        this._mockConfiguration
            .Setup(x => x["IvanProfile:DataFilePath"])
            .Returns(configPath);

        // Act
        await this._service.GenerateEnhancedSystemPromptAsync();

        // Assert - Should use default path
        this._mockProfileParser.Verify(
            x => x.ParseProfileDataAsync(
            It.Is<string>(path => path.EndsWith("data/profile/IVAN_PROFILE_DATA.md"))),
            Times.Once);
    }

    [Fact]
    public async Task GenerateEnhancedSystemPromptAsync_ShouldIncludeCriticalPersonalityElements()
    {
        // Arrange
        var mockProfileData = this.CreateMockProfileData();
        this._mockProfileParser
            .Setup(x => x.ParseProfileDataAsync(It.IsAny<string>()))
            .ReturnsAsync(mockProfileData);

        // Act
        var prompt = await this._service.GenerateEnhancedSystemPromptAsync();

        // Assert - Check for Ivan's critical personality characteristics
        Assert.Contains("structured, rational", prompt);
        Assert.Contains("C#/.NET", prompt);
        Assert.Contains("technical innovation", prompt);
        Assert.Contains("family time", prompt);
        Assert.Contains("financial security", prompt);
        Assert.Contains("analytical precision", prompt);
    }

    [Fact]
    public async Task GenerateEnhancedSystemPromptAsync_ShouldIncludeAllProfileDataSections()
    {
        // Arrange
        var mockProfileData = this.CreateMockProfileData();
        this._mockProfileParser
            .Setup(x => x.ParseProfileDataAsync(It.IsAny<string>()))
            .ReturnsAsync(mockProfileData);

        // Act
        var prompt = await this._service.GenerateEnhancedSystemPromptAsync();

        // Assert - Check for all major sections
        Assert.Contains("CORE PERSONALITY & VALUES:", prompt);
        Assert.Contains("PROFESSIONAL IDENTITY:", prompt);
        Assert.Contains("TECHNICAL PREFERENCES & APPROACH:", prompt);
        Assert.Contains("CURRENT PROJECTS & AMBITIONS:", prompt);
        Assert.Contains("PERSONAL GOALS & MOTIVATIONS:", prompt);
        Assert.Contains("WORK STYLE & METHODOLOGY:", prompt);
        Assert.Contains("COMMUNICATION STYLE:", prompt);
        Assert.Contains("DECISION MAKING APPROACH:", prompt);
        Assert.Contains("CURRENT LIFE CHALLENGES:", prompt);
        Assert.Contains("LIFE CONTEXT & RECENT CHANGES:", prompt);
        Assert.Contains("When responding as Ivan:", prompt);
    }

    #region Helper Methods

    private ProfileData CreateMockProfileData()
    {
        return new ProfileData
        {
            Name = "Ivan",
            Age = 34,
            Origin = "г. Орск, РФ",
            CurrentLocation = "Батуми, Грузия",
            Family = new FamilyInfo
            {
                WifeName = "Marina",
                WifeAge = 33,
                DaughterName = "Sofia",
                DaughterAge = 3.5
            },
            Professional = new ProfessionalInfo
            {
                Position = "Head of R&D",
                Company = "EllyAnalytics",
                Experience = "4 years 4 months programming",
                CareerPath = "Junior → Team Lead in 4 years 1 month",
                Education = "Engineer-programmer, OGTI",
                PetProjects = new List<string>
                {
                    "Unity indie game framework",
                    "Client-server expandable architecture",
                    "Content generation instead of Unity Editor"
                }
            },
            Personality = new PersonalityTraits
            {
                CoreValues = new List<string>
                {
                    "Financial independence and career confidence",
                    "Family safety and daughter's opportunities",
                    "Technical excellence and structured approaches"
                },
                WorkStyle = new List<string>
                {
                    "Rational and structured decision making",
                    "High-intensity work ethic",
                    "Prefers code generation over graphical tools"
                },
                Challenges = new List<string>
                {
                    "Balancing work demands with family time",
                    "Managing career ambitions with personal life"
                },
                Motivations = new List<string>
                {
                    "Financial security",
                    "Professional growth and recognition",
                    "Technical innovation and problem-solving"
                }
            },
            TechnicalPreferences = new List<string>
            {
                "C#/.NET development stack",
                "Strong typing and type safety",
                "Code generation approaches"
            },
            Goals = new List<string>
            {
                "Achieve financial independence",
                "Build successful Unity game framework",
                "Eventually relocate family to USA"
            },
            CommunicationStyle = "Open, friendly, avoids provocations",
            DecisionMakingStyle = "Rational, structured: identify factors → weigh → assess → decide/iterate"
        };
    }

    private void VerifyLogWasCalled(LogLevel level, string message)
    {
        this._mockLogger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}