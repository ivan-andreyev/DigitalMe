using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DigitalMe.Services.Learning.ErrorLearning.SuggestionEngine;
using DigitalMe.Services.Learning.ErrorLearning.SuggestionEngine.Models;

namespace DigitalMe.Controllers;

/// <summary>
/// Controller for Advanced Optimization Suggestion Engine operations
/// Provides endpoints for intelligent suggestion generation and campaign management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AdvancedSuggestionController : ControllerBase
{
    private readonly ILogger<AdvancedSuggestionController> _logger;
    private readonly IAdvancedSuggestionEngine _advancedSuggestionEngine;

    public AdvancedSuggestionController(
        ILogger<AdvancedSuggestionController> logger,
        IAdvancedSuggestionEngine advancedSuggestionEngine)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _advancedSuggestionEngine = advancedSuggestionEngine ?? throw new ArgumentNullException(nameof(advancedSuggestionEngine));
    }

    /// <summary>
    /// Generates prioritized optimization suggestions using advanced algorithms
    /// </summary>
    /// <param name="maxSuggestions">Maximum number of suggestions to return</param>
    [HttpPost("generate-prioritized")]
    public async Task<IActionResult> GeneratePrioritizedSuggestions([FromQuery] int maxSuggestions = 20)
    {
        try
        {
            _logger.LogInformation("Generating {MaxSuggestions} prioritized optimization suggestions", maxSuggestions);

            var suggestions = await _advancedSuggestionEngine.GeneratePrioritizedSuggestionsAsync(maxSuggestions);

            return Ok(new
            {
                Message = "Prioritized optimization suggestions generated successfully",
                SuggestionCount = suggestions.Count,
                Suggestions = suggestions.Select(s => new
                {
                    s.Id,
                    s.Title,
                    s.Description,
                    s.Type,
                    s.Priority,
                    s.ConfidenceScore,
                    s.EstimatedEffortHours,
                    s.TargetComponent,
                    s.ExpectedImpact,
                    s.Tags
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate prioritized optimization suggestions");
            return StatusCode(500, new { Error = "Failed to generate suggestions", Details = ex.Message });
        }
    }

    /// <summary>
    /// Creates optimization campaigns from existing suggestions
    /// </summary>
    /// <param name="suggestionIds">List of suggestion IDs to group into campaigns</param>
    [HttpPost("create-campaigns")]
    public async Task<IActionResult> CreateOptimizationCampaigns([FromBody] CreateCampaignsRequest request)
    {
        try
        {
            if (request?.SuggestionIds == null || !request.SuggestionIds.Any())
            {
                return BadRequest("Suggestion IDs are required");
            }

            _logger.LogInformation("Creating optimization campaigns from {SuggestionCount} suggestions",
                request.SuggestionIds.Count);

            // For demo purposes, generate prioritized suggestions first
            // In real implementation, you'd fetch suggestions by IDs
            var allSuggestions = await _advancedSuggestionEngine.GeneratePrioritizedSuggestionsAsync(50);
            var selectedSuggestions = allSuggestions.Take(request.SuggestionIds.Count).ToList();

            var campaigns = await _advancedSuggestionEngine.GroupSuggestionsIntoCampaignsAsync(selectedSuggestions);

            return Ok(new
            {
                Message = "Optimization campaigns created successfully",
                CampaignCount = campaigns.Count,
                Campaigns = campaigns.Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Description,
                    c.Theme,
                    c.Priority,
                    c.EstimatedImpact,
                    c.ConfidenceScore,
                    c.EstimatedEffortHours,
                    SuggestionCount = c.Suggestions.Count,
                    ExpectedOutcomes = c.ExpectedOutcomes,
                    SuccessMetrics = c.SuccessMetrics,
                    PhaseCount = c.ImplementationPhases.Count,
                    c.Status
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create optimization campaigns");
            return StatusCode(500, new { Error = "Failed to create campaigns", Details = ex.Message });
        }
    }

    /// <summary>
    /// Generates contextual suggestions based on current system state
    /// </summary>
    [HttpPost("generate-contextual")]
    public async Task<IActionResult> GenerateContextualSuggestions([FromBody] SystemContext context)
    {
        try
        {
            if (context == null)
            {
                // Create default context for demo
                context = CreateDefaultSystemContext();
            }

            _logger.LogInformation("Generating contextual optimization suggestions for environment: {Environment}",
                context.Environment);

            var suggestions = await _advancedSuggestionEngine.GenerateContextualSuggestionsAsync(context);

            return Ok(new
            {
                Message = "Contextual optimization suggestions generated successfully",
                Context = new
                {
                    context.Environment,
                    context.Timestamp,
                    SystemLoad = new
                    {
                        context.SystemLoad.CpuUtilization,
                        context.SystemLoad.MemoryUtilization,
                        context.SystemLoad.RequestRate,
                        context.SystemLoad.AverageResponseTime
                    },
                    ErrorTrends = new
                    {
                        context.ErrorTrends.CurrentErrorRate,
                        context.ErrorTrends.TrendDirection,
                        context.ErrorTrends.RecentErrorCategories
                    }
                },
                SuggestionCount = suggestions.Count,
                Suggestions = suggestions.Select(s => new
                {
                    s.Id,
                    s.Title,
                    s.Description,
                    s.Type,
                    s.Priority,
                    s.ConfidenceScore,
                    s.EstimatedEffortHours,
                    s.TargetComponent,
                    s.Tags
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate contextual optimization suggestions");
            return StatusCode(500, new { Error = "Failed to generate contextual suggestions", Details = ex.Message });
        }
    }

    /// <summary>
    /// Analyzes suggestion effectiveness and updates confidence scores
    /// </summary>
    [HttpPost("analyze-effectiveness")]
    public async Task<IActionResult> AnalyzeSuggestionEffectiveness()
    {
        try
        {
            _logger.LogInformation("Analyzing optimization suggestion effectiveness");

            var analyzedCount = await _advancedSuggestionEngine.AnalyzeSuggestionEffectivenessAsync();

            return Ok(new
            {
                Message = "Suggestion effectiveness analysis completed",
                AnalyzedSuggestions = analyzedCount,
                Analysis = new
                {
                    CompletedAt = DateTime.UtcNow,
                    ProcessedSuggestions = analyzedCount,
                    ConfidenceScoreUpdates = analyzedCount,
                    Notes = "Confidence scores updated based on real-world implementation effectiveness"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze suggestion effectiveness");
            return StatusCode(500, new { Error = "Failed to analyze effectiveness", Details = ex.Message });
        }
    }

    /// <summary>
    /// Gets optimization engine statistics and performance metrics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetEngineStatistics()
    {
        try
        {
            // Generate some sample suggestions to demonstrate engine capabilities
            var sampleSuggestions = await _advancedSuggestionEngine.GeneratePrioritizedSuggestionsAsync(100);

            var stats = new
            {
                Message = "Advanced Suggestion Engine statistics",
                EngineCapabilities = new
                {
                    TotalSuggestionsGenerated = sampleSuggestions.Count,
                    SuggestionTypes = sampleSuggestions.GroupBy(s => s.Type)
                        .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                    PriorityDistribution = sampleSuggestions.GroupBy(s => s.Priority)
                        .ToDictionary(g => $"Priority{g.Key}", g => g.Count()),
                    AverageConfidenceScore = sampleSuggestions.Average(s => s.ConfidenceScore),
                    AverageEstimatedEffort = sampleSuggestions.Where(s => s.EstimatedEffortHours.HasValue)
                        .Average(s => s.EstimatedEffortHours ?? 0)
                },
                AlgorithmMetrics = new
                {
                    IntelligentPrioritization = "Multi-factor scoring: impact, urgency, feasibility, confidence",
                    DeduplicationEfficiency = "String similarity + semantic analysis",
                    ContextualAwareness = "System load, error trends, maintenance windows",
                    CampaignOptimization = "Theme-based grouping with implementation phases"
                },
                GeneratedAt = DateTime.UtcNow
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get engine statistics");
            return StatusCode(500, new { Error = "Failed to get statistics", Details = ex.Message });
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Creates a default system context for demonstration purposes
    /// </summary>
    private SystemContext CreateDefaultSystemContext()
    {
        return new SystemContext
        {
            Environment = "Development",
            Timestamp = DateTime.UtcNow,
            SystemLoad = new SystemLoad
            {
                CpuUtilization = 45.0,
                MemoryUtilization = 60.0,
                DiskUtilization = 30.0,
                NetworkUtilization = 25.0,
                RequestRate = 150.0,
                AverageResponseTime = 250.0
            },
            ErrorTrends = new ErrorRateTrends
            {
                CurrentErrorRate = 2.5,
                ErrorRateOneHourAgo = 3.1,
                ErrorRateOneDayAgo = 2.8,
                ErrorRateOneWeekAgo = 2.2,
                TrendDirection = TrendDirection.Decreasing,
                RecentErrorCategories = new Dictionary<string, int>
                {
                    { "Network", 15 },
                    { "HTTP", 8 },
                    { "Data", 5 },
                    { "General", 12 }
                }
            },
            UsagePatterns = new UsagePatterns
            {
                ActiveSessions = 25,
                PeakHours = new List<int> { 9, 10, 11, 14, 15, 16 },
                PopularEndpoints = new Dictionary<string, int>
                {
                    { "/api/test/endpoint1", 45 },
                    { "/api/test/endpoint2", 32 },
                    { "/api/test/endpoint3", 28 }
                }
            },
            BusinessContext = new BusinessContext
            {
                IsPeakHours = false,
                IsMaintenanceWindow = false,
                Priority = BusinessPriority.Normal
            },
            Resources = new ResourceAvailability
            {
                DevelopmentCapacity = 40.0,
                TestingCapacity = 20.0,
                InfrastructureBudget = 10000.0,
                EmergencyMaintenancePossible = true
            }
        };
    }

    #endregion
}

/// <summary>
/// Request model for creating optimization campaigns
/// </summary>
public class CreateCampaignsRequest
{
    /// <summary>
    /// List of suggestion IDs to group into campaigns
    /// </summary>
    public List<int> SuggestionIds { get; set; } = new();
}