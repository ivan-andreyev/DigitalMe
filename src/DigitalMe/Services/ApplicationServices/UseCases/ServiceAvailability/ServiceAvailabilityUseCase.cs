using DigitalMe.Services;
using DigitalMe.Services.CaptchaSolving;
using DigitalMe.Services.Voice;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.ApplicationServices.UseCases.ServiceAvailability;

/// <summary>
/// Implementation of service availability use case.
/// Focuses solely on service availability checking logic.
/// </summary>
public class ServiceAvailabilityUseCase : IServiceAvailabilityUseCase
{
    private readonly ICaptchaSolvingService _captchaSolvingService;
    private readonly IVoiceService _voiceService;
    private readonly IIvanPersonalityService _ivanPersonalityService;
    private readonly ILogger<ServiceAvailabilityUseCase> _logger;

    public ServiceAvailabilityUseCase(
        ICaptchaSolvingService captchaSolvingService,
        IVoiceService voiceService,
        IIvanPersonalityService ivanPersonalityService,
        ILogger<ServiceAvailabilityUseCase> logger)
    {
        _captchaSolvingService = captchaSolvingService;
        _voiceService = voiceService;
        _ivanPersonalityService = ivanPersonalityService;
        _logger = logger;
    }

    public async Task<ServiceAvailabilityResult> ExecuteAsync(ServiceAvailabilityQuery query)
    {
        try
        {
            return query.ServiceName.ToLowerInvariant() switch
            {
                "captcha-solving" => await CheckCaptchaSolvingAvailabilityAsync(),
                "voice" => await CheckVoiceServiceAvailabilityAsync(),
                "personality" => await CheckPersonalityServiceAvailabilityAsync(),
                _ => new ServiceAvailabilityResult(
                    Success: false,
                    ServiceName: query.ServiceName,
                    ServiceAvailable: false,
                    ErrorMessage: "Unknown service")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Service availability workflow failed for {ServiceName}", query.ServiceName);
            return new ServiceAvailabilityResult(
                Success: false,
                ServiceName: query.ServiceName,
                ServiceAvailable: false,
                ErrorMessage: ex.Message);
        }
    }

    private async Task<ServiceAvailabilityResult> CheckCaptchaSolvingAvailabilityAsync()
    {
        _logger.LogInformation("Testing CAPTCHA solving service availability");

        var isAvailable = await _captchaSolvingService.IsServiceAvailableAsync();
        var supportedTypes = new Dictionary<string, object>
        {
            ["image/jpg"] = true,
            ["image/png"] = true,
            ["image/gif"] = true,
            ["recaptcha-v2"] = true,
            ["recaptcha-v3"] = true,
            ["hcaptcha"] = true
        };

        return new ServiceAvailabilityResult(
            Success: true,
            ServiceName: "CaptchaSolving",
            ServiceAvailable: isAvailable,
            AdditionalData: new Dictionary<string, object> { ["supportedTypes"] = supportedTypes },
            Message: isAvailable ? "CAPTCHA service is available" : "CAPTCHA service is not available (check API key)");
    }

    private async Task<ServiceAvailabilityResult> CheckVoiceServiceAvailabilityAsync()
    {
        _logger.LogInformation("Testing voice service availability");

        var isAvailable = await _voiceService.IsServiceAvailableAsync();
        var voicesResult = await _voiceService.GetAvailableVoicesAsync();
        var formatsResult = await _voiceService.GetSupportedAudioFormatsAsync();

        var voices = new[] { "alloy", "echo", "fable", "nova", "onyx", "shimmer" };
        var formats = new[] { "mp3", "opus", "aac", "flac", "wav" };

        return new ServiceAvailabilityResult(
            Success: true,
            ServiceName: "Voice",
            ServiceAvailable: isAvailable,
            AdditionalData: new Dictionary<string, object>
            {
                ["availableVoices"] = voices,
                ["supportedFormats"] = formats,
                ["voiceCount"] = voices.Length,
                ["formatCount"] = formats.Length
            },
            Message: isAvailable ? "Voice service is fully functional" : "Voice service is not available (check API key)");
    }

    private async Task<ServiceAvailabilityResult> CheckPersonalityServiceAvailabilityAsync()
    {
        _logger.LogInformation("Testing Ivan personality service availability");

        var personality = await _ivanPersonalityService.GetIvanPersonalityAsync();
        var basicPrompt = _ivanPersonalityService.GenerateSystemPrompt(personality);
        var enhancedPrompt = await _ivanPersonalityService.GenerateEnhancedSystemPromptAsync();

        var personalityLoaded = personality != null;
        var basicPromptGenerated = !string.IsNullOrEmpty(basicPrompt) && basicPrompt.Contains("Ivan");
        var enhancedPromptGenerated = !string.IsNullOrEmpty(enhancedPrompt) && enhancedPrompt.Contains("Ivan");

        return new ServiceAvailabilityResult(
            Success: true,
            ServiceName: "IvanPersonality",
            ServiceAvailable: personalityLoaded && basicPromptGenerated && enhancedPromptGenerated,
            AdditionalData: new Dictionary<string, object>
            {
                ["personalityLoaded"] = personalityLoaded,
                ["personalityName"] = personality?.Name ?? "Unknown",
                ["traitCount"] = personality?.Traits?.Count ?? 0,
                ["basicPromptGenerated"] = basicPromptGenerated,
                ["enhancedPromptGenerated"] = enhancedPromptGenerated,
                ["basicPromptPreview"] = basicPrompt?.Length > 150 ? basicPrompt.Substring(0, 150) : basicPrompt ?? string.Empty,
                ["enhancedPromptPreview"] = enhancedPrompt?.Length > 150 ? enhancedPrompt.Substring(0, 150) : enhancedPrompt ?? string.Empty
            });
    }
}