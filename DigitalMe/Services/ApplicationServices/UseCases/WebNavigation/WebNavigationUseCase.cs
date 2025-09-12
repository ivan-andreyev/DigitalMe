using Microsoft.Extensions.Logging;
using DigitalMe.Services.WebNavigation;

namespace DigitalMe.Services.ApplicationServices.UseCases.WebNavigation;

/// <summary>
/// Implementation of web navigation use case.
/// Focuses solely on web navigation business logic.
/// </summary>
public class WebNavigationUseCase : IWebNavigationUseCase
{
    private readonly IWebNavigationService _webNavigationService;
    private readonly ILogger<WebNavigationUseCase> _logger;

    public WebNavigationUseCase(
        IWebNavigationService webNavigationService,
        ILogger<WebNavigationUseCase> logger)
    {
        _webNavigationService = webNavigationService;
        _logger = logger;
    }

    public async Task<WebNavigationResult> ExecuteAsync()
    {
        try
        {
            _logger.LogInformation("Executing web navigation workflow");

            // Step 1: Test browser initialization
            var initResult = await _webNavigationService.InitializeBrowserAsync();
            var isReady = await _webNavigationService.IsBrowserReadyAsync();

            if (!initResult.Success || !isReady)
            {
                return new WebNavigationResult(
                    Success: false,
                    BrowserInitialized: false,
                    Message: "Browser failed to initialize",
                    ErrorMessage: initResult.Message);
            }

            // Step 2: Clean up
            await _webNavigationService.DisposeBrowserAsync();

            return new WebNavigationResult(
                Success: true,
                BrowserInitialized: true,
                Message: "Web navigation service is functional");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Web navigation workflow failed");
            return new WebNavigationResult(
                Success: false,
                BrowserInitialized: false,
                Message: "Web navigation workflow failed",
                ErrorMessage: ex.Message);
        }
    }
}