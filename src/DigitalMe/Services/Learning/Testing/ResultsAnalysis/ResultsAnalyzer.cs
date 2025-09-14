using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DigitalMe.Services.Learning.Testing.Statistics;
using DigitalMe.Services.Learning.Testing.TestExecution;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Learning.Testing.ResultsAnalysis;

/// <summary>
/// Implementation of results analysis for test execution results and capability validation
/// Focuses on Single Responsibility Principle - analyzing test results and providing insights
/// Extracted from SelfTestingFramework God Class as part of Clean Architecture refactoring
/// </summary>
public class ResultsAnalyzer : IResultsAnalyzer
{
    private readonly ILogger<ResultsAnalyzer> _logger;
    private readonly ITestExecutor _testExecutor;
    private readonly IStatisticalAnalyzer _statisticalAnalyzer;

    public ResultsAnalyzer(ILogger<ResultsAnalyzer> logger, ITestExecutor testExecutor, IStatisticalAnalyzer statisticalAnalyzer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _testExecutor = testExecutor ?? throw new ArgumentNullException(nameof(testExecutor));
        _statisticalAnalyzer = statisticalAnalyzer ?? throw new ArgumentNullException(nameof(statisticalAnalyzer));
    }

    /// <inheritdoc />
    public async Task<CapabilityValidationResult> ValidateLearnedCapabilityAsync(string apiName, LearnedCapability capability)
    {
        try
        {
            _logger.LogInformation("Validating learned capability: {CapabilityName}", capability.Name);

            var result = new CapabilityValidationResult
            {
                CapabilityName = capability.Name
            };

            // Execute validation tests using test executor
            var suiteResult = await _testExecutor.ExecuteTestSuiteAsync(capability.ValidationTests);
            result.ValidationResults = suiteResult.TestResults;

            // Calculate confidence score based on test results
            result.ConfidenceScore = CalculateConfidenceScore(suiteResult);
            result.IsValid = result.ConfidenceScore >= 0.8; // 80% threshold

            // Analyze strengths and weaknesses
            AnalyzeCapabilityResults(result, suiteResult);

            // Determine new status
            result.NewStatus = DetermineCapabilityStatus(result);

            _logger.LogInformation("Capability validation completed for {CapabilityName}: Valid={IsValid}, Confidence={ConfidenceScore:F2}",
                capability.Name, result.IsValid, result.ConfidenceScore);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating capability: {CapabilityName}", capability.Name);
            return new CapabilityValidationResult
            {
                CapabilityName = capability.Name,
                IsValid = false,
                ConfidenceScore = 0,
                NewStatus = CapabilityStatus.Failed
            };
        }
    }

    /// <inheritdoc />
    public async Task<PerformanceBenchmarkResult> BenchmarkNewSkillAsync(string skillName, List<TestExecutionResult> testResults)
    {
        try
        {
            _logger.LogInformation("Benchmarking skill performance: {SkillName}", skillName);

            if (!testResults.Any())
            {
                return new PerformanceBenchmarkResult
                {
                    SkillName = skillName,
                    Grade = PerformanceGrade.F
                };
            }

            var successfulTests = testResults.Where(r => r.Success).ToList();
            var executionTimes = testResults.Select(r => r.ExecutionTime).ToList();

            var benchmark = new PerformanceBenchmarkResult
            {
                SkillName = skillName,
                AverageExecutionTime = TimeSpan.FromMilliseconds(executionTimes.Average(t => t.TotalMilliseconds)),
                MinExecutionTime = executionTimes.Min(),
                MaxExecutionTime = executionTimes.Max(),
                SuccessRate = testResults.Count > 0 ? (double)successfulTests.Count / testResults.Count : 0,
                TotalOperations = testResults.Count
            };

            // Collect additional performance metrics
            benchmark.PerformanceMetrics = CollectDetailedMetrics(testResults);

            // Assign performance grade
            benchmark.Grade = AssignPerformanceGrade(benchmark.SuccessRate);

            // Generate performance recommendations
            benchmark.PerformanceRecommendations = GeneratePerformanceRecommendations(benchmark);

            _logger.LogInformation("Performance benchmark completed for {SkillName}: Grade={Grade}, Success Rate={SuccessRate:F2}%, Avg Time={AvgTime}ms",
                skillName, benchmark.Grade, benchmark.SuccessRate * 100, benchmark.AverageExecutionTime.TotalMilliseconds);

            return benchmark;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error benchmarking skill: {SkillName}", skillName);
            return new PerformanceBenchmarkResult
            {
                SkillName = skillName,
                Grade = PerformanceGrade.F
            };
        }
    }

    /// <inheritdoc />
    public async Task<TestAnalysisResult> AnalyzeTestFailuresAsync(List<TestExecutionResult> failedTests)
    {
        try
        {
            _logger.LogInformation("Analyzing {FailedTestCount} failed tests", failedTests.Count);

            var analysis = new TestAnalysisResult
            {
                TotalFailedTests = failedTests.Count
            };

            // Categorize failures
            analysis.FailureCategories = CategorizeFailures(failedTests);

            // Identify common patterns
            analysis.CommonPatterns = await IdentifyCommonFailurePatternsAsync(failedTests);

            // Generate improvement suggestions
            analysis.Suggestions = GenerateImprovementSuggestions(analysis);

            // Calculate overall health score
            analysis.OverallHealthScore = CalculateHealthScore(analysis);

            // Identify critical issues
            analysis.CriticalIssues = IdentifyCriticalIssues(analysis);

            _logger.LogInformation("Test failure analysis completed: {PatternCount} patterns identified, Health Score: {HealthScore:F2}",
                analysis.CommonPatterns.Count, analysis.OverallHealthScore);

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing test failures");
            return new TestAnalysisResult
            {
                TotalFailedTests = failedTests.Count,
                OverallHealthScore = 0
            };
        }
    }

    /// <inheritdoc />
    public double CalculateConfidenceScore(TestSuiteResult suiteResult)
    {
        // Delegate to statistical analyzer for confidence calculations
        var executionTimes = suiteResult.TestResults.Select(r => r.ExecutionTime.TotalMilliseconds);
        return _statisticalAnalyzer.CalculateConfidenceScore(suiteResult.SuccessRate, suiteResult.TotalTests, executionTimes);
    }

    /// <inheritdoc />
    public Dictionary<string, double> CollectDetailedMetrics(List<TestExecutionResult> testResults)
    {
        if (!testResults.Any())
        {
            return new Dictionary<string, double>();
        }

        // Delegate to statistical analyzer for metric calculations
        var executionTimes = testResults.Select(r => r.ExecutionTime.TotalMilliseconds);
        var successCount = testResults.Count(r => r.Success);
        return _statisticalAnalyzer.CalculatePerformanceMetrics(executionTimes, successCount, testResults.Count);
    }

    #region Private Helper Methods

    /// <summary>
    /// Analyzes capability validation results to identify strengths, weaknesses, and improvement suggestions
    /// </summary>
    /// <param name="result">The validation result to populate with analysis</param>
    /// <param name="suiteResult">The test suite result to analyze</param>
    private void AnalyzeCapabilityResults(CapabilityValidationResult result, TestSuiteResult suiteResult)
    {
        var successfulTests = suiteResult.TestResults.Where(r => r.Success).ToList();
        var failedTests = suiteResult.TestResults.Where(r => !r.Success).ToList();

        // Identify strengths
        if (successfulTests.Any())
        {
            result.Strengths.Add($"{successfulTests.Count} test cases passed successfully");
            
            var avgSuccessTime = successfulTests.Average(t => t.ExecutionTime.TotalMilliseconds);
            if (avgSuccessTime < 1000)
            {
                result.Strengths.Add("Fast execution time for successful operations");
            }
        }

        // Identify weaknesses
        if (failedTests.Any())
        {
            result.Weaknesses.Add($"{failedTests.Count} test cases failed");
            
            var commonErrors = failedTests
                .Where(t => !string.IsNullOrEmpty(t.ErrorMessage))
                .GroupBy(t => t.ErrorMessage)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();
            
            if (commonErrors != null)
            {
                result.Weaknesses.Add($"Common error pattern: {commonErrors.Key}");
            }
        }

        // Generate improvement suggestions
        if (result.ConfidenceScore < 0.8)
        {
            result.ImprovementSuggestions.Add("Increase test coverage to improve confidence");
        }
        
        if (failedTests.Any(t => t.ErrorMessage?.Contains("timeout") == true))
        {
            result.ImprovementSuggestions.Add("Consider increasing timeout values for slow operations");
        }
    }

    /// <summary>
    /// Determines the appropriate capability status based on confidence score
    /// </summary>
    /// <param name="result">The validation result containing confidence score</param>
    /// <returns>The determined capability status</returns>
    private CapabilityStatus DetermineCapabilityStatus(CapabilityValidationResult result)
    {
        if (result.ConfidenceScore >= 0.9) return CapabilityStatus.Validated;
        if (result.ConfidenceScore >= 0.7) return CapabilityStatus.Learned;
        if (result.ConfidenceScore >= 0.5) return CapabilityStatus.Learning;
        return CapabilityStatus.Failed;
    }


    /// <summary>
    /// Assigns a performance grade based on success rate percentage
    /// </summary>
    /// <param name="successRate">Success rate as a decimal (0.0 to 1.0)</param>
    /// <returns>The assigned performance grade</returns>
    private PerformanceGrade AssignPerformanceGrade(double successRate)
    {
        var percentage = successRate * 100;
        
        return percentage switch
        {
            >= 90 => PerformanceGrade.A,
            >= 80 => PerformanceGrade.B,
            >= 70 => PerformanceGrade.C,
            >= 60 => PerformanceGrade.D,
            _ => PerformanceGrade.F
        };
    }

    /// <summary>
    /// Generates performance improvement recommendations based on benchmark results
    /// </summary>
    /// <param name="benchmark">The performance benchmark result to analyze</param>
    /// <returns>List of performance improvement recommendations</returns>
    private List<string> GeneratePerformanceRecommendations(PerformanceBenchmarkResult benchmark)
    {
        var recommendations = new List<string>();

        if (benchmark.SuccessRate < 0.9)
        {
            recommendations.Add("Improve error handling to increase success rate");
        }

        if (benchmark.AverageExecutionTime.TotalSeconds > 5)
        {
            recommendations.Add("Consider optimizing for faster execution times");
        }

        if (benchmark.MaxExecutionTime.TotalMilliseconds > benchmark.AverageExecutionTime.TotalMilliseconds * 3)
        {
            recommendations.Add("High execution time variance detected - investigate performance inconsistencies");
        }

        return recommendations;
    }

    /// <summary>
    /// Categorizes failed tests into failure types and counts occurrences
    /// </summary>
    /// <param name="failedTests">List of failed test execution results</param>
    /// <returns>Dictionary mapping failure categories to their occurrence counts</returns>
    private Dictionary<string, int> CategorizeFailures(List<TestExecutionResult> failedTests)
    {
        var categories = new Dictionary<string, int>();

        foreach (var test in failedTests)
        {
            var category = CategorizeFailure(test);
            categories[category] = categories.GetValueOrDefault(category, 0) + 1;
        }

        return categories;
    }

    /// <summary>
    /// Categorizes a single test failure based on error message and assertion results
    /// </summary>
    /// <param name="test">The failed test execution result</param>
    /// <returns>The failure category as a string</returns>
    private string CategorizeFailure(TestExecutionResult test)
    {
        if (test.ErrorMessage?.Contains("timeout") == true)
            return "Timeout";
        if (test.ErrorMessage?.Contains("401") == true)
            return "Authentication";
        if (test.ErrorMessage?.Contains("404") == true)
            return "Not Found";
        if (test.ErrorMessage?.Contains("500") == true)
            return "Server Error";
        if (test.AssertionResults.Any(a => !a.Passed))
            return "Assertion Failure";
        
        return "Unknown";
    }

    /// <summary>
    /// Identifies common failure patterns from a collection of failed tests
    /// </summary>
    /// <param name="failedTests">List of failed test execution results to analyze</param>
    /// <returns>List of identified common failure patterns</returns>
    private async Task<List<CommonFailurePattern>> IdentifyCommonFailurePatternsAsync(List<TestExecutionResult> failedTests)
    {
        var patterns = new List<CommonFailurePattern>();

        // Group by error message
        var errorGroups = failedTests
            .Where(t => !string.IsNullOrEmpty(t.ErrorMessage))
            .GroupBy(t => t.ErrorMessage)
            .Where(g => g.Count() > 1);

        foreach (var group in errorGroups)
        {
            patterns.Add(new CommonFailurePattern
            {
                Pattern = "Error Message",
                Description = group.Key!,
                Frequency = group.Count(),
                AffectedTests = group.Select(t => t.TestCaseName).ToList(),
                Severity = DetermineFailureSeverity(group.Count(), failedTests.Count)
            });
        }

        // Group by assertion failures
        var assertionGroups = failedTests
            .SelectMany(t => t.AssertionResults.Where(a => !a.Passed))
            .GroupBy(a => a.AssertionName)
            .Where(g => g.Count() > 1);

        foreach (var group in assertionGroups)
        {
            patterns.Add(new CommonFailurePattern
            {
                Pattern = "Assertion Failure",
                Description = $"'{group.Key}' assertion consistently failing",
                Frequency = group.Count(),
                Severity = DetermineFailureSeverity(group.Count(), failedTests.Count)
            });
        }

        return patterns;
    }

    /// <summary>
    /// Determines the severity of a failure pattern based on its frequency
    /// </summary>
    /// <param name="frequency">Number of occurrences of the pattern</param>
    /// <param name="totalFailures">Total number of failed tests</param>
    /// <returns>The determined failure severity level</returns>
    private FailureSeverity DetermineFailureSeverity(int frequency, int totalFailures)
    {
        var percentage = (double)frequency / totalFailures;
        
        return percentage switch
        {
            >= 0.8 => FailureSeverity.Critical,
            >= 0.5 => FailureSeverity.High,
            >= 0.3 => FailureSeverity.Medium,
            _ => FailureSeverity.Low
        };
    }

    /// <summary>
    /// Generates improvement suggestions based on test failure analysis results
    /// </summary>
    /// <param name="analysis">The test analysis result containing failure patterns and categories</param>
    /// <returns>List of prioritized improvement suggestions</returns>
    private List<ImprovementSuggestion> GenerateImprovementSuggestions(TestAnalysisResult analysis)
    {
        var suggestions = new List<ImprovementSuggestion>();

        // Suggestions based on failure categories
        foreach (var category in analysis.FailureCategories.Where(kv => kv.Value > 2))
        {
            suggestions.Add(new ImprovementSuggestion
            {
                Title = $"Address {category.Key} Issues",
                Description = $"Multiple tests ({category.Value}) are failing due to {category.Key.ToLowerInvariant()} issues",
                Priority = SuggestionPriority.High,
                ActionSteps = new List<string>
                {
                    $"Review and fix {category.Key.ToLowerInvariant()} related problems",
                    "Update test expectations if necessary",
                    "Add more robust error handling"
                }
            });
        }

        // Suggestions based on common patterns
        foreach (var pattern in analysis.CommonPatterns.Where(p => p.Severity >= FailureSeverity.High))
        {
            suggestions.Add(new ImprovementSuggestion
            {
                Title = $"Fix {pattern.Pattern} Pattern",
                Description = pattern.Description,
                Priority = pattern.Severity == FailureSeverity.Critical ? SuggestionPriority.Urgent : SuggestionPriority.High,
                ActionSteps = new List<string>
                {
                    "Investigate root cause of pattern",
                    "Implement systematic fix",
                    "Re-run affected tests to validate fix"
                }
            });
        }

        return suggestions;
    }

    /// <summary>
    /// Calculates an overall health score based on test analysis results and failure patterns
    /// </summary>
    /// <param name="analysis">The test analysis result containing failure patterns</param>
    /// <returns>Health score as percentage (0-100)</returns>
    private double CalculateHealthScore(TestAnalysisResult analysis)
    {
        if (analysis.TotalFailedTests == 0) return 100;

        var baseScore = 100.0;
        
        // Reduce score based on failure severity
        foreach (var pattern in analysis.CommonPatterns)
        {
            var reduction = pattern.Severity switch
            {
                FailureSeverity.Critical => 30,
                FailureSeverity.High => 20,
                FailureSeverity.Medium => 10,
                FailureSeverity.Low => 5,
                _ => 0
            };
            
            baseScore -= reduction * (pattern.Frequency / (double)analysis.TotalFailedTests);
        }

        return Math.Max(0, baseScore);
    }

    /// <summary>
    /// Identifies critical issues from test analysis results that require immediate attention
    /// </summary>
    /// <param name="analysis">The test analysis result to examine for critical issues</param>
    /// <returns>List of critical issues descriptions</returns>
    private List<string> IdentifyCriticalIssues(TestAnalysisResult analysis)
    {
        var issues = new List<string>();

        var criticalPatterns = analysis.CommonPatterns.Where(p => p.Severity == FailureSeverity.Critical);
        foreach (var pattern in criticalPatterns)
        {
            issues.Add($"Critical {pattern.Pattern}: {pattern.Description} (affects {pattern.Frequency} tests)");
        }

        if (analysis.OverallHealthScore < 50)
        {
            issues.Add($"Overall system health is critically low: {analysis.OverallHealthScore:F1}%");
        }

        return issues;
    }

    #endregion
}