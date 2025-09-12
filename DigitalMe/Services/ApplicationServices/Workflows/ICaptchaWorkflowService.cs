using DigitalMe.Services.CaptchaSolving;

namespace DigitalMe.Services.ApplicationServices.Workflows;

/// <summary>
/// Service for orchestrating CAPTCHA solving workflows.
/// Handles complex CAPTCHA operations with retry logic, validation, and coordination.
/// Extracted from IvanLevelWorkflowService following Single Responsibility Principle.
/// </summary>
public interface ICaptchaWorkflowService
{
    /// <summary>
    /// Executes CAPTCHA service availability check with comprehensive validation.
    /// </summary>
    /// <returns>Service availability workflow result with detailed status information</returns>
    Task<ServiceAvailabilityWorkflowResult> ExecuteCaptchaSolvingAvailabilityAsync();

    /// <summary>
    /// Executes a comprehensive CAPTCHA solving workflow with retry logic.
    /// </summary>
    /// <param name="request">CAPTCHA workflow request parameters</param>
    /// <returns>CAPTCHA workflow result with solution and performance metrics</returns>
    Task<CaptchaWorkflowResult> ExecuteCaptchaSolvingWorkflowAsync(CaptchaWorkflowRequest request);

    /// <summary>
    /// Validates CAPTCHA solution and provides feedback for service improvement.
    /// </summary>
    /// <param name="captchaId">ID of the solved CAPTCHA</param>
    /// <param name="isCorrect">Whether the solution was correct</param>
    /// <returns>Validation result with feedback submission status</returns>
    Task<CaptchaValidationResult> ValidateCaptchaSolutionAsync(string captchaId, bool isCorrect);

    /// <summary>
    /// Executes batch CAPTCHA solving with coordination and optimization.
    /// </summary>
    /// <param name="requests">Collection of CAPTCHA requests to process</param>
    /// <returns>Batch processing results with individual solution status</returns>
    Task<BatchCaptchaWorkflowResult> ExecuteBatchCaptchaSolvingAsync(IEnumerable<CaptchaWorkflowRequest> requests);
}

/// <summary>
/// Request parameters for CAPTCHA solving workflow
/// </summary>
public class CaptchaWorkflowRequest
{
    public CaptchaType Type { get; set; }
    public string? ImageData { get; set; }
    public string? ImageUrl { get; set; }
    public string? SiteKey { get; set; }
    public string? PageUrl { get; set; }
    public string? Action { get; set; }
    public double MinScore { get; set; } = 0.3;
    public string? TextData { get; set; }
    public int MaxRetries { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 300;
    public bool EnableValidation { get; set; } = true;
}

/// <summary>
/// CAPTCHA type enumeration for workflow routing
/// </summary>
public enum CaptchaType
{
    ImageCaptcha,
    ImageCaptchaFromUrl,
    RecaptchaV2,
    RecaptchaV3,
    HCaptcha,
    TextCaptcha
}

/// <summary>
/// Result of CAPTCHA solving workflow execution
/// </summary>
public class CaptchaWorkflowResult
{
    public bool Success { get; init; }
    public object? SolutionData { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? CaptchaId { get; init; }
    public int AttemptsUsed { get; init; }
    public TimeSpan TotalTime { get; init; }
    public decimal? Cost { get; init; }
    public string? ErrorDetails { get; init; }

    public static CaptchaWorkflowResult SuccessResult(object? data, string captchaId, int attempts, TimeSpan time, decimal? cost)
        => new() { Success = true, SolutionData = data, CaptchaId = captchaId, AttemptsUsed = attempts, TotalTime = time, Cost = cost, Message = "CAPTCHA solved successfully" };

    public static CaptchaWorkflowResult ErrorResult(string message, int attempts, TimeSpan time, string? details = null)
        => new() { Success = false, Message = message, AttemptsUsed = attempts, TotalTime = time, ErrorDetails = details };
}

/// <summary>
/// Result of CAPTCHA solution validation
/// </summary>
public class CaptchaValidationResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool FeedbackSubmitted { get; init; }
    public string? ErrorDetails { get; init; }

    public static CaptchaValidationResult SuccessResult(string message = "Validation completed successfully", bool feedbackSubmitted = true)
        => new() { Success = true, Message = message, FeedbackSubmitted = feedbackSubmitted };

    public static CaptchaValidationResult ErrorResult(string message, string? details = null)
        => new() { Success = false, Message = message, ErrorDetails = details };
}

/// <summary>
/// Result of batch CAPTCHA solving workflow
/// </summary>
public class BatchCaptchaWorkflowResult
{
    public bool OverallSuccess { get; init; }
    public int TotalRequests { get; init; }
    public int SuccessfulSolutions { get; init; }
    public int FailedSolutions { get; init; }
    public List<CaptchaWorkflowResult> Results { get; init; } = new();
    public TimeSpan TotalTime { get; init; }
    public decimal? TotalCost { get; init; }
    public string Summary { get; init; } = string.Empty;

    public double SuccessRate => TotalRequests > 0 ? (double)SuccessfulSolutions / TotalRequests : 0;
}

