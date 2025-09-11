using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.WebNavigation;

/// <summary>
/// Service for web navigation and automation capabilities
/// Provides Ivan-Level web interaction including page navigation, element interaction, and content extraction
/// Following Clean Architecture patterns with dependency injection
/// </summary>
public interface IWebNavigationService
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

    /// <summary>
    /// Waits for a specific element or condition on the page
    /// </summary>
    /// <param name="selector">CSS selector to wait for</param>
    /// <param name="state">Element state to wait for (visible, hidden, attached, etc.)</param>
    /// <param name="timeout">Wait timeout in milliseconds (default: 30000)</param>
    /// <returns>Wait operation result</returns>
    Task<WebNavigationResult> WaitForElementAsync(string selector, ElementState state = ElementState.Visible, int timeout = 30000);

    /// <summary>
    /// Executes JavaScript code in the browser context
    /// </summary>
    /// <param name="script">JavaScript code to execute</param>
    /// <param name="args">Arguments to pass to the script</param>
    /// <returns>Script execution result</returns>
    Task<WebNavigationResult> ExecuteScriptAsync(string script, params object[] args);

    /// <summary>
    /// Gets current page information including URL, title, and metadata
    /// </summary>
    /// <returns>Current page information</returns>
    Task<WebNavigationResult> GetPageInfoAsync();

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

/// <summary>
/// Result object for web navigation operations
/// Contains operation success status, data, and error details
/// </summary>
public class WebNavigationResult
{
    public bool Success { get; init; }
    public object? Data { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? ErrorDetails { get; init; }

    /// <summary>
    /// Creates a successful result with data and message
    /// </summary>
    /// <param name="data">Operation result data</param>
    /// <param name="message">Success message</param>
    /// <returns>Successful WebNavigationResult</returns>
    public static WebNavigationResult SuccessResult(object? data = null, string message = "Operation completed successfully")
        => new() { Success = true, Data = data, Message = message };

    /// <summary>
    /// Creates an error result with message and details
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="details">Detailed error information</param>
    /// <returns>Error WebNavigationResult</returns>
    public static WebNavigationResult ErrorResult(string message, string? details = null)
        => new() { Success = false, Message = message, ErrorDetails = details };
}

/// <summary>
/// Options for click operations
/// </summary>
public class ClickOptions
{
    /// <summary>
    /// Mouse button to use for clicking
    /// </summary>
    public MouseButton Button { get; set; } = MouseButton.Left;

    /// <summary>
    /// Number of clicks (for double-click, etc.)
    /// </summary>
    public int ClickCount { get; set; } = 1;

    /// <summary>
    /// Keyboard modifiers to hold during click
    /// </summary>
    public KeyModifiers Modifiers { get; set; } = KeyModifiers.None;

    /// <summary>
    /// Specific position to click relative to element
    /// </summary>
    public Position? Position { get; set; }
}

/// <summary>
/// Options for text input fill operations
/// </summary>
public class FillOptions
{
    /// <summary>
    /// Whether to clear the input before filling
    /// </summary>
    public bool Clear { get; set; } = true;

    /// <summary>
    /// Delay between keystrokes in milliseconds
    /// </summary>
    public int Delay { get; set; } = 0;
}

/// <summary>
/// Options for screenshot operations
/// </summary>
public class ScreenshotOptions
{
    /// <summary>
    /// Screenshot format
    /// </summary>
    public ScreenshotFormat Format { get; set; } = ScreenshotFormat.Png;

    /// <summary>
    /// Image quality (0-100, only for JPEG)
    /// </summary>
    public int Quality { get; set; } = 90;

    /// <summary>
    /// Whether to capture full page
    /// </summary>
    public bool FullPage { get; set; } = false;
}

/// <summary>
/// Browser launch options
/// </summary>
public class BrowserOptions
{
    /// <summary>
    /// Whether to run browser in headless mode
    /// </summary>
    public bool Headless { get; set; } = true;

    /// <summary>
    /// Browser viewport width
    /// </summary>
    public int ViewportWidth { get; set; } = 1920;

    /// <summary>
    /// Browser viewport height
    /// </summary>
    public int ViewportHeight { get; set; } = 1080;

    /// <summary>
    /// User agent string to use
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Browser executable path (optional)
    /// </summary>
    public string? ExecutablePath { get; set; }
}

/// <summary>
/// Mouse button enumeration
/// </summary>
public enum MouseButton
{
    Left,
    Right,
    Middle
}

/// <summary>
/// Keyboard modifier flags
/// </summary>
[Flags]
public enum KeyModifiers
{
    None = 0,
    Alt = 1,
    Control = 2,
    Meta = 4,
    Shift = 8
}

/// <summary>
/// Element state enumeration for waiting operations
/// </summary>
public enum ElementState
{
    Attached,
    Detached,
    Visible,
    Hidden
}

/// <summary>
/// Screenshot format enumeration
/// </summary>
public enum ScreenshotFormat
{
    Png,
    Jpeg
}

/// <summary>
/// Position coordinates
/// </summary>
public record Position(double X, double Y);