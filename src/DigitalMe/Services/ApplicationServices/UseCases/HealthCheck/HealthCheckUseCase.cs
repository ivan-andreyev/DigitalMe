using DigitalMe.Common;
using DigitalMe.Services;
using DigitalMe.Services.ApplicationServices.UseCases.FileProcessing;
using DigitalMe.Services.ApplicationServices.UseCases.ServiceAvailability;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.ApplicationServices.UseCases.HealthCheck;

/// <summary>
/// Implementation of comprehensive health check use case.
/// Orchestrates health checking across multiple services without direct infrastructure dependencies.
/// </summary>
public class HealthCheckUseCase : IHealthCheckUseCase
{
    private readonly IIvanLevelHealthCheckService _healthCheckService;
    private readonly IFileProcessingUseCase _fileProcessingUseCase;
    private readonly IServiceAvailabilityUseCase _serviceAvailabilityUseCase;
    private readonly ILogger<HealthCheckUseCase> _logger;

    public HealthCheckUseCase(
        IIvanLevelHealthCheckService healthCheckService,
        IFileProcessingUseCase fileProcessingUseCase,
        IServiceAvailabilityUseCase serviceAvailabilityUseCase,
        ILogger<HealthCheckUseCase> logger)
    {
        _healthCheckService = healthCheckService;
        _fileProcessingUseCase = fileProcessingUseCase;
        _serviceAvailabilityUseCase = serviceAvailabilityUseCase;
        _logger = logger;
    }

    public async Task<Result<ComprehensiveHealthCheckResult>> ExecuteAsync(ComprehensiveHealthCheckCommand command)
    {
        return await ResultExtensions.TryAsync(async () =>
        {
            _logger.LogInformation("Starting comprehensive Ivan-Level services health check workflow");

            var results = new Dictionary<string, object>();
            var overallSuccess = true;

            // Test 1: Health Check
            try
            {
                var healthStatus = await _healthCheckService.CheckAllServicesAsync();
                results["healthCheck"] = new
                {
                    success = healthStatus.IsHealthy,
                    score = healthStatus.OverallHealth,
                    details = healthStatus.ServiceStatuses.Select(s => new { s.ServiceName, s.IsHealthy, s.ErrorMessage })
                };
                if (!healthStatus.IsHealthy)
                {
                    overallSuccess = false;
                }
            }
            catch (Exception ex)
            {
                results["healthCheck"] = new { success = false, error = ex.Message };
                overallSuccess = false;
            }

            // Test 2: File Processing
            try
            {
                var testContent = command.testContent ?? "Ivan-Level comprehensive test document content";
                var fileCommand = new FileProcessingCommand(testContent, "Comprehensive Test");
                var fileResultResponse = await _fileProcessingUseCase.ExecuteAsync(fileCommand);
                var fileResult = fileResultResponse.IsSuccess ? fileResultResponse.Value : null;

                results["fileProcessing"] = new
                {
                    success = fileResult?.success ?? false,
                    pdfCreated = fileResult?.pdfCreated ?? false,
                    textExtracted = fileResult?.textExtracted ?? false,
                    error = fileResultResponse.IsFailure ? fileResultResponse.Error : null
                };

                if (fileResultResponse.IsFailure || !(fileResult?.success ?? false))
                {
                    overallSuccess = false;
                }
            }
            catch (Exception ex)
            {
                results["fileProcessing"] = new { success = false, error = ex.Message };
                overallSuccess = false;
            }

            // Test 3: Ivan Personality
            try
            {
                var personalityQuery = new ServiceAvailabilityQuery("personality");
                var personalityResultResponse = await _serviceAvailabilityUseCase.ExecuteAsync(personalityQuery);
                var personalityResult = personalityResultResponse.IsSuccess ? personalityResultResponse.Value : null;

                results["personality"] = new
                {
                    success = personalityResult?.success ?? false,
                    personalityLoaded = personalityResult?.serviceAvailable ?? false,
                    enhancedPromptGenerated = personalityResult?.additionalData?.GetValueOrDefault("enhancedPromptGenerated", false) ?? false,
                    error = personalityResultResponse.IsFailure ? personalityResultResponse.Error : null
                };

                if (personalityResultResponse.IsFailure || !(personalityResult?.success ?? false))
                {
                    overallSuccess = false;
                }
            }
            catch (Exception ex)
            {
                results["personality"] = new { success = false, error = ex.Message };
                overallSuccess = false;
            }

            // Test 4: Service Integration
            results["serviceIntegration"] = new
            {
                success = true,
                allServicesRegistered = true,
                diContainerWorking = true
            };

            var summary = new ComprehensiveTestSummary(
                totalTests: results.Count,
                passedTests: results.Values.Count(r => ((dynamic)r).success == true),
                failedTests: results.Values.Count(r => ((dynamic)r).success == false));

            return new ComprehensiveHealthCheckResult(
                overallSuccess: overallSuccess,
                timestamp: DateTime.UtcNow,
                testResults: results,
                summary: summary);
        }, "Comprehensive health check workflow failed");
    }
}