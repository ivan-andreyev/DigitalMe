using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DigitalMe.Services.Learning.ErrorLearning.Models;
using DigitalMe.Services.Learning;

namespace DigitalMe.Services.Learning.ErrorLearning.Integration;

/// <summary>
/// Service for capturing test failures from SelfTestingFramework
/// and converting them into error learning data
/// Implements SRP by focusing solely on failure capture and conversion
/// </summary>
public class TestFailureCaptureService : ITestFailureCapture
{
    private readonly ILogger<TestFailureCaptureService> _logger;
    private readonly IErrorLearningService _errorLearningService;

    public TestFailureCaptureService(
        ILogger<TestFailureCaptureService> logger,
        IErrorLearningService errorLearningService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _errorLearningService = errorLearningService ?? throw new ArgumentNullException(nameof(errorLearningService));
    }

    /// <inheritdoc />
    public async Task<LearningHistoryEntry> CaptureTestFailureAsync(TestExecutionResult testExecutionResult)
    {
        if (testExecutionResult == null)
            throw new ArgumentNullException(nameof(testExecutionResult));

        if (testExecutionResult.Success)
        {
            _logger.LogWarning("Attempted to capture successful test as failure: {TestCaseId}", testExecutionResult.TestCaseId);
            throw new ArgumentException("Cannot capture successful test as failure", nameof(testExecutionResult));
        }

        try
        {
            _logger.LogInformation("Capturing test failure for learning: {TestCaseName}", testExecutionResult.TestCaseName);

            var errorMessage = BuildErrorMessage(testExecutionResult);
            var stackTrace = testExecutionResult.Exception?.StackTrace;
            var environmentContext = BuildEnvironmentContext(testExecutionResult);

            // Extract API details from test execution result
            var apiEndpoint = ExtractApiEndpoint(testExecutionResult);
            var httpMethod = ExtractHttpMethod(testExecutionResult);
            var httpStatusCode = ExtractHttpStatusCode(testExecutionResult);

            var learningEntry = await _errorLearningService.RecordErrorAsync(
                source: "SelfTestingFramework",
                errorMessage: errorMessage,
                testCaseName: testExecutionResult.TestCaseName,
                apiName: ExtractApiName(testExecutionResult),
                httpMethod: httpMethod,
                apiEndpoint: apiEndpoint,
                httpStatusCode: httpStatusCode,
                requestDetails: BuildRequestDetails(testExecutionResult),
                responseDetails: BuildResponseDetails(testExecutionResult),
                stackTrace: stackTrace,
                environmentContext: environmentContext);

            _logger.LogInformation("Successfully captured test failure for learning: {LearningEntryId}", learningEntry.Id);
            return learningEntry;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture test failure: {TestCaseId}", testExecutionResult.TestCaseId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<LearningHistoryEntry>> CaptureTestFailuresBatchAsync(IEnumerable<TestExecutionResult> failedTestResults)
    {
        if (failedTestResults == null)
            throw new ArgumentNullException(nameof(failedTestResults));

        var failedTests = failedTestResults.Where(t => !t.Success).ToList();

        if (!failedTests.Any())
        {
            _logger.LogInformation("No failed tests to capture for learning");
            return new List<LearningHistoryEntry>();
        }

        _logger.LogInformation("Capturing batch of {FailureCount} test failures for learning", failedTests.Count);

        var learningEntries = new List<LearningHistoryEntry>();

        foreach (var failedTest in failedTests)
        {
            try
            {
                var learningEntry = await CaptureTestFailureAsync(failedTest);
                learningEntries.Add(learningEntry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to capture individual test failure in batch: {TestCaseId}", failedTest.TestCaseId);
                // Continue processing other failures even if one fails
            }
        }

        _logger.LogInformation("Successfully captured {CapturedCount}/{TotalCount} test failures for learning",
            learningEntries.Count, failedTests.Count);

        return learningEntries;
    }

    #region Private Helper Methods

    /// <summary>
    /// Builds comprehensive error message from test execution result
    /// </summary>
    private string BuildErrorMessage(TestExecutionResult testExecutionResult)
    {
        var errorParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(testExecutionResult.ErrorMessage))
        {
            errorParts.Add($"Test Error: {testExecutionResult.ErrorMessage}");
        }

        if (testExecutionResult.Exception != null)
        {
            errorParts.Add($"Exception: {testExecutionResult.Exception.Message}");
        }

        // Add failed assertions
        var failedAssertions = testExecutionResult.AssertionResults?.Where(a => !a.Passed).ToList();
        if (failedAssertions?.Any() == true)
        {
            var assertionErrors = failedAssertions.Select(a =>
                $"Assertion '{a.AssertionName}' failed: Expected '{a.ExpectedValue}', Got '{a.ActualValue}'. {a.ErrorMessage}");
            errorParts.Add($"Failed Assertions: {string.Join("; ", assertionErrors)}");
        }

        return string.Join(" | ", errorParts);
    }

    /// <summary>
    /// Builds environment context from test execution metrics
    /// </summary>
    private string BuildEnvironmentContext(TestExecutionResult testExecutionResult)
    {
        var context = new
        {
            ExecutionTime = testExecutionResult.ExecutionTime.ToString(),
            AttemptNumber = testExecutionResult.AttemptNumber,
            ExecutedAt = testExecutionResult.ExecutedAt.ToString("O"),
            Metrics = testExecutionResult.Metrics,
            TotalAssertions = testExecutionResult.AssertionResults?.Count ?? 0,
            FailedAssertions = testExecutionResult.AssertionResults?.Count(a => !a.Passed) ?? 0
        };

        return System.Text.Json.JsonSerializer.Serialize(context);
    }

    /// <summary>
    /// Extracts API endpoint from test metrics or response
    /// </summary>
    private string? ExtractApiEndpoint(TestExecutionResult testExecutionResult)
    {
        // Try to extract from metrics first
        if (testExecutionResult.Metrics?.TryGetValue("ApiEndpoint", out var endpointObj) == true)
        {
            return endpointObj?.ToString();
        }

        // Try to extract from response or other sources
        return null;
    }

    /// <summary>
    /// Extracts HTTP method from test metrics
    /// </summary>
    private string? ExtractHttpMethod(TestExecutionResult testExecutionResult)
    {
        if (testExecutionResult.Metrics?.TryGetValue("HttpMethod", out var methodObj) == true)
        {
            return methodObj?.ToString();
        }

        return null;
    }

    /// <summary>
    /// Extracts HTTP status code from response or metrics
    /// </summary>
    private int? ExtractHttpStatusCode(TestExecutionResult testExecutionResult)
    {
        if (testExecutionResult.Metrics?.TryGetValue("HttpStatusCode", out var statusObj) == true)
        {
            if (int.TryParse(statusObj?.ToString(), out var statusCode))
            {
                return statusCode;
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts API name from test metrics
    /// </summary>
    private string? ExtractApiName(TestExecutionResult testExecutionResult)
    {
        if (testExecutionResult.Metrics?.TryGetValue("ApiName", out var apiNameObj) == true)
        {
            return apiNameObj?.ToString();
        }

        return null;
    }

    /// <summary>
    /// Builds request details JSON from test metrics
    /// </summary>
    private string? BuildRequestDetails(TestExecutionResult testExecutionResult)
    {
        var requestData = new Dictionary<string, object>();

        // Extract request-related metrics
        foreach (var metric in testExecutionResult.Metrics ?? new Dictionary<string, object>())
        {
            if (metric.Key.StartsWith("Request", StringComparison.OrdinalIgnoreCase))
            {
                requestData[metric.Key] = metric.Value;
            }
        }

        return requestData.Any() ? System.Text.Json.JsonSerializer.Serialize(requestData) : null;
    }

    /// <summary>
    /// Builds response details JSON from test response and metrics
    /// </summary>
    private string? BuildResponseDetails(TestExecutionResult testExecutionResult)
    {
        var responseData = new Dictionary<string, object>();

        // Include response object if available
        if (testExecutionResult.Response != null)
        {
            responseData["Response"] = testExecutionResult.Response;
        }

        // Extract response-related metrics
        foreach (var metric in testExecutionResult.Metrics ?? new Dictionary<string, object>())
        {
            if (metric.Key.StartsWith("Response", StringComparison.OrdinalIgnoreCase))
            {
                responseData[metric.Key] = metric.Value;
            }
        }

        return responseData.Any() ? System.Text.Json.JsonSerializer.Serialize(responseData) : null;
    }

    #endregion
}