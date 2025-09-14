using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DigitalMe.Services.Learning.ErrorLearning.Models;
using DigitalMe.Services.Learning.ErrorLearning.Repositories;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Learning.ErrorLearning;

/// <summary>
/// Service for managing optimization suggestions lifecycle
/// Implements SRP by focusing only on optimization suggestion operations
/// </summary>
public class OptimizationSuggestionManagementService : IOptimizationSuggestionManagementService
{
    private readonly ILogger<OptimizationSuggestionManagementService> _logger;
    private readonly IErrorPatternRepository _errorPatternRepository;
    private readonly IOptimizationSuggestionRepository _optimizationSuggestionRepository;

    public OptimizationSuggestionManagementService(
        ILogger<OptimizationSuggestionManagementService> logger,
        IErrorPatternRepository errorPatternRepository,
        IOptimizationSuggestionRepository optimizationSuggestionRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _errorPatternRepository = errorPatternRepository ?? throw new ArgumentNullException(nameof(errorPatternRepository));
        _optimizationSuggestionRepository = optimizationSuggestionRepository ?? throw new ArgumentNullException(nameof(optimizationSuggestionRepository));
    }

    /// <inheritdoc />
    public async Task<List<OptimizationSuggestion>> GenerateOptimizationSuggestionsAsync(int errorPatternId)
    {
        try
        {
            var pattern = await _errorPatternRepository.GetByIdAsync(errorPatternId);
            if (pattern == null)
            {
                _logger.LogWarning("Pattern not found: {ErrorPatternId}", errorPatternId);
                return new List<OptimizationSuggestion>();
            }

            _logger.LogInformation("Generating optimization suggestions for pattern: {PatternId}", errorPatternId);

            var suggestions = new List<OptimizationSuggestion>();

            // Generate suggestions based on pattern characteristics
            suggestions.AddRange(GenerateTestCaseOptimizations(pattern));
            suggestions.AddRange(GenerateErrorHandlingImprovements(pattern));
            suggestions.AddRange(GenerateTimeoutOptimizations(pattern));
            suggestions.AddRange(GenerateAssertionImprovements(pattern));

            // Save suggestions to database
            var savedSuggestions = await _optimizationSuggestionRepository.CreateBatchAsync(suggestions);

            _logger.LogInformation("Generated {SuggestionCount} optimization suggestions for pattern {PatternId}",
                savedSuggestions.Count, errorPatternId);

            return savedSuggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate optimization suggestions for pattern {PatternId}", errorPatternId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<OptimizationSuggestion>> GetOptimizationSuggestionsAsync(
        OptimizationType? type = null,
        SuggestionStatus? status = null,
        int? minPriority = null,
        double? minConfidenceScore = null)
    {
        return await _optimizationSuggestionRepository.GetSuggestionsAsync(
            type, status, minPriority, minConfidenceScore);
    }

    /// <inheritdoc />
    public async Task<OptimizationSuggestion> UpdateSuggestionStatusAsync(
        int suggestionId,
        SuggestionStatus status,
        string? reviewerNotes = null)
    {
        return await _optimizationSuggestionRepository.UpdateStatusAsync(suggestionId, status, reviewerNotes);
    }

    #region Private Suggestion Generation Methods

    /// <summary>
    /// Generates test case optimization suggestions based on error patterns
    /// </summary>
    private List<OptimizationSuggestion> GenerateTestCaseOptimizations(ErrorPattern pattern)
    {
        var suggestions = new List<OptimizationSuggestion>();

        // Test timeout optimization
        if (pattern.Category == "Network" && pattern.Subcategory == "Timeout")
        {
            suggestions.Add(new OptimizationSuggestion
            {
                ErrorPatternId = pattern.Id,
                Type = OptimizationType.TestCaseOptimization,
                Title = "Increase test timeout for network operations",
                Description = $"Tests for {pattern.ApiEndpoint} are timing out frequently. Consider increasing timeout from default to account for network latency.",
                Priority = pattern.SeverityLevel,
                ConfidenceScore = Math.Min(0.9, pattern.ConfidenceScore + 0.1),
                Status = SuggestionStatus.Generated,
                GeneratedAt = DateTime.UtcNow,
                TargetComponent = pattern.ApiEndpoint ?? "Network Operations",
                ExpectedImpact = "Reduced test flakiness due to timeouts",
                ImplementationDetails = "Update test configuration to use longer timeout values for network-dependent operations"
            });
        }

        // Test parallelization issues
        if (pattern.OccurrenceCount > 10 && pattern.Category == "General")
        {
            suggestions.Add(new OptimizationSuggestion
            {
                ErrorPatternId = pattern.Id,
                Type = OptimizationType.TestCaseOptimization,
                Title = "Review test isolation for parallel execution",
                Description = "Frequent errors may indicate test isolation issues when running in parallel.",
                Priority = 3,
                ConfidenceScore = pattern.ConfidenceScore * 0.8,
                Status = SuggestionStatus.Generated,
                GeneratedAt = DateTime.UtcNow,
                TargetComponent = pattern.ApiEndpoint ?? "Test Framework",
                ExpectedImpact = "Improved test reliability in parallel execution",
                ImplementationDetails = "Review shared state, static variables, and resource contention"
            });
        }

        return suggestions;
    }

    /// <summary>
    /// Generates error handling improvement suggestions
    /// </summary>
    private List<OptimizationSuggestion> GenerateErrorHandlingImprovements(ErrorPattern pattern)
    {
        var suggestions = new List<OptimizationSuggestion>();

        // HTTP error handling
        if (pattern.Category == "HTTP" && pattern.HttpStatusCode.HasValue)
        {
            var statusCode = pattern.HttpStatusCode.Value;

            if (statusCode == 429) // Rate limiting
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    ErrorPatternId = pattern.Id,
                    Type = OptimizationType.ErrorHandlingImprovement,
                    Title = "Implement retry logic with exponential backoff",
                    Description = $"API endpoint {pattern.ApiEndpoint} frequently returns 429 (Too Many Requests). Implement proper retry mechanism.",
                    Priority = 4,
                    ConfidenceScore = Math.Min(0.95, pattern.ConfidenceScore + 0.2),
                    Status = SuggestionStatus.Generated,
                    GeneratedAt = DateTime.UtcNow,
                    TargetComponent = pattern.ApiEndpoint ?? "HTTP Client",
                    ExpectedImpact = "Reduced rate limit errors through intelligent retry",
                    ImplementationDetails = "Use exponential backoff with jitter, respect Retry-After headers"
                });
            }
            else if (statusCode >= 500)
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    ErrorPatternId = pattern.Id,
                    Type = OptimizationType.ErrorHandlingImprovement,
                    Title = "Add circuit breaker pattern for server errors",
                    Description = $"Frequent server errors (5xx) from {pattern.ApiEndpoint}. Consider implementing circuit breaker pattern.",
                    Priority = pattern.SeverityLevel,
                    ConfidenceScore = pattern.ConfidenceScore,
                    Status = SuggestionStatus.Generated,
                    GeneratedAt = DateTime.UtcNow,
                    TargetComponent = pattern.ApiEndpoint ?? "API Client",
                    ExpectedImpact = "Prevent cascade failures during service outages",
                    ImplementationDetails = "Implement circuit breaker with configurable failure threshold and timeout"
                });
            }
        }

        return suggestions;
    }

    /// <summary>
    /// Generates timeout optimization suggestions
    /// </summary>
    private List<OptimizationSuggestion> GenerateTimeoutOptimizations(ErrorPattern pattern)
    {
        var suggestions = new List<OptimizationSuggestion>();

        if (pattern.Category == "Network" && pattern.Subcategory == "Timeout")
        {
            suggestions.Add(new OptimizationSuggestion
            {
                ErrorPatternId = pattern.Id,
                Type = OptimizationType.PerformanceOptimization,
                Title = "Optimize connection timeout settings",
                Description = $"Connection timeouts detected for {pattern.ApiEndpoint}. Review and optimize timeout configurations.",
                Priority = Math.Min(5, pattern.SeverityLevel + 1),
                ConfidenceScore = pattern.ConfidenceScore,
                Status = SuggestionStatus.Generated,
                GeneratedAt = DateTime.UtcNow,
                TargetComponent = pattern.ApiEndpoint ?? "Network Configuration",
                ExpectedImpact = "Improved reliability and user experience",
                ImplementationDetails = "Configure appropriate connection and read timeout values based on API characteristics"
            });
        }

        return suggestions;
    }

    /// <summary>
    /// Generates assertion improvement suggestions
    /// </summary>
    private List<OptimizationSuggestion> GenerateAssertionImprovements(ErrorPattern pattern)
    {
        var suggestions = new List<OptimizationSuggestion>();

        if (pattern.Category == "Data" && pattern.Subcategory == "ValidationError")
        {
            suggestions.Add(new OptimizationSuggestion
            {
                ErrorPatternId = pattern.Id,
                Type = OptimizationType.TestCaseOptimization,
                Title = "Improve test data validation assertions",
                Description = "Validation errors suggest assertions could be more specific about expected data formats.",
                Priority = 2,
                ConfidenceScore = pattern.ConfidenceScore * 0.7,
                Status = SuggestionStatus.Generated,
                GeneratedAt = DateTime.UtcNow,
                TargetComponent = "Test Assertions",
                ExpectedImpact = "Clearer test failures with more specific error messages",
                ImplementationDetails = "Add specific assertions for data format, range, and business rule validation"
            });
        }

        return suggestions;
    }

    #endregion
}