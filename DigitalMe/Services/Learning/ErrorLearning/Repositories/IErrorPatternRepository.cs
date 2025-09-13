using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalMe.Services.Learning.ErrorLearning.Models;

namespace DigitalMe.Services.Learning.ErrorLearning.Repositories;

/// <summary>
/// Repository interface for Error Pattern data access operations
/// Provides CRUD operations and specialized queries for error patterns
/// Following Repository pattern with clean separation of concerns
/// </summary>
public interface IErrorPatternRepository
{
    /// <summary>
    /// Creates a new error pattern in the database
    /// </summary>
    /// <param name="errorPattern">Error pattern to create</param>
    /// <returns>Created error pattern with assigned ID</returns>
    Task<ErrorPattern> CreateAsync(ErrorPattern errorPattern);

    /// <summary>
    /// Updates an existing error pattern
    /// </summary>
    /// <param name="errorPattern">Error pattern to update</param>
    /// <returns>Updated error pattern</returns>
    Task<ErrorPattern> UpdateAsync(ErrorPattern errorPattern);

    /// <summary>
    /// Gets an error pattern by its ID
    /// </summary>
    /// <param name="id">Error pattern ID</param>
    /// <returns>Error pattern if found, null otherwise</returns>
    Task<ErrorPattern?> GetByIdAsync(int id);

    /// <summary>
    /// Gets an error pattern by its pattern hash (unique identifier)
    /// </summary>
    /// <param name="patternHash">Pattern hash to search for</param>
    /// <returns>Error pattern if found, null otherwise</returns>
    Task<ErrorPattern?> GetByPatternHashAsync(string patternHash);

    /// <summary>
    /// Gets error patterns matching the specified criteria
    /// </summary>
    /// <param name="category">Filter by error category</param>
    /// <param name="apiEndpoint">Filter by API endpoint</param>
    /// <param name="minOccurrenceCount">Minimum occurrence count</param>
    /// <param name="minSeverityLevel">Minimum severity level (1-5)</param>
    /// <param name="minConfidenceScore">Minimum confidence score (0.0-1.0)</param>
    /// <param name="limit">Maximum number of results to return</param>
    /// <returns>List of matching error patterns</returns>
    Task<List<ErrorPattern>> GetPatternsAsync(
        string? category = null,
        string? apiEndpoint = null,
        int? minOccurrenceCount = null,
        int? minSeverityLevel = null,
        double? minConfidenceScore = null,
        int limit = 100);

    /// <summary>
    /// Gets error patterns for similarity matching during pattern recognition
    /// Returns patterns that might be similar to new errors for analysis
    /// </summary>
    /// <param name="category">Primary category to focus search</param>
    /// <param name="subcategory">Subcategory for more focused results</param>
    /// <param name="limit">Maximum number of patterns to return</param>
    /// <returns>List of error patterns for similarity analysis</returns>
    Task<List<ErrorPattern>> GetPatternsForSimilarityAnalysisAsync(
        string category,
        string? subcategory = null,
        int limit = 50);

    /// <summary>
    /// Gets the most frequent error patterns by occurrence count
    /// Useful for identifying the most problematic patterns
    /// </summary>
    /// <param name="limit">Maximum number of patterns to return</param>
    /// <param name="minSeverityLevel">Minimum severity level to include</param>
    /// <returns>List of error patterns ordered by occurrence count</returns>
    Task<List<ErrorPattern>> GetMostFrequentPatternsAsync(int limit = 20, int minSeverityLevel = 1);

    /// <summary>
    /// Gets error patterns that haven't been updated recently
    /// Useful for identifying stale patterns that may need review
    /// </summary>
    /// <param name="olderThanDays">Number of days to consider as stale</param>
    /// <param name="limit">Maximum number of patterns to return</param>
    /// <returns>List of stale error patterns</returns>
    Task<List<ErrorPattern>> GetStalePatternsAsync(int olderThanDays = 30, int limit = 50);

    /// <summary>
    /// Gets error patterns by API endpoint for endpoint-specific analysis
    /// </summary>
    /// <param name="apiEndpoint">API endpoint to filter by</param>
    /// <param name="limit">Maximum number of patterns to return</param>
    /// <returns>List of error patterns for the specified endpoint</returns>
    Task<List<ErrorPattern>> GetPatternsByEndpointAsync(string apiEndpoint, int limit = 50);

    /// <summary>
    /// Deletes an error pattern and all associated learning history
    /// Use with caution as this permanently removes learning data
    /// </summary>
    /// <param name="id">ID of the error pattern to delete</param>
    /// <returns>True if deleted successfully, false if not found</returns>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Gets count of error patterns matching the specified criteria
    /// Useful for pagination and statistics
    /// </summary>
    /// <param name="category">Filter by error category</param>
    /// <param name="apiEndpoint">Filter by API endpoint</param>
    /// <param name="minSeverityLevel">Minimum severity level</param>
    /// <returns>Count of matching error patterns</returns>
    Task<int> GetCountAsync(
        string? category = null,
        string? apiEndpoint = null,
        int? minSeverityLevel = null);

    /// <summary>
    /// Gets error pattern statistics for reporting and analysis
    /// </summary>
    /// <returns>Dictionary with various statistics about error patterns</returns>
    Task<Dictionary<string, object>> GetStatisticsAsync();

    /// <summary>
    /// Gets error patterns filtered by category
    /// </summary>
    /// <param name="category">Category to filter by</param>
    /// <param name="limit">Maximum number of patterns to return</param>
    /// <returns>List of error patterns in the specified category</returns>
    Task<List<ErrorPattern>> GetPatternsByCategoryAsync(string category, int limit = 50);
}