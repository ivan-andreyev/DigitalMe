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
/// Entity Framework implementation of IOptimizationSuggestionRepository
/// Provides optimized data access for optimization suggestions
/// </summary>
public class OptimizationSuggestionRepository : IOptimizationSuggestionRepository
{
    private readonly DigitalMeDbContext _context;
    private readonly ILogger<OptimizationSuggestionRepository> _logger;

    public OptimizationSuggestionRepository(
        DigitalMeDbContext context,
        ILogger<OptimizationSuggestionRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<OptimizationSuggestion> CreateAsync(OptimizationSuggestion suggestion)
    {
        if (suggestion == null)
            throw new ArgumentNullException(nameof(suggestion));

        try
        {
            _context.OptimizationSuggestions.Add(suggestion);
            await _context.SaveChangesAsync();
            
            _logger.LogDebug("Created optimization suggestion with ID {SuggestionId} for pattern {ErrorPatternId}", 
                suggestion.Id, suggestion.ErrorPatternId);
            
            return suggestion;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to create optimization suggestion for pattern {ErrorPatternId}", suggestion.ErrorPatternId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<OptimizationSuggestion>> CreateBatchAsync(List<OptimizationSuggestion> suggestions)
    {
        if (suggestions == null || suggestions.Count == 0)
            throw new ArgumentException("Suggestions list cannot be null or empty", nameof(suggestions));

        try
        {
            _context.OptimizationSuggestions.AddRange(suggestions);
            await _context.SaveChangesAsync();
            
            _logger.LogDebug("Created {Count} optimization suggestions in batch", suggestions.Count);
            
            return suggestions;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to create batch of {Count} optimization suggestions", suggestions.Count);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<OptimizationSuggestion> UpdateAsync(OptimizationSuggestion suggestion)
    {
        if (suggestion == null)
            throw new ArgumentNullException(nameof(suggestion));

        try
        {
            _context.OptimizationSuggestions.Update(suggestion);
            await _context.SaveChangesAsync();
            
            _logger.LogDebug("Updated optimization suggestion with ID {SuggestionId}", suggestion.Id);
            
            return suggestion;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to update optimization suggestion with ID {SuggestionId}", suggestion.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<OptimizationSuggestion?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.OptimizationSuggestions
                .Include(os => os.ErrorPattern)
                .FirstOrDefaultAsync(os => os.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get optimization suggestion by ID {SuggestionId}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<OptimizationSuggestion>> GetSuggestionsAsync(
        OptimizationType? type = null,
        SuggestionStatus? status = null,
        int? minPriority = null,
        double? minConfidenceScore = null,
        int limit = 50)
    {
        try
        {
            var query = _context.OptimizationSuggestions.AsQueryable();

            if (type.HasValue)
                query = query.Where(os => os.Type == type.Value);

            if (status.HasValue)
                query = query.Where(os => os.Status == status.Value);

            if (minPriority.HasValue)
                query = query.Where(os => os.Priority >= minPriority.Value);

            if (minConfidenceScore.HasValue)
                query = query.Where(os => os.ConfidenceScore >= minConfidenceScore.Value);

            return await query
                .OrderByDescending(os => os.Priority)
                .ThenByDescending(os => os.GeneratedAt)
                .Take(limit)
                .Include(os => os.ErrorPattern)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get optimization suggestions with filters");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<OptimizationSuggestion>> GetByErrorPatternAsync(
        int errorPatternId,
        bool includeImplemented = false,
        int limit = 20)
    {
        try
        {
            var query = _context.OptimizationSuggestions
                .Where(os => os.ErrorPatternId == errorPatternId);

            if (!includeImplemented)
            {
                query = query.Where(os => os.Status != SuggestionStatus.Implemented);
            }

            return await query
                .OrderByDescending(os => os.Priority)
                .ThenByDescending(os => os.GeneratedAt)
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get optimization suggestions for pattern {ErrorPatternId}", errorPatternId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<OptimizationSuggestion>> GetPendingSuggestionsAsync(int limit = 50)
    {
        try
        {
            return await _context.OptimizationSuggestions
                .Where(os => os.Status == SuggestionStatus.Generated || os.Status == SuggestionStatus.UnderReview)
                .OrderByDescending(os => os.Priority)
                .ThenBy(os => os.GeneratedAt) // Older suggestions first for review
                .Take(limit)
                .Include(os => os.ErrorPattern)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pending optimization suggestions");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<OptimizationSuggestion>> GetHighPrioritySuggestionsAsync(
        int minPriority = 4,
        SuggestionStatus? status = null,
        int limit = 30)
    {
        try
        {
            var query = _context.OptimizationSuggestions
                .Where(os => os.Priority >= minPriority);

            if (status.HasValue)
                query = query.Where(os => os.Status == status.Value);

            return await query
                .OrderByDescending(os => os.Priority)
                .ThenByDescending(os => os.ConfidenceScore)
                .Take(limit)
                .Include(os => os.ErrorPattern)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get high priority optimization suggestions");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<OptimizationSuggestion>> GetByTargetComponentAsync(string targetComponent, int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(targetComponent))
            throw new ArgumentException("Target component cannot be null or whitespace", nameof(targetComponent));

        try
        {
            return await _context.OptimizationSuggestions
                .Where(os => os.TargetComponent == targetComponent)
                .OrderByDescending(os => os.Priority)
                .ThenByDescending(os => os.GeneratedAt)
                .Take(limit)
                .Include(os => os.ErrorPattern)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get optimization suggestions for component {TargetComponent}", targetComponent);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<OptimizationType, List<OptimizationSuggestion>>> GetGroupedByTypeAsync(
        SuggestionStatus? status = null)
    {
        try
        {
            var query = _context.OptimizationSuggestions.AsQueryable();

            if (status.HasValue)
                query = query.Where(os => os.Status == status.Value);

            var suggestions = await query
                .Include(os => os.ErrorPattern)
                .OrderByDescending(os => os.Priority)
                .ToListAsync();

            return suggestions
                .GroupBy(os => os.Type)
                .ToDictionary(g => g.Key, g => g.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get optimization suggestions grouped by type");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<OptimizationSuggestion> UpdateStatusAsync(
        int suggestionId,
        SuggestionStatus status,
        string? reviewerNotes = null)
    {
        try
        {
            var suggestion = await _context.OptimizationSuggestions.FindAsync(suggestionId);
            if (suggestion == null)
                throw new InvalidOperationException($"Optimization suggestion with ID {suggestionId} not found");

            suggestion.Status = status;
            suggestion.IsReviewed = true;
            suggestion.ReviewedAt = DateTime.UtcNow;
            
            if (!string.IsNullOrWhiteSpace(reviewerNotes))
                suggestion.ReviewerNotes = reviewerNotes;

            await _context.SaveChangesAsync();
            
            _logger.LogDebug("Updated status of optimization suggestion {SuggestionId} to {Status}", 
                suggestionId, status);
            
            return suggestion;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update status of optimization suggestion {SuggestionId}", suggestionId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> UpdateStatusBatchAsync(List<(int SuggestionId, SuggestionStatus Status, string? ReviewerNotes)> updates)
    {
        if (updates == null || updates.Count == 0)
            throw new ArgumentException("Updates list cannot be null or empty", nameof(updates));

        try
        {
            var suggestionIds = updates.Select(u => u.SuggestionId).ToList();
            var suggestions = await _context.OptimizationSuggestions
                .Where(os => suggestionIds.Contains(os.Id))
                .ToListAsync();

            foreach (var suggestion in suggestions)
            {
                var update = updates.First(u => u.SuggestionId == suggestion.Id);
                suggestion.Status = update.Status;
                suggestion.IsReviewed = true;
                suggestion.ReviewedAt = DateTime.UtcNow;
                
                if (!string.IsNullOrWhiteSpace(update.ReviewerNotes))
                    suggestion.ReviewerNotes = update.ReviewerNotes;
            }

            var updatedCount = await _context.SaveChangesAsync();
            
            _logger.LogDebug("Updated status of {Count} optimization suggestions in batch", suggestions.Count);
            
            return suggestions.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update status of {Count} optimization suggestions in batch", updates.Count);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var suggestion = await _context.OptimizationSuggestions.FindAsync(id);
            if (suggestion == null)
                return false;

            _context.OptimizationSuggestions.Remove(suggestion);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Deleted optimization suggestion with ID {SuggestionId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete optimization suggestion with ID {SuggestionId}", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> DeleteOldSuggestionsAsync(
        int olderThanDays = 180,
        List<SuggestionStatus>? statuses = null)
    {
        statuses ??= new List<SuggestionStatus> { SuggestionStatus.Rejected, SuggestionStatus.Implemented };
        var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);

        try
        {
            var deletedCount = await _context.OptimizationSuggestions
                .Where(os => statuses.Contains(os.Status) && os.GeneratedAt < cutoffDate)
                .ExecuteDeleteAsync();

            _logger.LogInformation("Deleted {Count} old optimization suggestions older than {Days} days", 
                deletedCount, olderThanDays);
            
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete old optimization suggestions");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> GetCountAsync(
        OptimizationType? type = null,
        SuggestionStatus? status = null,
        int? errorPatternId = null)
    {
        try
        {
            var query = _context.OptimizationSuggestions.AsQueryable();

            if (type.HasValue)
                query = query.Where(os => os.Type == type.Value);

            if (status.HasValue)
                query = query.Where(os => os.Status == status.Value);

            if (errorPatternId.HasValue)
                query = query.Where(os => os.ErrorPatternId == errorPatternId.Value);

            return await query.CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get optimization suggestion count");
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

            var query = _context.OptimizationSuggestions.AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(os => os.GeneratedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(os => os.GeneratedAt <= toDate.Value);

            // Basic counts
            statistics["TotalSuggestions"] = await query.CountAsync();
            statistics["PendingSuggestions"] = await query.CountAsync(os => 
                os.Status == SuggestionStatus.Generated || os.Status == SuggestionStatus.UnderReview);
            statistics["ApprovedSuggestions"] = await query.CountAsync(os => os.Status == SuggestionStatus.Approved);
            statistics["ImplementedSuggestions"] = await query.CountAsync(os => os.Status == SuggestionStatus.Implemented);
            statistics["RejectedSuggestions"] = await query.CountAsync(os => os.Status == SuggestionStatus.Rejected);

            // Type distribution
            var typeStats = await query
                .GroupBy(os => os.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToDictionaryAsync(x => x.Type.ToString(), x => (object)x.Count);
            statistics["TypeDistribution"] = typeStats;

            // Status distribution
            var statusStats = await query
                .GroupBy(os => os.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status.ToString(), x => (object)x.Count);
            statistics["StatusDistribution"] = statusStats;

            // Priority distribution
            var priorityStats = await query
                .GroupBy(os => os.Priority)
                .Select(g => new { Priority = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Priority)
                .ToDictionaryAsync(x => $"Priority{x.Priority}", x => (object)x.Count);
            statistics["PriorityDistribution"] = priorityStats;

            // Average confidence score
            var avgConfidence = await query
                .Where(os => os.ConfidenceScore > 0)
                .AverageAsync(os => (double?)os.ConfidenceScore) ?? 0.0;
            statistics["AverageConfidenceScore"] = Math.Round(avgConfidence, 3);

            // Target component distribution
            var componentStats = await query
                .Where(os => !string.IsNullOrEmpty(os.TargetComponent))
                .GroupBy(os => os.TargetComponent)
                .Select(g => new { Component = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToDictionaryAsync(x => x.Component ?? "Unknown", x => (object)x.Count);
            statistics["ComponentDistribution"] = componentStats;

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get optimization suggestion statistics");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<OptimizationSuggestion>> GetSimilarSuggestionsAsync(
        string title,
        OptimizationType type,
        string? targetComponent = null,
        int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be null or whitespace", nameof(title));

        try
        {
            var query = _context.OptimizationSuggestions
                .Where(os => os.Type == type);

            if (!string.IsNullOrWhiteSpace(targetComponent))
                query = query.Where(os => os.TargetComponent == targetComponent);

            // Find suggestions with similar titles using PostgreSQL full-text search or LIKE
            query = query.Where(os => EF.Functions.Like(os.Title, $"%{title}%") ||
                                     EF.Functions.Like(title, $"%{os.Title}%"));

            return await query
                .OrderByDescending(os => os.ConfidenceScore)
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get similar optimization suggestions for title '{Title}'", title);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<OptimizationSuggestion> UpdateConfidenceScoreAsync(int suggestionId, double confidenceScore)
    {
        if (confidenceScore < 0.0 || confidenceScore > 1.0)
            throw new ArgumentOutOfRangeException(nameof(confidenceScore), "Confidence score must be between 0.0 and 1.0");

        try
        {
            var suggestion = await _context.OptimizationSuggestions.FindAsync(suggestionId);
            if (suggestion == null)
                throw new InvalidOperationException($"Optimization suggestion with ID {suggestionId} not found");

            suggestion.ConfidenceScore = confidenceScore;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated confidence score for suggestion {SuggestionId} to {ConfidenceScore}",
                suggestionId, confidenceScore);

            return suggestion;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update confidence score for suggestion {SuggestionId}", suggestionId);
            throw;
        }
    }
}