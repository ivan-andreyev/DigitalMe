using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.WebNavigation;

/// <summary>
/// Comprehensive web navigation service interface.
/// Composes focused interfaces following Interface Segregation Principle.
/// Provides backward compatibility for existing consumers.
/// </summary>
public interface IWebNavigationService : 
    IWebBrowserManager,
    IWebNavigator,
    IWebElementInteractor,
    IWebContentExtractor,
    IWebScriptExecutor
{
    // All methods inherited from focused interfaces
    // This maintains backward compatibility while allowing clients
    // to depend only on the specific capabilities they need
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
public record Position(double x, double y);