using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using DigitalMe.Services.Learning.Testing;
using DigitalMe.Services.Learning.Testing.ResultsAnalysis;
using DigitalMe.Services.Learning.Testing.TestExecution;
using DigitalMe.Services.Learning.Testing.TestGeneration;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Learning;

/// <summary>
/// True orchestrator implementation for self-testing framework - Phase 1 Advanced Cognitive Capabilities
/// Coordinates focused testing services without implementing their contracts directly
/// Following true orchestrator pattern and SOLID principles
/// Acts as a facade for coordinating ITestOrchestrator, ICapabilityValidator, and ITestAnalyzer
/// </summary>
public class SelfTestingFramework : ISelfTestingFramework
{
    private readonly ILogger<SelfTestingFramework> _logger;
    private readonly ITestOrchestrator _testOrchestrator;
    private readonly ICapabilityValidator _capabilityValidator;
    private readonly ITestAnalyzer _testAnalyzer;

    public SelfTestingFramework(
        ILogger<SelfTestingFramework> logger,
        ITestOrchestrator testOrchestrator,
        ICapabilityValidator capabilityValidator,
        ITestAnalyzer testAnalyzer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _testOrchestrator = testOrchestrator ?? throw new ArgumentNullException(nameof(testOrchestrator));
        _capabilityValidator = capabilityValidator ?? throw new ArgumentNullException(nameof(capabilityValidator));
        _testAnalyzer = testAnalyzer ?? throw new ArgumentNullException(nameof(testAnalyzer));
    }

    /// <inheritdoc />
    public async Task<List<SelfGeneratedTestCase>> GenerateTestCasesAsync(DocumentationParseResult apiDocumentation)
    {
        _logger.LogInformation("Orchestrating test case generation for API: {ApiName}", apiDocumentation?.ApiName ?? "Unknown");

        if (apiDocumentation == null)
        {
            _logger.LogWarning("Cannot generate test cases: apiDocumentation is null");
            return new List<SelfGeneratedTestCase>();
        }

        return await _testOrchestrator.GenerateTestCasesAsync(apiDocumentation);
    }

    /// <inheritdoc />
    public async Task<TestExecutionResult> ExecuteTestCaseAsync(SelfGeneratedTestCase testCase)
    {
        _logger.LogInformation("Orchestrating test case execution: {TestCaseId}", testCase?.Id ?? "Unknown");

        if (testCase == null)
        {
            _logger.LogWarning("Cannot execute test case: testCase is null");
            return new TestExecutionResult
            {
                Success = false,
                ErrorMessage = "Test case is null",
                TestCaseName = "Unknown"
            };
        }

        return await _testOrchestrator.ExecuteTestCaseAsync(testCase);
    }

    /// <inheritdoc />
    public async Task<TestSuiteResult> ExecuteTestSuiteAsync(List<SelfGeneratedTestCase> testCases)
    {
        _logger.LogInformation("Orchestrating test suite execution with {TestCount} test cases", testCases?.Count ?? 0);

        if (testCases == null)
        {
            _logger.LogWarning("Cannot execute test suite: testCases list is null");
            return new TestSuiteResult
            {
                Status = TestSuiteStatus.Failed,
                SuiteName = "Unknown"
            };
        }

        return await _testOrchestrator.ExecuteTestSuiteAsync(testCases);
    }

    /// <inheritdoc />
    public async Task<CapabilityValidationResult> ValidateLearnedCapabilityAsync(string apiName, LearnedCapability capability)
    {
        _logger.LogInformation("Orchestrating capability validation: {CapabilityName} for API: {ApiName}", capability?.Name ?? "Unknown", apiName);

        if (capability == null)
        {
            _logger.LogWarning("Cannot validate capability: capability is null for API {ApiName}", apiName);
            return new CapabilityValidationResult
            {
                CapabilityName = apiName,
                IsValid = false,
                ConfidenceScore = 0.0,
                ImprovementSuggestions = new List<string> { "Capability is null" }
            };
        }

        return await _capabilityValidator.ValidateLearnedCapabilityAsync(apiName, capability);
    }

    /// <inheritdoc />
    public async Task<PerformanceBenchmarkResult> BenchmarkNewSkillAsync(string skillName, List<TestExecutionResult> testResults)
    {
        _logger.LogInformation("Orchestrating performance benchmarking for skill: {SkillName}", skillName);
        return await _capabilityValidator.BenchmarkNewSkillAsync(skillName, testResults);
    }

    /// <inheritdoc />
    public async Task<TestAnalysisResult> AnalyzeTestFailuresAsync(List<TestExecutionResult> failedTests)
    {
        _logger.LogInformation("Orchestrating test failure analysis for {FailedTestCount} failed tests", failedTests?.Count ?? 0);

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

        return await _testAnalyzer.AnalyzeTestFailuresAsync(failedTests);
    }

}
