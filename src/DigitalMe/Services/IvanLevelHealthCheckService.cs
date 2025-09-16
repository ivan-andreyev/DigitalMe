using DigitalMe.Common;
using DigitalMe.Services.CaptchaSolving;
using DigitalMe.Services.FileProcessing;
using DigitalMe.Services.Voice;
using DigitalMe.Services.WebNavigation;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services;

/// <summary>
/// Health check service specifically for Ivan-Level capabilities.
/// Monitors all Phase B services and provides comprehensive health status.
/// </summary>
public interface IIvanLevelHealthCheckService
{
    /// <summary>
    /// Performs comprehensive health check of all Ivan-Level services.
    /// </summary>
    Task<IvanLevelHealthStatus> CheckAllServicesAsync();

    /// <summary>
    /// Checks specific service health.
    /// </summary>
    Task<ServiceHealthStatus> CheckServiceHealthAsync(string serviceName);

    /// <summary>
    /// Gets overall Ivan-Level readiness score.
    /// </summary>
    Task<double> GetReadinessScoreAsync();
}

/// <summary>
/// Implementation of Ivan-Level health check service.
/// </summary>
public class IvanLevelHealthCheckService : IIvanLevelHealthCheckService
{
    private readonly IFileProcessingService _fileProcessingService;
    private readonly IWebNavigationService _webNavigationService;
    private readonly ICaptchaSolvingService _captchaSolvingService;
    private readonly IVoiceService _voiceService;
    private readonly IIvanPersonalityService _ivanPersonalityService;
    private readonly ILogger<IvanLevelHealthCheckService> _logger;

    public IvanLevelHealthCheckService(
        IFileProcessingService fileProcessingService,
        IWebNavigationService webNavigationService,
        ICaptchaSolvingService captchaSolvingService,
        IVoiceService voiceService,
        IIvanPersonalityService ivanPersonalityService,
        ILogger<IvanLevelHealthCheckService> logger)
    {
        _fileProcessingService = fileProcessingService;
        _webNavigationService = webNavigationService;
        _captchaSolvingService = captchaSolvingService;
        _voiceService = voiceService;
        _ivanPersonalityService = ivanPersonalityService;
        _logger = logger;
    }

    public async Task<IvanLevelHealthStatus> CheckAllServicesAsync()
    {
        _logger.LogInformation("Starting comprehensive Ivan-Level health check");

        var healthStatus = new IvanLevelHealthStatus
        {
            CheckTimestamp = DateTime.UtcNow
        };

        var serviceChecks = new List<Task<ServiceHealthStatus>>
        {
            CheckFileProcessingHealthAsync(),
            CheckWebNavigationHealthAsync(),
            CheckCaptchaSolvingHealthAsync(),
            CheckVoiceServiceHealthAsync(),
            CheckIvanPersonalityHealthAsync()
        };

        var results = await Task.WhenAll(serviceChecks);
        healthStatus.ServiceStatuses = results.ToList();

        // Calculate overall health
        healthStatus.OverallHealth = CalculateOverallHealth(results);
        healthStatus.IsHealthy = healthStatus.OverallHealth >= 0.8; // 80% threshold

        _logger.LogInformation("Ivan-Level health check completed. Overall health: {Health:P1}", 
            healthStatus.OverallHealth);

        return healthStatus;
    }

    public async Task<ServiceHealthStatus> CheckServiceHealthAsync(string serviceName)
    {
        return serviceName.ToLowerInvariant() switch
        {
            "fileprocessing" => await CheckFileProcessingHealthAsync(),
            "webnavigation" => await CheckWebNavigationHealthAsync(),
            "captchasolving" => await CheckCaptchaSolvingHealthAsync(),
            "voice" => await CheckVoiceServiceHealthAsync(),
            "ivanpersonality" => await CheckIvanPersonalityHealthAsync(),
            _ => new ServiceHealthStatus
            {
                ServiceName = serviceName,
                IsHealthy = false,
                ErrorMessage = "Unknown service",
                LastChecked = DateTime.UtcNow
            }
        };
    }

    public async Task<double> GetReadinessScoreAsync()
    {
        var healthStatus = await CheckAllServicesAsync();
        return healthStatus.OverallHealth;
    }

    private async Task<ServiceHealthStatus> CheckFileProcessingHealthAsync()
    {
        var status = new ServiceHealthStatus { ServiceName = "FileProcessing" };

        try
        {
            // Test basic file operations
            var testFilePath = Path.GetTempFileName() + ".pdf";
            var parameters = new Dictionary<string, object> { ["content"] = "Health check test content", ["title"] = "Health Check" };
            var result = await _fileProcessingService.ProcessPdfAsync("create", testFilePath, parameters);
            
            if (result.Success && File.Exists(testFilePath))
            {
                // Test text extraction
                var extractedText = await _fileProcessingService.ExtractTextAsync(testFilePath);
                status.IsHealthy = !string.IsNullOrEmpty(extractedText) && extractedText.Contains("Health check");
                
                // Cleanup
                if (File.Exists(testFilePath))
                    File.Delete(testFilePath);
            }
            else
            {
                status.IsHealthy = false;
                status.ErrorMessage = "Failed to create PDF file";
            }
        }
        catch (Exception ex)
        {
            status.IsHealthy = false;
            status.ErrorMessage = ex.Message;
            _logger.LogError(ex, "FileProcessing health check failed");
        }

        status.LastChecked = DateTime.UtcNow;
        return status;
    }

    private async Task<ServiceHealthStatus> CheckWebNavigationHealthAsync()
    {
        var status = new ServiceHealthStatus { ServiceName = "WebNavigation" };

        try
        {
            // Test browser initialization
            var initResult = await _webNavigationService.InitializeBrowserAsync();
            var isReady = await _webNavigationService.IsBrowserReadyAsync();
            
            status.IsHealthy = initResult.Success && isReady;
            
            if (!initResult.Success)
            {
                status.ErrorMessage = "Browser failed to initialize";
            }
            else if (!isReady)
            {
                status.ErrorMessage = "Browser is not ready";
            }

            // Cleanup
            await _webNavigationService.DisposeBrowserAsync();
        }
        catch (Exception ex)
        {
            status.IsHealthy = false;
            status.ErrorMessage = ex.Message;
            _logger.LogError(ex, "WebNavigation health check failed");
        }

        status.LastChecked = DateTime.UtcNow;
        return status;
    }

    private async Task<ServiceHealthStatus> CheckCaptchaSolvingHealthAsync()
    {
        var status = new ServiceHealthStatus { ServiceName = "CaptchaSolving" };

        try
        {
            // Test service availability
            var isAvailable = await _captchaSolvingService.IsServiceAvailableAsync();
            
            status.IsHealthy = isAvailable;
            
            if (!isAvailable)
            {
                status.ErrorMessage = "CAPTCHA service is not available (check API key)";
            }
        }
        catch (Exception ex)
        {
            status.IsHealthy = false;
            status.ErrorMessage = ex.Message;
            _logger.LogError(ex, "CaptchaSolving health check failed");
        }

        status.LastChecked = DateTime.UtcNow;
        return status;
    }

    private async Task<ServiceHealthStatus> CheckVoiceServiceHealthAsync()
    {
        var status = new ServiceHealthStatus { ServiceName = "Voice" };

        try
        {
            // Test service availability and basic functionality
            var isAvailable = await _voiceService.IsServiceAvailableAsync();
            var voicesResult = await _voiceService.GetAvailableVoicesAsync();
            var formatsResult = await _voiceService.GetSupportedAudioFormatsAsync();

            status.IsHealthy = isAvailable && voicesResult.Success && formatsResult.Success;
            
            if (!voicesResult.Success)
            {
                status.ErrorMessage = "Failed to get available voices";
            }
            else if (!formatsResult.Success)
            {
                status.ErrorMessage = "Failed to get supported audio formats";
            }
            else if (!isAvailable)
            {
                status.ErrorMessage = "Voice service is not available (check API key)";
            }
        }
        catch (Exception ex)
        {
            status.IsHealthy = false;
            status.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Voice service health check failed");
        }

        status.LastChecked = DateTime.UtcNow;
        return status;
    }

    private async Task<ServiceHealthStatus> CheckIvanPersonalityHealthAsync()
    {
        var status = new ServiceHealthStatus { ServiceName = "IvanPersonality" };

        try
        {
            // Test personality service and profile data loading
            var personalityResult = await _ivanPersonalityService.GetIvanPersonalityAsync();
            var basicPromptResult = personalityResult.IsSuccess ?
                _ivanPersonalityService.GenerateSystemPrompt(personalityResult.Value!) :
                Result<string>.Failure("Cannot generate prompt - personality loading failed");
            var enhancedPromptResult = await _ivanPersonalityService.GenerateEnhancedSystemPromptAsync();

            var basicPrompt = basicPromptResult.IsSuccess ? basicPromptResult.Value : string.Empty;
            var enhancedPrompt = enhancedPromptResult.IsSuccess ? enhancedPromptResult.Value : string.Empty;

            var hasBasicData = basicPromptResult.IsSuccess && !string.IsNullOrEmpty(basicPrompt) && basicPrompt.Contains("Ivan");
            var hasEnhancedData = enhancedPromptResult.IsSuccess && !string.IsNullOrEmpty(enhancedPrompt) && enhancedPrompt.Contains("Ivan");

            status.IsHealthy = hasBasicData && hasEnhancedData;

            if (!hasBasicData)
            {
                status.ErrorMessage = "Basic personality prompt generation failed";
            }
            else if (!hasEnhancedData)
            {
                status.ErrorMessage = "Enhanced personality prompt generation failed";
            }
        }
        catch (Exception ex)
        {
            status.IsHealthy = false;
            status.ErrorMessage = ex.Message;
            _logger.LogError(ex, "IvanPersonality health check failed");
        }

        status.LastChecked = DateTime.UtcNow;
        return status;
    }

    private double CalculateOverallHealth(ServiceHealthStatus[] serviceStatuses)
    {
        if (!serviceStatuses.Any())
            return 0.0;

        var healthyCount = serviceStatuses.Count(s => s.IsHealthy);
        return (double)healthyCount / serviceStatuses.Length;
    }
}

/// <summary>
/// Overall health status for Ivan-Level capabilities.
/// </summary>
public class IvanLevelHealthStatus
{
    public DateTime CheckTimestamp { get; set; }
    public bool IsHealthy { get; set; }
    public double OverallHealth { get; set; }
    public List<ServiceHealthStatus> ServiceStatuses { get; set; } = new();

    public IEnumerable<ServiceHealthStatus> HealthyServices => 
        ServiceStatuses.Where(s => s.IsHealthy);

    public IEnumerable<ServiceHealthStatus> UnhealthyServices => 
        ServiceStatuses.Where(s => !s.IsHealthy);
}

/// <summary>
/// Health status for individual Ivan-Level service.
/// </summary>
public class ServiceHealthStatus
{
    public string ServiceName { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime LastChecked { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}