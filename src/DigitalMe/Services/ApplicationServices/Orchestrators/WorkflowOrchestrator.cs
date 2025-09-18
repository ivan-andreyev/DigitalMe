using DigitalMe.Services.ApplicationServices.UseCases.FileProcessing;
using DigitalMe.Services.ApplicationServices.UseCases.HealthCheck;
using DigitalMe.Services.ApplicationServices.UseCases.ServiceAvailability;
using DigitalMe.Services.ApplicationServices.UseCases.WebNavigation;
using Microsoft.Extensions.Logging;

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
        var result = await _fileProcessingUseCase.ExecuteAsync(command);
        return result.IsSuccess && result.Value != null ? result.Value : new FileProcessingResult(
            Success: false,
            PdfCreated: false,
            TextExtracted: false,
            ContentMatch: false,
            FileId: null,
            ExtractedTextPreview: null,
            ErrorMessage: result.Error);
    }

    public async Task<WebNavigationResult> ExecuteWebNavigationWorkflowAsync()
    {
        _logger.LogInformation("Orchestrating web navigation workflow");
        return await _webNavigationUseCase.ExecuteAsync();
    }

    public async Task<ServiceAvailabilityResult> ExecuteServiceAvailabilityWorkflowAsync(ServiceAvailabilityQuery query)
    {
        _logger.LogInformation("Orchestrating service availability workflow for {ServiceName}", query.ServiceName);
        var result = await _serviceAvailabilityUseCase.ExecuteAsync(query);
        return result.IsSuccess && result.Value != null ? result.Value : new ServiceAvailabilityResult(
            Success: false,
            ServiceName: query.ServiceName,
            ServiceAvailable: false,
            ErrorMessage: result.Error);
    }

    public async Task<ComprehensiveHealthCheckResult> ExecuteComprehensiveHealthCheckWorkflowAsync(ComprehensiveHealthCheckCommand command)
    {
        _logger.LogInformation("Orchestrating comprehensive health check workflow");
        var result = await _healthCheckUseCase.ExecuteAsync(command);
        return result.IsSuccess && result.Value != null ? result.Value : new ComprehensiveHealthCheckResult(
            OverallSuccess: false,
            Timestamp: DateTime.UtcNow,
            TestResults: new Dictionary<string, object> { ["error"] = result.Error },
            Summary: new ComprehensiveTestSummary(0, 0, 1));
    }
}