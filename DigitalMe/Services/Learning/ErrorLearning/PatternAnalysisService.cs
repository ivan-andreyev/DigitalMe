using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DigitalMe.Services.Learning.ErrorLearning.Models;
using DigitalMe.Services.Learning.ErrorLearning.Repositories;

namespace DigitalMe.Services.Learning.ErrorLearning;

/// <summary>
/// Service for analyzing error patterns and performing machine learning operations
/// Implements SRP by focusing only on pattern analysis and ML algorithms
/// </summary>
public class PatternAnalysisService : IPatternAnalysisService
{
    private readonly ILogger<PatternAnalysisService> _logger;
    private readonly IErrorPatternRepository _errorPatternRepository;
    private readonly ILearningHistoryRepository _learningHistoryRepository;

    // Learning algorithm parameters - should be moved to configuration
    private const double MinPatternConfidenceThreshold = 0.6;
    private const int MinOccurrenceCountForPattern = 3;
    private const double SimilarityThreshold = 0.8;

    public PatternAnalysisService(
        ILogger<PatternAnalysisService> logger,
        IErrorPatternRepository errorPatternRepository,
        ILearningHistoryRepository learningHistoryRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _errorPatternRepository = errorPatternRepository ?? throw new ArgumentNullException(nameof(errorPatternRepository));
        _learningHistoryRepository = learningHistoryRepository ?? throw new ArgumentNullException(nameof(learningHistoryRepository));
    }

    /// <inheritdoc />
    public async Task<int> AnalyzeErrorPatternsAsync(int batchSize = 100)
    {
        try
        {
            _logger.LogInformation("Starting error pattern analysis with batch size: {BatchSize}", batchSize);

            var unanalyzedEntries = await _learningHistoryRepository.GetUnanalyzedEntriesAsync(batchSize);
            if (!unanalyzedEntries.Any())
            {
                _logger.LogDebug("No unanalyzed entries found");
                return 0;
            }

            var patternsUpdated = 0;

            // Group entries by similar characteristics for pattern matching
            var entryGroups = GroupSimilarEntries(unanalyzedEntries);

            foreach (var group in entryGroups)
            {
                if (group.Count >= MinOccurrenceCountForPattern)
                {
                    var pattern = await FindOrCreatePatternForGroup(group);
                    if (pattern != null)
                    {
                        // Update pattern with new insights
                        await UpdatePatternWithLearning(pattern, group);
                        patternsUpdated++;
                    }
                }

                // Mark entries as analyzed using batch update for performance
                var entryIds = group.Select(e => e.Id).ToList();
                await _learningHistoryRepository.MarkAsAnalyzedAsync(entryIds, true, 0.8);
            }

            _logger.LogInformation("Pattern analysis completed. Updated {PatternsCount} patterns", patternsUpdated);
            return patternsUpdated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during pattern analysis");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<ErrorPattern>> GetErrorPatternsAsync(
        string? category = null,
        string? apiEndpoint = null,
        int? minOccurrenceCount = null,
        int? minSeverityLevel = null,
        double? minConfidenceScore = null)
    {
        return await _errorPatternRepository.GetPatternsAsync(
            category, apiEndpoint, minOccurrenceCount, minSeverityLevel, minConfidenceScore);
    }

    /// <inheritdoc />
    public async Task<List<LearningHistoryEntry>> GetLearningHistoryAsync(int errorPatternId, int limit = 50)
    {
        return await _learningHistoryRepository.GetByErrorPatternAsync(errorPatternId, limit);
    }

    #region Private Helper Methods

    /// <summary>
    /// Groups similar learning entries for pattern analysis
    /// </summary>
    private List<List<LearningHistoryEntry>> GroupSimilarEntries(List<LearningHistoryEntry> entries)
    {
        var groups = new List<List<LearningHistoryEntry>>();

        foreach (var entry in entries)
        {
            var similarGroup = FindSimilarGroup(groups, entry);
            if (similarGroup != null)
            {
                similarGroup.Add(entry);
            }
            else
            {
                groups.Add(new List<LearningHistoryEntry> { entry });
            }
        }

        return groups;
    }

    /// <summary>
    /// Finds a group that this entry is similar to
    /// </summary>
    private List<LearningHistoryEntry>? FindSimilarGroup(
        List<List<LearningHistoryEntry>> groups,
        LearningHistoryEntry entry)
    {
        foreach (var group in groups)
        {
            if (IsEntrySimilarToGroup(entry, group))
            {
                return group;
            }
        }

        return null;
    }

    /// <summary>
    /// Determines if an entry is similar to entries in a group
    /// </summary>
    private bool IsEntrySimilarToGroup(LearningHistoryEntry entry, List<LearningHistoryEntry> group)
    {
        if (!group.Any())
        {
            return false;
        }

        var representative = group.First();

        // Check similarity based on error message, API name, and source
        var messageSimilarity = CalculateStringSimilarity(entry.ErrorMessage, representative.ErrorMessage);
        var apiSimilarity = string.Equals(entry.ApiName, representative.ApiName, StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0;
        var sourceSimilarity = string.Equals(entry.Source, representative.Source, StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0;

        var overallSimilarity = (messageSimilarity * 0.6) + (apiSimilarity * 0.2) + (sourceSimilarity * 0.2);
        return overallSimilarity >= SimilarityThreshold;
    }

    /// <summary>
    /// Calculates string similarity using simple algorithm
    /// </summary>
    private double CalculateStringSimilarity(string str1, string str2)
    {
        if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
        {
            return 0.0;
        }

        if (str1.Equals(str2, StringComparison.OrdinalIgnoreCase))
        {
            return 1.0;
        }

        // Simple similarity based on common words
        var words1 = str1.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var words2 = str2.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var commonWords = words1.Intersect(words2).Count();
        var totalWords = Math.Max(words1.Length, words2.Length);

        return totalWords > 0 ? (double)commonWords / totalWords : 0.0;
    }

    /// <summary>
    /// Finds or creates an error pattern for a group of similar entries
    /// </summary>
    private async Task<ErrorPattern?> FindOrCreatePatternForGroup(List<LearningHistoryEntry> group)
    {
        if (!group.Any())
        {
            return null;
        }

        // Use representative entry to search for existing patterns
        var representative = group.First();
        var existingPatterns = await _errorPatternRepository.GetPatternsForSimilarityAnalysisAsync(
            representative.Source, null, 10);

        // Look for similar existing patterns
        foreach (var pattern in existingPatterns)
        {
            var similarity = CalculateStringSimilarity(representative.ErrorMessage, pattern.Description);
            if (similarity >= SimilarityThreshold)
            {
                return pattern;
            }
        }

        // No similar pattern found - would create new one, but this requires classification logic
        // For now, return null to avoid incomplete implementation
        _logger.LogDebug("No similar pattern found for group with {Count} entries", group.Count);
        return null;
    }

    /// <summary>
    /// Updates error pattern with insights from new learning entries
    /// </summary>
    private async Task UpdatePatternWithLearning(ErrorPattern pattern, List<LearningHistoryEntry> entries)
    {
        pattern.OccurrenceCount += entries.Count;
        pattern.LastObserved = entries.Max(e => e.Timestamp);
        pattern.ConfidenceScore = Math.Min(0.95, pattern.ConfidenceScore + (entries.Count * 0.05));

        await _errorPatternRepository.UpdateAsync(pattern);
    }

    #endregion
}