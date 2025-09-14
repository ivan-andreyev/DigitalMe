using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalMe.Services.Learning.ErrorLearning.Models;

namespace DigitalMe.Services.Learning.ErrorLearning;

/// <summary>
/// Service for managing optimization suggestions lifecycle
/// Follows SRP - focused only on optimization suggestion operations
/// </summary>
public interface IOptimizationSuggestionManagementService
{
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
}