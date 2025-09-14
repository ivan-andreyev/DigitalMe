using System.Threading.Tasks;
using DigitalMe.Services.Learning.ErrorLearning.Models;

namespace DigitalMe.Services.Learning.ErrorLearning;

/// <summary>
/// Service for recording and initial classification of errors
/// Follows SRP - focused only on error recording operations
/// </summary>
public interface IErrorRecordingService
{
    /// <summary>
    /// Records a new error occurrence for learning analysis
    /// </summary>
    /// <param name="request">Error recording request with all necessary details</param>
    /// <returns>Learning history entry created for this error</returns>
    Task<LearningHistoryEntry> RecordErrorAsync(ErrorRecordingRequest request);
}

/// <summary>
/// Request object for error recording to avoid God Method anti-pattern
/// </summary>
public class ErrorRecordingRequest
{
    /// <summary>
    /// Source of the error (e.g., "SelfTestingFramework", "AutoDocumentationParser")
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Full error message or exception details
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Name of the test case that failed (if applicable)
    /// </summary>
    public string? TestCaseName { get; set; }

    /// <summary>
    /// API name being tested when error occurred
    /// </summary>
    public string? ApiName { get; set; }

    /// <summary>
    /// HTTP method if applicable
    /// </summary>
    public string? HttpMethod { get; set; }

    /// <summary>
    /// API endpoint where error occurred
    /// </summary>
    public string? ApiEndpoint { get; set; }

    /// <summary>
    /// HTTP status code if applicable
    /// </summary>
    public int? HttpStatusCode { get; set; }

    /// <summary>
    /// Request details as JSON string
    /// </summary>
    public string? RequestDetails { get; set; }

    /// <summary>
    /// Response details as JSON string
    /// </summary>
    public string? ResponseDetails { get; set; }

    /// <summary>
    /// Stack trace if available
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// Environment context as JSON string
    /// </summary>
    public string? EnvironmentContext { get; set; }
}