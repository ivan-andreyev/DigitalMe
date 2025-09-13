using System.Collections.Generic;
using System.Threading.Tasks;

namespace DigitalMe.Services.Learning.Testing.ResultsAnalysis;

/// <summary>
/// Interface for analyzing test results and capabilities validation
/// Follows Interface Segregation Principle with focused methods for results analysis
/// </summary>
public interface IResultsAnalyzer
{
    /// <summary>
    /// Validate learned capabilities by running comprehensive tests and analyzing results
    /// </summary>
    /// <param name="apiName">Name of the API being validated</param>
    /// <param name="capability">The capability to validate</param>
    /// <returns>Comprehensive validation result with confidence scoring</returns>
    Task<CapabilityValidationResult> ValidateLearnedCapabilityAsync(string apiName, LearnedCapability capability);

    /// <summary>
    /// Generate performance benchmarks for new skills based on test execution results
    /// </summary>
    /// <param name="skillName">Name of the skill to benchmark</param>
    /// <param name="testResults">Test execution results to analyze</param>
    /// <returns>Performance benchmark with metrics and recommendations</returns>
    Task<PerformanceBenchmarkResult> BenchmarkNewSkillAsync(string skillName, List<TestExecutionResult> testResults);

    /// <summary>
    /// Analyze test failures to identify patterns and suggest improvements
    /// </summary>
    /// <param name="failedTests">Collection of failed test results</param>
    /// <returns>Analysis with failure patterns, suggestions, and health score</returns>
    Task<TestAnalysisResult> AnalyzeTestFailuresAsync(List<TestExecutionResult> failedTests);

    /// <summary>
    /// Calculate confidence score for a test suite result
    /// </summary>
    /// <param name="suiteResult">Test suite result to analyze</param>
    /// <returns>Confidence score between 0 and 1</returns>
    double CalculateConfidenceScore(TestSuiteResult suiteResult);

    /// <summary>
    /// Collect detailed performance metrics from test execution results
    /// </summary>
    /// <param name="testResults">Test results to analyze</param>
    /// <returns>Dictionary of detailed metrics</returns>
    Dictionary<string, double> CollectDetailedMetrics(List<TestExecutionResult> testResults);
}