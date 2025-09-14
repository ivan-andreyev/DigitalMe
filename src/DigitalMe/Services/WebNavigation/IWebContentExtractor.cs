using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.WebNavigation;

/// <summary>
/// Focused interface for content extraction operations
/// Handles text extraction and screenshot capture
/// Following Interface Segregation Principle
/// </summary>
public interface IWebContentExtractor
{
    /// <summary>
    /// Extracts text content from specified elements
    /// </summary>
    /// <param name="selector">CSS selector of elements to extract text from</param>
    /// <param name="multiple">Whether to extract from multiple matching elements</param>
    /// <returns>Extracted text content</returns>
    Task<WebNavigationResult> ExtractTextAsync(string selector, bool multiple = false);

    /// <summary>
    /// Takes a screenshot of the current page or specific element
    /// </summary>
    /// <param name="selector">Optional CSS selector to screenshot specific element</param>
    /// <param name="options">Screenshot options including format and quality</param>
    /// <returns>Screenshot data as byte array</returns>
    Task<WebNavigationResult> TakeScreenshotAsync(string? selector = null, ScreenshotOptions? options = null);
}