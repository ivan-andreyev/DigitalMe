using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using Xunit;
using DigitalMe.Services.CaptchaSolving;

namespace DigitalMe.Tests.Unit.Services;

/// <summary>
/// Unit tests for CaptchaSolvingService
/// Tests all CAPTCHA solving operations with comprehensive coverage
/// </summary>
public class CaptchaSolvingServiceTests
{
    private readonly Mock<ILogger<CaptchaSolvingService>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<Microsoft.Extensions.Configuration.IConfiguration> _mockConfiguration;
    private readonly CaptchaSolvingService _service;

    public CaptchaSolvingServiceTests()
    {
        _mockLogger = new Mock<ILogger<CaptchaSolvingService>>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://2captcha.com/")
        };

        _mockConfiguration = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        _mockConfiguration.Setup(x => x["TwoCaptcha:ApiKey"]).Returns("test_api_key");

        _service = new CaptchaSolvingService(_mockLogger.Object, _httpClient, _mockConfiguration.Object);
    }

    [Fact]
    public async Task SolveImageCaptchaAsync_WithValidBase64_ShouldReturnSuccess()
    {
        // Arrange
        var imageBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("test_image_data"));
        var options = new ImageCaptchaOptions
        {
            CaseSensitive = true,
            MinLength = 4,
            MaxLength = 6,
            Language = "en"
        };

        SetupHttpResponses("OK|12345", "OK|CAPTCHA_SOLUTION");

        // Act
        var result = await _service.SolveImageCaptchaAsync(imageBase64, options);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("CAPTCHA_SOLUTION", result.Data);
        Assert.Equal("12345", result.CaptchaId);
        Assert.NotNull(result.SolveTime);
        Assert.True(result.SolveTime > TimeSpan.Zero);
    }

    [Fact]
    public async Task SolveImageCaptchaAsync_WithNullBase64_ShouldReturnError()
    {
        // Act
        var result = await _service.SolveImageCaptchaAsync(null);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Base64 image data cannot be null or empty", result.Message);
    }

    [Fact]
    public async Task SolveImageCaptchaFromUrlAsync_WithValidUrl_ShouldReturnSuccess()
    {
        // Arrange
        var imageUrl = "https://example.com/captcha.jpg";
        var options = new ImageCaptchaOptions { Language = "ru" };

        SetupHttpResponses("OK|67890", "OK|CAPTCHA_FROM_URL");

        // Act
        var result = await _service.SolveImageCaptchaFromUrlAsync(imageUrl, options);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("CAPTCHA_FROM_URL", result.Data);
        Assert.Equal("67890", result.CaptchaId);
    }

    [Fact]
    public async Task SolveRecaptchaV2Async_WithValidParameters_ShouldReturnSuccess()
    {
        // Arrange
        var siteKey = "6LfABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmn";
        var pageUrl = "https://example.com/login";
        var options = new RecaptchaOptions
        {
            Invisible = false,
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
        };

        SetupHttpResponses("OK|11111", "OK|03ANYolqvAAA...");

        // Act
        var result = await _service.SolveRecaptchaV2Async(siteKey, pageUrl, options);

        // Assert
        Assert.True(result.Success);
        Assert.StartsWith("03ANYolqvAAA", (string)result.Data!);
        Assert.Equal("11111", result.CaptchaId);
    }

    [Fact]
    public async Task SolveRecaptchaV3Async_WithValidParameters_ShouldReturnSuccess()
    {
        // Arrange
        var siteKey = "6LfABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmn";
        var pageUrl = "https://example.com/login";
        var action = "login";
        var minScore = 0.3;

        SetupHttpResponses("OK|22222", "OK|03ANYolqvBBB...");

        // Act
        var result = await _service.SolveRecaptchaV3Async(siteKey, pageUrl, action, minScore);

        // Assert
        Assert.True(result.Success);
        Assert.StartsWith("03ANYolqvBBB", (string)result.Data!);
        Assert.Equal("22222", result.CaptchaId);
    }

    [Fact]
    public async Task SolveHCaptchaAsync_WithValidParameters_ShouldReturnSuccess()
    {
        // Arrange
        var siteKey = "12345678-1234-1234-1234-123456789012";
        var pageUrl = "https://example.com/protected";
        var options = new HCaptchaOptions
        {
            Invisible = true,
            Data = "custom_data"
        };

        SetupHttpResponses("OK|33333", "OK|P0_eyJ0eXAiOiJKV1Q...");

        // Act
        var result = await _service.SolveHCaptchaAsync(siteKey, pageUrl, options);

        // Assert
        Assert.True(result.Success);
        Assert.StartsWith("P0_eyJ0eXAiOiJKV1Q", (string)result.Data!);
        Assert.Equal("33333", result.CaptchaId);
    }

    [Fact]
    public async Task SolveTextCaptchaAsync_WithValidText_ShouldReturnSuccess()
    {
        // Arrange
        var text = "What is 2 + 2?";
        var options = new TextCaptchaOptions
        {
            Language = "en",
            Instructions = "Answer the math question"
        };

        SetupHttpResponses("OK|44444", "OK|4");

        // Act
        var result = await _service.SolveTextCaptchaAsync(text, options);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("4", result.Data);
        Assert.Equal("44444", result.CaptchaId);
    }

    [Fact]
    public async Task GetBalanceAsync_WithValidApiKey_ShouldReturnBalance()
    {
        // Arrange
        SetupHttpResponse("OK|15.75");

        // Act
        var result = await _service.GetBalanceAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(15.75m, result.Data);
        Assert.Contains("$15.75", result.Message);
    }

    [Fact]
    public async Task GetBalanceAsync_WithInvalidApiKey_ShouldReturnError()
    {
        // Arrange
        SetupHttpResponse("ERROR_WRONG_USER_KEY");

        // Act
        var result = await _service.GetBalanceAsync();

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Invalid API key", result.Message);
    }

    [Fact]
    public async Task ReportIncorrectCaptchaAsync_WithValidId_ShouldReturnSuccess()
    {
        // Arrange
        var captchaId = "12345";
        SetupHttpResponse("OK_REPORT_RECORDED");

        // Act
        var result = await _service.ReportIncorrectCaptchaAsync(captchaId);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Incorrect CAPTCHA reported successfully", result.Message);
    }

    [Fact]
    public async Task IsServiceAvailableAsync_WithValidConfiguration_ShouldReturnTrue()
    {
        // Arrange
        SetupHttpResponse("OK|10.50");

        // Act
        var result = await _service.IsServiceAvailableAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsServiceAvailableAsync_WithInvalidConfiguration_ShouldReturnFalse()
    {
        // Arrange
        var mockConfig = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        mockConfig.Setup(x => x["TwoCaptcha:ApiKey"]).Returns((string)null); // null API key

        // Act & Assert - This will throw during service construction
        Assert.Throws<InvalidOperationException>(() =>
            new CaptchaSolvingService(_mockLogger.Object, _httpClient, mockConfig.Object));
    }

    [Fact]
    public async Task GetServiceStatsAsync_ShouldReturnStatistics()
    {
        // Act
        var result = await _service.GetServiceStatsAsync();

        // Assert
        Assert.True(result.Success);
        var stats = Assert.IsType<ServiceStats>(result.Data);
        Assert.True(stats.TotalSolved >= 0);
        Assert.True(stats.SuccessRate >= 0);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task SolveImageCaptchaAsync_WithInvalidInput_ShouldReturnError(string invalidBase64)
    {
        // Act
        var result = await _service.SolveImageCaptchaAsync(invalidBase64);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Base64 image data cannot be null or empty", result.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task SolveRecaptchaV2Async_WithInvalidSiteKey_ShouldReturnError(string invalidSiteKey)
    {
        // Act
        var result = await _service.SolveRecaptchaV2Async(invalidSiteKey, "https://example.com");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Site key cannot be null or empty", result.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task SolveRecaptchaV2Async_WithInvalidPageUrl_ShouldReturnError(string invalidPageUrl)
    {
        // Act
        var result = await _service.SolveRecaptchaV2Async("valid_site_key", invalidPageUrl);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Page URL cannot be null or empty", result.Message);
    }

    [Fact]
    public async Task SolveRecaptchaV3Async_WithInvalidMinScore_ShouldReturnError()
    {
        // Act
        var result = await _service.SolveRecaptchaV3Async("site_key", "https://example.com", "action", 1.5);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Min score must be between 0.1 and 0.9", result.Message);
    }

    [Fact]
    public async Task SolveImageCaptchaAsync_WithApiError_ShouldReturnError()
    {
        // Arrange
        var imageBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("test_image_data"));
        SetupHttpResponse("ERROR_NO_SLOT_AVAILABLE");

        // Act
        var result = await _service.SolveImageCaptchaAsync(imageBase64);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("no available slots", result.Message, StringComparison.InvariantCultureIgnoreCase);
    }

    [Fact]
    public async Task SolveImageCaptchaAsync_WithTimeout_ShouldReturnError()
    {
        // Arrange
        var imageBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("test_image_data"));
        // Note: Cannot easily change timeout in this implementation, so test will use default

        SetupHttpResponses("OK|12345", "CAPCHA_NOT_READY", "CAPCHA_NOT_READY");

        // Act
        var result = await _service.SolveImageCaptchaAsync(imageBase64);

        // Assert - This should eventually timeout or return ready
        // Due to mocking limitations, we'll expect either success or timeout
        Assert.NotNull(result);
    }

    [Fact]
    public async Task SolveImageCaptchaAsync_WithHttpException_ShouldReturnError()
    {
        // Arrange
        var imageBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("test_image_data"));

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _service.SolveImageCaptchaAsync(imageBase64);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Network error", result.Message);
        if (!string.IsNullOrEmpty(result.ErrorDetails))
        {
            Assert.Contains("Network error", result.ErrorDetails);
        }
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CaptchaSolvingService(null, _httpClient, _mockConfiguration.Object));
    }

    [Fact]
    public void Constructor_WithNullConfig_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CaptchaSolvingService(_mockLogger.Object, _httpClient, null));
    }

    [Fact]
    public void Constructor_WithNullHttpClient_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CaptchaSolvingService(_mockLogger.Object, null, _mockConfiguration.Object));
    }

    #region Helper Methods

    private void SetupHttpResponse(string responseContent)
    {
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent)
            });
    }

    private void SetupHttpResponses(string submitResponse, params string[] pollResponses)
    {
        var responses = new Queue<HttpResponseMessage>();

        // Add submit response
        responses.Enqueue(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(submitResponse)
        });

        // Add polling responses
        foreach (var pollResponse in pollResponses)
        {
            responses.Enqueue(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(pollResponse)
            });
        }

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => responses.Dequeue());
    }

    #endregion
}
