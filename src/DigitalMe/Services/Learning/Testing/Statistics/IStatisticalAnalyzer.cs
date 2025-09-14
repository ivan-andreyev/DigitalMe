using System.Collections.Generic;

namespace DigitalMe.Services.Learning.Testing.Statistics;

/// <summary>
/// Interface for core statistical analysis operations
/// Extracted from SelfTestingFramework God Class following SOLID principles
/// Focuses on pure mathematical and statistical computations for performance metrics, confidence calculations, and trend analysis
/// </summary>
public interface IStatisticalAnalyzer
{
    /// <summary>
    /// Calculate statistical confidence score based on test results and execution patterns
    /// </summary>
    /// <param name="successRate">Success rate as percentage (0-100)</param>
    /// <param name="totalTests">Total number of tests executed</param>
    /// <param name="executionTimes">Collection of execution times for consistency analysis</param>
    /// <returns>Confidence score between 0 and 1</returns>
    double CalculateConfidenceScore(double successRate, int totalTests, IEnumerable<double> executionTimes);

    /// <summary>
    /// Calculate comprehensive performance metrics from execution data
    /// </summary>
    /// <param name="executionTimes">Collection of execution times in milliseconds</param>
    /// <param name="successCount">Number of successful executions</param>
    /// <param name="totalCount">Total number of executions</param>
    /// <returns>Dictionary of detailed performance metrics</returns>
    Dictionary<string, double> CalculatePerformanceMetrics(IEnumerable<double> executionTimes, int successCount, int totalCount);

    /// <summary>
    /// Calculate median value from a collection of numerical values
    /// </summary>
    /// <param name="values">Collection of numerical values</param>
    /// <returns>Median value</returns>
    double CalculateMedian(IEnumerable<double> values);

    /// <summary>
    /// Calculate standard deviation for measuring data dispersion
    /// </summary>
    /// <param name="values">Collection of numerical values</param>
    /// <returns>Standard deviation value</returns>
    double CalculateStandardDeviation(IEnumerable<double> values);

    /// <summary>
    /// Analyze performance trends to determine improvement or degradation patterns
    /// </summary>
    /// <param name="historicalMetrics">Time-series performance data</param>
    /// <returns>Trend analysis result with direction, rate of change, and confidence level</returns>
    TrendAnalysisResult AnalyzeTrends(IEnumerable<HistoricalMetric> historicalMetrics);
}

/// <summary>
/// Historical performance metric for trend analysis
/// </summary>
public class HistoricalMetric
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public string MetricName { get; set; } = string.Empty;
}

/// <summary>
/// Result of trend analysis containing direction, rate of change, and confidence
/// </summary>
public class TrendAnalysisResult
{
    public TrendDirection Direction { get; set; }
    public double RateOfChange { get; set; }
    public double TrendConfidence { get; set; }
    public string TrendDescription { get; set; } = string.Empty;
    public List<string> TrendRecommendations { get; set; } = new();
}

/// <summary>
/// Direction of performance trend
/// </summary>
public enum TrendDirection
{
    Improving,
    Stable,
    Declining,
    Insufficient_Data
}