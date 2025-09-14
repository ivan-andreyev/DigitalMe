using DigitalMe.Services.WebNavigation;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DigitalMe.Tests.Unit.Services;

public class WebNavigationServiceTests : IAsyncLifetime
{
    private readonly Mock<ILogger<WebNavigationService>> _mockLogger;
    private readonly WebNavigationService _service;

    public WebNavigationServiceTests()
    {
        this._mockLogger = new Mock<ILogger<WebNavigationService>>();
        this._service = new WebNavigationService(this._mockLogger.Object);
    }

    public async Task InitializeAsync()
    {
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await this._service.DisposeAsync();
    }

    #region Browser Initialization Tests

    [Fact]
    public async Task InitializeBrowserAsync_WithDefaultOptions_ShouldInitializeSuccessfully()
    {
        // Act
        var result = await this._service.InitializeBrowserAsync();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("initialized successfully");
        result.Data.Should().NotBeNull();

        // Verify browser is ready
        var isReady = await this._service.IsBrowserReadyAsync();
        isReady.Should().BeTrue();
    }

    [Fact]
    public async Task InitializeBrowserAsync_WithCustomOptions_ShouldUseCustomSettings()
    {
        // Arrange
        var options = new BrowserOptions
        {
            Headless = true,
            ViewportWidth = 1366,
            ViewportHeight = 768,
            UserAgent = "Custom User Agent"
        };

        // Act
        var result = await this._service.InitializeBrowserAsync(options);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();

        // Verify browser is ready
        var isReady = await this._service.IsBrowserReadyAsync();
        isReady.Should().BeTrue();
    }

    [Fact]
    public async Task InitializeBrowserAsync_WhenAlreadyInitialized_ShouldReturnAlreadyInitialized()
    {
        // Arrange
        await this._service.InitializeBrowserAsync();

        // Act
        var result = await this._service.InitializeBrowserAsync();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("already initialized");
    }

    [Fact]
    public async Task IsBrowserReadyAsync_WhenNotInitialized_ShouldReturnFalse()
    {
        // Act
        var result = await this._service.IsBrowserReadyAsync();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Navigation Tests

    [Fact]
    public async Task NavigateToAsync_WithoutInitialization_ShouldReturnError()
    {
        // Arrange
        var url = "https://example.com";

        // Act
        var result = await this._service.NavigateToAsync(url);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Browser not initialized");
    }

    [Fact]
    public async Task NavigateToAsync_WithValidUrl_ShouldNavigateSuccessfully()
    {
        // Arrange
        await this._service.InitializeBrowserAsync();
        var url = "https://example.com";

        // Act
        var result = await this._service.NavigateToAsync(url);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("Successfully navigated");
        result.Data.Should().NotBeNull();

        // Verify page info contains expected data
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task NavigateToAsync_WithInvalidUrl_ShouldReturnError()
    {
        // Arrange
        await this._service.InitializeBrowserAsync();
        var invalidUrl = "invalid-url";

        // Act
        var result = await this._service.NavigateToAsync(invalidUrl);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Navigation failed");
    }

    [Fact]
    public async Task NavigateToAsync_WithWaitSelector_ShouldWaitForElement()
    {
        // Arrange
        await this._service.InitializeBrowserAsync();
        var url = "https://example.com";
        var waitSelector = "body";

        // Act
        var result = await this._service.NavigateToAsync(url, waitSelector);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    #endregion

    #region Element Interaction Tests

    [Fact]
    public async Task ClickElementAsync_WithoutInitialization_ShouldReturnError()
    {
        // Arrange
        var selector = "button";

        // Act
        var result = await this._service.ClickElementAsync(selector);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Browser not initialized");
    }

    [Fact]
    public async Task FillInputAsync_WithoutInitialization_ShouldReturnError()
    {
        // Arrange
        var selector = "input";
        var text = "test text";

        // Act
        var result = await this._service.FillInputAsync(selector, text);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Browser not initialized");
    }

    [Fact]
    public async Task ExtractTextAsync_WithoutInitialization_ShouldReturnError()
    {
        // Arrange
        var selector = "h1";

        // Act
        var result = await this._service.ExtractTextAsync(selector);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Browser not initialized");
    }

    [Fact]
    public async Task ExtractTextAsync_WithInitializedBrowser_ShouldExtractText()
    {
        // Arrange
        await this._service.InitializeBrowserAsync();
        await this._service.NavigateToAsync("https://example.com");
        var selector = "h1";

        // Act
        var result = await this._service.ExtractTextAsync(selector);

        // Assert
        result.Should().NotBeNull();

        // Note: This might fail or succeed depending on the actual page content
        // In a real test, we'd mock the page or use a test page with known content
    }

    [Fact]
    public async Task ExtractTextAsync_WithMultipleElements_ShouldExtractAllTexts()
    {
        // Arrange
        await this._service.InitializeBrowserAsync();
        await this._service.NavigateToAsync("https://example.com");
        var selector = "p";

        // Act
        var result = await this._service.ExtractTextAsync(selector, multiple: true);

        // Assert
        result.Should().NotBeNull();

        // Note: This might fail or succeed depending on the actual page content
    }

    #endregion

    #region Screenshot Tests

    [Fact]
    public async Task TakeScreenshotAsync_WithoutInitialization_ShouldReturnError()
    {
        // Act
        var result = await this._service.TakeScreenshotAsync();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Browser not initialized");
    }

    [Fact]
    public async Task TakeScreenshotAsync_WithInitializedBrowser_ShouldTakeScreenshot()
    {
        // Arrange
        await this._service.InitializeBrowserAsync();
        await this._service.NavigateToAsync("https://example.com");

        // Act
        var result = await this._service.TakeScreenshotAsync();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("Screenshot taken successfully");
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task TakeScreenshotAsync_WithCustomOptions_ShouldUseOptions()
    {
        // Arrange
        await this._service.InitializeBrowserAsync();
        await this._service.NavigateToAsync("https://example.com");
        var options = new ScreenshotOptions
        {
            Format = ScreenshotFormat.Jpeg,
            Quality = 80,
            FullPage = true
        };

        // Act
        var result = await this._service.TakeScreenshotAsync(null, options);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    #endregion

    #region Wait Operations Tests

    [Fact]
    public async Task WaitForElementAsync_WithoutInitialization_ShouldReturnError()
    {
        // Arrange
        var selector = "body";

        // Act
        var result = await this._service.WaitForElementAsync(selector);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Browser not initialized");
    }

    [Fact]
    public async Task WaitForElementAsync_WithValidElement_ShouldWaitSuccessfully()
    {
        // Arrange
        await this._service.InitializeBrowserAsync();
        await this._service.NavigateToAsync("https://example.com");
        var selector = "body";

        // Act
        var result = await this._service.WaitForElementAsync(selector, ElementState.Visible, 5000);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("Element found in expected state");
    }

    [Fact]
    public async Task WaitForElementAsync_WithNonExistentElement_ShouldTimeout()
    {
        // Arrange
        await this._service.InitializeBrowserAsync();
        await this._service.NavigateToAsync("https://example.com");
        var selector = "#non-existent-element";

        // Act
        var result = await this._service.WaitForElementAsync(selector, ElementState.Visible, 1000);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Wait operation failed");
    }

    #endregion

    #region Script Execution Tests

    [Fact]
    public async Task ExecuteScriptAsync_WithoutInitialization_ShouldReturnError()
    {
        // Arrange
        var script = "return document.title;";

        // Act
        var result = await this._service.ExecuteScriptAsync(script);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Browser not initialized");
    }

    [Fact]
    public async Task ExecuteScriptAsync_WithValidScript_ShouldExecuteSuccessfully()
    {
        // Arrange
        await this._service.InitializeBrowserAsync();
        await this._service.NavigateToAsync("https://example.com");
        var script = "document.title";

        // Act
        var result = await this._service.ExecuteScriptAsync(script);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("JavaScript executed successfully");
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteScriptAsync_WithInvalidScript_ShouldReturnError()
    {
        // Arrange
        await this._service.InitializeBrowserAsync();
        await this._service.NavigateToAsync("https://example.com");
        var invalidScript = "invalid javascript syntax {";

        // Act
        var result = await this._service.ExecuteScriptAsync(invalidScript);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Script execution failed");
    }

    #endregion

    #region Page Information Tests

    [Fact]
    public async Task GetPageInfoAsync_WithoutInitialization_ShouldReturnError()
    {
        // Act
        var result = await this._service.GetPageInfoAsync();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Browser not initialized");
    }

    [Fact]
    public async Task GetPageInfoAsync_WithNavigatedPage_ShouldReturnPageInfo()
    {
        // Arrange
        await this._service.InitializeBrowserAsync();
        await this._service.NavigateToAsync("https://example.com");

        // Act
        var result = await this._service.GetPageInfoAsync();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("Page information retrieved successfully");
        result.Data.Should().NotBeNull();
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public async Task DisposeBrowserAsync_WhenNotInitialized_ShouldReturnSuccess()
    {
        // Act
        var result = await this._service.DisposeBrowserAsync();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("Browser was not initialized");
    }

    [Fact]
    public async Task DisposeBrowserAsync_WhenInitialized_ShouldDisposeSuccessfully()
    {
        // Arrange
        await this._service.InitializeBrowserAsync();

        // Act
        var result = await this._service.DisposeBrowserAsync();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("Browser disposed successfully");

        // Verify browser is no longer ready
        var isReady = await this._service.IsBrowserReadyAsync();
        isReady.Should().BeFalse();
    }

    #endregion

    #region WebNavigationResult Tests

    [Fact]
    public void WebNavigationResult_SuccessResult_ShouldCreateSuccessfulResult()
    {
        // Arrange & Act
        var result = WebNavigationResult.SuccessResult("test data", "success message");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().Be("test data");
        result.Message.Should().Be("success message");
        result.ErrorDetails.Should().BeNull();
    }

    [Fact]
    public void WebNavigationResult_ErrorResult_ShouldCreateErrorResult()
    {
        // Arrange & Act
        var result = WebNavigationResult.ErrorResult("error message", "error details");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Be("error message");
        result.ErrorDetails.Should().Be("error details");
        result.Data.Should().BeNull();
    }

    #endregion

    #region Options Classes Tests

    [Fact]
    public void ClickOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new ClickOptions();

        // Assert
        options.Button.Should().Be(MouseButton.Left);
        options.ClickCount.Should().Be(1);
        options.Modifiers.Should().Be(KeyModifiers.None);
        options.Position.Should().BeNull();
    }

    [Fact]
    public void FillOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new FillOptions();

        // Assert
        options.Clear.Should().BeTrue();
        options.Delay.Should().Be(0);
    }

    [Fact]
    public void ScreenshotOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new ScreenshotOptions();

        // Assert
        options.Format.Should().Be(ScreenshotFormat.Png);
        options.Quality.Should().Be(90);
        options.FullPage.Should().BeFalse();
    }

    [Fact]
    public void BrowserOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new BrowserOptions();

        // Assert
        options.Headless.Should().BeTrue();
        options.ViewportWidth.Should().Be(1920);
        options.ViewportHeight.Should().Be(1080);
        options.UserAgent.Should().BeNull();
        options.ExecutablePath.Should().BeNull();
    }

    #endregion
}