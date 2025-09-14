using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DigitalMe.Services.Learning.ErrorLearning.Models;
using DigitalMe.Services.Learning.ErrorLearning.Repositories;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Learning.ErrorLearning;

/// <summary>
/// Service for learning statistics and metrics reporting
/// Implements SRP by focusing only on statistics aggregation and reporting
/// </summary>
public class LearningStatisticsService : ILearningStatisticsService
{
    private readonly ILogger<LearningStatisticsService> _logger;
    private readonly IErrorPatternRepository _errorPatternRepository;
    private readonly ILearningHistoryRepository _learningHistoryRepository;
    private readonly IOptimizationSuggestionRepository _optimizationSuggestionRepository;

    public LearningStatisticsService(
        ILogger<LearningStatisticsService> logger,
        IErrorPatternRepository errorPatternRepository,
        ILearningHistoryRepository learningHistoryRepository,
        IOptimizationSuggestionRepository optimizationSuggestionRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _errorPatternRepository = errorPatternRepository ?? throw new ArgumentNullException(nameof(errorPatternRepository));
        _learningHistoryRepository = learningHistoryRepository ?? throw new ArgumentNullException(nameof(learningHistoryRepository));
        _optimizationSuggestionRepository = optimizationSuggestionRepository ?? throw new ArgumentNullException(nameof(optimizationSuggestionRepository));
    }

    /// <inheritdoc />
    public async Task<LearningStatistics> GetLearningStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            _logger.LogInformation("Generating learning statistics from {FromDate} to {ToDate}", fromDate, toDate);

            // Get statistics from all repositories in parallel for performance
            var patternStatsTask = _errorPatternRepository.GetStatisticsAsync();
            var historyStatsTask = _learningHistoryRepository.GetStatisticsAsync(fromDate, toDate);
            var suggestionStatsTask = _optimizationSuggestionRepository.GetStatisticsAsync(fromDate, toDate);

            await Task.WhenAll(patternStatsTask, historyStatsTask, suggestionStatsTask);

            var patternStats = await patternStatsTask;
            var historyStats = await historyStatsTask;
            var suggestionStats = await suggestionStatsTask;

            // Build comprehensive learning statistics
            var statistics = new LearningStatistics
            {
                TotalErrorPatterns = GetIntValue(patternStats, "TotalPatterns"),
                TotalLearningEntries = GetIntValue(historyStats, "TotalEntries"),
                TotalOptimizationSuggestions = GetIntValue(suggestionStats, "TotalSuggestions"),
                UnanalyzedEntries = GetIntValue(historyStats, "UnanalyzedEntries"),
                PendingSuggestions = GetIntValue(suggestionStats, "PendingSuggestions"),

                // Extract category distributions
                TopErrorCategories = GetDictionaryValue<int>(patternStats, "CategoryDistribution"),
                TopErrorEndpoints = GetDictionaryValue<int>(patternStats, "EndpointDistribution"),

                // Calculate effectiveness metrics
                AveragePatternConfidence = GetDoubleValue(patternStats, "AverageConfidenceScore"),

                EffectivenessMetrics = CalculateEffectivenessMetrics(patternStats, historyStats, suggestionStats)
            };

            _logger.LogDebug("Generated learning statistics: {TotalPatterns} patterns, {TotalEntries} entries, {TotalSuggestions} suggestions",
                statistics.TotalErrorPatterns, statistics.TotalLearningEntries, statistics.TotalOptimizationSuggestions);

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate learning statistics");
            throw;
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Safely extracts integer value from statistics dictionary
    /// </summary>
    private int GetIntValue(Dictionary<string, object> stats, string key)
    {
        if (stats.TryGetValue(key, out var value) && value is int intValue)
        {
            return intValue;
        }

        return 0;
    }

    /// <summary>
    /// Safely extracts double value from statistics dictionary
    /// </summary>
    private double GetDoubleValue(Dictionary<string, object> stats, string key)
    {
        if (stats.TryGetValue(key, out var value))
        {
            return value switch
            {
                double doubleValue => doubleValue,
                int intValue => intValue,
                float floatValue => floatValue,
                _ => 0.0
            };
        }

        return 0.0;
    }

    /// <summary>
    /// Safely extracts dictionary value from statistics dictionary
    /// </summary>
    private Dictionary<string, T> GetDictionaryValue<T>(Dictionary<string, object> stats, string key)
    {
        if (stats.TryGetValue(key, out var value) && value is Dictionary<string, object> dict)
        {
            var result = new Dictionary<string, T>();

            foreach (var kvp in dict)
            {
                if (kvp.Value is T typedValue)
                {
                    result[kvp.Key] = typedValue;
                }
            }

            return result;
        }

        return new Dictionary<string, T>();
    }

    /// <summary>
    /// Calculates effectiveness metrics from aggregated statistics
    /// </summary>
    private Dictionary<string, double> CalculateEffectivenessMetrics(
        Dictionary<string, object> patternStats,
        Dictionary<string, object> historyStats,
        Dictionary<string, object> suggestionStats)
    {
        var metrics = new Dictionary<string, double>();

        // Analysis rate - percentage of entries that have been analyzed
        var totalEntries = GetIntValue(historyStats, "TotalEntries");
        var analyzedEntries = GetIntValue(historyStats, "AnalyzedEntries");

        if (totalEntries > 0)
        {
            metrics["AnalysisRate"] = Math.Round((double)analyzedEntries / totalEntries * 100, 2);
        }
        else
        {
            metrics["AnalysisRate"] = 0.0;
        }

        // Suggestion implementation rate
        var totalSuggestions = GetIntValue(suggestionStats, "TotalSuggestions");
        var implementedSuggestions = GetIntValue(suggestionStats, "ImplementedSuggestions");

        if (totalSuggestions > 0)
        {
            metrics["SuggestionImplementationRate"] = Math.Round((double)implementedSuggestions / totalSuggestions * 100, 2);
        }
        else
        {
            metrics["SuggestionImplementationRate"] = 0.0;
        }

        // Pattern recognition accuracy - based on average confidence scores
        var patternConfidence = GetDoubleValue(patternStats, "AverageConfidenceScore");
        var historyConfidence = GetDoubleValue(historyStats, "AverageConfidenceScore");

        metrics["PatternRecognitionAccuracy"] = Math.Round((patternConfidence + historyConfidence) / 2 * 100, 2);

        // Learning velocity - patterns per entry ratio
        var totalPatterns = GetIntValue(patternStats, "TotalPatterns");
        if (totalEntries > 0)
        {
            metrics["LearningVelocity"] = Math.Round((double)totalPatterns / totalEntries, 4);
        }
        else
        {
            metrics["LearningVelocity"] = 0.0;
        }

        // Suggestion quality - ratio of approved/implemented vs total
        var approvedSuggestions = GetIntValue(suggestionStats, "ApprovedSuggestions");
        if (totalSuggestions > 0)
        {
            metrics["SuggestionQuality"] = Math.Round((double)(approvedSuggestions + implementedSuggestions) / totalSuggestions * 100, 2);
        }
        else
        {
            metrics["SuggestionQuality"] = 0.0;
        }

        // Pattern effectiveness - average occurrences per pattern
        if (totalPatterns > 0 && totalEntries > 0)
        {
            metrics["PatternEffectiveness"] = Math.Round((double)totalEntries / totalPatterns, 2);
        }
        else
        {
            metrics["PatternEffectiveness"] = 0.0;
        }

        return metrics;
    }

    #endregion
}