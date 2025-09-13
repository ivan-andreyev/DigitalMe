using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalMe.Services.Learning;

namespace DigitalMe.Services.Learning.Testing;

/// <summary>
/// Core test orchestration interface - Phase 1 Advanced Cognitive Capabilities
/// Handles test generation and basic execution workflows
/// Following ISP: ≤5 methods focused on orchestration concerns
/// </summary>
public interface ITestOrchestrator
{
    /// <summary>
    /// Generate test cases based on learned patterns and API documentation
    /// </summary>
    Task<List<SelfGeneratedTestCase>> GenerateTestCasesAsync(DocumentationParseResult apiDocumentation);

    /// <summary>
    /// Execute a single test case and validate results
    /// </summary>
    Task<TestExecutionResult> ExecuteTestCaseAsync(SelfGeneratedTestCase testCase);

    /// <summary>
    /// Execute multiple test cases and provide comprehensive results
    /// </summary>
    Task<TestSuiteResult> ExecuteTestSuiteAsync(List<SelfGeneratedTestCase> testCases);
}