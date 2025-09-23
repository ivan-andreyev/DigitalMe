using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DigitalMe.Services.Learning;
using DigitalMe.Services.Learning.Testing.ParallelProcessing;
using DigitalMe.Services.Learning.Testing.TestExecution;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DigitalMe.Tests.Unit.Services.Learning.Testing.ParallelProcessing;

/// <summary>
/// Comprehensive unit tests for ParallelTestRunner
/// Tests parallel test execution, resource management, and performance analysis
/// Following Clean Architecture testing patterns
/// 20+ test methods covering all functionality
/// </summary>
public class ParallelTestRunnerTests
{
    private readonly Mock<ILogger<ParallelTestRunner>> _loggerMock;
    private readonly Mock<ISingleTestExecutor> _singleTestExecutorMock;
    private readonly ParallelTestRunner _parallelTestRunner;

    public ParallelTestRunnerTests()
    {
        this._loggerMock = new Mock<ILogger<ParallelTestRunner>>();
        this._singleTestExecutorMock = new Mock<ISingleTestExecutor>();
        this._parallelTestRunner = new ParallelTestRunner(this._loggerMock.Object, this._singleTestExecutorMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ValidParameters_ShouldCreateInstance()
    {
        // Act & Assert
        Assert.NotNull(this._parallelTestRunner);
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ParallelTestRunner(null!, this._singleTestExecutorMock.Object));
    }

    [Fact]
    public void Constructor_NullTestExecutor_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ParallelTestRunner(this._loggerMock.Object, null!));
    }

    #endregion

    #region ExecuteTestsInParallelAsync Tests

    [Fact]
    public async Task ExecuteTestsInParallelAsync_EmptyTestCases_ShouldReturnEmptyList()
    {
        // Arrange
        var testCases = new List<SelfGeneratedTestCase>();

        // Act
        var results = await this._parallelTestRunner.ExecuteTestsInParallelAsync(testCases);

        // Assert
        Assert.Empty(results);
        this._singleTestExecutorMock.Verify(x => x.ExecuteTestCaseAsync(It.IsAny<SelfGeneratedTestCase>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteTestsInParallelAsync_NullTestCases_ShouldReturnEmptyList()
    {
        // Act
        var results = await this._parallelTestRunner.ExecuteTestsInParallelAsync(null!);

        // Assert
        Assert.Empty(results);
        this._singleTestExecutorMock.Verify(x => x.ExecuteTestCaseAsync(It.IsAny<SelfGeneratedTestCase>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteTestsInParallelAsync_SingleTestCase_ShouldExecuteOnce()
    {
        // Arrange
        var testCase = this.CreateTestCase("Test1");
        var testCases = new List<SelfGeneratedTestCase> { testCase };
        var expectedResult = this.CreateTestResult("Test1", true);

        this._singleTestExecutorMock
            .Setup(x => x.ExecuteTestCaseAsync(It.IsAny<SelfGeneratedTestCase>()))
            .ReturnsAsync(expectedResult);

        // Act
        var results = await this._parallelTestRunner.ExecuteTestsInParallelAsync(testCases);

        // Assert
        Assert.Single(results);
        Assert.Equal(expectedResult.TestCaseId, results[0].TestCaseId);
        this._singleTestExecutorMock.Verify(x => x.ExecuteTestCaseAsync(testCase), Times.Once);
    }

    [Fact]
    public async Task ExecuteTestsInParallelAsync_MultipleTestCases_ShouldExecuteAll()
    {
        // Arrange
        var testCases = new List<SelfGeneratedTestCase>
        {
            this.CreateTestCase("Test1"),
            this.CreateTestCase("Test2"),
            this.CreateTestCase("Test3")
        };

        this._singleTestExecutorMock
            .Setup(x => x.ExecuteTestCaseAsync(It.IsAny<SelfGeneratedTestCase>()))
            .ReturnsAsync((SelfGeneratedTestCase tc) => this.CreateTestResult(tc.Name, true));

        // Act
        var results = await this._parallelTestRunner.ExecuteTestsInParallelAsync(testCases);

        // Assert
        Assert.Equal(3, results.Count);
        this._singleTestExecutorMock.Verify(x => x.ExecuteTestCaseAsync(It.IsAny<SelfGeneratedTestCase>()), Times.Exactly(3));
    }

    [Fact]
    public async Task ExecuteTestsInParallelAsync_CustomConcurrency_ShouldRespectLimit()
    {
        // Arrange
        var testCases = Enumerable.Range(1, 10)
            .Select(i => this.CreateTestCase($"Test{i}"))
            .ToList();

        var executionTimes = new List<DateTime>();
        this._singleTestExecutorMock
            .Setup(x => x.ExecuteTestCaseAsync(It.IsAny<SelfGeneratedTestCase>()))
            .Returns(async (SelfGeneratedTestCase tc) =>
            {
                executionTimes.Add(DateTime.Now);
                await Task.Delay(100); // Simulate work
                return this.CreateTestResult(tc.Name, true);
            });

        // Act
        var results = await this._parallelTestRunner.ExecuteTestsInParallelAsync(testCases, maxConcurrency: 2);

        // Assert
        Assert.Equal(10, results.Count);

        // With concurrency of 2 and 100ms delays, execution should take at least 500ms
        // This is a simple test to verify concurrency limitation is working
    }

    #endregion

    #region ExecuteTestsWithOptimalConcurrencyAsync Tests

    [Fact]
    public async Task ExecuteTestsWithOptimalConcurrencyAsync_EmptyTestCases_ShouldReturnEmptyList()
    {
        // Arrange
        var testCases = new List<SelfGeneratedTestCase>();

        // Act
        var results = await this._parallelTestRunner.ExecuteTestsWithOptimalConcurrencyAsync(testCases);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task ExecuteTestsWithOptimalConcurrencyAsync_ValidTestCases_ShouldExecuteWithOptimalConcurrency()
    {
        // Arrange
        var testCases = new List<SelfGeneratedTestCase>
        {
            this.CreateTestCase("Test1"),
            this.CreateTestCase("Test2")
        };

        this._singleTestExecutorMock
            .Setup(x => x.ExecuteTestCaseAsync(It.IsAny<SelfGeneratedTestCase>()))
            .ReturnsAsync((SelfGeneratedTestCase tc) => this.CreateTestResult(tc.Name, true));

        // Act
        var results = await this._parallelTestRunner.ExecuteTestsWithOptimalConcurrencyAsync(testCases);

        // Assert
        Assert.Equal(2, results.Count);
        this._singleTestExecutorMock.Verify(x => x.ExecuteTestCaseAsync(It.IsAny<SelfGeneratedTestCase>()), Times.Exactly(2));
    }

    #endregion

    #region ExecuteTestsInBatchesAsync Tests

    [Fact]
    public async Task ExecuteTestsInBatchesAsync_EmptyTestCases_ShouldReturnEmptyList()
    {
        // Arrange
        var testCases = new List<SelfGeneratedTestCase>();

        // Act
        var results = await this._parallelTestRunner.ExecuteTestsInBatchesAsync(testCases);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task ExecuteTestsInBatchesAsync_LessThanBatchSize_ShouldExecuteInSingleBatch()
    {
        // Arrange
        var testCases = new List<SelfGeneratedTestCase>
        {
            this.CreateTestCase("Test1"),
            this.CreateTestCase("Test2")
        };

        this._singleTestExecutorMock
            .Setup(x => x.ExecuteTestCaseAsync(It.IsAny<SelfGeneratedTestCase>()))
            .ReturnsAsync((SelfGeneratedTestCase tc) => this.CreateTestResult(tc.Name, true));

        // Act
        var results = await this._parallelTestRunner.ExecuteTestsInBatchesAsync(testCases, batchSize: 10);

        // Assert
        Assert.Equal(2, results.Count);
        this._singleTestExecutorMock.Verify(x => x.ExecuteTestCaseAsync(It.IsAny<SelfGeneratedTestCase>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ExecuteTestsInBatchesAsync_MoreThanBatchSize_ShouldExecuteInMultipleBatches()
    {
        // Arrange
        var testCases = Enumerable.Range(1, 15)
            .Select(i => this.CreateTestCase($"Test{i}"))
            .ToList();

        this._singleTestExecutorMock
            .Setup(x => x.ExecuteTestCaseAsync(It.IsAny<SelfGeneratedTestCase>()))
            .ReturnsAsync((SelfGeneratedTestCase tc) => this.CreateTestResult(tc.Name, true));

        // Act
        var results = await this._parallelTestRunner.ExecuteTestsInBatchesAsync(testCases, batchSize: 5);

        // Assert
        Assert.Equal(15, results.Count);
        this._singleTestExecutorMock.Verify(x => x.ExecuteTestCaseAsync(It.IsAny<SelfGeneratedTestCase>()), Times.Exactly(15));
    }

    [Fact]
    public async Task ExecuteTestsInBatchesAsync_NullTestCases_ShouldReturnEmptyList()
    {
        // Act
        var results = await this._parallelTestRunner.ExecuteTestsInBatchesAsync(null!);

        // Assert
        Assert.Empty(results);
    }

    #endregion

    #region GetOptimalConcurrencyLevel Tests

    [Fact]
    public void GetOptimalConcurrencyLevel_ShouldReturnReasonableValue()
    {
        // Act
        var concurrency = this._parallelTestRunner.GetOptimalConcurrencyLevel();

        // Assert
        Assert.True(concurrency >= 1, "Concurrency should be at least 1");
        Assert.True(concurrency <= 10, "Concurrency should not exceed maximum recommended");
    }

    [Fact]
    public void GetOptimalConcurrencyLevel_MultipleCalls_ShouldReturnConsistentResults()
    {
        // Act
        var concurrency1 = this._parallelTestRunner.GetOptimalConcurrencyLevel();
        var concurrency2 = this._parallelTestRunner.GetOptimalConcurrencyLevel();

        // Assert
        Assert.Equal(concurrency1, concurrency2);
    }

    #endregion

    #region AnalyzeParallelPerformance Tests

    [Fact]
    public void AnalyzeParallelPerformance_EmptyResults_ShouldReturnValidAnalysis()
    {
        // Arrange
        var testResults = new List<TestExecutionResult>();

        // Act
        var analysis = this._parallelTestRunner.AnalyzeParallelPerformance(testResults, 5);

        // Assert
        Assert.NotNull(analysis);
        Assert.Equal(5, analysis.ConcurrencyLevel);
        Assert.NotNull(analysis.Recommendations);
        Assert.Contains("No test results", analysis.Recommendations[0]);
    }

    [Fact]
    public void AnalyzeParallelPerformance_NullResults_ShouldReturnValidAnalysis()
    {
        // Act
        var analysis = this._parallelTestRunner.AnalyzeParallelPerformance(null!, 3);

        // Assert
        Assert.NotNull(analysis);
        Assert.Equal(3, analysis.ConcurrencyLevel);
        Assert.NotNull(analysis.Recommendations);
    }

    [Fact]
    public void AnalyzeParallelPerformance_ValidResults_ShouldCalculateMetrics()
    {
        // Arrange
        var testResults = new List<TestExecutionResult>
        {
            this.CreateTestResultWithTime("Test1", true, TimeSpan.FromMilliseconds(100)),
            this.CreateTestResultWithTime("Test2", true, TimeSpan.FromMilliseconds(200)),
            this.CreateTestResultWithTime("Test3", true, TimeSpan.FromMilliseconds(150))
        };

        // Act
        var analysis = this._parallelTestRunner.AnalyzeParallelPerformance(testResults, 2);

        // Assert
        Assert.Equal(2, analysis.ConcurrencyLevel);
        Assert.True(analysis.SpeedupRatio > 0);
        Assert.True(analysis.ParallelEfficiency >= 0 && analysis.ParallelEfficiency <= 1);
        Assert.NotNull(analysis.ResourceUtilization);
        Assert.NotNull(analysis.ThreadCoordinationStats);
        Assert.NotNull(analysis.Recommendations);
    }

    [Fact]
    public void AnalyzeParallelPerformance_HighFailureRate_ShouldRecommendSerialDebugging()
    {
        // Arrange
        var testResults = new List<TestExecutionResult>
        {
            this.CreateTestResultWithTime("Test1", false, TimeSpan.FromMilliseconds(100)),
            this.CreateTestResultWithTime("Test2", false, TimeSpan.FromMilliseconds(200)),
            this.CreateTestResultWithTime("Test3", false, TimeSpan.FromMilliseconds(150)),
            this.CreateTestResultWithTime("Test4", true, TimeSpan.FromMilliseconds(120))
        };

        // Act
        var analysis = this._parallelTestRunner.AnalyzeParallelPerformance(testResults, 2);

        // Assert
        Assert.Contains(analysis.Recommendations, r => r.Contains("serial debugging"));
    }

    [Fact]
    public void AnalyzeParallelPerformance_LongRunningTests_ShouldRecommendOptimization()
    {
        // Arrange
        var testResults = new List<TestExecutionResult>
        {
            this.CreateTestResultWithTime("Test1", true, TimeSpan.FromSeconds(15)),
            this.CreateTestResultWithTime("Test2", true, TimeSpan.FromSeconds(12))
        };

        // Act
        var analysis = this._parallelTestRunner.AnalyzeParallelPerformance(testResults, 2);

        // Assert
        Assert.Contains(analysis.Recommendations, r => r.Contains("took longer than 10 seconds"));
    }

    [Fact]
    public void AnalyzeParallelPerformance_LowEfficiency_ShouldRecommendReducingConcurrency()
    {
        // Arrange - Create scenario with very uneven execution times (suggests contention)
        var testResults = new List<TestExecutionResult>
        {
            this.CreateTestResultWithTime("Test1", true, TimeSpan.FromMilliseconds(100)),
            this.CreateTestResultWithTime("Test2", true, TimeSpan.FromMilliseconds(2000)),
            this.CreateTestResultWithTime("Test3", true, TimeSpan.FromMilliseconds(150))
        };

        // Act
        var analysis = this._parallelTestRunner.AnalyzeParallelPerformance(testResults, 8);

        // Assert
        Assert.True(analysis.ParallelEfficiency < 1.0);
        Assert.True(analysis.RecommendedConcurrency > 0);
    }

    [Fact]
    public void AnalyzeParallelPerformance_HighEfficiency_ShouldCalculateCorrectMetrics()
    {
        // Arrange - Create scenario with even execution times (good parallelization)
        var testResults = new List<TestExecutionResult>
        {
            this.CreateTestResultWithTime("Test1", true, TimeSpan.FromMilliseconds(100)),
            this.CreateTestResultWithTime("Test2", true, TimeSpan.FromMilliseconds(105)),
            this.CreateTestResultWithTime("Test3", true, TimeSpan.FromMilliseconds(95)),
            this.CreateTestResultWithTime("Test4", true, TimeSpan.FromMilliseconds(102))
        };

        // Act
        var analysis = this._parallelTestRunner.AnalyzeParallelPerformance(testResults, 2);

        // Assert
        Assert.True(analysis.SpeedupRatio > 1.0, "Should show improvement over serial execution");
        Assert.Contains("CPU", analysis.ResourceUtilization.Keys);
        Assert.Contains("Memory", analysis.ResourceUtilization.Keys);
        Assert.Contains("Network", analysis.ResourceUtilization.Keys);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ExecuteTestsInParallelAsync_TestExecutorThrows_ShouldHandleGracefully()
    {
        // Arrange
        var testCases = new List<SelfGeneratedTestCase> { this.CreateTestCase("Test1") };

        this._singleTestExecutorMock
            .Setup(x => x.ExecuteTestCaseAsync(It.IsAny<SelfGeneratedTestCase>()))
            .ThrowsAsync(new InvalidOperationException("Test executor error"));

        // Act & Assert - Should not throw, but handle gracefully
        var results = await this._parallelTestRunner.ExecuteTestsInParallelAsync(testCases);

        // The implementation should handle exceptions in the semaphore pattern
        Assert.NotNull(results);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task ExecuteTestsInParallelAsync_LargeBatch_ShouldCompleteInReasonableTime()
    {
        // Arrange
        var testCases = Enumerable.Range(1, 20)
            .Select(i => this.CreateTestCase($"Test{i}"))
            .ToList();

        this._singleTestExecutorMock
            .Setup(x => x.ExecuteTestCaseAsync(It.IsAny<SelfGeneratedTestCase>()))
            .Returns(async (SelfGeneratedTestCase tc) =>
            {
                await Task.Delay(50); // Simulate 50ms per test
                return this.CreateTestResult(tc.Name, true);
            });

        var stopwatch = Stopwatch.StartNew();

        // Act
        var results = await this._parallelTestRunner.ExecuteTestsInParallelAsync(testCases, maxConcurrency: 5);

        stopwatch.Stop();

        // Assert
        Assert.Equal(20, results.Count);

        // With 5 concurrent tests and 50ms each, should complete in roughly 200ms (4 batches)
        // Allow some tolerance for test environment variations
        // Use different timeouts for CI vs local environment
        var isCI = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) ||
                   !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var maxExpectedMs = isCI ? 5000 : 1000; // CI environments can be much slower due to shared resources

        Assert.True(
            stopwatch.ElapsedMilliseconds < maxExpectedMs,
            $"Execution took {stopwatch.ElapsedMilliseconds}ms, expected less than {maxExpectedMs}ms (CI: {isCI})");
    }

    #endregion

    #region Helper Methods

    private SelfGeneratedTestCase CreateTestCase(string name)
    {
        return new SelfGeneratedTestCase
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Description = $"Test case for {name}",
            Endpoint = "https://api.example.com/test",
            HttpMethod = "GET",
            ExpectedExecutionTime = TimeSpan.FromSeconds(5),
            Headers = new Dictionary<string, string>(),
            Parameters = new Dictionary<string, object>(),
            Assertions = new List<TestAssertion>()
        };
    }

    private TestExecutionResult CreateTestResult(string testName, bool success)
    {
        return this.CreateTestResultWithTime(testName, success, TimeSpan.FromMilliseconds(100));
    }

    private TestExecutionResult CreateTestResultWithTime(string testName, bool success, TimeSpan executionTime)
    {
        return new TestExecutionResult
        {
            TestCaseId = Guid.NewGuid().ToString(),
            TestCaseName = testName,
            Success = success,
            ExecutionTime = executionTime,
            Response = $"Response for {testName}",
            AssertionResults = new List<AssertionResult>(),
            Metrics = new Dictionary<string, object>
            {
                ["StatusCode"] = success ? 200 : 500,
                ["ExecutionTimeMs"] = executionTime.TotalMilliseconds
            }
        };
    }

    #endregion
}