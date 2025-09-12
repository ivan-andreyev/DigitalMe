using DigitalMe.Services.ApplicationServices.Commands;
using DigitalMe.Services.ApplicationServices.Queries;

namespace DigitalMe.Services.ApplicationServices.UseCases.HealthCheck;

/// <summary>
/// Use case for health checking operations.
/// Implements Clean Architecture Application Services layer patterns.
/// </summary>
public interface IHealthCheckUseCase : IApplicationService
{
    /// <summary>
    /// Executes comprehensive health check across all services.
    /// </summary>
    Task<ComprehensiveHealthCheckResult> ExecuteAsync(ComprehensiveHealthCheckCommand command);
}

/// <summary>
/// Command for comprehensive health check operations.
/// </summary>
public record ComprehensiveHealthCheckCommand(
    string? TestContent = null,
    bool IncludeWebNavigation = false,
    bool IncludeCaptcha = false);

/// <summary>
/// Result of comprehensive health check operations.
/// </summary>
public record ComprehensiveHealthCheckResult(
    bool OverallSuccess,
    DateTime Timestamp,
    Dictionary<string, object> TestResults,
    ComprehensiveTestSummary Summary);

/// <summary>
/// Summary of comprehensive test results.
/// </summary>
public record ComprehensiveTestSummary(
    int TotalTests,
    int PassedTests,
    int FailedTests);