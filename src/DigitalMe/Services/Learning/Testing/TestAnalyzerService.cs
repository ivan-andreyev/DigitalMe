using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalMe.Services.Learning;
using DigitalMe.Services.Learning.Testing.ResultsAnalysis;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Learning.Testing;

/// <summary>
/// Dedicated test analysis service implementation
/// Focused on failure analysis and improvement suggestions
/// Following Single Responsibility Principle
/// </summary>
public class TestAnalyzerService : ITestAnalyzer
{
    private readonly ILogger<TestAnalyzerService> _logger;
    private readonly IResultsAnalyzer _resultsAnalyzer;

    public TestAnalyzerService(
        ILogger<TestAnalyzerService> logger,
        IResultsAnalyzer resultsAnalyzer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _resultsAnalyzer = resultsAnalyzer ?? throw new ArgumentNullException(nameof(resultsAnalyzer));
    }

    /// <inheritdoc />
    public async Task<TestAnalysisResult> AnalyzeTestFailuresAsync(List<TestExecutionResult> failedTests)
    {
        _logger.LogDebug("Analyzing {FailedTestCount} failed tests", failedTests?.Count ?? 0);

        if (failedTests == null)
        {
            _logger.LogWarning("Cannot analyze test failures: failedTests list is null");
            return new TestAnalysisResult
            {
                TotalFailedTests = 0,
                OverallHealthScore = 0.0,
                CriticalIssues = new List<string> { "Failed tests list is null" }
            };
        }

        return await _resultsAnalyzer.AnalyzeTestFailuresAsync(failedTests);
    }
}