using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DigitalMe.Data;
using DigitalMe.Services.Learning.ErrorLearning.Models;

namespace DigitalMe.Services.Learning.ErrorLearning.Repositories;

/// <summary>
/// Entity Framework implementation of ILearningHistoryRepository
/// Provides optimized data access for learning history entries
/// </summary>
public class LearningHistoryRepository : ILearningHistoryRepository
{
    private readonly DigitalMeDbContext _context;
    private readonly ILogger<LearningHistoryRepository> _logger;

    public LearningHistoryRepository(
        DigitalMeDbContext context,
        ILogger<LearningHistoryRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<LearningHistoryEntry> CreateAsync(LearningHistoryEntry entry)
    {
        if (entry == null)
            throw new ArgumentNullException(nameof(entry));

        try
        {
            _context.LearningHistoryEntries.Add(entry);
            await _context.SaveChangesAsync();
            
            _logger.LogDebug("Created learning history entry with ID {EntryId} for pattern {ErrorPatternId}", 
                entry.Id, entry.ErrorPatternId);
            
            return entry;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to create learning history entry for pattern {ErrorPatternId}", entry.ErrorPatternId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<LearningHistoryEntry>> CreateBatchAsync(List<LearningHistoryEntry> entries)
    {
        if (entries == null || entries.Count == 0)
            throw new ArgumentException("Entries list cannot be null or empty", nameof(entries));

        try
        {
            _context.LearningHistoryEntries.AddRange(entries);
            await _context.SaveChangesAsync();
            
            _logger.LogDebug("Created {Count} learning history entries in batch", entries.Count);
            
            return entries;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to create batch of {Count} learning history entries", entries.Count);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<LearningHistoryEntry> UpdateAsync(LearningHistoryEntry entry)
    {
        if (entry == null)
            throw new ArgumentNullException(nameof(entry));

        try
        {
            _context.LearningHistoryEntries.Update(entry);
            await _context.SaveChangesAsync();
            
            _logger.LogDebug("Updated learning history entry with ID {EntryId}", entry.Id);
            
            return entry;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to update learning history entry with ID {EntryId}", entry.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<LearningHistoryEntry?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.LearningHistoryEntries
                .Include(lh => lh.ErrorPattern)
                .FirstOrDefaultAsync(lh => lh.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get learning history entry by ID {EntryId}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<LearningHistoryEntry>> GetByErrorPatternAsync(
        int errorPatternId, 
        int limit = 50, 
        bool includeAnalyzed = true)
    {
        try
        {
            var query = _context.LearningHistoryEntries
                .Where(lh => lh.ErrorPatternId == errorPatternId);

            if (!includeAnalyzed)
            {
                query = query.Where(lh => !lh.IsAnalyzed);
            }

            return await query
                .OrderByDescending(lh => lh.Timestamp)
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get learning history entries for pattern {ErrorPatternId}", errorPatternId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<LearningHistoryEntry>> GetUnanalyzedEntriesAsync(
        int batchSize = 100,
        string? source = null)
    {
        try
        {
            var query = _context.LearningHistoryEntries
                .Where(lh => !lh.IsAnalyzed);

            if (!string.IsNullOrWhiteSpace(source))
            {
                query = query.Where(lh => lh.Source == source);
            }

            return await query
                .OrderBy(lh => lh.Timestamp) // Process oldest first
                .Take(batchSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get unanalyzed learning history entries");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<LearningHistoryEntry>> GetBySourceAsync(
        string source,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int limit = 100)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source cannot be null or whitespace", nameof(source));

        try
        {
            var query = _context.LearningHistoryEntries
                .Where(lh => lh.Source == source);

            if (fromDate.HasValue)
            {
                query = query.Where(lh => lh.Timestamp >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(lh => lh.Timestamp <= toDate.Value);
            }

            return await query
                .OrderByDescending(lh => lh.Timestamp)
                .Take(limit)
                .Include(lh => lh.ErrorPattern)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get learning history entries by source {Source}", source);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<LearningHistoryEntry>> GetByApiNameAsync(string apiName, int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(apiName))
            throw new ArgumentException("API name cannot be null or whitespace", nameof(apiName));

        try
        {
            return await _context.LearningHistoryEntries
                .Where(lh => lh.ApiName == apiName)
                .OrderByDescending(lh => lh.Timestamp)
                .Take(limit)
                .Include(lh => lh.ErrorPattern)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get learning history entries by API name {ApiName}", apiName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<LearningHistoryEntry>> GetRecentEntriesAsync(int hours = 24, int limit = 100)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-hours);

        try
        {
            return await _context.LearningHistoryEntries
                .Where(lh => lh.Timestamp >= cutoffTime)
                .OrderByDescending(lh => lh.Timestamp)
                .Take(limit)
                .Include(lh => lh.ErrorPattern)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recent learning history entries from last {Hours} hours", hours);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> MarkAsAnalyzedAsync(
        List<int> entryIds, 
        bool contributedToPattern = true,
        double confidenceScore = 0.8)
    {
        if (entryIds == null || entryIds.Count == 0)
            throw new ArgumentException("Entry IDs list cannot be null or empty", nameof(entryIds));

        try
        {
            var affectedRows = await _context.LearningHistoryEntries
                .Where(lh => entryIds.Contains(lh.Id))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(lh => lh.IsAnalyzed, true)
                    .SetProperty(lh => lh.ContributedToPattern, contributedToPattern)
                    .SetProperty(lh => lh.ConfidenceScore, confidenceScore));

            _logger.LogDebug("Marked {Count} learning history entries as analyzed", affectedRows);
            
            return affectedRows;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark {Count} entries as analyzed", entryIds.Count);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> DeleteOldEntriesAsync(int olderThanDays = 90, int keepMinimum = 5)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);

        try
        {
            // Get entries to delete, ensuring we keep minimum per pattern
            var entriesToDelete = await _context.LearningHistoryEntries
                .Where(lh => lh.Timestamp < cutoffDate)
                .GroupBy(lh => lh.ErrorPatternId)
                .SelectMany(g => g.OrderBy(lh => lh.Timestamp).Take(g.Count() - keepMinimum))
                .Where(lh => lh.Timestamp < cutoffDate) // Double-check the date filter
                .Select(lh => lh.Id)
                .ToListAsync();

            if (entriesToDelete.Count == 0)
                return 0;

            var deletedCount = await _context.LearningHistoryEntries
                .Where(lh => entriesToDelete.Contains(lh.Id))
                .ExecuteDeleteAsync();

            _logger.LogInformation("Deleted {Count} old learning history entries older than {Days} days", 
                deletedCount, olderThanDays);
            
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete old learning history entries");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> GetCountAsync(
        int? errorPatternId = null,
        string? source = null,
        bool? isAnalyzed = null)
    {
        try
        {
            var query = _context.LearningHistoryEntries.AsQueryable();

            if (errorPatternId.HasValue)
            {
                query = query.Where(lh => lh.ErrorPatternId == errorPatternId.Value);
            }

            if (!string.IsNullOrWhiteSpace(source))
            {
                query = query.Where(lh => lh.Source == source);
            }

            if (isAnalyzed.HasValue)
            {
                query = query.Where(lh => lh.IsAnalyzed == isAnalyzed.Value);
            }

            return await query.CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get learning history entry count");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, object>> GetStatisticsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        try
        {
            var statistics = new Dictionary<string, object>();

            var query = _context.LearningHistoryEntries.AsQueryable();

            if (fromDate.HasValue)
            {
                query = query.Where(lh => lh.Timestamp >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(lh => lh.Timestamp <= toDate.Value);
            }

            // Basic counts
            statistics["TotalEntries"] = await query.CountAsync();
            statistics["AnalyzedEntries"] = await query.CountAsync(lh => lh.IsAnalyzed);
            statistics["UnanalyzedEntries"] = await query.CountAsync(lh => !lh.IsAnalyzed);
            statistics["EntriesContributedToPatterns"] = await query.CountAsync(lh => lh.ContributedToPattern);

            // Source distribution
            var sourceStats = await query
                .GroupBy(lh => lh.Source)
                .Select(g => new { Source = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToDictionaryAsync(x => x.Source, x => (object)x.Count);
            statistics["SourceDistribution"] = sourceStats;

            // API distribution
            var apiStats = await query
                .Where(lh => !string.IsNullOrEmpty(lh.ApiName))
                .GroupBy(lh => lh.ApiName)
                .Select(g => new { ApiName = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToDictionaryAsync(x => x.ApiName ?? "Unknown", x => (object)x.Count);
            statistics["ApiDistribution"] = apiStats;

            // Recent activity (last 24 hours)
            var recentCutoff = DateTime.UtcNow.AddHours(-24);
            statistics["RecentEntries"] = await query.CountAsync(lh => lh.Timestamp >= recentCutoff);

            // Average confidence score
            var avgConfidence = await query
                .Where(lh => lh.ConfidenceScore > 0)
                .AverageAsync(lh => (double?)lh.ConfidenceScore) ?? 0.0;
            statistics["AverageConfidenceScore"] = Math.Round(avgConfidence, 3);

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get learning history statistics");
            throw;
        }
    }
}