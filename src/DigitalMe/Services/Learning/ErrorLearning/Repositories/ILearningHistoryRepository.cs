using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalMe.Services.Learning.ErrorLearning.Models;

namespace DigitalMe.Services.Learning.ErrorLearning.Repositories;

/// <summary>
/// Repository interface for Learning History Entry data access operations
/// Provides CRUD operations and specialized queries for error learning history
/// </summary>
public interface ILearningHistoryRepository
{
    /// <summary>
    /// Creates a new learning history entry
    /// </summary>
    /// <param name="entry">Learning history entry to create</param>
    /// <returns>Created entry with assigned ID</returns>
    Task<LearningHistoryEntry> CreateAsync(LearningHistoryEntry entry);

    /// <summary>
    /// Creates multiple learning history entries in a batch
    /// </summary>
    /// <param name="entries">List of learning history entries to create</param>
    /// <returns>List of created entries with assigned IDs</returns>
    Task<List<LearningHistoryEntry>> CreateBatchAsync(List<LearningHistoryEntry> entries);

    /// <summary>
    /// Updates an existing learning history entry
    /// </summary>
    /// <param name="entry">Entry to update</param>
    /// <returns>Updated entry</returns>
    Task<LearningHistoryEntry> UpdateAsync(LearningHistoryEntry entry);

    /// <summary>
    /// Gets a learning history entry by its ID
    /// </summary>
    /// <param name="id">Entry ID</param>
    /// <returns>Learning history entry if found, null otherwise</returns>
    Task<LearningHistoryEntry?> GetByIdAsync(int id);

    /// <summary>
    /// Gets learning history entries for a specific error pattern
    /// </summary>
    /// <param name="errorPatternId">ID of the error pattern</param>
    /// <param name="limit">Maximum number of entries to return</param>
    /// <param name="includeAnalyzed">Whether to include already analyzed entries</param>
    /// <returns>List of learning history entries</returns>
    Task<List<LearningHistoryEntry>> GetByErrorPatternAsync(
        int errorPatternId, 
        int limit = 50, 
        bool includeAnalyzed = true);

    /// <summary>
    /// Gets unanalyzed learning history entries for pattern analysis
    /// </summary>
    /// <param name="batchSize">Maximum number of entries to return</param>
    /// <param name="source">Optional filter by error source</param>
    /// <returns>List of unanalyzed learning history entries</returns>
    Task<List<LearningHistoryEntry>> GetUnanalyzedEntriesAsync(
        int batchSize = 100,
        string? source = null);

    /// <summary>
    /// Gets learning history entries by source system
    /// </summary>
    /// <param name="source">Source system name</param>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    /// <param name="limit">Maximum number of entries to return</param>
    /// <returns>List of learning history entries from the specified source</returns>
    Task<List<LearningHistoryEntry>> GetBySourceAsync(
        string source,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int limit = 100);

    /// <summary>
    /// Gets learning history entries for a specific API
    /// </summary>
    /// <param name="apiName">API name to filter by</param>
    /// <param name="limit">Maximum number of entries to return</param>
    /// <returns>List of learning history entries for the API</returns>
    Task<List<LearningHistoryEntry>> GetByApiNameAsync(string apiName, int limit = 50);

    /// <summary>
    /// Gets recent learning history entries
    /// </summary>
    /// <param name="hours">Number of hours to look back</param>
    /// <param name="limit">Maximum number of entries to return</param>
    /// <returns>List of recent learning history entries</returns>
    Task<List<LearningHistoryEntry>> GetRecentEntriesAsync(int hours = 24, int limit = 100);

    /// <summary>
    /// Marks learning history entries as analyzed
    /// </summary>
    /// <param name="entryIds">List of entry IDs to mark as analyzed</param>
    /// <param name="contributedToPattern">Whether these entries contributed to pattern recognition</param>
    /// <param name="confidenceScore">Confidence score for the analysis</param>
    /// <returns>Number of entries updated</returns>
    Task<int> MarkAsAnalyzedAsync(
        List<int> entryIds, 
        bool contributedToPattern = true,
        double confidenceScore = 0.8);

    /// <summary>
    /// Deletes old learning history entries to manage storage
    /// </summary>
    /// <param name="olderThanDays">Delete entries older than this many days</param>
    /// <param name="keepMinimum">Minimum number of entries to keep per error pattern</param>
    /// <returns>Number of entries deleted</returns>
    Task<int> DeleteOldEntriesAsync(int olderThanDays = 90, int keepMinimum = 5);

    /// <summary>
    /// Gets count of learning history entries matching criteria
    /// </summary>
    /// <param name="errorPatternId">Optional error pattern ID filter</param>
    /// <param name="source">Optional source filter</param>
    /// <param name="isAnalyzed">Optional filter by analysis status</param>
    /// <returns>Count of matching entries</returns>
    Task<int> GetCountAsync(
        int? errorPatternId = null,
        string? source = null,
        bool? isAnalyzed = null);

    /// <summary>
    /// Gets learning history statistics
    /// </summary>
    /// <param name="fromDate">Optional start date for statistics</param>
    /// <param name="toDate">Optional end date for statistics</param>
    /// <returns>Dictionary with various statistics</returns>
    Task<Dictionary<string, object>> GetStatisticsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null);
}