using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using DigitalMe.Services.Learning.Testing.ResultsAnalysis;
using DigitalMe.Services.Learning.Testing.TestExecution;
using DigitalMe.Services.Learning.Testing.Statistics;
using DigitalMe.Services.Learning;

namespace DigitalMe.Tests.Unit.Services.Learning.Testing.ResultsAnalysis;

/// <summary>
/// Comprehensive unit tests for ResultsAnalyzer service
/// Tests extracted results analysis functionality following Clean Architecture principles
/// Validates all methods: capability validation, performance benchmarking, failure analysis
/// </summary>
public class ResultsAnalyzerTests
{
    private readonly Mock<ILogger<ResultsAnalyzer>> _mockLogger;
    private readonly Mock<ITestExecutor> _mockTestExecutor;
    private readonly Mock<IStatisticalAnalyzer> _mockStatisticalAnalyzer;
    private readonly ResultsAnalyzer _resultsAnalyzer;

    public ResultsAnalyzerTests()
    {
        _mockLogger = new Mock<ILogger<ResultsAnalyzer>>();
        _mockTestExecutor = new Mock<ITestExecutor>();
        _mockStatisticalAnalyzer = new Mock<IStatisticalAnalyzer>();
        
        // Setup common mock behaviors
        SetupStatisticalAnalyzerMocks();
        
        _resultsAnalyzer = new ResultsAnalyzer(_mockLogger.Object, _mockTestExecutor.Object, _mockStatisticalAnalyzer.Object);
    }

    private void SetupStatisticalAnalyzerMocks()
    {
        // Setup default confidence score calculation
        _mockStatisticalAnalyzer
            .Setup(x => x.CalculateConfidenceScore(It.IsAny<double>(), It.IsAny<int>(), It.IsAny<IEnumerable<double>>()))
            .Returns((double successRate, int totalTests, IEnumerable<double> executionTimes) => 
            {
                // Simple confidence calculation: base on success rate with sample size adjustment
                var baseConfidence = successRate / 100.0;
                var sampleSizeAdjustment = Math.Min(1.0, totalTests / 3.0); // Full confidence with 3+ tests
                var result = baseConfidence * sampleSizeAdjustment;
                
                // Ensure perfect success rate with sufficient tests gets high confidence
                if (successRate >= 100 && totalTests >= 3)
                {
                    result = Math.Max(result, 0.95);
                }
                
                return Math.Min(1.0, result);
            });

        // Setup performance metrics calculation
        _mockStatisticalAnalyzer
            .Setup(x => x.CalculatePerformanceMetrics(It.IsAny<IEnumerable<double>>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns((IEnumerable<double> executionTimes, int successCount, int totalCount) =>
            {
                var times = executionTimes.ToArray();
                if (times.Length == 0)
                {
                    return new Dictionary<string, double>();
                }

                return new Dictionary<string, double>
                {
                    { "AverageExecutionTimeMs", times.Average() },
                    { "MedianExecutionTimeMs", times.OrderBy(t => t).ElementAt(times.Length / 2) },
                    { "StandardDeviationMs", CalculateStandardDeviation(times) },
                    { "SuccessfulTestsPercentage", totalCount > 0 ? (double)successCount / totalCount * 100 : 0 }
                };
            });
    }

    private static double CalculateStandardDeviation(double[] values)
    {
        if (values.Length == 0) return 0;
        
        var average = values.Average();
        var sumOfSquaresOfDifferences = values.Select(val => (val - average) * (val - average)).Sum();
        return Math.Sqrt(sumOfSquaresOfDifferences / values.Length);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var analyzer = new ResultsAnalyzer(_mockLogger.Object, _mockTestExecutor.Object, _mockStatisticalAnalyzer.Object);

        // Assert
        Assert.NotNull(analyzer);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new ResultsAnalyzer(null!, _mockTestExecutor.Object, _mockStatisticalAnalyzer.Object));
        
        Assert.Equal("logger", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullTestExecutor_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new ResultsAnalyzer(_mockLogger.Object, null!, _mockStatisticalAnalyzer.Object));
        
        Assert.Equal("testExecutor", exception.ParamName);
    }

    #endregion

    #region ValidateLearnedCapabilityAsync Tests

    [Fact]
    public async Task ValidateLearnedCapabilityAsync_WithValidCapability_ShouldReturnValidationResult()
    {
        // Arrange
        var capability = CreateTestCapability();
        var suiteResult = CreateSuccessfulTestSuiteResult();
        
        _mockTestExecutor.Setup(x => x.ExecuteTestSuiteAsync(It.IsAny<List<SelfGeneratedTestCase>>()))
            .ReturnsAsync(suiteResult);

        // Act
        var result = await _resultsAnalyzer.ValidateLearnedCapabilityAsync("TestAPI", capability);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(capability.Name, result.CapabilityName);
        Assert.True(result.IsValid);
        Assert.True(result.ConfidenceScore >= 0.8);
        Assert.Equal(CapabilityStatus.Validated, result.NewStatus);
        Assert.NotEmpty(result.Strengths);
        Assert.Empty(result.Weaknesses); // No failures in this test
    }

    [Fact]
    public async Task ValidateLearnedCapabilityAsync_WithFailedTests_ShouldReturnFailedValidation()
    {
        // Arrange
        var capability = CreateTestCapability();
        var suiteResult = CreateFailedTestSuiteResult();
        
        _mockTestExecutor.Setup(x => x.ExecuteTestSuiteAsync(It.IsAny<List<SelfGeneratedTestCase>>()))
            .ReturnsAsync(suiteResult);

        // Act
        var result = await _resultsAnalyzer.ValidateLearnedCapabilityAsync("TestAPI", capability);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.True(result.ConfidenceScore < 0.8);
        Assert.Equal(CapabilityStatus.Failed, result.NewStatus);
        Assert.NotEmpty(result.Weaknesses);
        Assert.NotEmpty(result.ImprovementSuggestions);
    }

    [Fact]
    public async Task ValidateLearnedCapabilityAsync_WithException_ShouldReturnFailedResult()
    {
        // Arrange
        var capability = CreateTestCapability();
        
        _mockTestExecutor.Setup(x => x.ExecuteTestSuiteAsync(It.IsAny<List<SelfGeneratedTestCase>>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act
        var result = await _resultsAnalyzer.ValidateLearnedCapabilityAsync("TestAPI", capability);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Equal(0, result.ConfidenceScore);
        Assert.Equal(CapabilityStatus.Failed, result.NewStatus);
    }

    #endregion

    #region BenchmarkNewSkillAsync Tests

    [Fact]
    public async Task BenchmarkNewSkillAsync_WithSuccessfulResults_ShouldReturnHighGrade()
    {
        // Arrange
        var testResults = CreateSuccessfulTestResults();

        // Act
        var result = await _resultsAnalyzer.BenchmarkNewSkillAsync("TestSkill", testResults);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestSkill", result.SkillName);
        Assert.Equal(PerformanceGrade.A, result.Grade);
        Assert.True(result.SuccessRate >= 0.9);
        Assert.True(result.TotalOperations > 0);
        Assert.NotEmpty(result.PerformanceMetrics);
    }

    [Fact]
    public async Task BenchmarkNewSkillAsync_WithEmptyResults_ShouldReturnFailingGrade()
    {
        // Arrange
        var testResults = new List<TestExecutionResult>();

        // Act
        var result = await _resultsAnalyzer.BenchmarkNewSkillAsync("TestSkill", testResults);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestSkill", result.SkillName);
        Assert.Equal(PerformanceGrade.F, result.Grade);
    }

    [Fact]
    public async Task BenchmarkNewSkillAsync_WithMixedResults_ShouldReturnMediumGrade()
    {
        // Arrange
        var testResults = CreateMixedTestResults();

        // Act
        var result = await _resultsAnalyzer.BenchmarkNewSkillAsync("TestSkill", testResults);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestSkill", result.SkillName);
        // Debug: Check what we actually get
        Assert.True(result.SuccessRate > 0.5, $"Expected success rate > 0.5 but got {result.SuccessRate}");
        Assert.True(result.Grade >= PerformanceGrade.C, $"Expected grade >= C but got {result.Grade} (success rate: {result.SuccessRate})");
        Assert.NotEmpty(result.PerformanceRecommendations);
    }

    [Fact]
    public async Task BenchmarkNewSkillAsync_WithException_ShouldReturnFailingGrade()
    {
        // Arrange
        var testResults = new List<TestExecutionResult> { null! };

        // Act
        var result = await _resultsAnalyzer.BenchmarkNewSkillAsync("TestSkill", testResults);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestSkill", result.SkillName);
        Assert.Equal(PerformanceGrade.F, result.Grade);
    }

    #endregion

    #region AnalyzeTestFailuresAsync Tests

    [Fact]
    public async Task AnalyzeTestFailuresAsync_WithFailedTests_ShouldReturnAnalysis()
    {
        // Arrange
        var failedTests = CreateFailedTestResults();

        // Act
        var result = await _resultsAnalyzer.AnalyzeTestFailuresAsync(failedTests);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(failedTests.Count, result.TotalFailedTests);
        Assert.NotEmpty(result.FailureCategories);
        Assert.NotEmpty(result.CommonPatterns);
        Assert.NotEmpty(result.Suggestions);
        Assert.True(result.OverallHealthScore >= 0);
    }

    [Fact]
    public async Task AnalyzeTestFailuresAsync_WithNoFailures_ShouldReturnEmptyAnalysis()
    {
        // Arrange
        var failedTests = new List<TestExecutionResult>();

        // Act
        var result = await _resultsAnalyzer.AnalyzeTestFailuresAsync(failedTests);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalFailedTests);
        Assert.Equal(100, result.OverallHealthScore);
    }

    [Fact]
    public async Task AnalyzeTestFailuresAsync_WithSevereCriticalFailures_ShouldReturnLowHealthScore()
    {
        // Arrange
        var failedTests = CreateCriticalFailedTestResults();

        // Act
        var result = await _resultsAnalyzer.AnalyzeTestFailuresAsync(failedTests);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.OverallHealthScore < 60, $"Expected health score < 60 but got {result.OverallHealthScore}");
        Assert.NotEmpty(result.CriticalIssues);
        Assert.Contains(result.Suggestions, s => s.Priority == SuggestionPriority.Urgent);
    }

    [Fact]
    public async Task AnalyzeTestFailuresAsync_WithException_ShouldReturnMinimalAnalysis()
    {
        // Arrange
        var failedTests = new List<TestExecutionResult> { null! };

        // Act
        var result = await _resultsAnalyzer.AnalyzeTestFailuresAsync(failedTests);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalFailedTests); // Count includes null item
        Assert.Equal(0, result.OverallHealthScore);
    }

    #endregion

    #region CalculateConfidenceScore Tests

    [Fact]
    public void CalculateConfidenceScore_WithEmptyTestSuite_ShouldReturnZero()
    {
        // Arrange
        var suiteResult = new TestSuiteResult
        {
            TestResults = new List<TestExecutionResult>()
        };

        // Act
        var score = _resultsAnalyzer.CalculateConfidenceScore(suiteResult);

        // Assert
        Assert.Equal(0, score);
    }

    [Fact]
    public void CalculateConfidenceScore_WithPerfectResults_ShouldReturnHighScore()
    {
        // Arrange
        var suiteResult = CreateSuccessfulTestSuiteResult();

        // Act
        var score = _resultsAnalyzer.CalculateConfidenceScore(suiteResult);

        // Assert
        Assert.True(score > 0.9);
    }

    [Fact]
    public void CalculateConfidenceScore_WithMixedResults_ShouldReturnMediumScore()
    {
        // Arrange
        var suiteResult = new TestSuiteResult
        {
            TestResults = CreateMixedTestResults()
        };

        // Act
        var score = _resultsAnalyzer.CalculateConfidenceScore(suiteResult);

        // Assert
        Assert.True(score >= 0.3 && score <= 0.8);
    }

    #endregion

    #region CollectDetailedMetrics Tests

    [Fact]
    public void CollectDetailedMetrics_WithValidResults_ShouldReturnMetrics()
    {
        // Arrange
        var testResults = CreateSuccessfulTestResults();

        // Act
        var metrics = _resultsAnalyzer.CollectDetailedMetrics(testResults);

        // Assert
        Assert.NotNull(metrics);
        Assert.True(metrics.ContainsKey("AverageExecutionTimeMs"));
        Assert.True(metrics.ContainsKey("MedianExecutionTimeMs"));
        Assert.True(metrics.ContainsKey("StandardDeviationMs"));
        Assert.True(metrics.ContainsKey("SuccessfulTestsPercentage"));
        Assert.True(metrics["SuccessfulTestsPercentage"] > 90); // Should be high for successful results
    }

    [Fact]
    public void CollectDetailedMetrics_WithEmptyResults_ShouldReturnEmptyMetrics()
    {
        // Arrange
        var testResults = new List<TestExecutionResult>();

        // Act
        var metrics = _resultsAnalyzer.CollectDetailedMetrics(testResults);

        // Assert
        Assert.NotNull(metrics);
        Assert.Empty(metrics);
    }

    [Fact]
    public void CollectDetailedMetrics_WithSingleResult_ShouldReturnValidMetrics()
    {
        // Arrange
        var testResults = new List<TestExecutionResult>
        {
            CreateSuccessfulTestResult("Test1")
        };

        // Act
        var metrics = _resultsAnalyzer.CollectDetailedMetrics(testResults);

        // Assert
        Assert.NotNull(metrics);
        Assert.Equal(4, metrics.Count);
        Assert.Equal(100.0, metrics["SuccessfulTestsPercentage"]);
    }

    #endregion

    #region Helper Methods for Test Data Creation

    private LearnedCapability CreateTestCapability()
    {
        return new LearnedCapability
        {
            Name = "TestCapability",
            Description = "A test capability for validation",
            ValidationTests = new List<SelfGeneratedTestCase>
            {
                CreateTestCase("Test1"),
                CreateTestCase("Test2"),
                CreateTestCase("Test3")
            }
        };
    }

    private SelfGeneratedTestCase CreateTestCase(string name)
    {
        return new SelfGeneratedTestCase
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Description = $"Test case for {name}",
            ApiName = "TestAPI",
            Endpoint = "/test",
            HttpMethod = "GET",
            ExpectedExecutionTime = TimeSpan.FromSeconds(1)
        };
    }

    private TestSuiteResult CreateSuccessfulTestSuiteResult()
    {
        return new TestSuiteResult
        {
            SuiteName = "TestSuite",
            TestResults = CreateSuccessfulTestResults(),
            TotalExecutionTime = TimeSpan.FromSeconds(5),
            Status = TestSuiteStatus.Completed
        };
    }

    private TestSuiteResult CreateFailedTestSuiteResult()
    {
        return new TestSuiteResult
        {
            SuiteName = "FailedTestSuite",
            TestResults = CreateFailedTestResults(),
            TotalExecutionTime = TimeSpan.FromSeconds(10),
            Status = TestSuiteStatus.Failed
        };
    }

    private List<TestExecutionResult> CreateSuccessfulTestResults()
    {
        return new List<TestExecutionResult>
        {
            CreateSuccessfulTestResult("Test1"),
            CreateSuccessfulTestResult("Test2"),
            CreateSuccessfulTestResult("Test3"),
            CreateSuccessfulTestResult("Test4"),
            CreateSuccessfulTestResult("Test5")
        };
    }

    private TestExecutionResult CreateSuccessfulTestResult(string testName)
    {
        return new TestExecutionResult
        {
            TestCaseId = Guid.NewGuid().ToString(),
            TestCaseName = testName,
            Success = true,
            ExecutionTime = TimeSpan.FromMilliseconds(500 + new Random().Next(0, 200)),
            AssertionResults = new List<AssertionResult>
            {
                new AssertionResult { AssertionName = "StatusCode", Passed = true, IsCritical = true },
                new AssertionResult { AssertionName = "ResponseTime", Passed = true, IsCritical = false }
            },
            Response = "Success response",
            ExecutedAt = DateTime.UtcNow
        };
    }

    private List<TestExecutionResult> CreateFailedTestResults()
    {
        return new List<TestExecutionResult>
        {
            CreateFailedTestResult("FailedTest1", "500 Internal Server Error"),
            CreateFailedTestResult("FailedTest2", "404 Not Found"),
            CreateFailedTestResult("FailedTest3", "Request timeout"),
            CreateFailedTestResult("FailedTest4", "401 Unauthorized")
        };
    }

    private List<TestExecutionResult> CreateCriticalFailedTestResults()
    {
        return new List<TestExecutionResult>
        {
            CreateFailedTestResult("CriticalFail1", "Critical system failure"),
            CreateFailedTestResult("CriticalFail2", "Critical system failure"),
            CreateFailedTestResult("CriticalFail3", "Critical system failure"),
            CreateFailedTestResult("CriticalFail4", "Critical database error"),
            CreateFailedTestResult("CriticalFail5", "Critical database error")
        };
    }

    private TestExecutionResult CreateFailedTestResult(string testName, string errorMessage)
    {
        return new TestExecutionResult
        {
            TestCaseId = Guid.NewGuid().ToString(),
            TestCaseName = testName,
            Success = false,
            ExecutionTime = TimeSpan.FromMilliseconds(1000 + new Random().Next(0, 500)),
            ErrorMessage = errorMessage,
            AssertionResults = new List<AssertionResult>
            {
                new AssertionResult { AssertionName = "StatusCode", Passed = false, IsCritical = true, ErrorMessage = errorMessage },
                new AssertionResult { AssertionName = "ResponseTime", Passed = true, IsCritical = false }
            },
            Response = null,
            ExecutedAt = DateTime.UtcNow
        };
    }

    private List<TestExecutionResult> CreateMixedTestResults()
    {
        var results = new List<TestExecutionResult>();
        
        // Create exactly 7 successful tests
        for (int i = 1; i <= 7; i++)
        {
            results.Add(CreateSuccessfulTestResult($"SuccessTest{i}"));
        }
        
        // Create exactly 3 failed tests  
        results.Add(CreateFailedTestResult("FailedTest1", "500 Internal Server Error"));
        results.Add(CreateFailedTestResult("FailedTest2", "404 Not Found"));
        results.Add(CreateFailedTestResult("FailedTest3", "Request timeout"));
        
        return results; // Exactly 10 tests: 7 success + 3 fail = 70% success rate
    }

    #endregion
}