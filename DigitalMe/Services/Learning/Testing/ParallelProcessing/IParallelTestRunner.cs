using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DigitalMe.Services.Learning.Testing.ParallelProcessing;

/// <summary>
/// Interface for parallel test execution engine - Single Responsibility: Manage concurrent test execution
/// Part of Clean Architecture refactoring from SelfTestingFramework God Class
/// Follows Interface Segregation Principle with focused parallel execution methods (≤5 methods)
/// </summary>
public interface IParallelTestRunner
{
    /// <summary>
    /// Execute multiple test cases concurrently with controlled parallelism
    /// Core responsibility: Concurrent test execution with resource management
    /// </summary>
    /// <param name="testCases">Collection of test cases to execute in parallel</param>
    /// <param name="maxConcurrency">Maximum number of concurrent test executions (default: 5)</param>
    /// <returns>Collection of test execution results</returns>
    Task<List<TestExecutionResult>> ExecuteTestsInParallelAsync(
        List<SelfGeneratedTestCase> testCases, 
        int maxConcurrency = 5);

    /// <summary>
    /// Execute test cases with dynamic concurrency based on system resources
    /// Core responsibility: Adaptive parallelism based on available resources
    /// </summary>
    /// <param name="testCases">Collection of test cases to execute</param>
    /// <returns>Collection of test execution results with optimal concurrency</returns>
    Task<List<TestExecutionResult>> ExecuteTestsWithOptimalConcurrencyAsync(List<SelfGeneratedTestCase> testCases);

    /// <summary>
    /// Execute test cases in batches to manage memory and resource usage
    /// Core responsibility: Batch processing for large test collections
    /// </summary>
    /// <param name="testCases">Collection of test cases to execute</param>
    /// <param name="batchSize">Number of tests to execute per batch (default: 10)</param>
    /// <returns>Collection of test execution results from all batches</returns>
    Task<List<TestExecutionResult>> ExecuteTestsInBatchesAsync(
        List<SelfGeneratedTestCase> testCases, 
        int batchSize = 10);

    /// <summary>
    /// Get optimal concurrency level based on current system resources
    /// Core responsibility: Resource-based concurrency calculation
    /// </summary>
    /// <returns>Recommended concurrency level for current system</returns>
    int GetOptimalConcurrencyLevel();

    /// <summary>
    /// Monitor parallel execution performance and provide recommendations
    /// Core responsibility: Performance monitoring and optimization suggestions
    /// </summary>
    /// <param name="testResults">Results from parallel test execution</param>
    /// <param name="concurrencyLevel">Concurrency level used</param>
    /// <returns>Performance analysis and recommendations</returns>
    ParallelExecutionAnalysis AnalyzeParallelPerformance(
        List<TestExecutionResult> testResults, 
        int concurrencyLevel);
}