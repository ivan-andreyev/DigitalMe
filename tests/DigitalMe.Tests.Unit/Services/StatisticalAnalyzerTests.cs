using System;
using System.Collections.Generic;
using System.Linq;
using DigitalMe.Services.Learning.Testing.Statistics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DigitalMe.Tests.Unit.Services;

/// <summary>
/// Comprehensive unit tests for StatisticalAnalyzer
/// Tests all statistical computation methods with various scenarios including edge cases
/// Follows T2.1-T2.3 testing patterns with 20+ test methods for thorough coverage
/// </summary>
public class StatisticalAnalyzerTests
{
    private readonly Mock<ILogger<StatisticalAnalyzer>> _mockLogger;
    private readonly StatisticalAnalyzer _analyzer;

    public StatisticalAnalyzerTests()
    {
        this._mockLogger = new Mock<ILogger<StatisticalAnalyzer>>();
        this._analyzer = new StatisticalAnalyzer(this._mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidLogger_ShouldInitialize()
    {
        // Arrange & Act
        var analyzer = new StatisticalAnalyzer(this._mockLogger.Object);

        // Assert
        Assert.NotNull(analyzer);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange, Act & Assert
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
        Assert.Throws<ArgumentNullException>(() => new StatisticalAnalyzer(null));
#pragma warning restore CS8625
    }

    #endregion

    #region CalculateConfidenceScore Tests

    [Fact]
    public void CalculateConfidenceScore_WithValidInputs_ShouldReturnCorrectScore()
    {
        // Arrange
        var successRate = 85.0; // 85%
        var totalTests = 10;
        var executionTimes = new List<double> { 100, 120, 110, 105, 115, 108, 112, 118, 109, 111 };

        // Act
        var result = this._analyzer.CalculateConfidenceScore(successRate, totalTests, executionTimes);

        // Assert
        Assert.True(result >= 0 && result <= 1, "Confidence score should be between 0 and 1");
        Assert.True(result > 0.7, "Should have high confidence for good success rate and consistent times");
    }

    [Fact]
    public void CalculateConfidenceScore_WithZeroTests_ShouldReturnZero()
    {
        // Arrange
        var successRate = 100.0;
        var totalTests = 0;
        var executionTimes = new List<double>();

        // Act
        var result = this._analyzer.CalculateConfidenceScore(successRate, totalTests, executionTimes);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculateConfidenceScore_WithLowSuccessRate_ShouldReturnLowScore()
    {
        // Arrange
        var successRate = 30.0; // 30%
        var totalTests = 10;
        var executionTimes = new List<double> { 100, 120, 110, 105, 115, 108, 112, 118, 109, 111 };

        // Act
        var result = this._analyzer.CalculateConfidenceScore(successRate, totalTests, executionTimes);

        // Assert
        Assert.True(result < 0.5, "Should have low confidence for poor success rate");
    }

    [Fact]
    public void CalculateConfidenceScore_WithHighVariance_ShouldReduceScore()
    {
        // Arrange
        var successRate = 85.0;
        var totalTests = 10;
        var executionTimes = new List<double> { 50, 200, 75, 300, 100, 250, 80, 400, 90, 350 }; // High variance

        // Act
        var result = this._analyzer.CalculateConfidenceScore(successRate, totalTests, executionTimes);

        // Assert
        Assert.True(result > 0, "Should still have some confidence");
        Assert.True(result < 0.9, "Should penalize high variance in execution times");
    }

    [Fact]
    public void CalculateConfidenceScore_WithManyTests_ShouldIncreaseConfidence()
    {
        // Arrange
        var successRate = 80.0;
        var manyTests = 20; // More than 10
        var executionTimes = Enumerable.Repeat(100.0, manyTests).ToList();

        // Act
        var result = this._analyzer.CalculateConfidenceScore(successRate, manyTests, executionTimes);

        // Assert
        Assert.True(result > 0.7, "Many consistent tests should increase confidence");
    }

    #endregion

    #region CalculatePerformanceMetrics Tests

    [Fact]
    public void CalculatePerformanceMetrics_WithValidData_ShouldReturnComprehensiveMetrics()
    {
        // Arrange
        var executionTimes = new List<double> { 100, 150, 120, 130, 110, 140, 125, 135, 115, 145 };
        var successCount = 8;
        var totalCount = 10;

        // Act
        var result = this._analyzer.CalculatePerformanceMetrics(executionTimes, successCount, totalCount);

        // Assert
        Assert.Contains("AverageExecutionTimeMs", result.Keys);
        Assert.Contains("MinExecutionTimeMs", result.Keys);
        Assert.Contains("MaxExecutionTimeMs", result.Keys);
        Assert.Contains("MedianExecutionTimeMs", result.Keys);
        Assert.Contains("StandardDeviationMs", result.Keys);
        Assert.Contains("SuccessRate", result.Keys);
        Assert.Contains("SuccessRatePercentage", result.Keys);
        Assert.Contains("P90_ExecutionTimeMs", result.Keys);
        Assert.Contains("P95_ExecutionTimeMs", result.Keys);
        Assert.Contains("P99_ExecutionTimeMs", result.Keys);

        Assert.Equal(127.0, result["AverageExecutionTimeMs"]);
        Assert.Equal(100, result["MinExecutionTimeMs"]);
        Assert.Equal(150, result["MaxExecutionTimeMs"]);
        Assert.Equal(0.8, result["SuccessRate"]);
        Assert.Equal(80.0, result["SuccessRatePercentage"]);
    }

    [Fact]
    public void CalculatePerformanceMetrics_WithEmptyData_ShouldReturnEmptyMetrics()
    {
        // Arrange
        var executionTimes = new List<double>();
        var successCount = 0;
        var totalCount = 0;

        // Act
        var result = this._analyzer.CalculatePerformanceMetrics(executionTimes, successCount, totalCount);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void CalculatePerformanceMetrics_WithAllSuccessful_ShouldReturn100PercentSuccess()
    {
        // Arrange
        var executionTimes = new List<double> { 100, 110, 120 };
        var successCount = 3;
        var totalCount = 3;

        // Act
        var result = this._analyzer.CalculatePerformanceMetrics(executionTimes, successCount, totalCount);

        // Assert
        Assert.Equal(1.0, result["SuccessRate"]);
        Assert.Equal(100.0, result["SuccessRatePercentage"]);
        Assert.Equal(0.0, result["FailureRate"]);
    }

    #endregion

    #region CalculateMedian Tests

    [Fact]
    public void CalculateMedian_WithOddNumberOfValues_ShouldReturnMiddleValue()
    {
        // Arrange
        var values = new List<double> { 1, 3, 5, 7, 9 };

        // Act
        var result = this._analyzer.CalculateMedian(values);

        // Assert
        Assert.Equal(5, result);
    }

    [Fact]
    public void CalculateMedian_WithEvenNumberOfValues_ShouldReturnAverage()
    {
        // Arrange
        var values = new List<double> { 1, 2, 3, 4 };

        // Act
        var result = this._analyzer.CalculateMedian(values);

        // Assert
        Assert.Equal(2.5, result);
    }

    [Fact]
    public void CalculateMedian_WithUnsortedValues_ShouldReturnCorrectMedian()
    {
        // Arrange
        var values = new List<double> { 9, 1, 5, 3, 7 };

        // Act
        var result = this._analyzer.CalculateMedian(values);

        // Assert
        Assert.Equal(5, result);
    }

    [Fact]
    public void CalculateMedian_WithEmptyCollection_ShouldReturnZero()
    {
        // Arrange
        var values = new List<double>();

        // Act
        var result = this._analyzer.CalculateMedian(values);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculateMedian_WithSingleValue_ShouldReturnThatValue()
    {
        // Arrange
        var values = new List<double> { 42.5 };

        // Act
        var result = this._analyzer.CalculateMedian(values);

        // Assert
        Assert.Equal(42.5, result);
    }

    #endregion

    #region CalculateStandardDeviation Tests

    [Fact]
    public void CalculateStandardDeviation_WithIdenticalValues_ShouldReturnZero()
    {
        // Arrange
        var values = new List<double> { 5, 5, 5, 5, 5 };

        // Act
        var result = this._analyzer.CalculateStandardDeviation(values);

        // Assert
        Assert.Equal(0, result, 10); // Allow for floating point precision
    }

    [Fact]
    public void CalculateStandardDeviation_WithKnownValues_ShouldReturnCorrectValue()
    {
        // Arrange - simple case where we can calculate manually
        var values = new List<double> { 1, 2, 3, 4, 5 };

        // Mean = 3, variance = 2, std dev = sqrt(2) ≈ 1.414

        // Act
        var result = this._analyzer.CalculateStandardDeviation(values);

        // Assert
        Assert.Equal(Math.Sqrt(2), result, 3); // 3 decimal places precision
    }

    [Fact]
    public void CalculateStandardDeviation_WithEmptyCollection_ShouldReturnZero()
    {
        // Arrange
        var values = new List<double>();

        // Act
        var result = this._analyzer.CalculateStandardDeviation(values);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculateStandardDeviation_WithSingleValue_ShouldReturnZero()
    {
        // Arrange
        var values = new List<double> { 42.5 };

        // Act
        var result = this._analyzer.CalculateStandardDeviation(values);

        // Assert
        Assert.Equal(0, result);
    }

    #endregion

    #region AnalyzeTrends Tests

    [Fact]
    public void AnalyzeTrends_WithInsufficientData_ShouldReturnInsufficientDataResult()
    {
        // Arrange
        var metrics = new List<HistoricalMetric>
        {
            new() { Timestamp = DateTime.Now, Value = 100, MetricName = "TestMetric" }
        };

        // Act
        var result = this._analyzer.AnalyzeTrends(metrics);

        // Assert
        Assert.Equal(TrendDirection.InsufficientData, result.Direction);
        Assert.Contains("Not enough historical data", result.TrendDescription);
    }

    [Fact]
    public void AnalyzeTrends_WithImprovingTrend_ShouldReturnImprovingDirection()
    {
        // Arrange
        var baseTime = DateTime.Now.AddDays(-10);
        var metrics = new List<HistoricalMetric>();

        for (int i = 0; i < 10; i++)
        {
            metrics.Add(new HistoricalMetric
            {
                Timestamp = baseTime.AddDays(i),
                Value = 50 + i * 10, // Steadily increasing
                MetricName = "Performance"
            });
        }

        // Act
        var result = this._analyzer.AnalyzeTrends(metrics);

        // Assert
        Assert.Equal(TrendDirection.Improving, result.Direction);
        Assert.True(result.RateOfChange > 0);
        Assert.True(result.TrendConfidence > 0.8, "Should have high confidence for clear trend");
        Assert.Contains("improving", result.TrendDescription.ToLower());
    }


    [Fact]
    public void AnalyzeTrends_WithStableData_ShouldReturnStableDirection()
    {
        // Arrange
        var baseTime = DateTime.Now.AddDays(-10);
        var metrics = new List<HistoricalMetric>();

        for (int i = 0; i < 10; i++)
        {
            metrics.Add(new HistoricalMetric
            {
                Timestamp = baseTime.AddDays(i),
                Value = 100 + (i % 2 == 0 ? 0.5 : -0.5), // Very minor fluctuations around 100
                MetricName = "Performance"
            });
        }

        // Act
        var result = this._analyzer.AnalyzeTrends(metrics);

        // Assert
        Assert.Equal(TrendDirection.Stable, result.Direction);
        Assert.True(Math.Abs(result.RateOfChange) < 5); // Low rate of change
        Assert.Contains("stable", result.TrendDescription.ToLower());
    }

    [Fact]
    public void AnalyzeTrends_WithRandomData_ShouldProvideReasonableResult()
    {
        // Arrange
        var baseTime = DateTime.Now.AddDays(-20);
        var random = new Random(42); // Fixed seed for reproducible tests
        var metrics = new List<HistoricalMetric>();

        for (int i = 0; i < 15; i++)
        {
            metrics.Add(new HistoricalMetric
            {
                Timestamp = baseTime.AddDays(i),
                Value = 100 + random.Next(-20, 20), // Random variations
                MetricName = "Performance"
            });
        }

        // Act
        var result = this._analyzer.AnalyzeTrends(metrics);

        // Assert
        Assert.NotEqual(TrendDirection.InsufficientData, result.Direction);
        Assert.True(result.TrendConfidence >= 0 && result.TrendConfidence <= 1);
        Assert.NotEmpty(result.TrendDescription);
        Assert.NotEmpty(result.TrendRecommendations);
    }

    #endregion

    #region Integration and Edge Case Tests

    [Fact]
    public void CalculatePerformanceMetrics_WithExtremeValues_ShouldHandleGracefully()
    {
        // Arrange
        var executionTimes = new List<double> { double.MinValue, 100, double.MaxValue };
        var successCount = 2;
        var totalCount = 3;

        // Act
        var result = this._analyzer.CalculatePerformanceMetrics(executionTimes, successCount, totalCount);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("SuccessRatePercentage", result.Keys);
        Assert.True(double.IsFinite(result["SuccessRatePercentage"]));
    }

    [Fact]
    public void AnalyzeTrends_WithDuplicateTimestamps_ShouldHandleGracefully()
    {
        // Arrange
        var timestamp = DateTime.Now;
        var metrics = new List<HistoricalMetric>
        {
            new() { Timestamp = timestamp, Value = 100, MetricName = "Test" },
            new() { Timestamp = timestamp, Value = 110, MetricName = "Test" },
            new() { Timestamp = timestamp, Value = 120, MetricName = "Test" }
        };

        // Act
        var result = this._analyzer.AnalyzeTrends(metrics);

        // Assert
        Assert.NotNull(result);
        Assert.True(Enum.IsDefined(typeof(TrendDirection), result.Direction));
    }

    [Fact]
    public void AllMethods_WithExceptionScenarios_ShouldReturnSafeDefaults()
    {
        // This test validates that all methods handle exceptions gracefully
        // Arrange - use null/empty inputs to potentially trigger exceptions
        var emptyTimes = new List<double>();

        // Act & Assert - methods should not throw exceptions
        var confidence = this._analyzer.CalculateConfidenceScore(100, 0, emptyTimes);
        var metrics = this._analyzer.CalculatePerformanceMetrics(emptyTimes, 0, 0);
        var median = this._analyzer.CalculateMedian(emptyTimes);
        var stdDev = this._analyzer.CalculateStandardDeviation(emptyTimes);
        var trends = this._analyzer.AnalyzeTrends(new List<HistoricalMetric>());

        // Validate safe defaults are returned
        Assert.Equal(0, confidence);
        Assert.Empty(metrics);
        Assert.Equal(0, median);
        Assert.Equal(0, stdDev);
        Assert.NotNull(trends);
    }

    #endregion

    #region Performance and Stress Tests

    [Fact]
    public void CalculatePerformanceMetrics_WithLargeDataset_ShouldCompleteReasonably()
    {
        // Arrange
        var largeDataset = Enumerable.Range(1, 10000).Select(i => (double)i).ToList();
        var successCount = 9500;
        var totalCount = 10000;

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = this._analyzer.CalculatePerformanceMetrics(largeDataset, successCount, totalCount);
        stopwatch.Stop();

        // Assert
        Assert.NotEmpty(result);
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, "Should complete within 1 second for 10k items");
        Assert.Equal(5000.5, result["MedianExecutionTimeMs"]); // Known median for 1..10000
    }

    #endregion
}