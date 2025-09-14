using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.WebNavigation;

/// <summary>
/// Focused interface for script execution operations
/// Handles JavaScript execution in browser context
/// Following Interface Segregation Principle
/// </summary>
public interface IWebScriptExecutor
{
    /// <summary>
    /// Executes JavaScript code in the browser context
    /// </summary>
    /// <param name="script">JavaScript code to execute</param>
    /// <param name="args">Arguments to pass to the script</param>
    /// <returns>Script execution result</returns>
    Task<WebNavigationResult> ExecuteScriptAsync(string script, params object[] args);
}