using Microsoft.Extensions.Logging;
using DigitalMe.Services.ApplicationServices.UseCases.FileProcessing;
using DigitalMe.Services.ApplicationServices.UseCases.WebNavigation;
using DigitalMe.Services.ApplicationServices.UseCases.ServiceAvailability;
using DigitalMe.Services.ApplicationServices.UseCases.HealthCheck;

namespace DigitalMe.Services.ApplicationServices.Orchestrators;

/// <summary>
/// Implementation of workflow orchestrator.
/// Composes focused use cases without containing business logic itself.
/// Follows Single Responsibility Principle - orchestration only.
/// </summary>
public class WorkflowOrchestrator : IWorkflowOrchestrator
{
    private readonly IFileProcessingUseCase _fileProcessingUseCase;
    private readonly IWebNavigationUseCase _webNavigationUseCase;
    private readonly IServiceAvailabilityUseCase _serviceAvailabilityUseCase;
    private readonly IHealthCheckUseCase _healthCheckUseCase;
    private readonly ILogger<WorkflowOrchestrator> _logger;

    public WorkflowOrchestrator(
        IFileProcessingUseCase fileProcessingUseCase,
        IWebNavigationUseCase webNavigationUseCase,
        IServiceAvailabilityUseCase serviceAvailabilityUseCase,
        IHealthCheckUseCase healthCheckUseCase,
        ILogger<WorkflowOrchestrator> logger)
    {
        _fileProcessingUseCase = fileProcessingUseCase;
        _webNavigationUseCase = webNavigationUseCase;
        _serviceAvailabilityUseCase = serviceAvailabilityUseCase;
        _healthCheckUseCase = healthCheckUseCase;
        _logger = logger;
    }

    public async Task<FileProcessingResult> ExecuteFileProcessingWorkflowAsync(FileProcessingCommand command)
    {
        _logger.LogInformation("Orchestrating file processing workflow");
        return await _fileProcessingUseCase.ExecuteAsync(command);
    }

    public async Task<WebNavigationResult> ExecuteWebNavigationWorkflowAsync()
    {
        _logger.LogInformation("Orchestrating web navigation workflow");
        return await _webNavigationUseCase.ExecuteAsync();
    }

    public async Task<ServiceAvailabilityResult> ExecuteServiceAvailabilityWorkflowAsync(ServiceAvailabilityQuery query)
    {
        _logger.LogInformation("Orchestrating service availability workflow for {ServiceName}", query.ServiceName);
        return await _serviceAvailabilityUseCase.ExecuteAsync(query);
    }

    public async Task<ComprehensiveHealthCheckResult> ExecuteComprehensiveHealthCheckWorkflowAsync(ComprehensiveHealthCheckCommand command)
    {
        _logger.LogInformation("Orchestrating comprehensive health check workflow");
        return await _healthCheckUseCase.ExecuteAsync(command);
    }
}