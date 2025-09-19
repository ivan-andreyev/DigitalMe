using System.Diagnostics;
using DigitalMe.Services.CaptchaSolving;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.ApplicationServices.Workflows;

/// <summary>
/// Implementation of CAPTCHA workflow orchestration service.
/// Provides comprehensive CAPTCHA solving coordination with retry logic, validation, and performance tracking.
/// Extracted from IvanLevelWorkflowService following Clean Architecture and Single Responsibility Principle.
/// </summary>
public class CaptchaWorkflowService : ICaptchaWorkflowService
{
    private readonly ICaptchaSolvingService _captchaSolvingService;
    private readonly ILogger<CaptchaWorkflowService> _logger;

    public CaptchaWorkflowService(
        ICaptchaSolvingService captchaSolvingService,
        ILogger<CaptchaWorkflowService> logger)
    {
        _captchaSolvingService = captchaSolvingService;
        _logger = logger;
    }

    /// <summary>
    /// Executes CAPTCHA service availability check with comprehensive validation.
    /// Moved from IvanLevelWorkflowService to maintain single responsibility.
    /// </summary>
    public async Task<ServiceAvailabilityWorkflowResult> ExecuteCaptchaSolvingAvailabilityAsync()
    {
        _logger.LogInformation("Testing CAPTCHA solving service availability");

        try
        {
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

            return new ServiceAvailabilityWorkflowResult(
                success: true,
                serviceName: "CaptchaSolving",
                serviceAvailable: isAvailable,
                additionalData: new Dictionary<string, object> { ["supportedTypes"] = supportedTypes },
                message: isAvailable ? "CAPTCHA service is available" : "CAPTCHA service is not available (check API key)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CAPTCHA service availability check failed");
            return new ServiceAvailabilityWorkflowResult(
                success: false,
                serviceName: "CaptchaSolving",
                serviceAvailable: false,
                errorMessage: $"Service availability check failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes comprehensive CAPTCHA solving workflow with retry logic and performance tracking.
    /// </summary>
    public async Task<CaptchaWorkflowResult> ExecuteCaptchaSolvingWorkflowAsync(CaptchaWorkflowRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var attempts = 0;
        CaptchaSolvingResult? lastResult = null;

        _logger.LogInformation("Starting CAPTCHA solving workflow for type: {CaptchaType}", request.Type);

        try
        {
            while (attempts < request.MaxRetries)
            {
                attempts++;
                _logger.LogDebug("CAPTCHA solving attempt {Attempt}/{MaxRetries}", attempts, request.MaxRetries);

                lastResult = await SolveCaptchaBasedOnTypeAsync(request);

                if (lastResult.Success)
                {
                    stopwatch.Stop();
                    _logger.LogInformation("CAPTCHA solved successfully on attempt {Attempt}, time: {ElapsedTime}ms", 
                        attempts, stopwatch.ElapsedMilliseconds);

                    return CaptchaWorkflowResult.SuccessResult(
                        lastResult.Data,
                        lastResult.CaptchaId ?? "unknown",
                        attempts,
                        stopwatch.Elapsed,
                        lastResult.Cost);
                }

                // If not the last attempt, wait before retrying
                if (attempts < request.MaxRetries)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempts)); // Exponential backoff
                    _logger.LogDebug("CAPTCHA solving failed, retrying after {DelaySeconds}s delay", delay.TotalSeconds);
                    await Task.Delay(delay);
                }
            }

            stopwatch.Stop();
            var errorMessage = $"CAPTCHA solving failed after {attempts} attempts";
            _logger.LogWarning(errorMessage + ": {LastError}", lastResult?.Message ?? "Unknown error");

            return CaptchaWorkflowResult.ErrorResult(
                errorMessage,
                attempts,
                stopwatch.Elapsed,
                lastResult?.ErrorDetails ?? lastResult?.Message);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "CAPTCHA solving workflow failed with exception after {Attempts} attempts", attempts);

            return CaptchaWorkflowResult.ErrorResult(
                $"Workflow failed with exception: {ex.Message}",
                attempts,
                stopwatch.Elapsed,
                ex.ToString());
        }
    }

    /// <summary>
    /// Routes CAPTCHA solving to appropriate method based on type.
    /// </summary>
    private async Task<CaptchaSolvingResult> SolveCaptchaBasedOnTypeAsync(CaptchaWorkflowRequest request)
    {
        return request.Type switch
        {
            CaptchaType.ImageCaptcha when !string.IsNullOrEmpty(request.ImageData) =>
                await _captchaSolvingService.SolveImageCaptchaAsync(request.ImageData, new ImageCaptchaOptions
                {
                    TimeoutSeconds = request.TimeoutSeconds
                }),

            CaptchaType.ImageCaptchaFromUrl when !string.IsNullOrEmpty(request.ImageUrl) =>
                await _captchaSolvingService.SolveImageCaptchaFromUrlAsync(request.ImageUrl, new ImageCaptchaOptions
                {
                    TimeoutSeconds = request.TimeoutSeconds
                }),

            CaptchaType.RecaptchaV2 when !string.IsNullOrEmpty(request.SiteKey) && !string.IsNullOrEmpty(request.PageUrl) =>
                await _captchaSolvingService.SolveRecaptchaV2Async(request.SiteKey, request.PageUrl, new RecaptchaOptions
                {
                    TimeoutSeconds = request.TimeoutSeconds
                }),

            CaptchaType.RecaptchaV3 when !string.IsNullOrEmpty(request.SiteKey) && !string.IsNullOrEmpty(request.PageUrl) =>
                await _captchaSolvingService.SolveRecaptchaV3Async(request.SiteKey, request.PageUrl, 
                    request.Action ?? "submit", request.MinScore, new RecaptchaOptions
                {
                    TimeoutSeconds = request.TimeoutSeconds
                }),

            CaptchaType.HCaptcha when !string.IsNullOrEmpty(request.SiteKey) && !string.IsNullOrEmpty(request.PageUrl) =>
                await _captchaSolvingService.SolveHCaptchaAsync(request.SiteKey, request.PageUrl, new HCaptchaOptions
                {
                    TimeoutSeconds = request.TimeoutSeconds
                }),

            CaptchaType.TextCaptcha when !string.IsNullOrEmpty(request.TextData) =>
                await _captchaSolvingService.SolveTextCaptchaAsync(request.TextData, new TextCaptchaOptions
                {
                    TimeoutSeconds = request.TimeoutSeconds
                }),

            _ => CaptchaSolvingResult.ErrorResult($"Invalid CAPTCHA type or missing required parameters for {request.Type}")
        };
    }

    /// <summary>
    /// Validates CAPTCHA solution and submits feedback to improve service quality.
    /// </summary>
    public async Task<CaptchaValidationResult> ValidateCaptchaSolutionAsync(string captchaId, bool isCorrect)
    {
        _logger.LogInformation("Validating CAPTCHA solution {CaptchaId}: {IsCorrect}", captchaId, isCorrect);

        try
        {
            if (string.IsNullOrEmpty(captchaId))
            {
                return CaptchaValidationResult.ErrorResult("CAPTCHA ID is required for validation");
            }

            if (!isCorrect)
            {
                var reportResult = await _captchaSolvingService.ReportIncorrectCaptchaAsync(captchaId);
                if (reportResult.Success)
                {
                    _logger.LogInformation("Reported incorrect CAPTCHA solution {CaptchaId}", captchaId);
                    return CaptchaValidationResult.SuccessResult("Incorrect solution reported successfully", true);
                }
                else
                {
                    _logger.LogWarning("Failed to report incorrect CAPTCHA solution {CaptchaId}: {Error}", 
                        captchaId, reportResult.Message);
                    return CaptchaValidationResult.ErrorResult("Failed to report incorrect solution", reportResult.ErrorDetails);
                }
            }

            return CaptchaValidationResult.SuccessResult("CAPTCHA solution validated as correct", false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CAPTCHA validation failed for {CaptchaId}", captchaId);
            return CaptchaValidationResult.ErrorResult($"Validation failed: {ex.Message}", ex.ToString());
        }
    }

    /// <summary>
    /// Executes batch CAPTCHA solving with coordination and performance optimization.
    /// </summary>
    public async Task<BatchCaptchaWorkflowResult> ExecuteBatchCaptchaSolvingAsync(IEnumerable<CaptchaWorkflowRequest> requests)
    {
        var requestList = requests.ToList();
        var totalStopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Starting batch CAPTCHA solving for {RequestCount} requests", requestList.Count);

        var results = new List<CaptchaWorkflowResult>();
        var totalCost = 0m;

        try
        {
            // Process requests concurrently with a degree of parallelism
            var semaphore = new SemaphoreSlim(5); // Limit concurrent requests to avoid overwhelming the service
            var tasks = requestList.Select(async request =>
            {
                await semaphore.WaitAsync();
                try
                {
                    return await ExecuteCaptchaSolvingWorkflowAsync(request);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            results.AddRange(await Task.WhenAll(tasks));

            totalStopwatch.Stop();

            var successfulSolutions = results.Count(r => r.Success);
            var failedSolutions = results.Count(r => !r.Success);
            totalCost = results.Where(r => r.Cost.HasValue).Sum(r => r.Cost!.Value);

            var summary = $"Batch processing completed: {successfulSolutions}/{requestList.Count} successful " +
                         $"(Success rate: {(double)successfulSolutions / requestList.Count:P2}), " +
                         $"Total time: {totalStopwatch.Elapsed:mm\\:ss}, Total cost: ${totalCost:F4}";

            _logger.LogInformation(summary);

            return new BatchCaptchaWorkflowResult
            {
                OverallSuccess = successfulSolutions > 0,
                TotalRequests = requestList.Count,
                SuccessfulSolutions = successfulSolutions,
                FailedSolutions = failedSolutions,
                Results = results,
                TotalTime = totalStopwatch.Elapsed,
                TotalCost = totalCost,
                Summary = summary
            };
        }
        catch (Exception ex)
        {
            totalStopwatch.Stop();
            _logger.LogError(ex, "Batch CAPTCHA solving failed");

            return new BatchCaptchaWorkflowResult
            {
                OverallSuccess = false,
                TotalRequests = requestList.Count,
                SuccessfulSolutions = 0,
                FailedSolutions = requestList.Count,
                Results = results,
                TotalTime = totalStopwatch.Elapsed,
                TotalCost = totalCost,
                Summary = $"Batch processing failed: {ex.Message}"
            };
        }
    }
}