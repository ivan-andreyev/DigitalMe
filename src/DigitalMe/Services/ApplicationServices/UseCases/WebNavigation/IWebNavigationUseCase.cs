using DigitalMe.Services.ApplicationServices.Commands;
using DigitalMe.Services.ApplicationServices.Queries;

namespace DigitalMe.Services.ApplicationServices.UseCases.WebNavigation;

/// <summary>
/// Use case for web navigation operations.
/// Implements Clean Architecture Application Services layer patterns.
/// </summary>
public interface IWebNavigationUseCase : IApplicationService
{
    /// <summary>
    /// Executes web navigation testing workflow.
    /// </summary>
    Task<WebNavigationResult> ExecuteAsync();
}

/// <summary>
/// Result of web navigation operations.
/// </summary>
public record WebNavigationResult(
    bool success,
    bool browserInitialized,
    string message,
    string? errorMessage = null);