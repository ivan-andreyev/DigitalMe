using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.WebNavigation;

/// <summary>
/// Focused interface for element interaction operations
/// Handles clicking, filling forms, and element manipulation
/// Following Interface Segregation Principle
/// </summary>
public interface IWebElementInteractor
{
    /// <summary>
    /// Clicks an element on the current page
    /// </summary>
    /// <param name="selector">CSS selector of the element to click</param>
    /// <param name="options">Click options including modifiers and position</param>
    /// <returns>Click operation result</returns>
    Task<WebNavigationResult> ClickElementAsync(string selector, ClickOptions? options = null);

    /// <summary>
    /// Fills text input fields on the current page
    /// </summary>
    /// <param name="selector">CSS selector of the input element</param>
    /// <param name="text">Text to fill into the input</param>
    /// <param name="options">Fill options including clear behavior</param>
    /// <returns>Fill operation result</returns>
    Task<WebNavigationResult> FillInputAsync(string selector, string text, FillOptions? options = null);
}