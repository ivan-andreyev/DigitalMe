using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace DigitalMe.Services.Learning.Testing.TestExecution;

/// <summary>
/// Interface for single test execution - Breaks circular dependency
/// Focused only on individual test case execution without parallelism concerns
/// </summary>
public interface ISingleTestExecutor
{
    /// <summary>
    /// Execute a single test case and validate results
    /// Core responsibility: HTTP request execution and response validation
    /// No parallel execution concerns - pure single test execution
    /// </summary>
    /// <param name="testCase">Test case to execute</param>
    /// <returns>Execution result with assertions, metrics, and timing</returns>
    Task<TestExecutionResult> ExecuteTestCaseAsync(SelfGeneratedTestCase testCase);
}
