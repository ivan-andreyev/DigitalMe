using System.Net;
using System.Text;
using System.Text.Json;
using DigitalMe.Exceptions;
using DigitalMe.Models.Integrations;
using DigitalMe.Models.Usage;
using DigitalMe.Services;
using DigitalMe.Services.Integrations;
using DigitalMe.Services.Usage;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace DigitalMe.Tests.Unit.Services.Integrations;

public class AnthropicServiceV2Tests
{
    private readonly Mock<IHttpClientFactory> _mockHttpFactory;
    private readonly Mock<IApiConfigurationService> _mockConfigService;
    private readonly Mock<IApiUsageTracker> _mockUsageTracker;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly AnthropicServiceV2 _service;

    public AnthropicServiceV2Tests()
    {
        _mockHttpFactory = new Mock<IHttpClientFactory>();
        _mockConfigService = new Mock<IApiConfigurationService>();
        _mockUsageTracker = new Mock<IApiUsageTracker>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://api.anthropic.com/")
        };

        _mockHttpFactory
            .Setup(f => f.CreateClient("Anthropic"))
            .Returns(httpClient);

        _service = new AnthropicServiceV2(
            _mockHttpFactory.Object,
            _mockConfigService.Object,
            _mockUsageTracker.Object,
            NullLogger<AnthropicServiceV2>.Instance);
    }

    #region SendMessageAsync Tests

    [Fact]
    public async Task SendMessageAsync_Should_Use_User_ApiKey_When_UserId_Provided()
    {
        // Arrange
        const string userId = "user123";
        const string userKey = "sk-ant-user-key";
        const string prompt = "Test prompt";

        _mockConfigService
            .Setup(c => c.GetApiKeyAsync("Anthropic", userId))
            .ReturnsAsync(userKey);

        SetupSuccessfulHttpResponse();

        // Act
        await _service.SendMessageAsync(prompt, userId);

        // Assert
        _mockConfigService.Verify(c => c.GetApiKeyAsync("Anthropic", userId), Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_Should_Use_System_ApiKey_When_UserId_Is_Null()
    {
        // Arrange
        const string systemKey = "sk-ant-system-key";
        const string prompt = "Test prompt";

        _mockConfigService
            .Setup(c => c.GetApiKeyAsync("Anthropic", "SYSTEM"))
            .ReturnsAsync(systemKey);

        SetupSuccessfulHttpResponse();

        // Act
        await _service.SendMessageAsync(prompt, userId: null);

        // Assert
        _mockConfigService.Verify(c => c.GetApiKeyAsync("Anthropic", "SYSTEM"), Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_Should_Parse_Token_Usage_Correctly()
    {
        // Arrange
        const string prompt = "Test prompt";
        var apiResponse = new AnthropicApiResponse
        {
            Content = new List<AnthropicContentBlock>
            {
                new() { Text = "Test response" }
            },
            Usage = new AnthropicUsage
            {
                InputTokens = 100,
                OutputTokens = 50
            }
        };

        _mockConfigService
            .Setup(c => c.GetApiKeyAsync("Anthropic", "SYSTEM"))
            .ReturnsAsync("sk-ant-key");

        SetupSuccessfulHttpResponse(apiResponse);

        // Act
        var result = await _service.SendMessageAsync(prompt);

        // Assert
        result.Content.Should().Be("Test response");
        result.InputTokens.Should().Be(100);
        result.OutputTokens.Should().Be(50);
        result.TokensUsed.Should().Be(150);
    }

    [Fact]
    public async Task SendMessageAsync_Should_Track_Usage_When_UserId_Provided()
    {
        // Arrange
        const string userId = "user123";
        const string prompt = "Test prompt";

        _mockConfigService
            .Setup(c => c.GetApiKeyAsync("Anthropic", userId))
            .ReturnsAsync("sk-ant-key");

        var apiResponse = new AnthropicApiResponse
        {
            Content = new List<AnthropicContentBlock> { new() { Text = "Response" } },
            Usage = new AnthropicUsage { InputTokens = 100, OutputTokens = 50 }
        };

        SetupSuccessfulHttpResponse(apiResponse);

        // Act
        await _service.SendMessageAsync(prompt, userId);

        // Assert
        _mockUsageTracker.Verify(
            t => t.RecordUsageAsync(
                userId,
                "Anthropic",
                It.Is<UsageDetails>(d =>
                    d.TokensUsed == 150 &&
                    d.Success == true &&
                    d.RequestType == "anthropic.message")),
            Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_Should_Not_Track_Usage_When_UserId_Is_Null()
    {
        // Arrange
        const string prompt = "Test prompt";

        _mockConfigService
            .Setup(c => c.GetApiKeyAsync("Anthropic", "SYSTEM"))
            .ReturnsAsync("sk-ant-key");

        SetupSuccessfulHttpResponse();

        // Act
        await _service.SendMessageAsync(prompt, userId: null);

        // Assert
        _mockUsageTracker.Verify(
            t => t.RecordUsageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UsageDetails>()),
            Times.Never);
    }

    [Fact]
    public async Task SendMessageAsync_Should_Include_SystemPrompt_When_Provided()
    {
        // Arrange
        const string prompt = "User message";
        const string systemPrompt = "You are a helpful assistant";
        string? capturedRequestBody = null;

        _mockConfigService
            .Setup(c => c.GetApiKeyAsync("Anthropic", "SYSTEM"))
            .ReturnsAsync("sk-ant-key");

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(async (req, _) =>
            {
                capturedRequestBody = await req.Content!.ReadAsStringAsync();
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new AnthropicApiResponse
                {
                    Content = new List<AnthropicContentBlock> { new() { Text = "Response" } }
                }))
            });

        // Act
        await _service.SendMessageAsync(prompt, systemPrompt: systemPrompt);

        // Assert
        capturedRequestBody.Should().NotBeNull();
        capturedRequestBody.Should().Contain(systemPrompt);
    }

    [Fact]
    public async Task SendMessageAsync_Should_Throw_RateLimitException_On_429_Status()
    {
        // Arrange
        const string prompt = "Test prompt";

        _mockConfigService
            .Setup(c => c.GetApiKeyAsync("Anthropic", "SYSTEM"))
            .ReturnsAsync("sk-ant-key");

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.TooManyRequests,
                Headers = { RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(60)) }
            });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<RateLimitException>(() =>
            _service.SendMessageAsync(prompt));

        exception.RetryAfterSeconds.Should().Be(60);
    }

    [Fact]
    public async Task SendMessageAsync_Should_Throw_When_No_ApiKey_Available()
    {
        // Arrange
        const string prompt = "Test prompt";

        _mockConfigService
            .Setup(c => c.GetApiKeyAsync("Anthropic", "SYSTEM"))
            .ReturnsAsync(string.Empty);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AnthropicServiceException>(() =>
            _service.SendMessageAsync(prompt));

        exception.Message.Should().Contain("API key");
    }

    [Fact]
    public async Task SendMessageAsync_Should_Track_Failure_On_Exception()
    {
        // Arrange
        const string userId = "user123";
        const string prompt = "Test prompt";

        _mockConfigService
            .Setup(c => c.GetApiKeyAsync("Anthropic", userId))
            .ReturnsAsync("sk-ant-key");

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act & Assert
        await Assert.ThrowsAsync<AnthropicServiceException>(() =>
            _service.SendMessageAsync(prompt, userId));

        _mockUsageTracker.Verify(
            t => t.RecordUsageAsync(
                userId,
                "Anthropic",
                It.Is<UsageDetails>(d =>
                    d.Success == false &&
                    d.ErrorType == "HttpRequestException")),
            Times.Once);
    }

    #endregion

    #region IsConnectedAsync Tests

    [Fact]
    public async Task IsConnectedAsync_Should_Return_False_When_No_ApiKey()
    {
        // Arrange
        _mockConfigService
            .Setup(c => c.GetApiKeyAsync("Anthropic", "SYSTEM"))
            .ReturnsAsync(string.Empty);

        // Act
        var result = await _service.IsConnectedAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsConnectedAsync_Should_Return_True_On_Successful_Test_Request()
    {
        // Arrange
        _mockConfigService
            .Setup(c => c.GetApiKeyAsync("Anthropic", "SYSTEM"))
            .ReturnsAsync("sk-ant-key");

        SetupSuccessfulHttpResponse();

        // Act
        var result = await _service.IsConnectedAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsConnectedAsync_Should_Use_User_ApiKey_When_UserId_Provided()
    {
        // Arrange
        const string userId = "user123";

        _mockConfigService
            .Setup(c => c.GetApiKeyAsync("Anthropic", userId))
            .ReturnsAsync("sk-ant-user-key");

        SetupSuccessfulHttpResponse();

        // Act
        await _service.IsConnectedAsync(userId);

        // Assert
        _mockConfigService.Verify(c => c.GetApiKeyAsync("Anthropic", userId), Times.Once);
    }

    #endregion

    #region Helper Methods

    private void SetupSuccessfulHttpResponse(AnthropicApiResponse? response = null)
    {
        response ??= new AnthropicApiResponse
        {
            Content = new List<AnthropicContentBlock>
            {
                new() { Text = "Default response" }
            },
            Usage = new AnthropicUsage
            {
                InputTokens = 10,
                OutputTokens = 10
            }
        };

        var json = JsonSerializer.Serialize(response);
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);
    }

    #endregion
}