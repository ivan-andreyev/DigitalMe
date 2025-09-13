using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DigitalMe.Services.Learning.ErrorLearning.Models;

/// <summary>
/// Represents a learned error pattern from test failures and API errors
/// Used by Error Learning System to identify recurring issues and suggest optimizations
/// </summary>
public class ErrorPattern
{
    /// <summary>
    /// Unique identifier for the error pattern
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Unique pattern identifier based on error characteristics
    /// Used to group similar errors together
    /// </summary>
    [Required]
    [StringLength(255)]
    public string PatternHash { get; set; } = string.Empty;

    /// <summary>
    /// Category of the error (e.g., "HTTP_TIMEOUT", "ASSERTION_FAILURE", "JSON_PARSE_ERROR")
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Subcategory for more specific classification
    /// </summary>
    [StringLength(100)]
    public string? Subcategory { get; set; }

    /// <summary>
    /// Human-readable description of the error pattern
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// HTTP status code if applicable
    /// </summary>
    public int? HttpStatusCode { get; set; }

    /// <summary>
    /// API endpoint where this error commonly occurs
    /// </summary>
    [StringLength(200)]
    public string? ApiEndpoint { get; set; }

    /// <summary>
    /// HTTP method associated with the error
    /// </summary>
    [StringLength(10)]
    public string? HttpMethod { get; set; }

    /// <summary>
    /// Number of times this pattern has been observed
    /// </summary>
    public int OccurrenceCount { get; set; }

    /// <summary>
    /// First time this pattern was observed
    /// </summary>
    public DateTime FirstObserved { get; set; }

    /// <summary>
    /// Last time this pattern was observed
    /// </summary>
    public DateTime LastObserved { get; set; }

    /// <summary>
    /// Severity level of this error pattern (1-5, where 5 is critical)
    /// </summary>
    public int SeverityLevel { get; set; }

    /// <summary>
    /// Confidence score for this pattern (0.0 - 1.0)
    /// Higher scores indicate more reliable patterns
    /// </summary>
    public double ConfidenceScore { get; set; }

    /// <summary>
    /// Common context or conditions when this error occurs
    /// Stored as JSON string for flexibility
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// Suggested solutions or optimizations for this error pattern
    /// Stored as JSON array of suggestions
    /// </summary>
    public string? SuggestedSolutions { get; set; }

    /// <summary>
    /// Learning history entries related to this pattern
    /// </summary>
    public virtual ICollection<LearningHistoryEntry> LearningHistory { get; set; } = new List<LearningHistoryEntry>();

    /// <summary>
    /// Optimization suggestions generated for this pattern
    /// </summary>
    public virtual ICollection<OptimizationSuggestion> OptimizationSuggestions { get; set; } = new List<OptimizationSuggestion>();
}