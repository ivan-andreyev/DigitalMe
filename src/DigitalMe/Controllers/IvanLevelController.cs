using DigitalMe.Services;
using DigitalMe.Services.ApplicationServices.Orchestrators;
using DigitalMe.Services.ApplicationServices.UseCases.FileProcessing;
using DigitalMe.Services.ApplicationServices.UseCases.HealthCheck;
using DigitalMe.Services.ApplicationServices.UseCases.ServiceAvailability;
using DigitalMe.Services.ApplicationServices.UseCases.WebNavigation;
using DigitalMe.Services.ApplicationServices.Workflows;
using Microsoft.AspNetCore.Mvc;

namespace DigitalMe.Controllers;

/// <summary>
/// API controller for Ivan-Level capabilities testing and health monitoring.
/// Follows Clean Architecture principles with presentation logic only.
/// Business logic delegated to Application Services layer.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class IvanLevelController : ControllerBase
{
    private readonly IIvanLevelHealthCheckService _healthCheckService;
    private readonly IWorkflowOrchestrator _workflowOrchestrator;
    private readonly IIvanLevelWorkflowService _ivanLevelWorkflowService;
    private readonly ILogger<IvanLevelController> _logger;

    public IvanLevelController(
        IIvanLevelHealthCheckService healthCheckService,
        IWorkflowOrchestrator workflowOrchestrator,
        IIvanLevelWorkflowService ivanLevelWorkflowService,
        ILogger<IvanLevelController> logger)
    {
        _healthCheckService = healthCheckService;
        _workflowOrchestrator = workflowOrchestrator;
        _ivanLevelWorkflowService = ivanLevelWorkflowService;
        _logger = logger;
    }

    /// <summary>
    /// Comprehensive health check of all Ivan-Level services.
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult<IvanLevelHealthStatus>> GetHealthStatus()
    {
        try
        {
            _logger.LogInformation("API health check requested for Ivan-Level services");
            var healthStatus = await _healthCheckService.CheckAllServicesAsync();
            
            if (!healthStatus.IsHealthy)
            {
                _logger.LogWarning("Ivan-Level services are not fully healthy: {OverallHealth:P1}", 
                    healthStatus.OverallHealth);
                return StatusCode(503, healthStatus); // Service Unavailable
            }
            
            return Ok(healthStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform Ivan-Level health check");
            return StatusCode(500, new { error = "Health check failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Health check for specific Ivan-Level service.
    /// </summary>
    [HttpGet("health/{serviceName}")]
    public async Task<ActionResult<ServiceHealthStatus>> GetServiceHealth(string serviceName)
    {
        try
        {
            var serviceHealth = await _healthCheckService.CheckServiceHealthAsync(serviceName);
            
            if (!serviceHealth.IsHealthy)
            {
                return StatusCode(503, serviceHealth); // Service Unavailable
            }
            
            return Ok(serviceHealth);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check health for service {ServiceName}", serviceName);
            return StatusCode(500, new { error = "Service health check failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Gets Ivan-Level readiness score (0.0 to 1.0).
    /// </summary>
    [HttpGet("readiness")]
    public async Task<ActionResult<object>> GetReadinessScore()
    {
        try
        {
            var score = await _healthCheckService.GetReadinessScoreAsync();
            var status = score >= 0.8 ? "Ready" : score >= 0.5 ? "Partially Ready" : "Not Ready";
            
            return Ok(new 
            {
                readinessScore = score,
                status = status,
                timestamp = DateTime.UtcNow,
                threshold = 0.8
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Ivan-Level readiness score");
            return StatusCode(500, new { error = "Readiness check failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Test file processing capabilities end-to-end.
    /// </summary>
    [HttpPost("test/file-processing")]
    public async Task<ActionResult<object>> TestFileProcessing([FromBody] TestFileRequest request)
    {
        try
        {
            var command = new FileProcessingCommand(request.Content, request.Title);
            var result = await _workflowOrchestrator.ExecuteFileProcessingWorkflowAsync(command);
            
            if (!result.success)
            {
                return BadRequest(new { error = result.errorMessage });
            }

            return Ok(new
            {
                success = result.success,
                pdfCreated = result.pdfCreated,
                textExtracted = result.textExtracted,
                contentMatch = result.contentMatch,
                fileId = result.fileId,
                extractedTextPreview = result.extractedTextPreview
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File processing test failed");
            return StatusCode(500, new { error = "File processing test failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Test web navigation capabilities.
    /// </summary>
    [HttpPost("test/web-navigation")]
    public async Task<ActionResult<object>> TestWebNavigation()
    {
        try
        {
            var result = await _workflowOrchestrator.ExecuteWebNavigationWorkflowAsync();
            
            if (!result.success)
            {
                return BadRequest(new { error = result.errorMessage });
            }

            return Ok(new
            {
                success = result.success,
                browserInitialized = result.browserInitialized,
                message = result.message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Web navigation test failed");
            return StatusCode(500, new { error = "Web navigation test failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Test CAPTCHA solving service availability.
    /// </summary>
    [HttpGet("test/captcha-solving")]
    public async Task<ActionResult<object>> TestCaptchaSolving()
    {
        try
        {
            var query = new ServiceAvailabilityQuery("captcha-solving");
            var result = await _workflowOrchestrator.ExecuteServiceAvailabilityWorkflowAsync(query);
            
            if (!result.success)
            {
                return StatusCode(500, new { error = result.errorMessage });
            }

            return Ok(new
            {
                success = result.success,
                serviceAvailable = result.serviceAvailable,
                supportedTypes = result.additionalData?["supportedTypes"],
                message = result.message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CAPTCHA solving test failed");
            return StatusCode(500, new { error = "CAPTCHA test failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Test voice service capabilities.
    /// </summary>
    [HttpGet("test/voice")]
    public async Task<ActionResult<object>> TestVoiceService()
    {
        try
        {
            var query = new ServiceAvailabilityQuery("voice");
            var result = await _workflowOrchestrator.ExecuteServiceAvailabilityWorkflowAsync(query);
            
            if (!result.success)
            {
                return StatusCode(500, new { error = result.errorMessage });
            }

            return Ok(new
            {
                success = result.success,
                serviceAvailable = result.serviceAvailable,
                availableVoices = result.additionalData?["availableVoices"],
                supportedFormats = result.additionalData?["supportedFormats"],
                voiceCount = result.additionalData?["voiceCount"],
                formatCount = result.additionalData?["formatCount"],
                message = result.message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Voice service test failed");
            return StatusCode(500, new { error = "Voice service test failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Test Ivan personality service and prompt generation.
    /// </summary>
    [HttpGet("test/personality")]
    public async Task<ActionResult<object>> TestPersonalityService()
    {
        try
        {
            var query = new ServiceAvailabilityQuery("personality");
            var result = await _workflowOrchestrator.ExecuteServiceAvailabilityWorkflowAsync(query);
            
            if (!result.success)
            {
                return StatusCode(500, new { error = result.errorMessage });
            }

            return Ok(new
            {
                success = result.success,
                personalityLoaded = result.additionalData?["personalityLoaded"],
                personalityName = result.additionalData?["personalityName"],
                traitCount = result.additionalData?["traitCount"],
                basicPromptGenerated = result.additionalData?["basicPromptGenerated"],
                enhancedPromptGenerated = result.additionalData?["enhancedPromptGenerated"],
                basicPromptPreview = result.additionalData?["basicPromptPreview"],
                enhancedPromptPreview = result.additionalData?["enhancedPromptPreview"]
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Personality service test failed");
            return StatusCode(500, new { error = "Personality service test failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Comprehensive end-to-end test of all Ivan-Level services.
    /// </summary>
    [HttpPost("test/comprehensive")]
    public async Task<ActionResult<object>> ComprehensiveTest([FromBody] ComprehensiveTestRequest request)
    {
        try
        {
            var command = new ComprehensiveHealthCheckCommand(
                request.TestContent, 
                request.IncludeWebNavigation, 
                request.IncludeCaptcha);
                
            var result = await _workflowOrchestrator.ExecuteComprehensiveHealthCheckWorkflowAsync(command);

            return Ok(new
            {
                overallSuccess = result.overallSuccess,
                timestamp = result.timestamp,
                testResults = result.testResults,
                summary = new
                {
                    totalTests = result.summary.totalTests,
                    passedTests = result.summary.passedTests,
                    failedTests = result.summary.failedTests
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Comprehensive test failed");
            return StatusCode(500, new { error = "Comprehensive test failed", details = ex.Message });
        }
    }

    /// <summary>
    /// CRITICAL: TRUE INTEGRATION - WebNavigation → CAPTCHA → File → Voice workflow.
    /// Real end-to-end workflow demonstrating all Ivan-Level services working together.
    /// </summary>
    [HttpPost("test/true-integration/web-to-voice")]
    public async Task<ActionResult<object>> TrueIntegrationWebToVoice([FromBody] WebToVoiceIntegrationRequest request)
    {
        try
        {
            _logger.LogInformation("🚨 CRITICAL: Starting TRUE INTEGRATION test: WebNavigation → CAPTCHA → File → Voice for URL: {TargetUrl}", request.TargetUrl);
            
            var workflowRequest = new WebToCaptchaToFileToVoiceRequest(
                request.TargetUrl,
                request.ExpectedContent,
                request.ProcessCaptcha,
                request.GenerateVoiceNarration,
                request.VoiceText);

            var result = await _ivanLevelWorkflowService.ExecuteWebToCaptchaToFileToVoiceWorkflowAsync(workflowRequest);

            return Ok(new
            {
                overallSuccess = result.overallSuccess,
                timestamp = result.timestamp,
                webNavigationStep = new
                {
                    success = result.webNavigationStep.success,
                    message = result.webNavigationStep.message,
                    contentExtracted = result.webNavigationStep.contentExtracted != null ? result.webNavigationStep.contentExtracted.Substring(0, Math.Min(200, result.webNavigationStep.contentExtracted.Length)) + "..." : null,
                    errorMessage = result.webNavigationStep.errorMessage
                },
                captchaStep = new
                {
                    success = result.captchaStep.success,
                    message = result.captchaStep.message,
                    captchaDetected = result.captchaStep.captchaDetected,
                    captchaSolved = result.captchaStep.captchaSolved,
                    errorMessage = result.captchaStep.errorMessage
                },
                fileProcessingStep = new
                {
                    success = result.fileProcessingStep.success,
                    message = result.fileProcessingStep.message,
                    filePath = result.fileProcessingStep.filePath,
                    extractedTextPreview = result.fileProcessingStep.extractedText != null ? result.fileProcessingStep.extractedText.Substring(0, Math.Min(100, result.fileProcessingStep.extractedText.Length)) + "..." : null,
                    errorMessage = result.fileProcessingStep.errorMessage
                },
                voiceStep = new
                {
                    success = result.voiceStep.success,
                    message = result.voiceStep.message,
                    audioFilePath = result.voiceStep.audioFilePath,
                    audioDurationSeconds = result.voiceStep.audioDurationSeconds,
                    errorMessage = result.voiceStep.errorMessage
                },
                errorMessage = result.errorMessage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🚨 CRITICAL: TRUE INTEGRATION WebToVoice workflow failed");
            return StatusCode(500, new { error = "TRUE INTEGRATION workflow failed", details = ex.Message });
        }
    }

    /// <summary>
    /// CRITICAL: TRUE INTEGRATION - Site registration → Form filling → Document → PDF workflow.
    /// Complex multi-step workflow demonstrating service coordination in realistic scenarios.
    /// </summary>
    [HttpPost("test/true-integration/site-to-document")]
    public async Task<ActionResult<object>> TrueIntegrationSiteToDocument([FromBody] SiteToDocumentIntegrationRequest request)
    {
        try
        {
            _logger.LogInformation("🚨 CRITICAL: Starting TRUE INTEGRATION test: Site Registration → Form Filling → Document → PDF for URL: {RegistrationUrl}", request.RegistrationUrl);
            
            var workflowRequest = new SiteRegistrationToDocumentRequest(
                request.RegistrationUrl,
                request.UserData,
                request.DocumentDownloadPath,
                request.ConvertToPdf);

            var result = await _ivanLevelWorkflowService.ExecuteSiteRegistrationToDocumentWorkflowAsync(workflowRequest);

            return Ok(new
            {
                overallSuccess = result.overallSuccess,
                timestamp = result.timestamp,
                registrationStep = new
                {
                    success = result.registrationStep.success,
                    message = result.registrationStep.message,
                    userRegistered = result.registrationStep.userRegistered,
                    errorMessage = result.registrationStep.errorMessage
                },
                formFillingStep = new
                {
                    success = result.formFillingStep.success,
                    message = result.formFillingStep.message,
                    fieldsFilled = result.formFillingStep.fieldsFilled,
                    errorMessage = result.formFillingStep.errorMessage
                },
                documentStep = new
                {
                    success = result.documentStep.success,
                    message = result.documentStep.message,
                    downloadedFilePath = result.documentStep.downloadedFilePath,
                    fileSizeBytes = result.documentStep.fileSizeBytes,
                    errorMessage = result.documentStep.errorMessage
                },
                pdfConversionStep = new
                {
                    success = result.pdfConversionStep.success,
                    message = result.pdfConversionStep.message,
                    pdfFilePath = result.pdfConversionStep.pdfFilePath,
                    pageCount = result.pdfConversionStep.pageCount,
                    errorMessage = result.pdfConversionStep.errorMessage
                },
                errorMessage = result.errorMessage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🚨 CRITICAL: TRUE INTEGRATION SiteToDocument workflow failed");
            return StatusCode(500, new { error = "TRUE INTEGRATION workflow failed", details = ex.Message });
        }
    }
}

/// <summary>
/// Request model for file processing test.
/// </summary>
public class TestFileRequest
{
    public string Content { get; set; } = string.Empty;
    public string? Title { get; set; }
}

/// <summary>
/// Request model for comprehensive test.
/// </summary>
public class ComprehensiveTestRequest
{
    public string? TestContent { get; set; }
    public bool IncludeWebNavigation { get; set; } = false;
    public bool IncludeCaptcha { get; set; } = false;
}

/// <summary>
/// CRITICAL: Request model for WebNavigation → CAPTCHA → File → Voice TRUE integration test.
/// </summary>
public class WebToVoiceIntegrationRequest
{
    public string TargetUrl { get; set; } = string.Empty;
    public string ExpectedContent { get; set; } = string.Empty;
    public bool ProcessCaptcha { get; set; } = true;
    public bool GenerateVoiceNarration { get; set; } = true;
    public string? VoiceText { get; set; }
}

/// <summary>
/// CRITICAL: Request model for Site registration → Form filling → Document → PDF TRUE integration test.
/// </summary>
public class SiteToDocumentIntegrationRequest
{
    public string RegistrationUrl { get; set; } = string.Empty;
    public Dictionary<string, string> UserData { get; set; } = new();
    public string DocumentDownloadPath { get; set; } = string.Empty;
    public bool ConvertToPdf { get; set; } = true;
}