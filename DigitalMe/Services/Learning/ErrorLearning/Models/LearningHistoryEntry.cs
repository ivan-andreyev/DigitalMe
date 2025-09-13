using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigitalMe.Services.Learning.ErrorLearning.Models;

/// <summary>
/// Represents a single learning event in the error learning system
/// Tracks individual occurrences of errors and their context for pattern analysis
/// </summary>
public class LearningHistoryEntry
{
    /// <summary>
    /// Unique identifier for the learning history entry
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the associated error pattern
    /// </summary>
    public int ErrorPatternId { get; set; }

    /// <summary>
    /// Navigation property to the associated error pattern
    /// </summary>
    [ForeignKey(nameof(ErrorPatternId))]
    public virtual ErrorPattern ErrorPattern { get; set; } = null!;

    /// <summary>
    /// Timestamp when this error occurred
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Source of the error (e.g., "SelfTestingFramework", "AutoDocumentationParser")
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Name of the test case that failed (if applicable)
    /// </summary>
    [StringLength(200)]
    public string? TestCaseName { get; set; }

    /// <summary>
    /// API name being tested when error occurred
    /// </summary>
    [StringLength(100)]
    public string? ApiName { get; set; }

    /// <summary>
    /// Full error message or exception details
    /// </summary>
    [Required]
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Stack trace if available
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// Request details when the error occurred
    /// Stored as JSON for flexibility
    /// </summary>
    public string? RequestDetails { get; set; }

    /// <summary>
    /// Response details when the error occurred
    /// Stored as JSON for flexibility
    /// </summary>
    public string? ResponseDetails { get; set; }

    /// <summary>
    /// Environment context (user agent, IP, etc.)
    /// Stored as JSON for flexibility
    /// </summary>
    public string? EnvironmentContext { get; set; }

    /// <summary>
    /// Whether this error has been analyzed and incorporated into learning
    /// </summary>
    public bool IsAnalyzed { get; set; }

    /// <summary>
    /// Whether this error contributed to pattern recognition
    /// </summary>
    public bool ContributedToPattern { get; set; }

    /// <summary>
    /// Confidence score for this specific error instance (0.0 - 1.0)
    /// </summary>
    public double ConfidenceScore { get; set; }

    /// <summary>
    /// Additional metadata about this learning event
    /// Stored as JSON for extensibility
    /// </summary>
    public string? Metadata { get; set; }
}