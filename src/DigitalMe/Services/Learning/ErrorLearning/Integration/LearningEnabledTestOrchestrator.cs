using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DigitalMe.Services.Learning.Testing;
using DigitalMe.Services.Learning.ErrorLearning.Models;
using DigitalMe.Services.Learning;

namespace DigitalMe.Services.Learning.ErrorLearning.Integration;

/// <summary>
/// Enhanced test orchestrator that integrates with Error Learning System
/// Automatically captures test failures for machine learning analysis
/// Implements Decorator pattern around existing ITestOrchestrator
/// </summary>
public class LearningEnabledTestOrchestrator : ITestOrchestrator
{
    private readonly ILogger<LearningEnabledTestOrchestrator> _logger;
    private readonly ITestOrchestrator _baseOrchestrator;
    private readonly ITestFailureCapture _testFailureCapture;

    public LearningEnabledTestOrchestrator(
        ILogger<LearningEnabledTestOrchestrator> logger,
        ITestOrchestrator baseOrchestrator,
        ITestFailureCapture testFailureCapture)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _baseOrchestrator = baseOrchestrator ?? throw new ArgumentNullException(nameof(baseOrchestrator));
        _testFailureCapture = testFailureCapture ?? throw new ArgumentNullException(nameof(testFailureCapture));
    }

    /// <inheritdoc />
    public async Task<List<SelfGeneratedTestCase>> GenerateTestCasesAsync(DocumentationParseResult apiDocumentation)
    {
        // Delegate test generation to base orchestrator
        return await _baseOrchestrator.GenerateTestCasesAsync(apiDocumentation);
    }

    /// <inheritdoc />
    public async Task<TestExecutionResult> ExecuteTestCaseAsync(SelfGeneratedTestCase testCase)
    {
        try
        {
            _logger.LogDebug("Executing test case with learning enabled: {TestCaseId}", testCase?.Id ?? "Unknown");

            // Execute test using base orchestrator
            var result = await _baseOrchestrator.ExecuteTestCaseAsync(testCase);

            // Capture failure for learning if test failed
            if (!result.Success)
            {
                await CaptureTestFailureForLearning(result);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during learning-enabled test execution: {TestCaseId}", testCase?.Id ?? "Unknown");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<TestSuiteResult> ExecuteTestSuiteAsync(List<SelfGeneratedTestCase> testCases)
    {
        try
        {
            _logger.LogInformation("Executing test suite with learning enabled: {TestCount} test cases", testCases?.Count ?? 0);

            // Execute test suite using base orchestrator
            var suiteResult = await _baseOrchestrator.ExecuteTestSuiteAsync(testCases);

            // Capture all failures for learning in batch
            await CaptureTestSuiteFailuresForLearning(suiteResult);

            // Enhance suite result with learning recommendations
            await EnhanceSuiteResultWithLearningInsights(suiteResult);

            return suiteResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during learning-enabled test suite execution");
            throw;
        }
    }

    #region Private Learning Integration Methods

    /// <summary>
    /// Captures individual test failure for machine learning analysis
    /// </summary>
    private async Task CaptureTestFailureForLearning(TestExecutionResult testResult)
    {
        try
        {
            _logger.LogDebug("Capturing test failure for learning: {TestCaseName}", testResult.TestCaseName);

            var learningEntry = await _testFailureCapture.CaptureTestFailureAsync(testResult);

            _logger.LogInformation("Test failure captured for learning: {TestCaseName} -> {LearningEntryId}",
                testResult.TestCaseName, learningEntry.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture test failure for learning: {TestCaseName}", testResult.TestCaseName);
            // Don't propagate learning capture failures to avoid breaking test execution
        }
    }

    /// <summary>
    /// Captures multiple test failures from suite execution for batch learning
    /// </summary>
    private async Task CaptureTestSuiteFailuresForLearning(TestSuiteResult suiteResult)
    {
        var failedTests = suiteResult.TestResults?.Where(t => !t.Success).ToList();

        if (failedTests?.Any() != true)
        {
            _logger.LogDebug("No failures to capture from test suite: {SuiteName}", suiteResult.SuiteName);
            return;
        }

        try
        {
            _logger.LogInformation("Capturing {FailureCount} test failures for batch learning from suite: {SuiteName}",
                failedTests.Count, suiteResult.SuiteName);

            var learningEntries = await _testFailureCapture.CaptureTestFailuresBatchAsync(failedTests);

            _logger.LogInformation("Successfully captured {CapturedCount}/{TotalCount} test failures for learning",
                learningEntries.Count, failedTests.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture test suite failures for learning: {SuiteName}", suiteResult.SuiteName);
            // Don't propagate learning capture failures
        }
    }

    /// <summary>
    /// Enhances test suite result with insights from Error Learning System
    /// </summary>
    private async Task EnhanceSuiteResultWithLearningInsights(TestSuiteResult suiteResult)
    {
        try
        {
            // This could be enhanced to query learned patterns and add recommendations
            // For now, just add basic learning-enabled recommendations

            var failedTests = suiteResult.TestResults?.Where(t => !t.Success).ToList();
            if (failedTests?.Any() == true)
            {
                suiteResult.Recommendations.Add($"✨ {failedTests.Count} test failures have been captured for machine learning analysis");
                suiteResult.Recommendations.Add("🎯 Error Learning System will analyze patterns to suggest optimizations");

                // Group failures by common characteristics
                var failuresByErrorType = failedTests
                    .GroupBy(t => GetPrimaryErrorCategory(t))
                    .Where(g => g.Count() > 1)
                    .ToList();

                foreach (var group in failuresByErrorType)
                {
                    suiteResult.Recommendations.Add(
                        $"🔍 Pattern detected: {group.Count()} tests failed with {group.Key} errors - review for systematic issue");
                }
            }
            else
            {
                suiteResult.Recommendations.Add("✅ All tests passed - no learning data captured for failure analysis");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enhance suite result with learning insights");
            // Don't break test execution due to learning enhancement failures
        }
    }

    /// <summary>
    /// Extracts primary error category for pattern grouping
    /// </summary>
    private string GetPrimaryErrorCategory(TestExecutionResult testResult)
    {
        // Simple categorization logic - could be enhanced with ML
        if (testResult.Exception != null)
        {
            return testResult.Exception.GetType().Name;
        }

        if (testResult.ErrorMessage?.Contains("timeout", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "Timeout";
        }

        if (testResult.ErrorMessage?.Contains("connection", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "Connection";
        }

        if (testResult.AssertionResults?.Any(a => !a.Passed) == true)
        {
            return "Assertion";
        }

        return "General";
    }

    #endregion
}