using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using DigitalMe.Services;
using DigitalMe.Services.FileProcessing;
using DigitalMe.Services.WebNavigation;
using DigitalMe.Services.CaptchaSolving;
using DigitalMe.Services.Voice;
using DigitalMe.Data.Entities;

namespace DigitalMe.Tests.Unit.Services;

/// <summary>
/// Unit tests for Ivan-Level health check service.
/// Tests monitoring and health status reporting for all Phase B services.
/// </summary>
public class IvanLevelHealthCheckServiceTests : IDisposable
{
    private readonly Mock<IFileProcessingService> _mockFileService;
    private readonly Mock<IWebNavigationService> _mockWebService;
    private readonly Mock<ICaptchaSolvingService> _mockCaptchaService;
    private readonly Mock<IVoiceService> _mockVoiceService;
    private readonly Mock<IIvanPersonalityService> _mockIvanService;
    private readonly Mock<ILogger<IvanLevelHealthCheckService>> _mockLogger;
    private readonly IvanLevelHealthCheckService _healthCheckService;
    private readonly List<string> _tempFilesToCleanup = new();

    public IvanLevelHealthCheckServiceTests()
    {
        _mockFileService = new Mock<IFileProcessingService>();
        _mockWebService = new Mock<IWebNavigationService>();
        _mockCaptchaService = new Mock<ICaptchaSolvingService>();
        _mockVoiceService = new Mock<IVoiceService>();
        _mockIvanService = new Mock<IIvanPersonalityService>();
        _mockLogger = new Mock<ILogger<IvanLevelHealthCheckService>>();

        _healthCheckService = new IvanLevelHealthCheckService(
            _mockFileService.Object,
            _mockWebService.Object,
            _mockCaptchaService.Object,
            _mockVoiceService.Object,
            _mockIvanService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task CheckAllServicesAsync_WithAllHealthyServices_ShouldReturnHealthy()
    {
        // Arrange
        SetupHealthyServices();

        // Act
        var result = await _healthCheckService.CheckAllServicesAsync();

        // Assert
        Assert.True(result.IsHealthy);
        Assert.Equal(1.0, result.OverallHealth);
        Assert.Equal(5, result.ServiceStatuses.Count);
        Assert.All(result.ServiceStatuses, status => Assert.True(status.IsHealthy));
    }

    [Fact]
    public async Task CheckAllServicesAsync_WithSomeUnhealthyServices_ShouldReturnPartialHealth()
    {
        // Arrange
        SetupPartiallyHealthyServices();

        // Act
        var result = await _healthCheckService.CheckAllServicesAsync();

        // Assert
        Assert.False(result.IsHealthy); // Below 80% threshold
        Assert.Equal(0.6, result.OverallHealth); // 3 out of 5 healthy
        Assert.Equal(3, result.HealthyServices.Count());
        Assert.Equal(2, result.UnhealthyServices.Count());
    }

    [Fact]
    public async Task CheckServiceHealthAsync_FileProcessingService_WithValidService_ShouldReturnHealthy()
    {
        // Arrange
        var mockResult = new FileProcessingResult 
        { 
            Success = true, 
            Message = "PDF created successfully" 
        };

        _mockFileService.Setup(x => x.ProcessPdfAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync((string operation, string filePath, Dictionary<string, object> parameters) =>
            {
                // Ensure the file exists at the path the health check will check
                if (!File.Exists(filePath))
                {
                    File.WriteAllText(filePath, "Health check test content");
                    _tempFilesToCleanup.Add(filePath); // Track for cleanup
                }
                return mockResult;
            });
        _mockFileService.Setup(x => x.ExtractTextAsync(It.IsAny<string>()))
            .ReturnsAsync("Health check test content");

        // Act
        var result = await _healthCheckService.CheckServiceHealthAsync("fileprocessing");

        // Assert
        Assert.True(result.IsHealthy);
        Assert.Equal("FileProcessing", result.ServiceName);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task CheckServiceHealthAsync_FileProcessingService_WithFailure_ShouldReturnUnhealthy()
    {
        // Arrange
        var mockResult = new FileProcessingResult 
        { 
            Success = false, 
            Message = "PDF creation failed" 
        };

        _mockFileService.Setup(x => x.ProcessPdfAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync(mockResult);

        // Act
        var result = await _healthCheckService.CheckServiceHealthAsync("fileprocessing");

        // Assert
        Assert.False(result.IsHealthy);
        Assert.Equal("FileProcessing", result.ServiceName);
        Assert.Contains("Failed to create PDF", result.ErrorMessage);
    }

    [Fact]
    public async Task CheckServiceHealthAsync_WebNavigationService_WithInitializedBrowser_ShouldReturnHealthy()
    {
        // Arrange
        var initResult = new WebNavigationResult { Success = true, Message = "Browser initialized" };
        _mockWebService.Setup(x => x.InitializeBrowserAsync(It.IsAny<BrowserOptions?>()))
            .ReturnsAsync(initResult);
        _mockWebService.Setup(x => x.IsBrowserReadyAsync())
            .ReturnsAsync(true);
        _mockWebService.Setup(x => x.DisposeBrowserAsync())
            .ReturnsAsync(WebNavigationResult.SuccessResult());

        // Act
        var result = await _healthCheckService.CheckServiceHealthAsync("webnavigation");

        // Assert
        Assert.True(result.IsHealthy);
        Assert.Equal("WebNavigation", result.ServiceName);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task CheckServiceHealthAsync_CaptchaSolvingService_WithAvailableService_ShouldReturnHealthy()
    {
        // Arrange
        _mockCaptchaService.Setup(x => x.IsServiceAvailableAsync())
            .ReturnsAsync(true);

        // Act
        var result = await _healthCheckService.CheckServiceHealthAsync("captchasolving");

        // Assert
        Assert.True(result.IsHealthy);
        Assert.Equal("CaptchaSolving", result.ServiceName);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task CheckServiceHealthAsync_VoiceService_WithAvailableVoices_ShouldReturnHealthy()
    {
        // Arrange
        _mockVoiceService.Setup(x => x.IsServiceAvailableAsync())
            .ReturnsAsync(true);
        _mockVoiceService.Setup(x => x.GetAvailableVoicesAsync())
            .ReturnsAsync(VoiceResult.SuccessResult(new[] { "alloy", "echo", "nova" }));
        _mockVoiceService.Setup(x => x.GetSupportedAudioFormatsAsync())
            .ReturnsAsync(VoiceResult.SuccessResult(new[] { "mp3", "wav" }));

        // Act
        var result = await _healthCheckService.CheckServiceHealthAsync("voice");

        // Assert
        Assert.True(result.IsHealthy);
        Assert.Equal("Voice", result.ServiceName);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task CheckServiceHealthAsync_IvanPersonalityService_WithValidPersonality_ShouldReturnHealthy()
    {
        // Arrange
        var mockPersonality = new PersonalityProfile
        {
            Name = "Ivan Digital Clone",
            Description = "Test personality"
        };

        _mockIvanService.Setup(x => x.GetIvanPersonalityAsync())
            .ReturnsAsync(mockPersonality);
        _mockIvanService.Setup(x => x.GenerateSystemPrompt(It.IsAny<PersonalityProfile>()))
            .Returns("System prompt for Ivan with structured decision making...");
        _mockIvanService.Setup(x => x.GenerateEnhancedSystemPromptAsync())
            .ReturnsAsync("Enhanced system prompt for Ivan with detailed personality data...");

        // Act
        var result = await _healthCheckService.CheckServiceHealthAsync("ivanpersonality");

        // Assert
        Assert.True(result.IsHealthy);
        Assert.Equal("IvanPersonality", result.ServiceName);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task CheckServiceHealthAsync_UnknownService_ShouldReturnUnhealthy()
    {
        // Act
        var result = await _healthCheckService.CheckServiceHealthAsync("unknownservice");

        // Assert
        Assert.False(result.IsHealthy);
        Assert.Equal("unknownservice", result.ServiceName);
        Assert.Equal("Unknown service", result.ErrorMessage);
    }

    [Fact]
    public async Task GetReadinessScoreAsync_WithAllHealthyServices_ShouldReturnFullScore()
    {
        // Arrange
        SetupHealthyServices();

        // Act
        var score = await _healthCheckService.GetReadinessScoreAsync();

        // Assert
        Assert.Equal(1.0, score);
    }

    [Fact]
    public async Task GetReadinessScoreAsync_WithPartiallyHealthyServices_ShouldReturnPartialScore()
    {
        // Arrange
        SetupPartiallyHealthyServices();

        // Act
        var score = await _healthCheckService.GetReadinessScoreAsync();

        // Assert
        Assert.Equal(0.6, score); // 3 out of 5 services healthy
    }

    [Fact]
    public async Task CheckAllServicesAsync_ShouldLogHealthCheckStart()
    {
        // Arrange
        SetupHealthyServices();

        // Act
        await _healthCheckService.CheckAllServicesAsync();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting comprehensive Ivan-Level health check")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckAllServicesAsync_ShouldLogHealthCheckCompletion()
    {
        // Arrange
        SetupHealthyServices();

        // Act
        await _healthCheckService.CheckAllServicesAsync();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Ivan-Level health check completed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    public void Dispose()
    {
        // Cleanup temp files created during tests
        foreach (var tempFile in _tempFilesToCleanup)
        {
            try
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
            catch
            {
                // Ignore cleanup failures in tests
            }
        }
    }

    #region Helper Methods

    private void SetupHealthyServices()
    {
        var fileResult = new FileProcessingResult { Success = true, Message = "PDF created" };
        _mockFileService.Setup(x => x.ProcessPdfAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync((string operation, string filePath, Dictionary<string, object> parameters) =>
            {
                // Ensure the file exists at the path the health check will check
                if (!File.Exists(filePath))
                {
                    File.WriteAllText(filePath, "Health check test content");
                    _tempFilesToCleanup.Add(filePath); // Track for cleanup
                }
                return fileResult;
            });
        _mockFileService.Setup(x => x.ExtractTextAsync(It.IsAny<string>())).ReturnsAsync("Health check test content");

        // Web Navigation
        var webResult = new WebNavigationResult { Success = true, Message = "Browser initialized" };
        _mockWebService.Setup(x => x.InitializeBrowserAsync(It.IsAny<BrowserOptions?>())).ReturnsAsync(webResult);
        _mockWebService.Setup(x => x.IsBrowserReadyAsync()).ReturnsAsync(true);
        _mockWebService.Setup(x => x.DisposeBrowserAsync()).ReturnsAsync(WebNavigationResult.SuccessResult());

        // CAPTCHA Solving
        _mockCaptchaService.Setup(x => x.IsServiceAvailableAsync()).ReturnsAsync(true);

        // Voice Service
        _mockVoiceService.Setup(x => x.IsServiceAvailableAsync()).ReturnsAsync(true);
        _mockVoiceService.Setup(x => x.GetAvailableVoicesAsync()).ReturnsAsync(VoiceResult.SuccessResult(new[] { "alloy", "echo" }));
        _mockVoiceService.Setup(x => x.GetSupportedAudioFormatsAsync()).ReturnsAsync(VoiceResult.SuccessResult(new[] { "mp3", "wav" }));

        // Ivan Personality
        var personality = new PersonalityProfile { Name = "Ivan Digital Clone" };
        _mockIvanService.Setup(x => x.GetIvanPersonalityAsync()).ReturnsAsync(personality);
        _mockIvanService.Setup(x => x.GenerateSystemPrompt(It.IsAny<PersonalityProfile>())).Returns("Ivan system prompt");
        _mockIvanService.Setup(x => x.GenerateEnhancedSystemPromptAsync()).ReturnsAsync("Enhanced Ivan system prompt");
    }

    private void SetupPartiallyHealthyServices()
    {
        // File Processing - Healthy
        var fileResult = new FileProcessingResult { Success = true, Message = "PDF created" };
        _mockFileService.Setup(x => x.ProcessPdfAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync((string operation, string filePath, Dictionary<string, object> parameters) =>
            {
                // Ensure the file exists at the path the health check will check
                if (!File.Exists(filePath))
                {
                    File.WriteAllText(filePath, "Health check test content");
                    _tempFilesToCleanup.Add(filePath); // Track for cleanup
                }
                return fileResult;
            });
        _mockFileService.Setup(x => x.ExtractTextAsync(It.IsAny<string>())).ReturnsAsync("Health check test content");

        // Web Navigation - Unhealthy
        _mockWebService.Setup(x => x.InitializeBrowserAsync(It.IsAny<BrowserOptions?>())).Throws(new Exception("Browser failed to start"));

        // CAPTCHA Solving - Healthy
        _mockCaptchaService.Setup(x => x.IsServiceAvailableAsync()).ReturnsAsync(true);

        // Voice Service - Healthy
        _mockVoiceService.Setup(x => x.IsServiceAvailableAsync()).ReturnsAsync(true);
        _mockVoiceService.Setup(x => x.GetAvailableVoicesAsync()).ReturnsAsync(VoiceResult.SuccessResult(new[] { "alloy" }));
        _mockVoiceService.Setup(x => x.GetSupportedAudioFormatsAsync()).ReturnsAsync(VoiceResult.SuccessResult(new[] { "mp3" }));

        // Ivan Personality - Unhealthy
        _mockIvanService.Setup(x => x.GetIvanPersonalityAsync()).ThrowsAsync(new Exception("Profile data not found"));
    }

    #endregion
}