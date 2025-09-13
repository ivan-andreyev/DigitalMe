using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalMe.Services.Learning;
using DigitalMe.Services.Learning.Testing.TestGeneration;
using DigitalMe.Services.Learning.Testing.TestExecution;

namespace DigitalMe.Services.Learning.Testing;

/// <summary>
/// Dedicated test orchestration service implementation
/// Focused on test generation and execution workflows
/// Following Single Responsibility Principle
/// </summary>
public class TestOrchestratorService : ITestOrchestrator
{
    private readonly ILogger<TestOrchestratorService> _logger;
    private readonly ITestCaseGenerator _testCaseGenerator;
    private readonly ITestExecutor _testExecutor;

    public TestOrchestratorService(
        ILogger<TestOrchestratorService> logger,
        ITestCaseGenerator testCaseGenerator,
        ITestExecutor testExecutor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _testCaseGenerator = testCaseGenerator ?? throw new ArgumentNullException(nameof(testCaseGenerator));
        _testExecutor = testExecutor ?? throw new ArgumentNullException(nameof(testExecutor));
    }

    /// <inheritdoc />
    public async Task<List<SelfGeneratedTestCase>> GenerateTestCasesAsync(DocumentationParseResult apiDocumentation)
    {
        _logger.LogDebug("Generating test cases for API: {ApiName}", apiDocumentation?.ApiName ?? "Unknown");
        return await _testCaseGenerator.GenerateTestCasesAsync(apiDocumentation);
    }

    /// <inheritdoc />
    public async Task<TestExecutionResult> ExecuteTestCaseAsync(SelfGeneratedTestCase testCase)
    {
        _logger.LogDebug("Executing test case: {TestCaseId}", testCase?.Id ?? "Unknown");
        return await _testExecutor.ExecuteTestCaseAsync(testCase);
    }

    /// <inheritdoc />
    public async Task<TestSuiteResult> ExecuteTestSuiteAsync(List<SelfGeneratedTestCase> testCases)
    {
        _logger.LogDebug("Executing test suite with {TestCount} test cases", testCases?.Count ?? 0);
        return await _testExecutor.ExecuteTestSuiteAsync(testCases);
    }
}