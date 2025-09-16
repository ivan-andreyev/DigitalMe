using DigitalMe.Services.ApplicationServices.Commands;
using DigitalMe.Services.ApplicationServices.Queries;

namespace DigitalMe.Services.ApplicationServices.Workflows;

/// <summary>
/// Service for orchestrating complex Personal-Level workflows across multiple services.
/// Implements Clean Architecture Application Services layer patterns.
/// </summary>
public interface IPersonalLevelWorkflowService : IApplicationService
{
    /// <summary>
    /// Executes file processing workflow with comprehensive testing.
    /// </summary>
    Task<FileProcessingWorkflowResult> ExecuteFileProcessingWorkflowAsync(FileProcessingWorkflowRequest request);

    /// <summary>
    /// Executes comprehensive multi-service testing workflow.
    /// </summary>
    Task<ComprehensiveTestWorkflowResult> ExecuteComprehensiveTestWorkflowAsync(ComprehensiveTestWorkflowRequest request);

    /// <summary>
    /// Executes web navigation testing workflow.
    /// </summary>
    Task<WebNavigationWorkflowResult> ExecuteWebNavigationWorkflowAsync();

    /// <summary>
    /// Executes service availability check workflow.
    /// </summary>
    Task<ServiceAvailabilityWorkflowResult> ExecuteServiceAvailabilityWorkflowAsync(string serviceName);

    /// <summary>
    /// CRITICAL: TRUE INTEGRATION - WebNavigation → CAPTCHA solving → File processing → Voice narration
    /// Real end-to-end workflow demonstrating all Ivan-Level services working together.
    /// </summary>
    Task<WebToCaptchaToFileToVoiceWorkflowResult> ExecuteWebToCaptchaToFileToVoiceWorkflowAsync(WebToCaptchaToFileToVoiceRequest request);

    /// <summary>
    /// CRITICAL: TRUE INTEGRATION - Site registration → Form filling → Document download → PDF conversion
    /// Complex multi-step workflow demonstrating service coordination in real scenarios.
    /// </summary>
    Task<SiteRegistrationToDocumentWorkflowResult> ExecuteSiteRegistrationToDocumentWorkflowAsync(SiteRegistrationToDocumentRequest request);
}

/// <summary>
/// Request for file processing workflow.
/// </summary>
public record FileProcessingWorkflowRequest(
    string Content,
    string? Title = null);

/// <summary>
/// Result of file processing workflow.
/// </summary>
public record FileProcessingWorkflowResult(
    bool Success,
    bool PdfCreated,
    bool TextExtracted,
    bool ContentMatch,
    string? FilePath,
    string? ExtractedTextPreview,
    string? ErrorMessage = null);

/// <summary>
/// Request for comprehensive test workflow.
/// </summary>
public record ComprehensiveTestWorkflowRequest(
    string? TestContent = null,
    bool IncludeWebNavigation = false,
    bool IncludeCaptcha = false);

/// <summary>
/// Result of comprehensive test workflow.
/// </summary>
public record ComprehensiveTestWorkflowResult(
    bool OverallSuccess,
    DateTime Timestamp,
    Dictionary<string, object> TestResults,
    ComprehensiveTestSummary Summary);

/// <summary>
/// Summary of comprehensive test results.
/// </summary>
public record ComprehensiveTestSummary(
    int TotalTests,
    int PassedTests,
    int FailedTests);

/// <summary>
/// Result of web navigation workflow.
/// </summary>
public record WebNavigationWorkflowResult(
    bool Success,
    bool BrowserInitialized,
    string Message,
    string? ErrorMessage = null);

/// <summary>
/// Result of service availability workflow.
/// </summary>
public record ServiceAvailabilityWorkflowResult(
    bool Success,
    string ServiceName,
    bool ServiceAvailable,
    Dictionary<string, object>? AdditionalData = null,
    string? Message = null,
    string? ErrorMessage = null);

/// <summary>
/// CRITICAL: Request for WebNavigation → CAPTCHA → File → Voice workflow.
/// </summary>
public record WebToCaptchaToFileToVoiceRequest(
    string TargetUrl,
    string ExpectedContent,
    bool ProcessCaptcha = true,
    bool GenerateVoiceNarration = true,
    string? VoiceText = null);

/// <summary>
/// CRITICAL: Result of WebNavigation → CAPTCHA → File → Voice workflow.
/// </summary>
public record WebToCaptchaToFileToVoiceWorkflowResult(
    bool OverallSuccess,
    WebNavigationStepResult WebNavigationStep,
    CaptchaStepResult CaptchaStep,
    FileProcessingStepResult FileProcessingStep,
    VoiceNarrationStepResult VoiceStep,
    DateTime Timestamp,
    string? ErrorMessage = null);

/// <summary>
/// CRITICAL: Request for Site registration → Form filling → Document → PDF workflow.
/// </summary>
public record SiteRegistrationToDocumentRequest(
    string RegistrationUrl,
    Dictionary<string, string> UserData,
    string DocumentDownloadPath,
    bool ConvertToPdf = true);

/// <summary>
/// CRITICAL: Result of Site registration → Form filling → Document → PDF workflow.
/// </summary>
public record SiteRegistrationToDocumentWorkflowResult(
    bool OverallSuccess,
    SiteRegistrationStepResult RegistrationStep,
    FormFillingStepResult FormFillingStep,
    DocumentDownloadStepResult DocumentStep,
    PdfConversionStepResult PdfConversionStep,
    DateTime Timestamp,
    string? ErrorMessage = null);

/// <summary>
/// Step results for TRUE integration workflows.
/// </summary>
public record WebNavigationStepResult(bool Success, string Message, string? ContentExtracted = null, string? ErrorMessage = null);
public record CaptchaStepResult(bool Success, string Message, bool CaptchaDetected = false, bool CaptchaSolved = false, string? ErrorMessage = null);
public record FileProcessingStepResult(bool Success, string Message, string? FilePath = null, string? ExtractedText = null, string? ErrorMessage = null);
public record VoiceNarrationStepResult(bool Success, string Message, string? AudioFilePath = null, double? AudioDurationSeconds = null, string? ErrorMessage = null);
public record SiteRegistrationStepResult(bool Success, string Message, bool UserRegistered = false, string? ErrorMessage = null);
public record FormFillingStepResult(bool Success, string Message, Dictionary<string, bool>? FieldsFilled = null, string? ErrorMessage = null);
public record DocumentDownloadStepResult(bool Success, string Message, string? DownloadedFilePath = null, long? FileSizeBytes = null, string? ErrorMessage = null);
public record PdfConversionStepResult(bool Success, string Message, string? PdfFilePath = null, int? PageCount = null, string? ErrorMessage = null);

/// <summary>
/// Legacy alias for IPersonalLevelWorkflowService for backward compatibility.
/// </summary>
[Obsolete("Use IPersonalLevelWorkflowService instead", false)]
public interface IIvanLevelWorkflowService : IPersonalLevelWorkflowService
{
}