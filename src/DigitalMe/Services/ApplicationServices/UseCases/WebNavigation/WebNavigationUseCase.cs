using DigitalMe.Services.WebNavigation;
using Microsoft.Extensions.Logging;

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
                    success: false,
                    browserInitialized: false,
                    message: "Browser failed to initialize",
                    errorMessage: initResult.Message);
            }

            // Step 2: Clean up
            await _webNavigationService.DisposeBrowserAsync();

            return new WebNavigationResult(
                success: true,
                browserInitialized: true,
                message: "Web navigation service is functional");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Web navigation workflow failed");
            return new WebNavigationResult(
                success: false,
                browserInitialized: false,
                message: "Web navigation workflow failed",
                errorMessage: ex.Message);
        }
    }
}