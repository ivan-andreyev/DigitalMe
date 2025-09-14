using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalMe.Services.Learning.ErrorLearning.Models;

namespace DigitalMe.Services.Learning.ErrorLearning.Repositories;

/// <summary>
/// Repository interface for Optimization Suggestion data access operations
/// Provides CRUD operations and specialized queries for optimization suggestions
/// </summary>
public interface IOptimizationSuggestionRepository
{
    /// <summary>
    /// Creates a new optimization suggestion
    /// </summary>
    /// <param name="suggestion">Optimization suggestion to create</param>
    /// <returns>Created suggestion with assigned ID</returns>
    Task<OptimizationSuggestion> CreateAsync(OptimizationSuggestion suggestion);

    /// <summary>
    /// Creates multiple optimization suggestions in a batch
    /// </summary>
    /// <param name="suggestions">List of optimization suggestions to create</param>
    /// <returns>List of created suggestions with assigned IDs</returns>
    Task<List<OptimizationSuggestion>> CreateBatchAsync(List<OptimizationSuggestion> suggestions);

    /// <summary>
    /// Updates an existing optimization suggestion
    /// </summary>
    /// <param name="suggestion">Suggestion to update</param>
    /// <returns>Updated suggestion</returns>
    Task<OptimizationSuggestion> UpdateAsync(OptimizationSuggestion suggestion);

    /// <summary>
    /// Gets an optimization suggestion by its ID
    /// </summary>
    /// <param name="id">Suggestion ID</param>
    /// <returns>Optimization suggestion if found, null otherwise</returns>
    Task<OptimizationSuggestion?> GetByIdAsync(int id);

    /// <summary>
    /// Gets optimization suggestions with optional filtering
    /// </summary>
    /// <param name="type">Filter by suggestion type</param>
    /// <param name="status">Filter by suggestion status</param>
    /// <param name="minPriority">Minimum priority level (1-5)</param>
    /// <param name="minConfidenceScore">Minimum confidence score (0.0-1.0)</param>
    /// <param name="limit">Maximum number of suggestions to return</param>
    /// <returns>List of matching optimization suggestions</returns>
    Task<List<OptimizationSuggestion>> GetSuggestionsAsync(
        OptimizationType? type = null,
        SuggestionStatus? status = null,
        int? minPriority = null,
        double? minConfidenceScore = null,
        int limit = 50);

    /// <summary>
    /// Gets optimization suggestions for a specific error pattern
    /// </summary>
    /// <param name="errorPatternId">ID of the error pattern</param>
    /// <param name="includeImplemented">Whether to include implemented suggestions</param>
    /// <param name="limit">Maximum number of suggestions to return</param>
    /// <returns>List of optimization suggestions for the error pattern</returns>
    Task<List<OptimizationSuggestion>> GetByErrorPatternAsync(
        int errorPatternId,
        bool includeImplemented = false,
        int limit = 20);

    /// <summary>
    /// Gets pending optimization suggestions that need review
    /// </summary>
    /// <param name="limit">Maximum number of suggestions to return</param>
    /// <returns>List of pending optimization suggestions</returns>
    Task<List<OptimizationSuggestion>> GetPendingSuggestionsAsync(int limit = 50);

    /// <summary>
    /// Gets high-priority optimization suggestions
    /// </summary>
    /// <param name="minPriority">Minimum priority level (1-5)</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="limit">Maximum number of suggestions to return</param>
    /// <returns>List of high-priority optimization suggestions</returns>
    Task<List<OptimizationSuggestion>> GetHighPrioritySuggestionsAsync(
        int minPriority = 4,
        SuggestionStatus? status = null,
        int limit = 30);

    /// <summary>
    /// Gets optimization suggestions by target component
    /// </summary>
    /// <param name="targetComponent">Component name to filter by</param>
    /// <param name="limit">Maximum number of suggestions to return</param>
    /// <returns>List of optimization suggestions for the component</returns>
    Task<List<OptimizationSuggestion>> GetByTargetComponentAsync(string targetComponent, int limit = 20);

    /// <summary>
    /// Gets optimization suggestions grouped by type
    /// </summary>
    /// <param name="status">Optional status filter</param>
    /// <returns>Dictionary with optimization types as keys and suggestion lists as values</returns>
    Task<Dictionary<OptimizationType, List<OptimizationSuggestion>>> GetGroupedByTypeAsync(
        SuggestionStatus? status = null);

    /// <summary>
    /// Updates the status of an optimization suggestion
    /// </summary>
    /// <param name="suggestionId">ID of the suggestion to update</param>
    /// <param name="status">New status</param>
    /// <param name="reviewerNotes">Optional reviewer notes</param>
    /// <returns>Updated optimization suggestion</returns>
    Task<OptimizationSuggestion> UpdateStatusAsync(
        int suggestionId,
        SuggestionStatus status,
        string? reviewerNotes = null);

    /// <summary>
    /// Updates multiple suggestion statuses in a batch
    /// </summary>
    /// <param name="updates">List of suggestion updates</param>
    /// <returns>Number of suggestions updated</returns>
    Task<int> UpdateStatusBatchAsync(List<(int SuggestionId, SuggestionStatus Status, string? ReviewerNotes)> updates);

    /// <summary>
    /// Deletes an optimization suggestion
    /// </summary>
    /// <param name="id">ID of the suggestion to delete</param>
    /// <returns>True if deleted successfully, false if not found</returns>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Deletes old rejected or implemented suggestions to manage storage
    /// </summary>
    /// <param name="olderThanDays">Delete suggestions older than this many days</param>
    /// <param name="statuses">Statuses to delete (default: Rejected, Implemented)</param>
    /// <returns>Number of suggestions deleted</returns>
    Task<int> DeleteOldSuggestionsAsync(
        int olderThanDays = 180,
        List<SuggestionStatus>? statuses = null);

    /// <summary>
    /// Gets count of optimization suggestions matching criteria
    /// </summary>
    /// <param name="type">Optional type filter</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="errorPatternId">Optional error pattern ID filter</param>
    /// <returns>Count of matching suggestions</returns>
    Task<int> GetCountAsync(
        OptimizationType? type = null,
        SuggestionStatus? status = null,
        int? errorPatternId = null);

    /// <summary>
    /// Gets optimization suggestion statistics and metrics
    /// </summary>
    /// <param name="fromDate">Optional start date for statistics</param>
    /// <param name="toDate">Optional end date for statistics</param>
    /// <returns>Dictionary with various statistics</returns>
    Task<Dictionary<string, object>> GetStatisticsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>
    /// Gets suggestions that are similar to the provided suggestion
    /// Useful for avoiding duplicate suggestions
    /// </summary>
    /// <param name="title">Title to match against</param>
    /// <param name="type">Optimization type to match</param>
    /// <param name="targetComponent">Optional target component to match</param>
    /// <param name="limit">Maximum number of similar suggestions to return</param>
    /// <returns>List of similar optimization suggestions</returns>
    Task<List<OptimizationSuggestion>> GetSimilarSuggestionsAsync(
        string title,
        OptimizationType type,
        string? targetComponent = null,
        int limit = 5);

    /// <summary>
    /// Updates the confidence score of an optimization suggestion
    /// </summary>
    /// <param name="suggestionId">ID of the suggestion to update</param>
    /// <param name="confidenceScore">New confidence score (0.0-1.0)</param>
    /// <returns>Updated optimization suggestion</returns>
    Task<OptimizationSuggestion> UpdateConfidenceScoreAsync(int suggestionId, double confidenceScore);
}