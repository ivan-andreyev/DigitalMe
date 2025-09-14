using DigitalMe.Services.ApplicationServices.UseCases.FileProcessing;
using DigitalMe.Services.ApplicationServices.UseCases.HealthCheck;
using DigitalMe.Services.ApplicationServices.UseCases.ServiceAvailability;
using DigitalMe.Services.ApplicationServices.UseCases.WebNavigation;

namespace DigitalMe.Services.ApplicationServices.Orchestrators;

/// <summary>
/// Orchestrator for Ivan-Level workflows.
/// Composes focused use cases to achieve complex business outcomes.
/// Follows Clean Architecture by delegating to specialized use cases.
/// </summary>
public interface IWorkflowOrchestrator : IApplicationService
{
    /// <summary>
    /// Orchestrates file processing workflow.
    /// </summary>
    Task<FileProcessingResult> ExecuteFileProcessingWorkflowAsync(FileProcessingCommand command);

    /// <summary>
    /// Orchestrates web navigation workflow.
    /// </summary>
    Task<WebNavigationResult> ExecuteWebNavigationWorkflowAsync();

    /// <summary>
    /// Orchestrates service availability workflow.
    /// </summary>
    Task<ServiceAvailabilityResult> ExecuteServiceAvailabilityWorkflowAsync(ServiceAvailabilityQuery query);

    /// <summary>
    /// Orchestrates comprehensive health check workflow.
    /// </summary>
    Task<ComprehensiveHealthCheckResult> ExecuteComprehensiveHealthCheckWorkflowAsync(ComprehensiveHealthCheckCommand command);
}