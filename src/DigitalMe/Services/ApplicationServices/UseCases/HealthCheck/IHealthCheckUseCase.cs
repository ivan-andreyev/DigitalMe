using DigitalMe.Common;
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
    Task<Result<ComprehensiveHealthCheckResult>> ExecuteAsync(ComprehensiveHealthCheckCommand command);
}

/// <summary>
/// Command for comprehensive health check operations.
/// </summary>
public record ComprehensiveHealthCheckCommand(
    string? testContent = null,
    bool includeWebNavigation = false,
    bool includeCaptcha = false);

/// <summary>
/// Result of comprehensive health check operations.
/// </summary>
public record ComprehensiveHealthCheckResult(
    bool overallSuccess,
    DateTime timestamp,
    Dictionary<string, object> testResults,
    ComprehensiveTestSummary summary);

/// <summary>
/// Summary of comprehensive test results.
/// </summary>
public record ComprehensiveTestSummary(
    int totalTests,
    int passedTests,
    int failedTests);