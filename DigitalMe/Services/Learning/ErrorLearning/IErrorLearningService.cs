using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalMe.Services.Learning.ErrorLearning.Models;

namespace DigitalMe.Services.Learning.ErrorLearning;

/// <summary>
/// Interface for Error Learning System - learns from test failures and API errors
/// Provides pattern recognition, optimization suggestions, and failure analysis
/// Following ISP principle with focused responsibilities for learning operations
/// </summary>
public interface IErrorLearningService
{
    /// <summary>
    /// Records a new error occurrence for learning analysis
    /// </summary>
    /// <param name="source">Source of the error (e.g., "SelfTestingFramework", "AutoDocumentationParser")</param>
    /// <param name="errorMessage">Full error message or exception details</param>
    /// <param name="testCaseName">Name of the test case that failed (if applicable)</param>
    /// <param name="apiName">API name being tested when error occurred</param>
    /// <param name="httpMethod">HTTP method if applicable</param>
    /// <param name="apiEndpoint">API endpoint where error occurred</param>
    /// <param name="httpStatusCode">HTTP status code if applicable</param>
    /// <param name="requestDetails">Request details as JSON string</param>
    /// <param name="responseDetails">Response details as JSON string</param>
    /// <param name="stackTrace">Stack trace if available</param>
    /// <param name="environmentContext">Environment context as JSON string</param>
    /// <returns>Learning history entry created for this error</returns>
    Task<LearningHistoryEntry> RecordErrorAsync(
        string source, 
        string errorMessage, 
        string? testCaseName = null,
        string? apiName = null,
        string? httpMethod = null,
        string? apiEndpoint = null,
        int? httpStatusCode = null,
        string? requestDetails = null,
        string? responseDetails = null,
        string? stackTrace = null,
        string? environmentContext = null);

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

    /// <summary>
    /// Generates optimization suggestions based on learned error patterns
    /// </summary>
    /// <param name="errorPatternId">ID of the error pattern to generate suggestions for</param>
    /// <returns>List of optimization suggestions generated</returns>
    Task<List<OptimizationSuggestion>> GenerateOptimizationSuggestionsAsync(int errorPatternId);

    /// <summary>
    /// Gets optimization suggestions with optional filtering
    /// </summary>
    /// <param name="type">Filter by suggestion type</param>
    /// <param name="status">Filter by suggestion status</param>
    /// <param name="minPriority">Minimum priority level (1-5)</param>
    /// <param name="minConfidenceScore">Minimum confidence score (0.0-1.0)</param>
    /// <returns>List of matching optimization suggestions</returns>
    Task<List<OptimizationSuggestion>> GetOptimizationSuggestionsAsync(
        OptimizationType? type = null,
        SuggestionStatus? status = null,
        int? minPriority = null,
        double? minConfidenceScore = null);

    /// <summary>
    /// Updates the status of an optimization suggestion
    /// </summary>
    /// <param name="suggestionId">ID of the suggestion to update</param>
    /// <param name="status">New status</param>
    /// <param name="reviewerNotes">Optional reviewer notes</param>
    /// <returns>Updated optimization suggestion</returns>
    Task<OptimizationSuggestion> UpdateSuggestionStatusAsync(
        int suggestionId, 
        SuggestionStatus status, 
        string? reviewerNotes = null);

    /// <summary>
    /// Gets learning statistics and metrics
    /// </summary>
    /// <param name="fromDate">Start date for statistics (optional)</param>
    /// <param name="toDate">End date for statistics (optional)</param>
    /// <returns>Learning statistics object</returns>
    Task<LearningStatistics> GetLearningStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null);
}