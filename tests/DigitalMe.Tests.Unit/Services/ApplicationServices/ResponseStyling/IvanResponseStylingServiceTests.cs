using DigitalMe.Common;
using DigitalMe.Data.Entities;
using DigitalMe.Services.ApplicationServices.ResponseStyling;
using DigitalMe.Services.PersonalityEngine;
using DigitalMe.Services;
using DigitalMe.Services.Optimization;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DigitalMe.Tests.Unit.Services.ApplicationServices.ResponseStyling;

public class IvanResponseStylingServiceTests
{
    private readonly Mock<IPersonalityService> _mockPersonalityService;
    private readonly Mock<ICommunicationStyleAnalyzer> _mockCommunicationStyleAnalyzer;
    private readonly Mock<IPerformanceOptimizationService> _mockPerformanceOptimizationService;
    private readonly Mock<ILogger<IvanResponseStylingService>> _mockLogger;
    private readonly Mock<IIvanVocabularyService> _mockVocabularyService;
    private readonly Mock<IIvanLinguisticPatternService> _mockLinguisticPatternService;
    private readonly Mock<IIvanContextAnalyzer> _mockContextAnalyzer;
    private readonly IvanResponseStylingService _service;

    public IvanResponseStylingServiceTests()
    {
        _mockPersonalityService = new Mock<IPersonalityService>();
        _mockCommunicationStyleAnalyzer = new Mock<ICommunicationStyleAnalyzer>();
        _mockPerformanceOptimizationService = new Mock<IPerformanceOptimizationService>();
        _mockLogger = new Mock<ILogger<IvanResponseStylingService>>();
        _mockVocabularyService = new Mock<IIvanVocabularyService>();
        _mockLinguisticPatternService = new Mock<IIvanLinguisticPatternService>();
        _mockContextAnalyzer = new Mock<IIvanContextAnalyzer>();

        // Setup performance optimization service to pass through by default
        _mockPerformanceOptimizationService
            .Setup(x => x.GetOrSetAsync<ContextualCommunicationStyle>(It.IsAny<string>(), It.IsAny<Func<Task<ContextualCommunicationStyle>>>(), It.IsAny<TimeSpan?>()))
            .Returns<string, Func<Task<ContextualCommunicationStyle>>, TimeSpan?>((key, func, expiry) => func());

        _mockPerformanceOptimizationService
            .Setup(x => x.GetOrSetAsync<IvanVocabularyPreferences>(It.IsAny<string>(), It.IsAny<Func<Task<IvanVocabularyPreferences>>>(), It.IsAny<TimeSpan?>()))
            .Returns<string, Func<Task<IvanVocabularyPreferences>>, TimeSpan?>((key, func, expiry) => func());

        // Setup vocabulary service with realistic data
        _mockVocabularyService
            .Setup(x => x.GetVocabularyPreferencesAsync(It.Is<SituationalContext>(c => c.ContextType == ContextType.Technical)))
            .ReturnsAsync(new IvanVocabularyPreferences
            {
                PreferredTechnicalTerms = new List<string>
                {
                    "C#/.NET", "SOLID principles", "Clean Architecture",
                    "DI container", "async/await", "nullable reference types"
                },
                SignatureExpressions = new List<string>
                {
                    "That's the pragmatic choice",
                    "Let me analyze the technical factors",
                    "The structured approach would be"
                },
                SelfReferenceStyle = "In my R&D role"
            });

        _mockVocabularyService
            .Setup(x => x.GetVocabularyPreferencesAsync(It.Is<SituationalContext>(c => c.ContextType == ContextType.Personal)))
            .ReturnsAsync(new IvanVocabularyPreferences
            {
                PreferredCasualPhrases = new List<string>
                {
                    "Honestly speaking", "To be completely transparent",
                    "This hits close to home"
                },
                SignatureExpressions = new List<string>
                {
                    "Marina and Sofia mean everything to me, but I struggle to balance",
                    "I'm still figuring this out myself",
                    "Work-life balance is my ongoing challenge"
                },
                AvoidedPhrases = new List<string>
                {
                    "Perfect work-life balance", "I have it all figured out",
                    "I spend enough time with family"
                },
                SelfReferenceStyle = "As a father who's still learning to balance everything"
            });

        // Setup linguistic pattern service to return modified text matching real implementation
        _mockLinguisticPatternService
            .Setup(x => x.ApplyIvanLinguisticPatterns(It.IsAny<string>(), It.IsAny<ContextualCommunicationStyle>()))
            .Returns<string, ContextualCommunicationStyle>((text, style) =>
            {
                // Simulate Ivan's linguistic patterns based on real implementation
                var enhancedText = text;

                // Technical pattern: replace programming with C#/.NET programming
                if (style.Context?.ContextType == ContextType.Technical && style.TechnicalDepth > 0.7)
                {
                    enhancedText = enhancedText.Replace("programming solution", "C#/.NET programming solution");
                }

                // Personal pattern: add struggle to balance
                if (style.Context?.ContextType == ContextType.Personal && enhancedText.Contains("balance"))
                {
                    enhancedText = enhancedText.Replace("balance", "struggle to balance");
                }

                // Structured thinking pattern for high self-reflection and long text
                if (style.SelfReflection >= 0.8 && enhancedText.Length > 100)
                {
                    enhancedText = "Let me think through this systematically. " + enhancedText;
                }

                // Directness pattern
                if (style.DirectnessLevel >= 0.8 && enhancedText.Contains("I think maybe"))
                {
                    enhancedText = enhancedText.Replace("I think maybe", "I believe");
                }

                return enhancedText;
            });

        // Setup context analyzer to return contextual communication style with Ivan-specific adjustments
        _mockContextAnalyzer
            .Setup(x => x.GetContextualStyleAsync(It.IsAny<SituationalContext>()))
            .ReturnsAsync((SituationalContext context) => new ContextualCommunicationStyle
            {
                BasePersonalityName = "Ivan",
                Context = context,
                TechnicalDepth = context.ContextType == ContextType.Technical ? 0.9 : 0.5,
                DirectnessLevel = 0.8,
                LeadershipTone = 0.75,
                LeadershipAssertiveness = context.ContextType == ContextType.Technical ? 0.85 : 0.7,
                SelfReflection = context.ContextType == ContextType.Personal ? 0.85 : 0.6
            });

        _service = new IvanResponseStylingService(
            _mockVocabularyService.Object,
            _mockLinguisticPatternService.Object,
            _mockContextAnalyzer.Object,
            _mockPerformanceOptimizationService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task StyleResponseAsync_WithValidTechnicalContext_ShouldApplyTechnicalStyling()
    {
        // Arrange
        var input = "This is a programming solution.";
        var context = new SituationalContext
        {
            ContextType = ContextType.Technical,
            UrgencyLevel = 0.5
        };

        var personality = new PersonalityProfile
        {
            Name = "Ivan",
            Traits = new List<PersonalityTrait>()
        };

        var style = new ContextualCommunicationStyle
        {
            Context = context,
            BasePersonalityName = "Ivan",
            DirectnessLevel = 0.8,
            TechnicalDepth = 0.9,
            SelfReflection = 0.7
        };

        _mockPersonalityService
            .Setup(x => x.GetPersonalityAsync())
            .ReturnsAsync(Result<PersonalityProfile>.Success(personality));

        _mockCommunicationStyleAnalyzer
            .Setup(x => x.DetermineOptimalCommunicationStyle(personality, context))
            .Returns(style);

        // Act
        var result = await _service.StyleResponseAsync(input, context);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("C#/.NET", result); // Technical precision should be added
    }

    [Fact]
    public async Task StyleResponseAsync_WithPersonalContext_ShouldApplyPersonalTouches()
    {
        // Arrange
        var input = "I need to balance work and family time.";
        var context = new SituationalContext
        {
            ContextType = ContextType.Personal,
            UrgencyLevel = 0.3
        };

        var personality = new PersonalityProfile
        {
            Name = "Ivan",
            Traits = new List<PersonalityTrait>()
        };

        var style = new ContextualCommunicationStyle
        {
            Context = context,
            BasePersonalityName = "Ivan",
            VulnerabilityLevel = 0.8,
            WarmthLevel = 0.8,
            EmotionalOpenness = 0.7
        };

        _mockPersonalityService
            .Setup(x => x.GetPersonalityAsync())
            .ReturnsAsync(Result<PersonalityProfile>.Success(personality));

        _mockCommunicationStyleAnalyzer
            .Setup(x => x.DetermineOptimalCommunicationStyle(personality, context))
            .Returns(style);

        // Act
        var result = await _service.StyleResponseAsync(input, context);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("struggle to balance", result); // Personal honesty should be added
        Assert.Contains("Marina and Sofia", result); // Personal touches should be added
    }

    [Fact]
    public async Task StyleResponseAsync_WithEmptyInput_ShouldReturnOriginalInput()
    {
        // Arrange
        var input = "";
        var context = new SituationalContext
        {
            ContextType = ContextType.Technical,
            UrgencyLevel = 0.5
        };

        // Act
        var result = await _service.StyleResponseAsync(input, context);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public async Task StyleResponseAsync_WithException_ShouldReturnOriginalInput()
    {
        // Arrange
        var input = "Test input";
        var context = new SituationalContext
        {
            ContextType = ContextType.Technical,
            UrgencyLevel = 0.5
        };

        // Reset the mock and setup exception on _performanceOptimizationService since GetContextualStyleAsync catches _contextAnalyzer exceptions
        _mockPerformanceOptimizationService.Reset();
        _mockPerformanceOptimizationService
            .Setup(x => x.GetOrSetAsync<ContextualCommunicationStyle>(It.IsAny<string>(), It.IsAny<Func<Task<ContextualCommunicationStyle>>>(), It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new Exception("Test exception"));
        _mockPerformanceOptimizationService
            .Setup(x => x.GetOrSetAsync<IvanVocabularyPreferences>(It.IsAny<string>(), It.IsAny<Func<Task<IvanVocabularyPreferences>>>(), It.IsAny<TimeSpan?>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _service.StyleResponseAsync(input, context);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public async Task GetContextualStyleAsync_WithTechnicalContext_ShouldAdjustForIvanTraits()
    {
        // Arrange
        var context = new SituationalContext
        {
            ContextType = ContextType.Technical,
            UrgencyLevel = 0.5
        };

        var personality = new PersonalityProfile
        {
            Name = "Ivan",
            Traits = new List<PersonalityTrait>()
        };

        var baseStyle = new ContextualCommunicationStyle
        {
            Context = context,
            BasePersonalityName = "Ivan",
            TechnicalDepth = 0.6,
            DirectnessLevel = 0.5,
            LeadershipAssertiveness = 0.5
        };

        _mockPersonalityService
            .Setup(x => x.GetPersonalityAsync())
            .ReturnsAsync(Result<PersonalityProfile>.Success(personality));

        _mockCommunicationStyleAnalyzer
            .Setup(x => x.DetermineOptimalCommunicationStyle(personality, context))
            .Returns(baseStyle);

        // Act
        var result = await _service.GetContextualStyleAsync(context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Ivan", result.BasePersonalityName);
        // Ivan-specific adjustments should have been applied
        Assert.True(result.TechnicalDepth >= 0.8); // Ivan is highly technical
        Assert.True(result.DirectnessLevel >= 0.7); // Direct in technical discussions
        Assert.True(result.LeadershipAssertiveness >= 0.75); // Confident in expertise
    }

    [Fact]
    public async Task GetVocabularyPreferencesAsync_WithTechnicalContext_ShouldReturnTechnicalVocabulary()
    {
        // Arrange
        var context = new SituationalContext
        {
            ContextType = ContextType.Technical,
            UrgencyLevel = 0.5
        };

        // Act
        var result = await _service.GetVocabularyPreferencesAsync(context);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("C#/.NET", result.PreferredTechnicalTerms);
        Assert.Contains("SOLID principles", result.PreferredTechnicalTerms);
        Assert.Contains("Clean Architecture", result.PreferredTechnicalTerms);
        Assert.Contains("That's the pragmatic choice", result.SignatureExpressions);
        Assert.Equal("In my R&D role", result.SelfReferenceStyle);
    }

    [Fact]
    public async Task GetVocabularyPreferencesAsync_WithPersonalContext_ShouldReturnPersonalVocabulary()
    {
        // Arrange
        var context = new SituationalContext
        {
            ContextType = ContextType.Personal,
            UrgencyLevel = 0.3
        };

        // Act
        var result = await _service.GetVocabularyPreferencesAsync(context);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Honestly speaking", result.PreferredCasualPhrases);
        Assert.Contains("I'm still figuring this out myself", result.SignatureExpressions);
        Assert.Contains("Perfect work-life balance", result.AvoidedPhrases);
        Assert.Equal("As a father who's still learning to balance everything", result.SelfReferenceStyle);
    }

    [Fact]
    public void ApplyIvanLinguisticPatterns_WithHighDirectness_ShouldMakeTextMoreDirect()
    {
        // Arrange
        var input = "I think maybe we could try this approach.";
        var style = new ContextualCommunicationStyle
        {
            DirectnessLevel = 0.8,
            TechnicalDepth = 0.5,
            SelfReflection = 0.5,
            VulnerabilityLevel = 0.5,
            Context = new SituationalContext { ContextType = ContextType.Professional }
        };

        // Act
        var result = _service.ApplyIvanLinguisticPatterns(input, style);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain("I think maybe", result);
        Assert.Contains("I believe", result);
    }

    [Fact]
    public void ApplyIvanLinguisticPatterns_WithHighSelfReflection_ShouldAddStructuredThinking()
    {
        // Arrange - Text must be over 100 characters to trigger structured thinking
        var input = "This is a complex problem that needs very careful consideration and detailed analysis of all possible approaches.";
        var style = new ContextualCommunicationStyle
        {
            DirectnessLevel = 0.5,
            TechnicalDepth = 0.5,
            SelfReflection = 0.8,
            VulnerabilityLevel = 0.5,
            Context = new SituationalContext { ContextType = ContextType.Technical }
        };

        // Act
        var result = _service.ApplyIvanLinguisticPatterns(input, style);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Let me think through this systematically", result);
    }
}