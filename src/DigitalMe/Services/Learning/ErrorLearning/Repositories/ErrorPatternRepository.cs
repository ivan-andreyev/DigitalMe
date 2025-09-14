using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DigitalMe.Data;
using DigitalMe.Services.Learning.ErrorLearning.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Learning.ErrorLearning.Repositories;

/// <summary>
/// Entity Framework implementation of IErrorPatternRepository
/// Provides optimized data access for error patterns with PostgreSQL-specific optimizations
/// </summary>
public class ErrorPatternRepository : IErrorPatternRepository
{
    private readonly DigitalMeDbContext _context;
    private readonly ILogger<ErrorPatternRepository> _logger;

    public ErrorPatternRepository(
        DigitalMeDbContext context,
        ILogger<ErrorPatternRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ErrorPattern> CreateAsync(ErrorPattern errorPattern)
    {
        if (errorPattern == null)
            throw new ArgumentNullException(nameof(errorPattern));

        try
        {
            _context.ErrorPatterns.Add(errorPattern);
            await _context.SaveChangesAsync();
            
            _logger.LogDebug("Created error pattern with ID {ErrorPatternId} and hash {PatternHash}", 
                errorPattern.Id, errorPattern.PatternHash);
            
            return errorPattern;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to create error pattern with hash {PatternHash}", errorPattern.PatternHash);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ErrorPattern> UpdateAsync(ErrorPattern errorPattern)
    {
        if (errorPattern == null)
            throw new ArgumentNullException(nameof(errorPattern));

        try
        {
            _context.ErrorPatterns.Update(errorPattern);
            await _context.SaveChangesAsync();
            
            _logger.LogDebug("Updated error pattern with ID {ErrorPatternId}", errorPattern.Id);
            
            return errorPattern;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to update error pattern with ID {ErrorPatternId}", errorPattern.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ErrorPattern?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.ErrorPatterns
                .Include(ep => ep.LearningHistory)
                .Include(ep => ep.OptimizationSuggestions)
                .FirstOrDefaultAsync(ep => ep.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get error pattern by ID {ErrorPatternId}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ErrorPattern?> GetByPatternHashAsync(string patternHash)
    {
        if (string.IsNullOrWhiteSpace(patternHash))
            throw new ArgumentException("Pattern hash cannot be null or whitespace", nameof(patternHash));

        try
        {
            return await _context.ErrorPatterns
                .Include(ep => ep.LearningHistory)
                .Include(ep => ep.OptimizationSuggestions)
                .FirstOrDefaultAsync(ep => ep.PatternHash == patternHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get error pattern by hash {PatternHash}", patternHash);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<ErrorPattern>> GetPatternsAsync(
        string? category = null,
        string? apiEndpoint = null,
        int? minOccurrenceCount = null,
        int? minSeverityLevel = null,
        double? minConfidenceScore = null,
        int limit = 100)
    {
        try
        {
            var query = _context.ErrorPatterns.AsQueryable();

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(ep => ep.Category == category);
            }

            if (!string.IsNullOrWhiteSpace(apiEndpoint))
            {
                query = query.Where(ep => ep.ApiEndpoint == apiEndpoint);
            }

            if (minOccurrenceCount.HasValue)
            {
                query = query.Where(ep => ep.OccurrenceCount >= minOccurrenceCount.Value);
            }

            if (minSeverityLevel.HasValue)
            {
                query = query.Where(ep => ep.SeverityLevel >= minSeverityLevel.Value);
            }

            if (minConfidenceScore.HasValue)
            {
                query = query.Where(ep => ep.ConfidenceScore >= minConfidenceScore.Value);
            }

            return await query
                .OrderByDescending(ep => ep.OccurrenceCount)
                .ThenByDescending(ep => ep.LastObserved)
                .Take(limit)
                .Include(ep => ep.LearningHistory.Take(5)) // Include latest 5 entries for context
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get error patterns with filters");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<ErrorPattern>> GetPatternsForSimilarityAnalysisAsync(
        string category,
        string? subcategory = null,
        int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be null or whitespace", nameof(category));

        try
        {
            var query = _context.ErrorPatterns
                .Where(ep => ep.Category == category);

            if (!string.IsNullOrWhiteSpace(subcategory))
            {
                query = query.Where(ep => ep.Subcategory == subcategory);
            }

            return await query
                .OrderByDescending(ep => ep.ConfidenceScore)
                .ThenByDescending(ep => ep.OccurrenceCount)
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get error patterns for similarity analysis with category {Category}", category);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<ErrorPattern>> GetMostFrequentPatternsAsync(int limit = 20, int minSeverityLevel = 1)
    {
        try
        {
            return await _context.ErrorPatterns
                .Where(ep => ep.SeverityLevel >= minSeverityLevel)
                .OrderByDescending(ep => ep.OccurrenceCount)
                .ThenByDescending(ep => ep.SeverityLevel)
                .Take(limit)
                .Include(ep => ep.LearningHistory.OrderByDescending(lh => lh.Timestamp).Take(3))
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get most frequent error patterns");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<ErrorPattern>> GetStalePatternsAsync(int olderThanDays = 30, int limit = 50)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);

        try
        {
            return await _context.ErrorPatterns
                .Where(ep => ep.LastObserved < cutoffDate)
                .OrderBy(ep => ep.LastObserved)
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get stale error patterns older than {Days} days", olderThanDays);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<ErrorPattern>> GetPatternsByEndpointAsync(string apiEndpoint, int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(apiEndpoint))
            throw new ArgumentException("API endpoint cannot be null or whitespace", nameof(apiEndpoint));

        try
        {
            return await _context.ErrorPatterns
                .Where(ep => ep.ApiEndpoint == apiEndpoint)
                .OrderByDescending(ep => ep.OccurrenceCount)
                .ThenByDescending(ep => ep.LastObserved)
                .Take(limit)
                .Include(ep => ep.OptimizationSuggestions.Where(os => os.Status == SuggestionStatus.Generated || os.Status == SuggestionStatus.UnderReview))
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get error patterns for endpoint {ApiEndpoint}", apiEndpoint);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var errorPattern = await _context.ErrorPatterns.FindAsync(id);
            if (errorPattern == null)
            {
                return false;
            }

            _context.ErrorPatterns.Remove(errorPattern);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Deleted error pattern with ID {ErrorPatternId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete error pattern with ID {ErrorPatternId}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> GetCountAsync(
        string? category = null,
        string? apiEndpoint = null,
        int? minSeverityLevel = null)
    {
        try
        {
            var query = _context.ErrorPatterns.AsQueryable();

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(ep => ep.Category == category);
            }

            if (!string.IsNullOrWhiteSpace(apiEndpoint))
            {
                query = query.Where(ep => ep.ApiEndpoint == apiEndpoint);
            }

            if (minSeverityLevel.HasValue)
            {
                query = query.Where(ep => ep.SeverityLevel >= minSeverityLevel.Value);
            }

            return await query.CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get error pattern count");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, object>> GetStatisticsAsync()
    {
        try
        {
            var statistics = new Dictionary<string, object>();

            // Basic counts
            statistics["TotalPatterns"] = await _context.ErrorPatterns.CountAsync();
            statistics["HighSeverityPatterns"] = await _context.ErrorPatterns.CountAsync(ep => ep.SeverityLevel >= 4);
            statistics["RecentPatterns"] = await _context.ErrorPatterns
                .CountAsync(ep => ep.LastObserved >= DateTime.UtcNow.AddDays(-7));

            // Category distribution
            var categoryStats = await _context.ErrorPatterns
                .GroupBy(ep => ep.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToDictionaryAsync(x => x.Category ?? "Unknown", x => (object)x.Count);
            statistics["CategoryDistribution"] = categoryStats;

            // API endpoint distribution
            var endpointStats = await _context.ErrorPatterns
                .Where(ep => !string.IsNullOrEmpty(ep.ApiEndpoint))
                .GroupBy(ep => ep.ApiEndpoint)
                .Select(g => new { Endpoint = g.Key, Count = g.Sum(ep => ep.OccurrenceCount) })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToDictionaryAsync(x => x.Endpoint ?? "Unknown", x => (object)x.Count);
            statistics["EndpointDistribution"] = endpointStats;

            // Severity level distribution
            var severityStats = await _context.ErrorPatterns
                .GroupBy(ep => ep.SeverityLevel)
                .Select(g => new { Level = g.Key, Count = g.Count() })
                .OrderBy(x => x.Level)
                .ToDictionaryAsync(x => $"Level{x.Level}", x => (object)x.Count);
            statistics["SeverityDistribution"] = severityStats;

            // Average confidence score
            var avgConfidence = await _context.ErrorPatterns
                .Where(ep => ep.ConfidenceScore > 0)
                .AverageAsync(ep => ep.ConfidenceScore);
            statistics["AverageConfidenceScore"] = Math.Round(avgConfidence, 3);

            // Total learning entries
            statistics["TotalLearningEntries"] = await _context.LearningHistoryEntries.CountAsync();

            // Unanalyzed entries
            statistics["UnanalyzedEntries"] = await _context.LearningHistoryEntries
                .CountAsync(lh => !lh.IsAnalyzed);

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get error pattern statistics");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<ErrorPattern>> GetPatternsByCategoryAsync(string category, int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be null or whitespace", nameof(category));

        try
        {
            return await _context.ErrorPatterns
                .Where(ep => ep.Category == category)
                .OrderByDescending(ep => ep.OccurrenceCount)
                .ThenByDescending(ep => ep.ConfidenceScore)
                .Take(limit)
                .Include(ep => ep.LearningHistory)
                .Include(ep => ep.OptimizationSuggestions)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get error patterns for category {Category}", category);
            throw;
        }
    }
}