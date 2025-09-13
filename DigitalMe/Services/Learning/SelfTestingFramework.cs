using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using DigitalMe.Services.Learning.Testing;
using DigitalMe.Services.Learning.Testing.TestGeneration;
using DigitalMe.Services.Learning.Testing.TestExecution;
using DigitalMe.Services.Learning.Testing.ResultsAnalysis;

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
        return await _testOrchestrator.GenerateTestCasesAsync(apiDocumentation);
    }

    /// <inheritdoc />
    public async Task<TestExecutionResult> ExecuteTestCaseAsync(SelfGeneratedTestCase testCase)
    {
        _logger.LogInformation("Orchestrating test case execution: {TestCaseId}", testCase?.Id ?? "Unknown");
        return await _testOrchestrator.ExecuteTestCaseAsync(testCase);
    }

    /// <inheritdoc />
    public async Task<TestSuiteResult> ExecuteTestSuiteAsync(List<SelfGeneratedTestCase> testCases)
    {
        _logger.LogInformation("Orchestrating test suite execution with {TestCount} test cases", testCases?.Count ?? 0);
        return await _testOrchestrator.ExecuteTestSuiteAsync(testCases);
    }

    /// <inheritdoc />
    public async Task<CapabilityValidationResult> ValidateLearnedCapabilityAsync(string apiName, LearnedCapability capability)
    {
        _logger.LogInformation("Orchestrating capability validation: {CapabilityName} for API: {ApiName}", capability?.Name ?? "Unknown", apiName);
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
        return await _testAnalyzer.AnalyzeTestFailuresAsync(failedTests);
    }

}