using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigitalMe.Data.Entities;

/// <summary>
/// Tracks API usage metrics for external AI provider requests.
/// Records detailed information about each API call including tokens, costs, performance, and success status.
/// </summary>
[Table("ApiUsageRecords")]
public class ApiUsageRecord : BaseEntity
{
    /// <summary>
    /// User ID who made this API request.
    /// </summary>
    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// AI provider name (e.g., "Anthropic", "OpenAI", "Google").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Optional reference to the ApiConfiguration used for this request.
    /// Null if user provided their own API key.
    /// </summary>
    [ForeignKey(nameof(ApiConfiguration))]
    public Guid? ConfigurationId { get; set; }

    /// <summary>
    /// AI model used for the request (e.g., "claude-3-opus", "gpt-4-turbo").
    /// </summary>
    [MaxLength(100)]
    public string? Model { get; set; }

    /// <summary>
    /// Type of API request (e.g., "chat.completion", "embeddings").
    /// </summary>
    [MaxLength(100)]
    public string? RequestType { get; set; }

    /// <summary>
    /// Total number of tokens used in the request (input + output).
    /// </summary>
    public int TokensUsed { get; set; } = 0;

    /// <summary>
    /// Number of input tokens (prompt).
    /// </summary>
    public int InputTokens { get; set; } = 0;

    /// <summary>
    /// Number of output tokens (completion).
    /// </summary>
    public int OutputTokens { get; set; } = 0;

    /// <summary>
    /// Estimated cost of the API call in USD.
    /// Uses decimal(10,6) for precise financial calculations (e.g., $0.015000).
    /// </summary>
    [Column(TypeName = "decimal(10, 6)")]
    public decimal CostEstimate { get; set; } = 0m;

    /// <summary>
    /// Response time in milliseconds.
    /// </summary>
    public int ResponseTimeMs { get; set; } = 0;

    /// <summary>
    /// Indicates if the API request completed successfully.
    /// Defaults to false until confirmed.
    /// </summary>
    public bool Success { get; set; } = false;

    /// <summary>
    /// Type of error if request failed (e.g., "RateLimitExceeded", "InvalidApiKey").
    /// </summary>
    [MaxLength(100)]
    public string? ErrorType { get; set; }

    /// <summary>
    /// Detailed error message if request failed.
    /// </summary>
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Timestamp when the API request was made (UTC).
    /// Defaults to current UTC time on creation.
    /// </summary>
    public DateTime RequestTimestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the associated ApiConfiguration (if any).
    /// </summary>
    public virtual ApiConfiguration? Configuration { get; set; }

    /// <summary>
    /// Default constructor for Entity Framework.
    /// </summary>
    public ApiUsageRecord() : base()
    {
    }
}