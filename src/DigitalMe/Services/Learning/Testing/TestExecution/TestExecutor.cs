using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using DigitalMe.Services.Learning.Testing.ParallelProcessing;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Learning.Testing.TestExecution;

/// <summary>
/// Test execution engine implementation - Single Responsibility: Execute tests
/// Extracted from SelfTestingFramework God Class following Clean Architecture
/// Responsible for HTTP request execution, response validation, and result aggregation
/// ~300 lines focused on test execution concerns only
/// </summary>
public class TestExecutor : ITestExecutor
{
    private readonly ILogger<TestExecutor> _logger;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ISingleTestExecutor _singleTestExecutor;

    public TestExecutor(
        ILogger<TestExecutor> logger,
        HttpClient httpClient,
        ISingleTestExecutor singleTestExecutor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _singleTestExecutor = singleTestExecutor ?? throw new ArgumentNullException(nameof(singleTestExecutor));
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    /// <inheritdoc />
    public async Task<TestExecutionResult> ExecuteTestCaseAsync(SelfGeneratedTestCase testCase)
    {
        // Delegate single test execution to specialized service
        return await _singleTestExecutor.ExecuteTestCaseAsync(testCase);
    }

    /// <inheritdoc />
    public async Task<TestSuiteResult> ExecuteTestSuiteAsync(List<SelfGeneratedTestCase> testCases)
    {
        var suiteStopwatch = Stopwatch.StartNew();
        var result = new TestSuiteResult
        {
            SuiteName = $"Self-Generated Test Suite ({testCases.Count} tests)",
            Status = TestSuiteStatus.Running
        };

        try
        {
            _logger.LogInformation("Executing test suite with {TestCount} test cases", testCases.Count);

            if (testCases.Count == 0)
            {
                // Handle empty test suite
                suiteStopwatch.Stop();
                result.TotalExecutionTime = suiteStopwatch.Elapsed;
                result.Status = TestSuiteStatus.Completed;
                result.TestResults = new List<TestExecutionResult>(); // Initialize empty test results
                result.Recommendations = new List<string>(); // Empty recommendations for empty suite
                
                _logger.LogInformation("Empty test suite completed in {ExecutionTime}ms", 
                    result.TotalExecutionTime.TotalMilliseconds);
            }
            else
            {
                // Execute tests in parallel directly using simple parallelism
                var maxConcurrency = Math.Min(5, testCases.Count); // Simple concurrency limit
                var semaphore = new SemaphoreSlim(maxConcurrency);
                
                var executionTasks = testCases.Select(async testCase =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        return await _singleTestExecutor.ExecuteTestCaseAsync(testCase);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                var testResults = await Task.WhenAll(executionTasks);
                result.TestResults = testResults.ToList();
            
                suiteStopwatch.Stop();
                result.TotalExecutionTime = suiteStopwatch.Elapsed;
                result.Status = TestSuiteStatus.Completed;

                // Generate recommendations based on results
                result.Recommendations = GenerateTestSuiteRecommendations(result);

                _logger.LogInformation("Test suite completed: {PassedTests}/{TotalTests} tests passed ({SuccessRate:F1}%) in {ExecutionTime}ms",
                    result.PassedTests, result.TotalTests, result.SuccessRate, result.TotalExecutionTime.TotalMilliseconds);
            }
        }
        catch (Exception ex)
        {
            suiteStopwatch.Stop();
            result.TotalExecutionTime = suiteStopwatch.Elapsed;
            result.Status = TestSuiteStatus.Failed;
            result.TestResults ??= new List<TestExecutionResult>(); // Ensure TestResults is never null
            result.Recommendations ??= new List<string>(); // Ensure Recommendations is never null
            _logger.LogError(ex, "Test suite execution failed");
        }

        return result;
    }

    #region Private Helper Methods

    private List<string> GenerateTestSuiteRecommendations(TestSuiteResult result)
    {
        var recommendations = new List<string>();

        if (result.SuccessRate < 50)
        {
            recommendations.Add("Overall test success rate is low. Review API configuration and test data.");
        }
        
        if (result.FailedTests > result.PassedTests)
        {
            recommendations.Add("More tests are failing than passing. Consider reviewing test expectations.");
        }

        var avgExecutionTime = result.TestResults.Average(r => r.ExecutionTime.TotalMilliseconds);
        if (avgExecutionTime > 5000)
        {
            recommendations.Add("Average test execution time is high. Consider optimizing API performance.");
        }

        return recommendations;
    }

    #endregion
}