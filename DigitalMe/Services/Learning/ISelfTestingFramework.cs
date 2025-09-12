using System.Collections.Generic;
using System.Threading.Tasks;

namespace DigitalMe.Services.Learning;

/// <summary>
/// Interface for self-testing framework - Phase 1 Advanced Cognitive Capabilities
/// Enables agent to automatically generate and execute tests for learned capabilities
/// </summary>
public interface ISelfTestingFramework
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

    /// <summary>
    /// Validate learned capabilities by running comprehensive tests
    /// </summary>
    Task<CapabilityValidationResult> ValidateLearnedCapabilityAsync(string apiName, LearnedCapability capability);

    /// <summary>
    /// Generate performance benchmarks for new skills
    /// </summary>
    Task<PerformanceBenchmarkResult> BenchmarkNewSkillAsync(string skillName, List<TestExecutionResult> testResults);

    /// <summary>
    /// Analyze test failures and suggest improvements
    /// </summary>
    Task<TestAnalysisResult> AnalyzeTestFailuresAsync(List<TestExecutionResult> failedTests);
}

/// <summary>
/// A self-generated test case for validating learned capabilities
/// </summary>
public class SelfGeneratedTestCase
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ApiName { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = "GET";
    public Dictionary<string, string> Headers { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public object? RequestBody { get; set; }
    public List<TestAssertion> Assertions { get; set; } = new();
    public TestPriority Priority { get; set; } = TestPriority.Medium;
    public TimeSpan ExpectedExecutionTime { get; set; } = TimeSpan.FromSeconds(5);
    public int RetryCount { get; set; } = 3;
    public Dictionary<string, object> TestData { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Assertion to validate test results
/// </summary>
public class TestAssertion
{
    public string Name { get; set; } = string.Empty;
    public AssertionType Type { get; set; }
    public string ExpectedValue { get; set; } = string.Empty;
    public string ActualValuePath { get; set; } = string.Empty; // JSON path or property path
    public ComparisonOperator Operator { get; set; } = ComparisonOperator.Equals;
    public bool IsCritical { get; set; } = true;
}

/// <summary>
/// Result of executing a single test case
/// </summary>
public class TestExecutionResult
{
    public string TestCaseId { get; set; } = string.Empty;
    public string TestCaseName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public List<AssertionResult> AssertionResults { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public Exception? Exception { get; set; }
    public object? Response { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = new();
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public int AttemptNumber { get; set; } = 1;
}

/// <summary>
/// Result of assertion validation
/// </summary>
public class AssertionResult
{
    public string AssertionName { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string? ExpectedValue { get; set; }
    public string? ActualValue { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsCritical { get; set; }
}

/// <summary>
/// Result of executing multiple test cases
/// </summary>
public class TestSuiteResult
{
    public string SuiteName { get; set; } = string.Empty;
    public List<TestExecutionResult> TestResults { get; set; } = new();
    public int TotalTests => TestResults.Count;
    public int PassedTests => TestResults.Count(r => r.Success);
    public int FailedTests => TestResults.Count(r => !r.Success);
    public double SuccessRate => TotalTests > 0 ? (double)PassedTests / TotalTests * 100 : 0;
    public TimeSpan TotalExecutionTime { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public TestSuiteStatus Status { get; set; }
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Represents a learned capability that can be validated
/// </summary>
public class LearnedCapability
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> RequiredSkills { get; set; } = new();
    public Dictionary<string, object> Configuration { get; set; } = new();
    public List<SelfGeneratedTestCase> ValidationTests { get; set; } = new();
    public DateTime LearnedAt { get; set; } = DateTime.UtcNow;
    public CapabilityStatus Status { get; set; } = CapabilityStatus.Learning;
}

/// <summary>
/// Result of capability validation
/// </summary>
public class CapabilityValidationResult
{
    public string CapabilityName { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public double ConfidenceScore { get; set; }
    public List<TestExecutionResult> ValidationResults { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
    public List<string> Strengths { get; set; } = new();
    public List<string> ImprovementSuggestions { get; set; } = new();
    public CapabilityStatus NewStatus { get; set; }
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Performance benchmark results for a skill
/// </summary>
public class PerformanceBenchmarkResult
{
    public string SkillName { get; set; } = string.Empty;
    public TimeSpan AverageExecutionTime { get; set; }
    public TimeSpan MinExecutionTime { get; set; }
    public TimeSpan MaxExecutionTime { get; set; }
    public double SuccessRate { get; set; }
    public int TotalOperations { get; set; }
    public Dictionary<string, double> PerformanceMetrics { get; set; } = new();
    public PerformanceGrade Grade { get; set; }
    public List<string> PerformanceRecommendations { get; set; } = new();
    public DateTime BenchmarkedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Analysis of test failures with suggestions
/// </summary>
public class TestAnalysisResult
{
    public int TotalFailedTests { get; set; }
    public Dictionary<string, int> FailureCategories { get; set; } = new();
    public List<CommonFailurePattern> CommonPatterns { get; set; } = new();
    public List<ImprovementSuggestion> Suggestions { get; set; } = new();
    public double OverallHealthScore { get; set; }
    public List<string> CriticalIssues { get; set; } = new();
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Common failure pattern identified in tests
/// </summary>
public class CommonFailurePattern
{
    public string Pattern { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Frequency { get; set; }
    public List<string> AffectedTests { get; set; } = new();
    public FailureSeverity Severity { get; set; }
}

/// <summary>
/// Suggestion for improving test results
/// </summary>
public class ImprovementSuggestion
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SuggestionPriority Priority { get; set; }
    public List<string> ActionSteps { get; set; } = new();
    public string? EstimatedImpact { get; set; }
}

#region Enums

/// <summary>
/// Priority level for test cases
/// </summary>
public enum TestPriority
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Type of assertion to perform
/// </summary>
public enum AssertionType
{
    StatusCode,
    ResponseTime,
    ResponseBody,
    ResponseHeader,
    JsonPath,
    Custom
}

/// <summary>
/// Comparison operator for assertions
/// </summary>
public enum ComparisonOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    LessThan,
    Contains,
    NotContains,
    StartsWith,
    EndsWith,
    Matches // For regex
}

/// <summary>
/// Status of test suite execution
/// </summary>
public enum TestSuiteStatus
{
    NotStarted,
    Running,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Status of learned capability
/// </summary>
public enum CapabilityStatus
{
    Learning,
    Learned,
    Validated,
    Deprecated,
    Failed
}

/// <summary>
/// Performance grade for skills
/// </summary>
public enum PerformanceGrade
{
    F, // 0-59%
    D, // 60-69%
    C, // 70-79%
    B, // 80-89%
    A  // 90-100%
}

/// <summary>
/// Severity of failure patterns
/// </summary>
public enum FailureSeverity
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Priority of improvement suggestions
/// </summary>
public enum SuggestionPriority
{
    Low,
    Medium,
    High,
    Urgent
}

#endregion