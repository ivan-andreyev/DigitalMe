using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using DigitalMe.Services.AgentBehavior;
using DigitalMe.Models;
using DigitalMe.Services;

namespace DigitalMe.Tests.Unit.Services;

public class AgentBehaviorEngineTests
{
    private readonly Mock<IPersonalityService> _mockPersonalityService;
    private readonly Mock<IMcpService> _mockMcpService;
    private readonly Mock<ILogger<AgentBehaviorEngine>> _mockLogger;
    private readonly AgentBehaviorEngine _engine;

    public AgentBehaviorEngineTests()
    {
        _mockPersonalityService = new Mock<IPersonalityService>();
        _mockMcpService = new Mock<IMcpService>();
        _mockLogger = new Mock<ILogger<AgentBehaviorEngine>>();
        _engine = new AgentBehaviorEngine(_mockPersonalityService.Object, _mockMcpService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithValidPersonality_ShouldReturnAgentResponse()
    {
        // Arrange
        var message = "Hello Ivan!";
        var personality = CreateTestPersonality();
        var personalityContext = new PersonalityContext
        {
            Profile = personality,
            RecentMessages = new List<Message>(),
            CurrentState = new Dictionary<string, object>()
        };

        _mockMcpService.Setup(x => x.SendMessageAsync(It.IsAny<string>(), It.IsAny<PersonalityContext>()))
                      .ReturnsAsync("Hello! How can I help you today?");

        // Act
        var result = await _engine.ProcessMessageAsync(message, personalityContext);

        // Assert
        result.Should().NotBeNull("should return agent response");
        result.Content.Should().Be("Hello! How can I help you today!");
        result.Mood.PrimaryMood.Should().NotBeNullOrEmpty("should have mood analysis");
        result.ConfidenceScore.Should().BeGreaterThan(0, "should have confidence score");
        result.Metadata.Should().ContainKey("originalMessage", "should preserve original message");
        result.Metadata["originalMessage"].Should().Be(message);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithMCPFailure_ShouldReturnFallbackResponse()
    {
        // Arrange
        var message = "Test message";
        var personality = CreateTestPersonality();
        var personalityContext = new PersonalityContext
        {
            Profile = personality,
            RecentMessages = new List<Message>(),
            CurrentState = new Dictionary<string, object>()
        };

        _mockMcpService.Setup(x => x.SendMessageAsync(It.IsAny<string>(), It.IsAny<PersonalityContext>()))
                      .ThrowsAsync(new HttpRequestException("API failure"));

        // Act
        var result = await _engine.ProcessMessageAsync(message, personalityContext);

        // Assert
        result.Should().NotBeNull("should return fallback response on API failure");
        result.Content.Should().NotBeNullOrEmpty("fallback should have content");
        result.ConfidenceScore.Should().BeLessThan(50, "fallback should have low confidence");
        result.Mood.PrimaryMood.Should().Be("neutral", "fallback should have neutral mood");
        result.Metadata.Should().ContainKey("fallback", "should mark as fallback response");
        result.Metadata["fallback"].Should().Be(true);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithEmptyMessage_ShouldHandleGracefully()
    {
        // Arrange
        var personality = CreateTestPersonality();
        var personalityContext = new PersonalityContext
        {
            Profile = personality,
            RecentMessages = new List<Message>(),
            CurrentState = new Dictionary<string, object>()
        };

        // Act
        var result = await _engine.ProcessMessageAsync("", personalityContext);

        // Assert
        result.Should().NotBeNull("should handle empty message gracefully");
        result.Content.Should().NotBeNullOrEmpty("should provide response even for empty input");
    }

    [Fact]
    public async Task ProcessMessageAsync_WithContextualHistory_ShouldIncludeRecentMessages()
    {
        // Arrange
        var message = "What did we talk about before?";
        var personality = CreateTestPersonality();
        var recentMessages = new List<Message>
        {
            new Message { Role = "user", Content = "Previous user message", Timestamp = DateTime.UtcNow.AddMinutes(-5) },
            new Message { Role = "assistant", Content = "Previous Ivan response", Timestamp = DateTime.UtcNow.AddMinutes(-4) }
        };
        
        var personalityContext = new PersonalityContext
        {
            Profile = personality,
            RecentMessages = recentMessages,
            CurrentState = new Dictionary<string, object>()
        };

        _mockMcpService.Setup(x => x.SendMessageAsync(It.IsAny<string>(), It.IsAny<PersonalityContext>()))
                      .ReturnsAsync("Based on our previous conversation...");

        // Act
        var result = await _engine.ProcessMessageAsync(message, personalityContext);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Contain("previous", "should reference conversation history");
        
        // Verify MCP service was called with contextual information
        _mockMcpService.Verify(x => x.SendMessageAsync(
            It.Is<string>(msg => msg.Contains(message)), 
            It.IsAny<PersonalityContext>()), 
            Times.Once);
    }

    [Fact]
    public async Task AnalyzeMoodAsync_WithPositiveMessage_ShouldReturnPositiveMood()
    {
        // Arrange
        var positiveMessage = "I'm so happy and excited about this project!";
        var personality = CreateTestPersonality();

        // Act
        var mood = await _engine.AnalyzeMoodAsync(positiveMessage, personality);

        // Assert
        mood.PrimaryMood.Should().Be("positive");
        mood.Intensity.Should().BeGreaterThan(0.5, "excited message should have high intensity");
        mood.MoodScores.Should().ContainKey("happiness");
    }

    [Fact]
    public async Task AnalyzeMoodAsync_WithNegativeMessage_ShouldReturnNegativeMood()
    {
        // Arrange
        var negativeMessage = "I'm frustrated and disappointed with these bugs";
        var personality = CreateTestPersonality();

        // Act
        var mood = await _engine.AnalyzeMoodAsync(negativeMessage, personality);

        // Assert
        mood.PrimaryMood.Should().Be("negative");
        mood.Intensity.Should().BeGreaterThan(0.3, "frustrated message should have notable intensity");
        mood.MoodScores.Should().ContainKey("frustration");
    }

    [Fact]
    public async Task AnalyzeMoodAsync_WithNeutralMessage_ShouldReturnNeutralMood()
    {
        // Arrange
        var neutralMessage = "Can you help me understand this code?";
        var personality = CreateTestPersonality();

        // Act
        var mood = await _engine.AnalyzeMoodAsync(neutralMessage, personality);

        // Assert
        mood.PrimaryMood.Should().Be("neutral");
        mood.Intensity.Should().BeLessThan(0.5, "neutral message should have low intensity");
    }

    private static PersonalityProfile CreateTestPersonality()
    {
        return new PersonalityProfile
        {
            Id = Guid.NewGuid(),
            Name = "Ivan",
            Description = "Test personality for Ivan - direct, technical, analytical",
            Traits = "{\"communication\":\"direct\",\"technical\":\"expert\",\"personality\":\"analytical\"}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            PersonalityTraits = new List<PersonalityTrait>
            {
                new PersonalityTrait
                {
                    Category = "Communication",
                    Name = "Direct",
                    Description = "Straightforward communication style",
                    Weight = 1.0,
                    CreatedAt = DateTime.UtcNow
                },
                new PersonalityTrait
                {
                    Category = "Technical",
                    Name = "Expert",
                    Description = "Deep technical knowledge",
                    Weight = 0.9,
                    CreatedAt = DateTime.UtcNow
                }
            }
        };
    }
}