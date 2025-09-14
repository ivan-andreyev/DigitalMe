using System;
using System.Collections.Generic;
using DigitalMe.Services.Learning.ErrorLearning.Models;

namespace DigitalMe.Services.Learning.ErrorLearning.SuggestionEngine.Models;

/// <summary>
/// Represents a group of related optimization suggestions that should be implemented together
/// Provides campaign-based optimization approach for maximum impact
/// </summary>
public class OptimizationCampaign
{
    /// <summary>
    /// Unique identifier for the optimization campaign
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Human-readable name for the campaign
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the campaign objectives
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Campaign theme/category (e.g., "Performance", "Reliability", "Security")
    /// </summary>
    public string Theme { get; set; } = string.Empty;

    /// <summary>
    /// Priority level of this campaign (1-5, where 5 is highest)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Estimated impact score for the entire campaign (0.0-1.0)
    /// </summary>
    public double EstimatedImpact { get; set; }

    /// <summary>
    /// Combined confidence score for campaign success (0.0-1.0)
    /// </summary>
    public double ConfidenceScore { get; set; }

    /// <summary>
    /// Estimated effort in hours for complete campaign implementation
    /// </summary>
    public double EstimatedEffortHours { get; set; }

    /// <summary>
    /// List of optimization suggestions included in this campaign
    /// </summary>
    public List<OptimizationSuggestion> Suggestions { get; set; } = new();

    /// <summary>
    /// Dependencies between suggestions within the campaign
    /// Key: Suggestion ID, Value: List of prerequisite suggestion IDs
    /// </summary>
    public Dictionary<int, List<int>> SuggestionDependencies { get; set; } = new();

    /// <summary>
    /// Expected business outcomes from campaign implementation
    /// </summary>
    public List<string> ExpectedOutcomes { get; set; } = new();

    /// <summary>
    /// Success metrics to measure campaign effectiveness
    /// </summary>
    public List<string> SuccessMetrics { get; set; } = new();

    /// <summary>
    /// Implementation timeline phases
    /// </summary>
    public List<CampaignPhase> ImplementationPhases { get; set; } = new();

    /// <summary>
    /// Campaign status
    /// </summary>
    public CampaignStatus Status { get; set; } = CampaignStatus.Generated;

    /// <summary>
    /// When this campaign was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When campaign implementation started
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When campaign was completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Represents a phase in campaign implementation
/// </summary>
public class CampaignPhase
{
    /// <summary>
    /// Phase identifier
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Phase name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Phase description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Suggestions to implement in this phase
    /// </summary>
    public List<int> SuggestionIds { get; set; } = new();

    /// <summary>
    /// Estimated duration for this phase
    /// </summary>
    public TimeSpan EstimatedDuration { get; set; }

    /// <summary>
    /// Prerequisites for starting this phase
    /// </summary>
    public List<string> Prerequisites { get; set; } = new();

    /// <summary>
    /// Expected deliverables from this phase
    /// </summary>
    public List<string> Deliverables { get; set; } = new();
}

/// <summary>
/// Status of optimization campaign
/// </summary>
public enum CampaignStatus
{
    /// <summary>
    /// Campaign has been generated but not yet reviewed
    /// </summary>
    Generated = 1,

    /// <summary>
    /// Campaign is under review
    /// </summary>
    UnderReview = 2,

    /// <summary>
    /// Campaign has been approved for implementation
    /// </summary>
    Approved = 3,

    /// <summary>
    /// Campaign implementation is in progress
    /// </summary>
    InProgress = 4,

    /// <summary>
    /// Campaign has been completed successfully
    /// </summary>
    Completed = 5,

    /// <summary>
    /// Campaign has been cancelled or rejected
    /// </summary>
    Cancelled = 6,

    /// <summary>
    /// Campaign is on hold pending other work
    /// </summary>
    OnHold = 7
}