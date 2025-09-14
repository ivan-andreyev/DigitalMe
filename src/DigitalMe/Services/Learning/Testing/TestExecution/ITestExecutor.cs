using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace DigitalMe.Services.Learning.Testing.TestExecution;

/// <summary>
/// Interface for test execution engine - Single Responsibility: Execute tests
/// Part of Clean Architecture refactoring from SelfTestingFramework God Class
/// Follows Interface Segregation Principle with focused test execution methods
/// </summary>
public interface ITestExecutor
{
    /// <summary>
    /// Execute a single test case and validate results
    /// Core responsibility: HTTP request execution and response validation
    /// </summary>
    /// <param name="testCase">Test case to execute</param>
    /// <returns>Execution result with assertions, metrics, and timing</returns>
    Task<TestExecutionResult> ExecuteTestCaseAsync(SelfGeneratedTestCase testCase);

    /// <summary>
    /// Execute multiple test cases with controlled parallelism
    /// Core responsibility: Manage concurrent test execution and aggregation
    /// </summary>
    /// <param name="testCases">Collection of test cases to execute</param>
    /// <returns>Comprehensive suite result with recommendations</returns>
    Task<TestSuiteResult> ExecuteTestSuiteAsync(List<SelfGeneratedTestCase> testCases);
}