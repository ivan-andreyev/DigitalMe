using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.WebNavigation;

/// <summary>
/// Focused interface for browser lifecycle management
/// Handles initialization, disposal, and status checking
/// Following Interface Segregation Principle
/// </summary>
public interface IWebBrowserManager
{
    /// <summary>
    /// Initializes browser instance for web navigation
    /// Should be called before using other navigation methods
    /// </summary>
    /// <param name="options">Browser launch options</param>
    /// <returns>Initialization result</returns>
    Task<WebNavigationResult> InitializeBrowserAsync(BrowserOptions? options = null);

    /// <summary>
    /// Disposes browser resources and closes all browser instances
    /// Should be called when navigation is no longer needed
    /// </summary>
    /// <returns>Disposal result</returns>
    Task<WebNavigationResult> DisposeBrowserAsync();

    /// <summary>
    /// Checks if browser is currently initialized and ready for navigation
    /// </summary>
    /// <returns>True if browser is ready, false otherwise</returns>
    Task<bool> IsBrowserReadyAsync();
}