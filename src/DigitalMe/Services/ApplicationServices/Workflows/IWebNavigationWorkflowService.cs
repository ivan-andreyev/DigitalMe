using DigitalMe.Services.ApplicationServices.Commands;
using DigitalMe.Services.ApplicationServices.Queries;

namespace DigitalMe.Services.ApplicationServices.Workflows;

/// <summary>
/// Service for orchestrating web navigation workflows.
/// Handles browser automation, web content extraction, and navigation-based workflows.
/// Extracted from IvanLevelWorkflowService to follow Single Responsibility Principle.
/// </summary>
public interface IWebNavigationWorkflowService : IApplicationService
{
    /// <summary>
    /// Executes basic web navigation testing workflow.
    /// Tests browser initialization and readiness.
    /// </summary>
    Task<WebNavigationWorkflowResult> ExecuteWebNavigationWorkflowAsync();

    /// <summary>
    /// CRITICAL: TRUE INTEGRATION - WebNavigation → CAPTCHA solving → File processing → Voice narration
    /// Real end-to-end workflow demonstrating all Ivan-Level services working together.
    /// </summary>
    Task<WebToCaptchaToFileToVoiceWorkflowResult> ExecuteWebToCaptchaToFileToVoiceWorkflowAsync(WebToCaptchaToFileToVoiceRequest request);

    /// <summary>
    /// CRITICAL: TRUE INTEGRATION - Site registration → Form filling → Document download → PDF conversion
    /// Complex multi-step workflow demonstrating service coordination in realistic scenarios.
    /// </summary>
    Task<SiteRegistrationToDocumentWorkflowResult> ExecuteSiteRegistrationToDocumentWorkflowAsync(SiteRegistrationToDocumentRequest request);
}