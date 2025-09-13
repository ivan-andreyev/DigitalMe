using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.WebNavigation;

/// <summary>
/// Focused interface for core navigation operations
/// Handles page navigation and waiting for elements
/// Following Interface Segregation Principle
/// </summary>
public interface IWebNavigator
{
    /// <summary>
    /// Navigates to a specified URL and waits for page load
    /// </summary>
    /// <param name="url">Target URL to navigate to</param>
    /// <param name="waitForSelector">Optional CSS selector to wait for before considering navigation complete</param>
    /// <param name="timeout">Navigation timeout in milliseconds (default: 30000)</param>
    /// <returns>Navigation result with page metadata</returns>
    Task<WebNavigationResult> NavigateToAsync(string url, string? waitForSelector = null, int timeout = 30000);

    /// <summary>
    /// Waits for a specific element or condition on the page
    /// </summary>
    /// <param name="selector">CSS selector to wait for</param>
    /// <param name="state">Element state to wait for (visible, hidden, attached, etc.)</param>
    /// <param name="timeout">Wait timeout in milliseconds (default: 30000)</param>
    /// <returns>Wait operation result</returns>
    Task<WebNavigationResult> WaitForElementAsync(string selector, ElementState state = ElementState.Visible, int timeout = 30000);

    /// <summary>
    /// Gets current page information including URL, title, and metadata
    /// </summary>
    /// <returns>Current page information</returns>
    Task<WebNavigationResult> GetPageInfoAsync();
}