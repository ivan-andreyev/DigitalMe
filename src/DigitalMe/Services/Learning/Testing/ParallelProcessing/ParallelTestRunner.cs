using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DigitalMe.Services.Learning;
using DigitalMe.Services.Learning.Testing.TestExecution;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Learning.Testing.ParallelProcessing;

/// <summary>
/// Parallel test execution engine implementation - Single Responsibility: Manage concurrent test execution
/// Extracted from SelfTestingFramework God Class following Clean Architecture
/// Responsible for concurrent test running, resource management, and thread coordination
/// ~250 lines focused on parallel execution concerns only
/// </summary>
public class ParallelTestRunner : IParallelTestRunner
{
    private readonly ILogger<ParallelTestRunner> _logger;
    private readonly ISingleTestExecutor _singleTestExecutor;

    public ParallelTestRunner(
        ILogger<ParallelTestRunner> logger,
        ISingleTestExecutor singleTestExecutor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _singleTestExecutor = singleTestExecutor ?? throw new ArgumentNullException(nameof(singleTestExecutor));
    }

    /// <inheritdoc />
    public async Task<List<TestExecutionResult>> ExecuteTestsInParallelAsync(
        List<SelfGeneratedTestCase> testCases, 
        int maxConcurrency = 5)
    {
        if (testCases == null || testCases.Count == 0)
        {
            _logger.LogDebug("No test cases provided for parallel execution");
            return new List<TestExecutionResult>();
        }

        _logger.LogInformation("Executing {TestCount} tests in parallel with max concurrency {MaxConcurrency}", 
            testCases.Count, maxConcurrency);

        var semaphore = new SemaphoreSlim(maxConcurrency);
        var executionTasks = testCases.Select(async testCase =>
        {
            await semaphore.WaitAsync();
            try
            {
                _logger.LogDebug("Starting parallel execution of test: {TestName}", testCase.Name);
                var result = await _singleTestExecutor.ExecuteTestCaseAsync(testCase);
                _logger.LogDebug("Completed parallel execution of test: {TestName} - {Status}", 
                    testCase.Name, result.Success ? "PASSED" : "FAILED");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing test case {TestName}: {ErrorMessage}", testCase.Name, ex.Message);
                // Return a failed test result instead of propagating exception
                return new TestExecutionResult
                {
                    TestCaseId = testCase.Id,
                    TestCaseName = testCase.Name,
                    Success = false,
                    ExecutionTime = TimeSpan.Zero,
                    Response = $"Test execution failed: {ex.Message}",
                    AssertionResults = new List<AssertionResult>(),
                    Metrics = new Dictionary<string, object>
                    {
                        ["Error"] = ex.Message,
                        ["ExceptionType"] = ex.GetType().Name
                    }
                };
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(executionTasks);
        
        _logger.LogInformation("Parallel execution completed: {PassedTests}/{TotalTests} tests passed", 
            results.Count(r => r.Success), results.Length);

        return results.ToList();
    }

    /// <inheritdoc />
    public async Task<List<TestExecutionResult>> ExecuteTestsWithOptimalConcurrencyAsync(List<SelfGeneratedTestCase> testCases)
    {
        if (testCases == null || testCases.Count == 0)
        {
            return new List<TestExecutionResult>();
        }

        var optimalConcurrency = GetOptimalConcurrencyLevel();
        _logger.LogInformation("Using optimal concurrency level {OptimalConcurrency} for {TestCount} tests", 
            optimalConcurrency, testCases.Count);

        return await ExecuteTestsInParallelAsync(testCases, optimalConcurrency);
    }

    /// <inheritdoc />
    public async Task<List<TestExecutionResult>> ExecuteTestsInBatchesAsync(
        List<SelfGeneratedTestCase> testCases, 
        int batchSize = 10)
    {
        if (testCases == null || testCases.Count == 0)
        {
            return new List<TestExecutionResult>();
        }

        _logger.LogInformation("Executing {TestCount} tests in batches of {BatchSize}", 
            testCases.Count, batchSize);

        var allResults = new List<TestExecutionResult>();
        var batches = testCases
            .Select((test, index) => new { Test = test, Index = index })
            .GroupBy(x => x.Index / batchSize)
            .Select(g => g.Select(x => x.Test).ToList())
            .ToList();

        var batchNumber = 1;
        foreach (var batch in batches)
        {
            _logger.LogDebug("Executing batch {BatchNumber}/{TotalBatches} with {BatchTestCount} tests", 
                batchNumber, batches.Count, batch.Count);

            var batchResults = await ExecuteTestsWithOptimalConcurrencyAsync(batch);
            allResults.AddRange(batchResults);

            _logger.LogDebug("Batch {BatchNumber} completed: {PassedTests}/{TotalTests} tests passed", 
                batchNumber, batchResults.Count(r => r.Success), batch.Count);
            
            batchNumber++;

            // Small delay between batches to allow for resource cleanup
            if (batchNumber <= batches.Count)
            {
                await Task.Delay(100);
            }
        }

        _logger.LogInformation("Batch execution completed: {PassedTests}/{TotalTests} tests passed across {BatchCount} batches", 
            allResults.Count(r => r.Success), allResults.Count, batches.Count);

        return allResults;
    }

    /// <inheritdoc />
    public int GetOptimalConcurrencyLevel()
    {
        // Base concurrency on processor count and available memory
        var processorCount = Environment.ProcessorCount;
        var availableMemoryMb = GC.GetTotalMemory(false) / (1024 * 1024);

        // Conservative approach: use 75% of processor count with memory considerations
        var baseConcurrency = Math.Max(1, (int)(processorCount * 0.75));

        // Adjust based on available memory (each concurrent test might use ~10MB)
        var memoryConcurrency = Math.Max(1, (int)(availableMemoryMb / 10));

        // Use the minimum of the two to avoid resource exhaustion
        var optimalConcurrency = Math.Min(baseConcurrency, memoryConcurrency);

        // Cap at reasonable maximum for HTTP client efficiency
        var maxRecommendedConcurrency = 10;
        optimalConcurrency = Math.Min(optimalConcurrency, maxRecommendedConcurrency);

        _logger.LogDebug("Calculated optimal concurrency: {OptimalConcurrency} " +
            "(Processors: {ProcessorCount}, Base: {BaseConcurrency}, Memory: {MemoryConcurrency})",
            optimalConcurrency, processorCount, baseConcurrency, memoryConcurrency);

        return optimalConcurrency;
    }

    /// <inheritdoc />
    public ParallelExecutionAnalysis AnalyzeParallelPerformance(
        List<TestExecutionResult> testResults, 
        int concurrencyLevel)
    {
        if (testResults == null || testResults.Count == 0)
        {
            _logger.LogWarning("No test results provided for parallel performance analysis");
            return new ParallelExecutionAnalysis
            {
                ConcurrencyLevel = concurrencyLevel,
                Recommendations = new List<string> { "No test results available for analysis" }
            };
        }

        _logger.LogDebug("Analyzing parallel performance for {TestCount} tests with concurrency {ConcurrencyLevel}", 
            testResults.Count, concurrencyLevel);

        // For parallel execution analysis, estimate actual parallel execution time
        // This considers how tests would be distributed across concurrent threads
        var avgTime = testResults.Average(r => r.ExecutionTime.TotalMilliseconds);
        var estimatedSerialTime = TimeSpan.FromMilliseconds(testResults.Sum(r => r.ExecutionTime.TotalMilliseconds));
        
        // Estimate parallel execution time based on concurrency level
        // Sort tests by execution time (longest first) to simulate optimal scheduling
        var sortedTimes = testResults.OrderByDescending(r => r.ExecutionTime.TotalMilliseconds).ToList();
        var parallelBuckets = new double[Math.Min(concurrencyLevel, testResults.Count)];
        
        // Distribute tests across parallel buckets (greedy scheduling)
        foreach (var test in sortedTimes)
        {
            var minBucketIndex = 0;
            for (int i = 1; i < parallelBuckets.Length; i++)
            {
                if (parallelBuckets[i] < parallelBuckets[minBucketIndex])
                    minBucketIndex = i;
            }
            parallelBuckets[minBucketIndex] += test.ExecutionTime.TotalMilliseconds;
        }
        
        var estimatedParallelTime = TimeSpan.FromMilliseconds(parallelBuckets.Max());

        // Speedup ratio = time if run serially / time when run in parallel
        var speedupRatio = estimatedParallelTime.TotalMilliseconds > 0 
            ? estimatedSerialTime.TotalMilliseconds / estimatedParallelTime.TotalMilliseconds 
            : 0;
        var theoreticalMaxSpeedup = Math.Min(concurrencyLevel, testResults.Count);
        var efficiency = speedupRatio / theoreticalMaxSpeedup;

        var analysis = new ParallelExecutionAnalysis
        {
            ConcurrencyLevel = concurrencyLevel,
            TotalExecutionTime = estimatedParallelTime,
            AverageExecutionTime = TimeSpan.FromMilliseconds(avgTime),
            EstimatedSerialTime = estimatedSerialTime,
            SpeedupRatio = speedupRatio,
            ParallelEfficiency = efficiency,
            RecommendedConcurrency = CalculateRecommendedConcurrency(testResults, concurrencyLevel, efficiency)
        };

        // Resource utilization analysis
        analysis.ResourceUtilization["CPU"] = Math.Min(1.0, concurrencyLevel / (double)Environment.ProcessorCount);
        analysis.ResourceUtilization["Memory"] = CalculateMemoryUtilization(testResults);
        analysis.ResourceUtilization["Network"] = CalculateNetworkUtilization(testResults, concurrencyLevel);

        // Thread coordination statistics
        analysis.ThreadCoordinationStats["AverageWaitTime"] = CalculateAverageWaitTime(testResults);
        analysis.ThreadCoordinationStats["ContentionRatio"] = CalculateContentionRatio(testResults, concurrencyLevel);

        // Generate recommendations
        analysis.Recommendations = GeneratePerformanceRecommendations(analysis, testResults);

        _logger.LogInformation("Parallel performance analysis completed: " +
            "Speedup {SpeedupRatio:F2}x, Efficiency {Efficiency:F1}%, Recommended concurrency {RecommendedConcurrency}",
            analysis.SpeedupRatio, analysis.ParallelEfficiency * 100, analysis.RecommendedConcurrency);

        return analysis;
    }

    #region Private Helper Methods

    private int CalculateRecommendedConcurrency(List<TestExecutionResult> testResults, int currentConcurrency, double efficiency)
    {
        var averageExecutionTime = testResults.Average(r => r.ExecutionTime.TotalMilliseconds);
        
        // If efficiency is high (>0.8), we can potentially increase concurrency
        if (efficiency > 0.8 && currentConcurrency < GetOptimalConcurrencyLevel())
        {
            return Math.Min(currentConcurrency + 2, GetOptimalConcurrencyLevel());
        }
        
        // If efficiency is low (<0.5), reduce concurrency
        if (efficiency < 0.5 && currentConcurrency > 2)
        {
            return Math.Max(2, currentConcurrency - 1);
        }

        return currentConcurrency; // Current level is optimal
    }

    private double CalculateMemoryUtilization(List<TestExecutionResult> testResults)
    {
        // Estimate memory usage based on response sizes and concurrent tests
        var responsesWithData = testResults
            .Where(r => r.Response is string responseStr && !string.IsNullOrEmpty(responseStr))
            .ToList();
            
        if (!responsesWithData.Any())
        {
            return 0.1; // Default low utilization if no response data
        }
        
        var avgResponseSize = responsesWithData.Average(r => ((string)r.Response!).Length);
        
        var totalMemory = GC.GetTotalMemory(false);
        var estimatedTestMemory = avgResponseSize * testResults.Count * 2; // Factor for processing overhead
        
        return Math.Min(1.0, estimatedTestMemory / totalMemory);
    }

    private double CalculateNetworkUtilization(List<TestExecutionResult> testResults, int concurrencyLevel)
    {
        // Simple heuristic based on average response time and concurrency
        var avgResponseTime = testResults.Average(r => r.ExecutionTime.TotalMilliseconds);
        
        // Lower response times with higher concurrency suggest good network utilization
        var utilization = Math.Min(1.0, concurrencyLevel * 0.1 + (1000.0 - avgResponseTime) / 1000.0);
        return Math.Max(0.0, utilization);
    }

    private double CalculateAverageWaitTime(List<TestExecutionResult> testResults)
    {
        // Estimate wait time based on execution time variance
        var executionTimes = testResults.Select(r => r.ExecutionTime.TotalMilliseconds).ToArray();
        var variance = CalculateVariance(executionTimes);
        
        // Higher variance suggests more waiting/contention
        return Math.Sqrt(variance);
    }

    private double CalculateContentionRatio(List<TestExecutionResult> testResults, int concurrencyLevel)
    {
        // Estimate contention based on execution time distribution
        var executionTimes = testResults.Select(r => r.ExecutionTime.TotalMilliseconds).ToArray();
        var maxTime = executionTimes.Max();
        var minTime = executionTimes.Min();
        
        // Higher ratio suggests more contention
        return maxTime > 0 ? (maxTime - minTime) / maxTime : 0;
    }

    private double CalculateVariance(double[] values)
    {
        if (values.Length == 0) return 0;
        
        var mean = values.Average();
        return values.Select(x => Math.Pow(x - mean, 2)).Average();
    }

    private List<string> GeneratePerformanceRecommendations(
        ParallelExecutionAnalysis analysis, 
        List<TestExecutionResult> testResults)
    {
        var recommendations = new List<string>();

        // Efficiency recommendations
        if (analysis.ParallelEfficiency < 0.5)
        {
            recommendations.Add($"Low parallel efficiency ({analysis.ParallelEfficiency:P0}). " +
                $"Consider reducing concurrency from {analysis.ConcurrencyLevel} to {analysis.RecommendedConcurrency}.");
        }
        else if (analysis.ParallelEfficiency > 0.8 && analysis.ConcurrencyLevel < GetOptimalConcurrencyLevel())
        {
            recommendations.Add($"High parallel efficiency ({analysis.ParallelEfficiency:P0}). " +
                $"Consider increasing concurrency to {analysis.RecommendedConcurrency} for better performance.");
        }

        // Speedup recommendations
        if (analysis.SpeedupRatio < 2.0 && analysis.ConcurrencyLevel >= 4)
        {
            recommendations.Add($"Low speedup ratio ({analysis.SpeedupRatio:F1}x). " +
                "Tests may be I/O bound. Consider optimizing test design or API performance.");
        }

        // Resource utilization recommendations
        if (analysis.ResourceUtilization.TryGetValue("Memory", out var memoryUtilization) && memoryUtilization > 0.8)
        {
            recommendations.Add("High memory utilization detected. Consider using batch execution for large test suites.");
        }

        // Test execution time recommendations
        var longRunningTests = testResults.Where(r => r.ExecutionTime.TotalSeconds > 10).ToList();
        if (longRunningTests.Count > 0)
        {
            recommendations.Add($"{longRunningTests.Count} tests took longer than 10 seconds. " +
                "Consider optimizing these tests or increasing timeout values.");
        }

        // Failure rate impact on parallelism
        var failureRate = testResults.Count(r => !r.Success) / (double)testResults.Count;
        if (failureRate > 0.2)
        {
            recommendations.Add($"High failure rate ({failureRate:P0}) detected. " +
                "Parallel execution may be masking systematic issues. Consider serial debugging.");
        }

        return recommendations;
    }

    #endregion
}