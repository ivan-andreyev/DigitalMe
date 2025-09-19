using DigitalMe.Common;
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
    private readonly IPersonalityService _ivanPersonalityService;
    private readonly ILogger<ServiceAvailabilityUseCase> _logger;

    public ServiceAvailabilityUseCase(
        ICaptchaSolvingService captchaSolvingService,
        IVoiceService voiceService,
        IPersonalityService ivanPersonalityService,
        ILogger<ServiceAvailabilityUseCase> logger)
    {
        _captchaSolvingService = captchaSolvingService;
        _voiceService = voiceService;
        _ivanPersonalityService = ivanPersonalityService;
        _logger = logger;
    }

    public async Task<Result<ServiceAvailabilityResult>> ExecuteAsync(ServiceAvailabilityQuery query)
    {
        return await ResultExtensions.TryAsync(async () =>
        {
            return query.serviceName.ToLowerInvariant() switch
            {
                "captcha-solving" => await CheckCaptchaSolvingAvailabilityAsync(),
                "voice" => await CheckVoiceServiceAvailabilityAsync(),
                "personality" => await CheckPersonalityServiceAvailabilityAsync(),
                _ => new ServiceAvailabilityResult(
                    success: false,
                    serviceName: query.serviceName,
                    serviceAvailable: false,
                    errorMessage: "Unknown service")
            };
        }, $"Service availability workflow failed for {query.serviceName}");
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
            success: true,
            serviceName: "CaptchaSolving",
            serviceAvailable: isAvailable,
            additionalData: new Dictionary<string, object> { ["supportedTypes"] = supportedTypes },
            message: isAvailable ? "CAPTCHA service is available" : "CAPTCHA service is not available (check API key)");
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
            success: true,
            serviceName: "Voice",
            serviceAvailable: isAvailable,
            additionalData: new Dictionary<string, object>
            {
                ["availableVoices"] = voices,
                ["supportedFormats"] = formats,
                ["voiceCount"] = voices.Length,
                ["formatCount"] = formats.Length
            },
            message: isAvailable ? "Voice service is fully functional" : "Voice service is not available (check API key)");
    }

    private async Task<ServiceAvailabilityResult> CheckPersonalityServiceAvailabilityAsync()
    {
        _logger.LogInformation("Testing Ivan personality service availability");

        var personalityResult = await _ivanPersonalityService.GetPersonalityAsync();
        var basicPromptResult = personalityResult.IsSuccess && personalityResult.Value != null
            ? _ivanPersonalityService.GenerateSystemPrompt(personalityResult.Value)
            : Result<string>.Failure("Personality not loaded");
        var enhancedPromptResult = await _ivanPersonalityService.GenerateEnhancedSystemPromptAsync();

        var personalityLoaded = personalityResult.IsSuccess;
        var basicPromptGenerated = basicPromptResult.IsSuccess &&
                                   !string.IsNullOrEmpty(basicPromptResult.Value) &&
                                   basicPromptResult.Value.Contains("Ivan");
        var enhancedPromptGenerated = enhancedPromptResult.IsSuccess &&
                                      !string.IsNullOrEmpty(enhancedPromptResult.Value) &&
                                      enhancedPromptResult.Value.Contains("Ivan");

        return new ServiceAvailabilityResult(
            success: true,
            serviceName: "IvanPersonality",
            serviceAvailable: personalityLoaded && basicPromptGenerated && enhancedPromptGenerated,
            additionalData: new Dictionary<string, object>
            {
                ["personalityLoaded"] = personalityLoaded,
                ["personalityName"] = personalityResult.IsSuccess && personalityResult.Value != null ? personalityResult.Value.Name : "Unknown",
                ["traitCount"] = personalityResult.IsSuccess && personalityResult.Value != null ? personalityResult.Value.Traits?.Count ?? 0 : 0,
                ["basicPromptGenerated"] = basicPromptGenerated,
                ["enhancedPromptGenerated"] = enhancedPromptGenerated,
                ["basicPromptPreview"] = basicPromptResult.IsSuccess && basicPromptResult.Value?.Length > 150
                    ? basicPromptResult.Value.Substring(0, 150)
                    : basicPromptResult.IsSuccess ? basicPromptResult.Value ?? string.Empty : string.Empty,
                ["enhancedPromptPreview"] = enhancedPromptResult.IsSuccess && enhancedPromptResult.Value?.Length > 150
                    ? enhancedPromptResult.Value.Substring(0, 150)
                    : enhancedPromptResult.IsSuccess ? enhancedPromptResult.Value ?? string.Empty : string.Empty
            });
    }
}