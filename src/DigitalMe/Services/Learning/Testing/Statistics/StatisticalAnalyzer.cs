using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Learning.Testing.Statistics;

/// <summary>
/// Implementation of statistical analysis for performance metrics, confidence calculations, and trend analysis
/// Extracted from SelfTestingFramework God Class following Single Responsibility Principle
/// Focuses purely on mathematical and statistical computations without business logic
/// </summary>
public class StatisticalAnalyzer : IStatisticalAnalyzer
{
    private readonly ILogger<StatisticalAnalyzer> _logger;

    public StatisticalAnalyzer(ILogger<StatisticalAnalyzer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public double CalculateConfidenceScore(double successRate, int totalTests, IEnumerable<double> executionTimes)
    {
        try
        {
            if (totalTests == 0)
            {
                _logger.LogWarning("Cannot calculate confidence score with zero tests");
                return 0;
            }

            // Convert percentage to decimal
            var baseScore = successRate / 100.0;
            
            // Adjust based on number of tests (more tests = higher confidence)
            var testCountFactor = Math.Min(1.0, totalTests / 10.0);
            
            // Adjust based on execution consistency
            var executionTimesList = executionTimes.ToList();
            var consistencyFactor = CalculateConsistencyFactor(executionTimesList);
            
            var confidenceScore = baseScore * testCountFactor * consistencyFactor;
            
            _logger.LogDebug("Calculated confidence score: {Score:F3} (base: {BaseScore:F3}, test factor: {TestFactor:F3}, consistency: {ConsistencyFactor:F3})",
                confidenceScore, baseScore, testCountFactor, consistencyFactor);
                
            return Math.Max(0, Math.Min(1, confidenceScore));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating confidence score");
            return 0;
        }
    }

    /// <inheritdoc />
    public Dictionary<string, double> CalculatePerformanceMetrics(IEnumerable<double> executionTimes, int successCount, int totalCount)
    {
        try
        {
            var metrics = new Dictionary<string, double>();
            var timesList = executionTimes.ToList();
            
            if (!timesList.Any())
            {
                _logger.LogWarning("No execution times provided for performance metrics calculation");
                return metrics;
            }

            // Basic time statistics
            metrics["AverageExecutionTimeMs"] = timesList.Average();
            metrics["MinExecutionTimeMs"] = timesList.Min();
            metrics["MaxExecutionTimeMs"] = timesList.Max();
            metrics["MedianExecutionTimeMs"] = CalculateMedian(timesList);
            metrics["StandardDeviationMs"] = CalculateStandardDeviation(timesList);

            // Success rate metrics
            if (totalCount > 0)
            {
                metrics["SuccessRate"] = (double)successCount / totalCount;
                metrics["SuccessRatePercentage"] = (double)successCount / totalCount * 100;
                metrics["FailureRate"] = (double)(totalCount - successCount) / totalCount;
            }

            // Variability metrics
            var coefficient = metrics["AverageExecutionTimeMs"] > 0 
                ? metrics["StandardDeviationMs"] / metrics["AverageExecutionTimeMs"] 
                : 0;
            metrics["CoefficientOfVariation"] = coefficient;

            // Percentile calculations
            metrics["P50_ExecutionTimeMs"] = CalculatePercentile(timesList, 0.50);
            metrics["P90_ExecutionTimeMs"] = CalculatePercentile(timesList, 0.90);
            metrics["P95_ExecutionTimeMs"] = CalculatePercentile(timesList, 0.95);
            metrics["P99_ExecutionTimeMs"] = CalculatePercentile(timesList, 0.99);

            _logger.LogDebug("Calculated {MetricCount} performance metrics for {ExecutionCount} executions", 
                metrics.Count, timesList.Count);

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating performance metrics");
            return new Dictionary<string, double>();
        }
    }

    /// <inheritdoc />
    public double CalculateMedian(IEnumerable<double> values)
    {
        try
        {
            var sorted = values.OrderBy(v => v).ToList();
            var count = sorted.Count;
            
            if (count == 0)
            {
                _logger.LogWarning("Cannot calculate median of empty collection");
                return 0;
            }
            
            if (count % 2 == 0)
            {
                // Even number of elements: average of two middle values
                return (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0;
            }
            else
            {
                // Odd number of elements: middle value
                return sorted[count / 2];
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating median");
            return 0;
        }
    }

    /// <inheritdoc />
    public double CalculateStandardDeviation(IEnumerable<double> values)
    {
        try
        {
            var valueList = values.ToList();
            if (!valueList.Any())
            {
                _logger.LogWarning("Cannot calculate standard deviation of empty collection");
                return 0;
            }

            var average = valueList.Average();
            var sumOfSquaresOfDifferences = valueList.Sum(val => (val - average) * (val - average));
            var standardDeviation = Math.Sqrt(sumOfSquaresOfDifferences / valueList.Count);
            
            _logger.LogDebug("Calculated standard deviation: {StdDev:F3} for {Count} values (avg: {Avg:F3})", 
                standardDeviation, valueList.Count, average);
                
            return standardDeviation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating standard deviation");
            return 0;
        }
    }

    /// <inheritdoc />
    public TrendAnalysisResult AnalyzeTrends(IEnumerable<HistoricalMetric> historicalMetrics)
    {
        try
        {
            var metrics = historicalMetrics.OrderBy(m => m.Timestamp).ToList();
            
            if (metrics.Count < 2)
            {
                _logger.LogWarning("Insufficient historical data for trend analysis: {Count} metrics", metrics.Count);
                return new TrendAnalysisResult
                {
                    Direction = TrendDirection.Insufficient_Data,
                    TrendDescription = "Not enough historical data for trend analysis (minimum 2 data points required)"
                };
            }

            // Calculate linear regression for trend
            var trendAnalysis = CalculateLinearTrend(metrics);
            
            _logger.LogInformation("Trend analysis completed: {Direction} trend with {Confidence:F2}% confidence", 
                trendAnalysis.Direction, trendAnalysis.TrendConfidence * 100);

            return trendAnalysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing trends");
            return new TrendAnalysisResult
            {
                Direction = TrendDirection.Insufficient_Data,
                TrendDescription = "Error occurred during trend analysis"
            };
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Calculates consistency factor based on execution time variance
    /// </summary>
    private double CalculateConsistencyFactor(List<double> executionTimes)
    {
        if (!executionTimes.Any())
        {
            return 1.0;
        }

        var avgTime = executionTimes.Average();
        var consistencyFactor = executionTimes.All(t => Math.Abs(t - avgTime) < avgTime * 0.5) ? 1.0 : 0.9;
        
        return consistencyFactor;
    }

    /// <summary>
    /// Calculates the specified percentile from a collection of values
    /// </summary>
    /// <param name="values">Collection of values to calculate percentile from</param>
    /// <param name="percentile">Percentile to calculate (0.0 to 1.0)</param>
    /// <returns>The calculated percentile value</returns>
    private double CalculatePercentile(List<double> values, double percentile)
    {
        if (!values.Any())
        {
            return 0;
        }

        var sorted = values.OrderBy(v => v).ToList();
        var index = percentile * (sorted.Count - 1);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);

        if (lower == upper)
        {
            return sorted[lower];
        }

        var weight = index - lower;
        return sorted[lower] * (1 - weight) + sorted[upper] * weight;
    }

    /// <summary>
    /// Performs linear regression analysis to determine trend direction and rate of change
    /// </summary>
    /// <param name="metrics">Historical metrics ordered by timestamp</param>
    /// <returns>Trend analysis result with direction, rate, and confidence</returns>
    private TrendAnalysisResult CalculateLinearTrend(List<HistoricalMetric> metrics)
    {
        var n = metrics.Count;
        var xValues = Enumerable.Range(0, n).Select(i => (double)i).ToList();
        var yValues = metrics.Select(m => m.Value).ToList();

        // Calculate linear regression coefficients
        var xMean = xValues.Average();
        var yMean = yValues.Average();

        var numerator = xValues.Zip(yValues, (x, y) => (x - xMean) * (y - yMean)).Sum();
        var denominator = xValues.Sum(x => (x - xMean) * (x - xMean));

        if (Math.Abs(denominator) < 1e-10)
        {
            return new TrendAnalysisResult
            {
                Direction = TrendDirection.Stable,
                RateOfChange = 0,
                TrendConfidence = 0.5,
                TrendDescription = "No significant trend detected - values remain stable"
            };
        }

        var slope = numerator / denominator;
        var intercept = yMean - slope * xMean;

        // Calculate correlation coefficient for confidence
        var yVariance = yValues.Sum(y => (y - yMean) * (y - yMean));
        var rSquared = Math.Abs(yVariance) > 1e-10 ? (numerator * numerator) / (denominator * yVariance) : 0;
        var correlation = Math.Sqrt(Math.Abs(rSquared)) * Math.Sign(slope);

        // Determine trend direction
        var direction = DetermineTrendDirection(slope, correlation);
        var rateOfChange = CalculateRateOfChange(slope, yMean);

        return new TrendAnalysisResult
        {
            Direction = direction,
            RateOfChange = rateOfChange,
            TrendConfidence = Math.Abs(correlation),
            TrendDescription = GenerateTrendDescription(direction, rateOfChange, correlation),
            TrendRecommendations = GenerateTrendRecommendations(direction, rateOfChange, correlation)
        };
    }

    /// <summary>
    /// Determines trend direction based on slope and correlation
    /// </summary>
    private TrendDirection DetermineTrendDirection(double slope, double correlation)
    {
        const double significanceThreshold = 0.3; // 30% significance threshold for more reliable trend detection
        
        if (Math.Abs(correlation) < significanceThreshold)
        {
            return TrendDirection.Stable;
        }

        return slope > 0 ? TrendDirection.Improving : TrendDirection.Declining;
    }

    /// <summary>
    /// Calculates rate of change as percentage of mean
    /// </summary>
    private double CalculateRateOfChange(double slope, double mean)
    {
        return Math.Abs(mean) > 1e-10 ? (slope / mean) * 100 : 0;
    }

    /// <summary>
    /// Generates human-readable trend description
    /// </summary>
    private string GenerateTrendDescription(TrendDirection direction, double rateOfChange, double correlation)
    {
        var confidenceLevel = correlation switch
        {
            >= 0.9 => "very high",
            >= 0.7 => "high",
            >= 0.5 => "moderate",
            >= 0.3 => "low",
            _ => "very low"
        };

        return direction switch
        {
            TrendDirection.Improving => $"Performance is improving at {Math.Abs(rateOfChange):F1}% rate with {confidenceLevel} confidence",
            TrendDirection.Declining => $"Performance is declining at {Math.Abs(rateOfChange):F1}% rate with {confidenceLevel} confidence",
            TrendDirection.Stable => $"Performance remains stable with {confidenceLevel} confidence",
            _ => "Insufficient data for reliable trend analysis"
        };
    }

    /// <summary>
    /// Generates actionable recommendations based on trend analysis
    /// </summary>
    private List<string> GenerateTrendRecommendations(TrendDirection direction, double rateOfChange, double correlation)
    {
        var recommendations = new List<string>();

        switch (direction)
        {
            case TrendDirection.Improving when correlation >= 0.7:
                recommendations.Add("Continue current optimization strategies");
                recommendations.Add("Monitor performance to maintain improvement trajectory");
                break;

            case TrendDirection.Declining when correlation >= 0.7:
                recommendations.Add("Investigate causes of performance degradation");
                recommendations.Add("Implement corrective measures immediately");
                if (Math.Abs(rateOfChange) > 10)
                {
                    recommendations.Add("Critical performance decline detected - urgent intervention required");
                }
                break;

            case TrendDirection.Stable when correlation >= 0.5:
                recommendations.Add("Performance is stable - consider optimization opportunities");
                recommendations.Add("Establish baseline metrics for future comparison");
                break;

            default:
                recommendations.Add("Collect more performance data for reliable trend analysis");
                recommendations.Add("Consider increasing measurement frequency for better insights");
                break;
        }

        return recommendations;
    }

    #endregion
}