using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalMe.Services.Learning.ErrorLearning.Models;

namespace DigitalMe.Services.Learning.ErrorLearning;

/// <summary>
/// Service for analyzing error patterns and performing machine learning operations
/// Follows SRP - focused only on pattern analysis and ML algorithms
/// </summary>
public interface IPatternAnalysisService
{
    /// <summary>
    /// Analyzes recorded errors to identify patterns and update existing patterns
    /// </summary>
    /// <param name="batchSize">Maximum number of unanalyzed entries to process in one batch</param>
    /// <returns>Number of patterns created or updated</returns>
    Task<int> AnalyzeErrorPatternsAsync(int batchSize = 100);

    /// <summary>
    /// Gets error patterns matching the specified criteria
    /// </summary>
    /// <param name="category">Filter by error category</param>
    /// <param name="apiEndpoint">Filter by API endpoint</param>
    /// <param name="minOccurrenceCount">Minimum occurrence count</param>
    /// <param name="minSeverityLevel">Minimum severity level (1-5)</param>
    /// <param name="minConfidenceScore">Minimum confidence score (0.0-1.0)</param>
    /// <returns>List of matching error patterns</returns>
    Task<List<ErrorPattern>> GetErrorPatternsAsync(
        string? category = null,
        string? apiEndpoint = null,
        int? minOccurrenceCount = null,
        int? minSeverityLevel = null,
        double? minConfidenceScore = null);

    /// <summary>
    /// Gets learning history for a specific error pattern
    /// </summary>
    /// <param name="errorPatternId">ID of the error pattern</param>
    /// <param name="limit">Maximum number of entries to return</param>
    /// <returns>List of learning history entries</returns>
    Task<List<LearningHistoryEntry>> GetLearningHistoryAsync(int errorPatternId, int limit = 50);
}