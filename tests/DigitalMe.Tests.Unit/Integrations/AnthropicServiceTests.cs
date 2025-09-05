using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using Moq.Protected;
using DigitalMe.Integrations.MCP;
using DigitalMe.Data.Entities;
using DigitalMe.Services;

namespace DigitalMe.Tests.Unit.Integrations;

public class AnthropicServiceTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly Mock<ILogger<AnthropicServiceSimple>> _mockLogger;
    private readonly Mock<IIvanPersonalityService> _mockPersonalityService;
    private readonly AnthropicServiceSimple _service;

    public AnthropicServiceTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _mockLogger = new Mock<ILogger<AnthropicServiceSimple>>();
        _mockPersonalityService = new Mock<IIvanPersonalityService>();

        var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://api.anthropic.com/")
        };

        // Set environment variable for API key
        Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", "test-api-key-sk-ant-test");

        var mockConfig = new Mock<IOptions<AnthropicConfiguration>>();
        mockConfig.Setup(x => x.Value).Returns(new AnthropicConfiguration 
        { 
            ApiKey = "test-api-key",
            Model = "claude-3-5-sonnet-20241022"
        });

        _service = new AnthropicServiceSimple(httpClient, mockConfig.Object, _mockLogger.Object, _mockPersonalityService.Object);
    }

    [Fact]
    public async Task SendMessageAsync_SuccessfulResponse_ShouldReturnContent()
    {
        // Arrange
        var message = "Hello, how are you?";
        var personality = CreateTestPersonality();
        var expectedResponse = "I'm doing well, thank you for asking!";

        var anthropicResponse = new
        {
            content = new[]
            {
                new { text = expectedResponse, type = "text" }
            },
            id = "msg_123",
            model = "claude-3-sonnet-20240229",
            role = "assistant",
            type = "message",
            usage = new { input_tokens = 10, output_tokens = 15 }
        };

        var responseJson = System.Text.Json.JsonSerializer.Serialize(anthropicResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _service.SendMessageAsync(message, personality);

        // Assert
        result.Should().Be(expectedResponse, "should return the content from Anthropic response");

        // Verify correct API call was made
        _mockHttpMessageHandler.Protected()
            .Verify("SendAsync", Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/v1/messages") &&
                    req.Headers.Contains("x-api-key")),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendMessageAsync_APIError_ShouldReturnFallbackResponse()
    {
        // Arrange
        var message = "Test message";
        var personality = CreateTestPersonality();

        var errorResponse = new
        {
            type = "error",
            error = new
            {
                type = "invalid_request_error",
                message = "Your credit balance is too low to access the Anthropic API. Please go to Plans & Billing to upgrade or purchase credits."
            }
        };

        var responseJson = System.Text.Json.JsonSerializer.Serialize(errorResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _service.SendMessageAsync(message, personality);

        // Assert
        result.Should().NotBeNullOrEmpty("should return fallback response on API error");
        result.Should().Contain("Claude", "fallback should mention connection issue");

        // Verify warning was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Anthropic API returned")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_NetworkError_ShouldReturnFallbackResponse()
    {
        // Arrange
        var message = "Test network error";
        var personality = CreateTestPersonality();

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _service.SendMessageAsync(message, personality);

        // Assert
        result.Should().NotBeNullOrEmpty("should return fallback response on network error");
        result.Should().Contain("подключением", "fallback should mention connection issue in Russian");

        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error calling Anthropic API")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_EmptyMessage_ShouldHandleGracefully()
    {
        // Arrange
        var personality = CreateTestPersonality();

        var anthropicResponse = new
        {
            content = new[]
            {
                new { text = "I'm here to help. What would you like to know?", type = "text" }
            },
            id = "msg_empty",
            model = "claude-3-sonnet-20240229",
            role = "assistant",
            type = "message"
        };

        var responseJson = System.Text.Json.JsonSerializer.Serialize(anthropicResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _service.SendMessageAsync("", personality);

        // Assert
        result.Should().NotBeNullOrEmpty("should handle empty message gracefully");
    }

    [Fact]
    public async Task SendMessageAsync_WithTraits_ShouldIncludePersonalityInSystemPrompt()
    {
        // Arrange
        var message = "How should I approach this technical problem?";
        var personality = CreateTestPersonality();

        var anthropicResponse = new
        {
            content = new[]
            {
                new { text = "Based on my technical experience, I'd recommend...", type = "text" }
            },
            id = "msg_personality",
            model = "claude-3-sonnet-20240229",
            role = "assistant",
            type = "message"
        };

        var responseJson = System.Text.Json.JsonSerializer.Serialize(anthropicResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        string? capturedRequestBody = null;
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) =>
            {
                capturedRequestBody = req.Content?.ReadAsStringAsync().Result;
            })
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _service.SendMessageAsync(message, personality);

        // Assert
        result.Should().NotBeNullOrEmpty();
        
        capturedRequestBody.Should().NotBeNull("should send request body");
        capturedRequestBody.Should().Contain("Ivan", "should include personality name in request");
        capturedRequestBody.Should().Contain("Direct", "should include personality traits");
        capturedRequestBody.Should().Contain("Technical", "should include technical trait");
        capturedRequestBody.Should().Contain("system", "should include system prompt");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task SendMessageAsync_MissingAPIKey_ShouldReturnFallbackResponse(string? apiKey)
    {
        // Arrange
        Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", apiKey);
        
        var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        var mockConfig = new Mock<IOptions<AnthropicConfiguration>>();
        mockConfig.Setup(x => x.Value).Returns(new AnthropicConfiguration 
        { 
            ApiKey = apiKey ?? "",
            Model = "claude-3-5-sonnet-20241022"
        });
        
        var service = new AnthropicServiceSimple(httpClient, mockConfig.Object, _mockLogger.Object, _mockPersonalityService.Object);
        var message = "Test with missing API key";
        var personality = CreateTestPersonality();

        // Act
        var result = await service.SendMessageAsync(message, personality);

        // Assert
        result.Should().NotBeNullOrEmpty("should return fallback when API key is missing");
        result.Should().Contain("API", "fallback should mention API issue");

        // Reset for other tests
        Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", "test-api-key-sk-ant-test");
    }

    private static PersonalityProfile CreateTestPersonality()
    {
        return new PersonalityProfile
        {
            Id = Guid.NewGuid(),
            Name = "Ivan",
            Description = "Test personality - direct, technical, analytical",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Traits = new List<PersonalityTrait>
            {
                new PersonalityTrait
                {
                    Category = "Communication",
                    Name = "Direct",
                    Description = "Straightforward communication",
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