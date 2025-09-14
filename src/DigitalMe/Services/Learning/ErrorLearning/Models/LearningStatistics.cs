using System.Collections.Generic;

namespace DigitalMe.Services.Learning.ErrorLearning.Models;

/// <summary>
/// Statistics and metrics about the error learning system performance
/// Provides insights into learning effectiveness, pattern recognition accuracy, and system health
/// </summary>
public class LearningStatistics
{
    /// <summary>
    /// Total number of error patterns identified
    /// </summary>
    public int TotalErrorPatterns { get; set; }

    /// <summary>
    /// Total number of learning history entries recorded
    /// </summary>
    public int TotalLearningEntries { get; set; }

    /// <summary>
    /// Total number of optimization suggestions generated
    /// </summary>
    public int TotalOptimizationSuggestions { get; set; }

    /// <summary>
    /// Number of learning entries that haven't been analyzed yet
    /// </summary>
    public int UnanalyzedEntries { get; set; }

    /// <summary>
    /// Number of optimization suggestions pending review
    /// </summary>
    public int PendingSuggestions { get; set; }

    /// <summary>
    /// Top error categories by occurrence count
    /// Key: Category name, Value: Occurrence count
    /// </summary>
    public Dictionary<string, int> TopErrorCategories { get; set; } = new();

    /// <summary>
    /// Top error endpoints by occurrence count
    /// Key: API endpoint, Value: Occurrence count
    /// </summary>
    public Dictionary<string, int> TopErrorEndpoints { get; set; } = new();

    /// <summary>
    /// Average confidence score across all error patterns (0.0 to 1.0)
    /// </summary>
    public double AveragePatternConfidence { get; set; }

    /// <summary>
    /// Various effectiveness metrics for the learning system
    /// Common metrics include:
    /// - AnalysisRate: Percentage of entries analyzed
    /// - SuggestionImplementationRate: Percentage of suggestions implemented
    /// - PatternRecognitionAccuracy: Accuracy of pattern matching
    /// - LearningVelocity: Rate of pattern discovery
    /// - SuggestionQuality: Quality score of generated suggestions
    /// - PatternEffectiveness: Effectiveness of pattern identification
    /// </summary>
    public Dictionary<string, double> EffectivenessMetrics { get; set; } = new();
}