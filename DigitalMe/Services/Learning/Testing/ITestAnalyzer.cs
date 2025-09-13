using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalMe.Services.Learning;

namespace DigitalMe.Services.Learning.Testing;

/// <summary>
/// Test analysis interface - Phase 1 Advanced Cognitive Capabilities
/// Handles failure analysis and improvement suggestions
/// Following ISP: ≤5 methods focused on analysis concerns
/// </summary>
public interface ITestAnalyzer
{
    /// <summary>
    /// Analyze test failures and suggest improvements
    /// </summary>
    Task<TestAnalysisResult> AnalyzeTestFailuresAsync(List<TestExecutionResult> failedTests);
}