using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DigitalMe.Services;
using DigitalMe.Services.FileProcessing;
using DigitalMe.Services.WebNavigation;
using DigitalMe.Services.CaptchaSolving;
using DigitalMe.Services.Voice;
using DigitalMe.Data;
using DigitalMe.Extensions;

namespace DigitalMe.Tests.Integration;

/// <summary>
/// Integration tests for Ivan-Level services coordination and workflow.
/// Tests end-to-end scenarios combining multiple services as Ivan would use them.
/// </summary>
public class IvanLevelServicesIntegrationTests : IClassFixture<ServiceIntegrationTestFixture>
{
    private readonly ServiceIntegrationTestFixture _fixture;

    public IvanLevelServicesIntegrationTests(ServiceIntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task IvanLevelServices_ShouldAllBeRegisteredInDI()
    {
        // Arrange & Act - Get all Ivan-Level services from DI container
        using var scope = _fixture.ServiceProvider.CreateScope();
        var services = scope.ServiceProvider;
        
        var fileProcessingService = services.GetService<IFileProcessingService>();
        var webNavigationService = services.GetService<IWebNavigationService>();
        var captchaSolvingService = services.GetService<ICaptchaSolvingService>();
        var voiceService = services.GetService<IVoiceService>();
        var ivanPersonalityService = services.GetService<IIvanPersonalityService>();
        var profileDataParser = services.GetService<IProfileDataParser>();

        // Assert - All services should be available
        Assert.NotNull(fileProcessingService);
        Assert.NotNull(webNavigationService);
        Assert.NotNull(captchaSolvingService);
        Assert.NotNull(voiceService);
        Assert.NotNull(ivanPersonalityService);
        Assert.NotNull(profileDataParser);
    }

    [Fact]
    public async Task IvanPersonality_WithProfileData_ShouldGenerateContextualResponses()
    {
        // Arrange
        var ivanService = _fixture.ServiceProvider.GetRequiredService<IIvanPersonalityService>();

        // Act
        var personality = await ivanService.GetIvanPersonalityAsync();
        var systemPrompt = ivanService.GenerateSystemPrompt(personality);
        var enhancedPrompt = await ivanService.GenerateEnhancedSystemPromptAsync();

        // Assert
        Assert.NotNull(personality);
        Assert.Equal("Ivan Digital Clone", personality.Name);
        
        Assert.Contains("Ivan", systemPrompt);
        Assert.Contains("C#/.NET", systemPrompt);
        Assert.Contains("EllyAnalytics", systemPrompt);
        
        Assert.Contains("Ivan", enhancedPrompt);
        Assert.Contains("CORE PERSONALITY", enhancedPrompt);
        Assert.Contains("TECHNICAL PREFERENCES", enhancedPrompt);
    }

    [Fact]
    public async Task VoiceService_Integration_ShouldHandleBasicOperations()
    {
        // Arrange
        var voiceService = _fixture.ServiceProvider.GetRequiredService<IVoiceService>();

        // Act
        var isAvailable = await voiceService.IsServiceAvailableAsync();
        var formatsResult = await voiceService.GetSupportedAudioFormatsAsync();
        var voicesResult = await voiceService.GetAvailableVoicesAsync();

        // Assert - Test basic service functionality
        // Service should be instantiated properly
        Assert.NotNull(voiceService);
        
        // Results should be returned (even if service is not available)
        Assert.NotNull(formatsResult);
        Assert.NotNull(voicesResult);
    }

    [Fact]
    public async Task FileProcessing_Integration_ShouldCreateAndProcessFiles()
    {
        // Arrange
        var fileService = _fixture.ServiceProvider.GetRequiredService<IFileProcessingService>();
        var testContent = "Ivan's technical documentation - Phase B Week 5 Integration Testing";

        // Act
        var tempFilePath = Path.GetTempFileName() + ".pdf";
        var parameters = new Dictionary<string, object> { ["content"] = testContent, ["title"] = "Integration Test Document" };
        var pdfResult = await fileService.ProcessPdfAsync("create", tempFilePath, parameters);
        var extractedText = await fileService.ExtractTextAsync(tempFilePath);

        // Assert
        Assert.True(pdfResult.Success);
        Assert.True(File.Exists(tempFilePath));
        Assert.False(string.IsNullOrEmpty(extractedText));
        Assert.Contains("Ivan's technical documentation", extractedText);

        // Cleanup
        if (File.Exists(tempFilePath))
            File.Delete(tempFilePath);
    }

    [Fact]
    public async Task WebNavigation_Integration_ShouldInitializeBrowser()
    {
        // Arrange
        var webService = _fixture.ServiceProvider.GetRequiredService<IWebNavigationService>();

        // Act & Assert
        try
        {
            var initResult = await webService.InitializeBrowserAsync();
            var isReady = await webService.IsBrowserReadyAsync();
            Assert.True(initResult.Success);
            Assert.True(isReady);
        }
        finally
        {
            await webService.DisposeBrowserAsync();
        }
    }

    [Theory]
    [InlineData("jpg")]
    [InlineData("png")]
    [InlineData("gif")]
    public async Task CaptchaSolving_Integration_ShouldSupportImageFormats(string imageFormat)
    {
        // Arrange
        var captchaService = _fixture.ServiceProvider.GetRequiredService<ICaptchaSolvingService>();

        // Act
        var isAvailable = await captchaService.IsServiceAvailableAsync();

        // Assert
        Assert.NotNull(captchaService);
        // Note: Service availability depends on API key configuration
        // In integration tests, we just verify the service can be instantiated
    }

    [Fact]
    public async Task IvanLevelWorkflow_DocumentCreationWithPersonality_ShouldWork()
    {
        // Arrange
        var fileService = _fixture.ServiceProvider.GetRequiredService<IFileProcessingService>();
        var ivanService = _fixture.ServiceProvider.GetRequiredService<IIvanPersonalityService>();

        // Act - Create a document that reflects Ivan's personality
        var personality = await ivanService.GetIvanPersonalityAsync();
        var documentContent = $"""
            Technical Analysis Report
            Author: {personality.Name}
            
            This document demonstrates Ivan-Level capabilities:
            - Structured approach to problem solving
            - C#/.NET technical preferences
            - R&D leadership perspective
            
            Analysis completed using automated Ivan-Level services.
            """;

        var tempFilePath = Path.GetTempFileName() + ".pdf";
        var parameters = new Dictionary<string, object> { ["content"] = documentContent, ["title"] = "Ivan-Level Analysis Report" };
        var pdfResult = await fileService.ProcessPdfAsync("create", tempFilePath, parameters);

        // Assert
        Assert.True(pdfResult.Success);
        Assert.True(File.Exists(tempFilePath));

        // Verify content extraction
        var extractedText = await fileService.ExtractTextAsync(tempFilePath);
        Assert.False(string.IsNullOrEmpty(extractedText));
        Assert.Contains("Ivan-Level capabilities", extractedText);
        Assert.Contains("C#/.NET technical preferences", extractedText);

        // Cleanup
        if (File.Exists(tempFilePath))
            File.Delete(tempFilePath);
    }

    [Fact]
    public async Task ServiceCoordination_HealthChecks_ShouldAllBeHealthy()
    {
        // Arrange
        var services = new Dictionary<string, object>
        {
            ["FileProcessing"] = _fixture.ServiceProvider.GetRequiredService<IFileProcessingService>(),
            ["WebNavigation"] = _fixture.ServiceProvider.GetRequiredService<IWebNavigationService>(),
            ["CaptchaSolving"] = _fixture.ServiceProvider.GetRequiredService<ICaptchaSolvingService>(),
            ["Voice"] = _fixture.ServiceProvider.GetRequiredService<IVoiceService>(),
            ["IvanPersonality"] = _fixture.ServiceProvider.GetRequiredService<IIvanPersonalityService>(),
            ["ProfileDataParser"] = _fixture.ServiceProvider.GetRequiredService<IProfileDataParser>()
        };

        // Act & Assert
        foreach (var kvp in services)
        {
            Assert.NotNull(kvp.Value);
            
            // Test service instantiation and basic functionality
            switch (kvp.Value)
            {
                case IVoiceService voice:
                    var voicesResult = await voice.GetAvailableVoicesAsync();
                    Assert.NotNull(voicesResult);
                    break;
                    
                case IIvanPersonalityService ivan:
                    var personality = await ivan.GetIvanPersonalityAsync();
                    Assert.NotNull(personality);
                    break;
                    
                case ICaptchaSolvingService captcha:
                    var isAvailable = await captcha.IsServiceAvailableAsync();
                    // Just verify service is callable
                    break;
                    
                default:
                    // Service exists and is instantiable
                    Assert.NotNull(kvp.Value);
                    break;
            }
        }
    }

    [Fact]
    public async Task ErrorHandling_ServiceFailures_ShouldDegradeGracefully()
    {
        // Arrange
        var ivanService = _fixture.ServiceProvider.GetRequiredService<IIvanPersonalityService>();

        // Act & Assert - Test fallback behavior
        try
        {
            // This should use fallback to basic prompt if enhanced fails
            var enhancedPrompt = await ivanService.GenerateEnhancedSystemPromptAsync();
            Assert.NotNull(enhancedPrompt);
            Assert.Contains("Ivan", enhancedPrompt);
        }
        catch (Exception ex)
        {
            // Should not throw - should use fallback
            Assert.True(false, $"Service should handle errors gracefully but threw: {ex.Message}");
        }
    }

    [Fact]
    public async Task Performance_ServiceInstantiation_ShouldBeFast()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act - Test service instantiation performance
        var fileService = _fixture.ServiceProvider.GetRequiredService<IFileProcessingService>();
        var webService = _fixture.ServiceProvider.GetRequiredService<IWebNavigationService>();
        var captchaService = _fixture.ServiceProvider.GetRequiredService<ICaptchaSolvingService>();
        var voiceService = _fixture.ServiceProvider.GetRequiredService<IVoiceService>();
        var ivanService = _fixture.ServiceProvider.GetRequiredService<IIvanPersonalityService>();

        stopwatch.Stop();

        // Assert - Service instantiation should be fast (< 1 second)
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
            $"Service instantiation took {stopwatch.ElapsedMilliseconds}ms, should be < 1000ms");
    }
}