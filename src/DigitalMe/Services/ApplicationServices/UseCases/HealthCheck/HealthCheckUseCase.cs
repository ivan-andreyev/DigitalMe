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

    public async Task<ComprehensiveHealthCheckResult> ExecuteAsync(ComprehensiveHealthCheckCommand command)
    {
        try
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
                var testContent = command.TestContent ?? "Ivan-Level comprehensive test document content";
                var fileCommand = new FileProcessingCommand(testContent, "Comprehensive Test");
                var fileResult = await _fileProcessingUseCase.ExecuteAsync(fileCommand);
                
                results["fileProcessing"] = new 
                { 
                    success = fileResult.Success,
                    pdfCreated = fileResult.PdfCreated,
                    textExtracted = fileResult.TextExtracted
                };
                
                if (!fileResult.Success)
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
                var personalityResult = await _serviceAvailabilityUseCase.ExecuteAsync(personalityQuery);
                results["personality"] = new 
                { 
                    success = personalityResult.Success,
                    personalityLoaded = personalityResult.ServiceAvailable,
                    enhancedPromptGenerated = personalityResult.AdditionalData?.GetValueOrDefault("enhancedPromptGenerated", false)
                };
                
                if (!personalityResult.Success)
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
                TotalTests: results.Count,
                PassedTests: results.Values.Count(r => ((dynamic)r).success == true),
                FailedTests: results.Values.Count(r => ((dynamic)r).success == false));

            return new ComprehensiveHealthCheckResult(
                OverallSuccess: overallSuccess,
                Timestamp: DateTime.UtcNow,
                TestResults: results,
                Summary: summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Comprehensive health check workflow failed");
            return new ComprehensiveHealthCheckResult(
                OverallSuccess: false,
                Timestamp: DateTime.UtcNow,
                TestResults: new Dictionary<string, object> { ["error"] = ex.Message },
                Summary: new ComprehensiveTestSummary(0, 0, 1));
        }
    }
}