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
    string content,
    string? title = null);

/// <summary>
/// Result of file processing workflow.
/// </summary>
public record FileProcessingWorkflowResult(
    bool success,
    bool pdfCreated,
    bool textExtracted,
    bool contentMatch,
    string? filePath,
    string? extractedTextPreview,
    string? errorMessage = null);

/// <summary>
/// Request for comprehensive test workflow.
/// </summary>
public record ComprehensiveTestWorkflowRequest(
    string? testContent = null,
    bool includeWebNavigation = false,
    bool includeCaptcha = false);

/// <summary>
/// Result of comprehensive test workflow.
/// </summary>
public record ComprehensiveTestWorkflowResult(
    bool overallSuccess,
    DateTime timestamp,
    Dictionary<string, object> testResults,
    ComprehensiveTestSummary summary);

/// <summary>
/// Summary of comprehensive test results.
/// </summary>
public record ComprehensiveTestSummary(
    int totalTests,
    int passedTests,
    int failedTests);

/// <summary>
/// Result of web navigation workflow.
/// </summary>
public record WebNavigationWorkflowResult(
    bool success,
    bool browserInitialized,
    string message,
    string? errorMessage = null);

/// <summary>
/// Result of service availability workflow.
/// </summary>
public record ServiceAvailabilityWorkflowResult(
    bool success,
    string serviceName,
    bool serviceAvailable,
    Dictionary<string, object>? additionalData = null,
    string? message = null,
    string? errorMessage = null);

/// <summary>
/// CRITICAL: Request for WebNavigation → CAPTCHA → File → Voice workflow.
/// </summary>
public record WebToCaptchaToFileToVoiceRequest(
    string targetUrl,
    string expectedContent,
    bool processCaptcha = true,
    bool generateVoiceNarration = true,
    string? voiceText = null);

/// <summary>
/// CRITICAL: Result of WebNavigation → CAPTCHA → File → Voice workflow.
/// </summary>
public record WebToCaptchaToFileToVoiceWorkflowResult(
    bool overallSuccess,
    WebNavigationStepResult webNavigationStep,
    CaptchaStepResult captchaStep,
    FileProcessingStepResult fileProcessingStep,
    VoiceNarrationStepResult voiceStep,
    DateTime timestamp,
    string? errorMessage = null);

/// <summary>
/// CRITICAL: Request for Site registration → Form filling → Document → PDF workflow.
/// </summary>
public record SiteRegistrationToDocumentRequest(
    string registrationUrl,
    Dictionary<string, string> userData,
    string documentDownloadPath,
    bool convertToPdf = true);

/// <summary>
/// CRITICAL: Result of Site registration → Form filling → Document → PDF workflow.
/// </summary>
public record SiteRegistrationToDocumentWorkflowResult(
    bool overallSuccess,
    SiteRegistrationStepResult registrationStep,
    FormFillingStepResult formFillingStep,
    DocumentDownloadStepResult documentStep,
    PdfConversionStepResult pdfConversionStep,
    DateTime timestamp,
    string? errorMessage = null);

/// <summary>
/// Step results for TRUE integration workflows.
/// </summary>
public record WebNavigationStepResult(bool success, string message, string? contentExtracted = null, string? errorMessage = null);
public record CaptchaStepResult(bool success, string message, bool captchaDetected = false, bool captchaSolved = false, string? errorMessage = null);
public record FileProcessingStepResult(bool success, string message, string? filePath = null, string? extractedText = null, string? errorMessage = null);
public record VoiceNarrationStepResult(bool success, string message, string? audioFilePath = null, double? audioDurationSeconds = null, string? errorMessage = null);
public record SiteRegistrationStepResult(bool success, string message, bool userRegistered = false, string? errorMessage = null);
public record FormFillingStepResult(bool success, string message, Dictionary<string, bool>? fieldsFilled = null, string? errorMessage = null);
public record DocumentDownloadStepResult(bool success, string message, string? downloadedFilePath = null, long? fileSizeBytes = null, string? errorMessage = null);
public record PdfConversionStepResult(bool success, string message, string? pdfFilePath = null, int? pageCount = null, string? errorMessage = null);

/// <summary>
/// Legacy alias for IPersonalLevelWorkflowService for backward compatibility.
/// </summary>
[Obsolete("Use IPersonalLevelWorkflowService instead", false)]
public interface IIvanLevelWorkflowService : IPersonalLevelWorkflowService
{
}