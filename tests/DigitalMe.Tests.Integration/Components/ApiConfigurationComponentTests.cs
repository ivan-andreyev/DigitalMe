using Bunit;
using DigitalMe.Services;
using DigitalMe.Web.Pages.Settings;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace DigitalMe.Tests.Integration.Components;

/// <summary>
/// TDD tests for ApiConfiguration Blazor component (Phase 6, Task 6.1).
/// Tests component rendering, key masking, connection testing, and save functionality.
/// </summary>
public class ApiConfigurationComponentTests : TestContext
{
    private readonly Mock<IApiConfigurationService> _mockConfigService;
    private readonly Mock<DigitalMe.Services.Usage.IQuotaManager> _mockQuotaManager;

    public ApiConfigurationComponentTests()
    {
        _mockConfigService = new Mock<IApiConfigurationService>();
        _mockQuotaManager = new Mock<DigitalMe.Services.Usage.IQuotaManager>();

        // Setup default quota manager behaviors
        _mockQuotaManager
            .Setup(q => q.GetQuotaStatusAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new DigitalMe.Models.Usage.QuotaStatus
            {
                DailyLimit = 10000,
                Used = 1000,
                Remaining = 9000,
                PercentUsed = 10.0m,
                ResetsAt = DateTime.UtcNow.AddHours(12)
            });

        _mockQuotaManager
            .Setup(q => q.CanUseTokensAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(true);

        // Register services
        Services.AddScoped(_ => _mockConfigService.Object);
        Services.AddScoped(_ => _mockQuotaManager.Object);

        // Mock authentication state
        var authState = Task.FromResult(new AuthenticationState(
            new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(new[]
                {
                    new System.Security.Claims.Claim("sub", "test-user-123"),
                    new System.Security.Claims.Claim("name", "Test User")
                }, "mock"))));

        Services.AddScoped<AuthenticationStateProvider>(_ =>
        {
            var mock = new Mock<AuthenticationStateProvider>();
            mock.Setup(x => x.GetAuthenticationStateAsync()).Returns(authState);
            return mock.Object;
        });
    }

    [Fact]
    public void Component_Should_Display_Provider_Cards()
    {
        // Arrange
        _mockConfigService
            .Setup(s => s.GetActiveConfigurationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((DigitalMe.Data.Entities.ApiConfiguration?)null);

        // Act
        var component = RenderComponent<ApiConfiguration>();

        // Assert
        var providerCards = component.FindAll(".provider-card");
        providerCards.Should().NotBeEmpty("component should render provider cards");
        providerCards.Count.Should().BeGreaterThanOrEqualTo(4, "should render at least Anthropic, OpenAI, Slack, GitHub");
    }

    [Fact]
    public void ProviderCard_Should_Mask_API_Keys_By_Default()
    {
        // Arrange
        var testConfig = new DigitalMe.Data.Entities.ApiConfiguration
        {
            Provider = "Anthropic",
            UserId = "test-user-123",
            IsActive = true,
            KeyFingerprint = "****1234"
        };

        _mockConfigService
            .Setup(s => s.GetActiveConfigurationAsync("test-user-123", "Anthropic"))
            .ReturnsAsync(testConfig);

        // Act
        var component = RenderComponent<ApiConfiguration>();

        // Assert
        var maskedInputs = component.FindAll("input[type='password']");
        maskedInputs.Should().NotBeEmpty("configured keys should be masked by default");
    }

    [Fact]
    public void ProviderCard_Should_Show_Edit_Mode_For_Unconfigured_Provider()
    {
        // Arrange
        _mockConfigService
            .Setup(s => s.GetActiveConfigurationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((DigitalMe.Data.Entities.ApiConfiguration?)null);

        // Act
        var component = RenderComponent<ApiConfiguration>();

        // Assert
        var editInputs = component.FindAll("input[type='password']");
        editInputs.Should().NotBeEmpty("unconfigured providers should show input for API key");
    }

    [Fact]
    public async Task SaveKey_Should_Call_ConfigService_SetUserApiKeyAsync()
    {
        // Arrange
        const string testUserId = "test-user-123";
        const string testProvider = "Anthropic";
        const string testApiKey = "sk-ant-api03-test-key-AA";

        _mockConfigService
            .Setup(s => s.GetActiveConfigurationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((DigitalMe.Data.Entities.ApiConfiguration?)null);

        _mockConfigService
            .Setup(s => s.SetUserApiKeyAsync(testProvider, testUserId, testApiKey))
            .Returns(Task.CompletedTask);

        var component = RenderComponent<ApiConfiguration>();

        // Find input for Anthropic provider
        var inputs = component.FindAll("input[type='password']");
        var anthropicInput = inputs.FirstOrDefault();
        anthropicInput.Should().NotBeNull("should have API key input");

        // Act: Enter API key and click save
        anthropicInput!.Input(testApiKey);

        var saveButtons = component.FindAll(".btn-primary");
        if (saveButtons.Any())
        {
            await saveButtons.First().ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        }

        // Assert
        _mockConfigService.Verify(
            s => s.SetUserApiKeyAsync(It.IsAny<string>(), testUserId, testApiKey),
            Times.AtLeastOnce,
            "SaveKey should invoke SetUserApiKeyAsync");
    }

    [Fact]
    public void Component_Should_Display_Configured_Badge_For_Active_Keys()
    {
        // Arrange
        var testConfig = new DigitalMe.Data.Entities.ApiConfiguration
        {
            Provider = "Anthropic",
            UserId = "test-user-123",
            IsActive = true,
            KeyFingerprint = "****5678"
        };

        _mockConfigService
            .Setup(s => s.GetActiveConfigurationAsync("test-user-123", "Anthropic"))
            .ReturnsAsync(testConfig);

        _mockConfigService
            .Setup(s => s.GetActiveConfigurationAsync("test-user-123", It.Is<string>(p => p != "Anthropic")))
            .ReturnsAsync((DigitalMe.Data.Entities.ApiConfiguration?)null);

        // Act
        var component = RenderComponent<ApiConfiguration>();

        // Assert
        var badges = component.FindAll(".badge");
        var configuredBadges = badges.Where(b => b.TextContent.Contains("Configured")).ToList();
        configuredBadges.Should().NotBeEmpty("should show 'Configured' badge for active keys");
    }

    [Fact]
    public void Component_Should_Render_Without_Errors_When_No_Configuration_Exists()
    {
        // Arrange
        _mockConfigService
            .Setup(s => s.GetActiveConfigurationAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((DigitalMe.Data.Entities.ApiConfiguration?)null);

        // Act
        var act = () => RenderComponent<ApiConfiguration>();

        // Assert
        act.Should().NotThrow("component should handle missing configuration gracefully");
    }
}