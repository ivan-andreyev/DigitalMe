using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DigitalMe.Services.Learning.ErrorLearning.Models;
using DigitalMe.Services.Learning.ErrorLearning.Repositories;
using DigitalMe.Services.Learning.ErrorLearning.SuggestionEngine.Models;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Learning.ErrorLearning.SuggestionEngine;

/// <summary>
/// Advanced optimization suggestion engine with intelligent algorithms
/// Provides sophisticated analysis and prioritization of optimization opportunities
/// Implements SRP by focusing solely on advanced suggestion generation logic
/// </summary>
public class AdvancedSuggestionEngine : IAdvancedSuggestionEngine
{
    private readonly ILogger<AdvancedSuggestionEngine> _logger;
    private readonly IErrorPatternRepository _errorPatternRepository;
    private readonly IOptimizationSuggestionRepository _optimizationSuggestionRepository;
    private readonly IOptimizationSuggestionManagementService _suggestionManagementService;

    public AdvancedSuggestionEngine(
        ILogger<AdvancedSuggestionEngine> logger,
        IErrorPatternRepository errorPatternRepository,
        IOptimizationSuggestionRepository optimizationSuggestionRepository,
        IOptimizationSuggestionManagementService suggestionManagementService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _errorPatternRepository = errorPatternRepository ?? throw new ArgumentNullException(nameof(errorPatternRepository));
        _optimizationSuggestionRepository = optimizationSuggestionRepository ?? throw new ArgumentNullException(nameof(optimizationSuggestionRepository));
        _suggestionManagementService = suggestionManagementService ?? throw new ArgumentNullException(nameof(suggestionManagementService));
    }

    /// <inheritdoc />
    public async Task<List<OptimizationSuggestion>> GenerateComprehensiveSuggestionsAsync(IEnumerable<ErrorPattern> patterns)
    {
        if (patterns == null)
            throw new ArgumentNullException(nameof(patterns));

        try
        {
            _logger.LogInformation("Generating comprehensive optimization suggestions for {PatternCount} patterns", patterns.Count());

            var allSuggestions = new List<OptimizationSuggestion>();

            // Generate suggestions for each pattern
            foreach (var pattern in patterns)
            {
                var patternSuggestions = await _suggestionManagementService.GenerateOptimizationSuggestionsAsync(pattern.Id);
                allSuggestions.AddRange(patternSuggestions);
            }

            // Apply intelligent filtering and enhancement
            var enhancedSuggestions = await EnhanceSuggestionsWithIntelligence(allSuggestions, patterns);

            // Remove duplicates and merge similar suggestions
            var dedupedSuggestions = await DeduplicateAndMergeSuggestions(enhancedSuggestions);

            // Apply impact-based prioritization
            var prioritizedSuggestions = PrioritizeSuggestionsByImpact(dedupedSuggestions);

            _logger.LogInformation("Generated {TotalSuggestions} comprehensive optimization suggestions", prioritizedSuggestions.Count);

            return prioritizedSuggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate comprehensive optimization suggestions");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<OptimizationSuggestion>> GeneratePrioritizedSuggestionsAsync(int maxSuggestions = 20)
    {
        try
        {
            _logger.LogInformation("Generating top {MaxSuggestions} prioritized optimization suggestions", maxSuggestions);

            // Get high-impact error patterns
            var patterns = await GetHighImpactErrorPatterns();

            // Generate comprehensive suggestions
            var allSuggestions = await GenerateComprehensiveSuggestionsAsync(patterns);

            // Apply advanced prioritization algorithm
            var prioritizedSuggestions = await ApplyAdvancedPrioritization(allSuggestions);

            // Take top N suggestions
            var topSuggestions = prioritizedSuggestions.Take(maxSuggestions).ToList();

            _logger.LogInformation("Selected top {Count} prioritized suggestions from {Total} candidates",
                topSuggestions.Count, allSuggestions.Count);

            return topSuggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate prioritized optimization suggestions");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<OptimizationCampaign>> GroupSuggestionsIntoCampaignsAsync(IEnumerable<OptimizationSuggestion> suggestions)
    {
        if (suggestions == null)
            throw new ArgumentNullException(nameof(suggestions));

        try
        {
            _logger.LogInformation("Grouping {SuggestionCount} suggestions into optimization campaigns", suggestions.Count());

            var campaigns = new List<OptimizationCampaign>();
            var suggestionList = suggestions.ToList();

            // Group by optimization theme
            var themeGroups = GroupSuggestionsByTheme(suggestionList);

            foreach (var themeGroup in themeGroups)
            {
                var campaign = await CreateOptimizationCampaign(themeGroup.Key, themeGroup.Value);
                campaigns.Add(campaign);
            }

            // Optimize campaign structure
            var optimizedCampaigns = await OptimizeCampaignStructure(campaigns);

            _logger.LogInformation("Created {CampaignCount} optimization campaigns", optimizedCampaigns.Count);

            return optimizedCampaigns;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to group suggestions into campaigns");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> AnalyzeSuggestionEffectivenessAsync()
    {
        try
        {
            _logger.LogInformation("Analyzing suggestion effectiveness for confidence score updates");

            // Get implemented suggestions with outcomes
            var implementedSuggestions = await _optimizationSuggestionRepository.GetSuggestionsAsync(
                status: SuggestionStatus.Implemented);

            var analyzedCount = 0;

            foreach (var suggestion in implementedSuggestions)
            {
                // Analyze effectiveness based on error pattern changes
                var effectiveness = await AnalyzeIndividualSuggestionEffectiveness(suggestion);

                // Update confidence score based on effectiveness
                await UpdateSuggestionConfidenceScore(suggestion, effectiveness);

                analyzedCount++;
            }

            _logger.LogInformation("Analyzed effectiveness of {Count} implemented suggestions", analyzedCount);

            return analyzedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze suggestion effectiveness");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<List<OptimizationSuggestion>> GenerateContextualSuggestionsAsync(SystemContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        try
        {
            _logger.LogInformation("Generating contextual optimization suggestions for environment: {Environment}",
                context.Environment);

            // Get suggestions based on current context
            var contextualSuggestions = new List<OptimizationSuggestion>();

            // Analyze context for urgent issues
            if (IsHighErrorRateContext(context))
            {
                var urgentSuggestions = await GenerateUrgentErrorReductionSuggestions(context);
                contextualSuggestions.AddRange(urgentSuggestions);
            }

            // Performance optimization during high load
            if (IsHighLoadContext(context))
            {
                var performanceSuggestions = await GeneratePerformanceOptimizationSuggestions(context);
                contextualSuggestions.AddRange(performanceSuggestions);
            }

            // Maintenance window optimizations
            if (IsMaintenanceWindowContext(context))
            {
                var maintenanceSuggestions = await GenerateMaintenanceWindowSuggestions(context);
                contextualSuggestions.AddRange(maintenanceSuggestions);
            }

            // Filter by resource availability
            var feasibleSuggestions = FilterSuggestionsByResourceAvailability(contextualSuggestions, context.Resources);

            _logger.LogInformation("Generated {Count} contextual optimization suggestions", feasibleSuggestions.Count);

            return feasibleSuggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate contextual optimization suggestions");
            throw;
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Enhances suggestions with additional intelligence and metadata
    /// </summary>
    private async Task<List<OptimizationSuggestion>> EnhanceSuggestionsWithIntelligence(
        List<OptimizationSuggestion> suggestions,
        IEnumerable<ErrorPattern> patterns)
    {
        var enhancedSuggestions = new List<OptimizationSuggestion>();

        foreach (var suggestion in suggestions)
        {
            // Calculate enhanced confidence score
            suggestion.ConfidenceScore = CalculateEnhancedConfidenceScore(suggestion, patterns);

            // Estimate effort hours if not set
            if (!suggestion.EstimatedEffortHours.HasValue)
            {
                suggestion.EstimatedEffortHours = EstimateImplementationEffort(suggestion);
            }

            // Add intelligent tags
            suggestion.Tags = GenerateIntelligentTags(suggestion, patterns);

            enhancedSuggestions.Add(suggestion);
        }

        return enhancedSuggestions;
    }

    /// <summary>
    /// Removes duplicate suggestions and merges similar ones
    /// </summary>
    private async Task<List<OptimizationSuggestion>> DeduplicateAndMergeSuggestions(
        List<OptimizationSuggestion> suggestions)
    {
        var dedupedSuggestions = new List<OptimizationSuggestion>();
        var processedSuggestions = new HashSet<int>();

        foreach (var suggestion in suggestions)
        {
            if (processedSuggestions.Contains(suggestion.Id))
                continue;

            // Find similar suggestions
            var similarSuggestions = suggestions
                .Where(s => s.Id != suggestion.Id && AreSuggestionsStimilar(suggestion, s))
                .ToList();

            if (similarSuggestions.Any())
            {
                // Merge similar suggestions
                var mergedSuggestion = MergeSimilarSuggestions(suggestion, similarSuggestions);
                dedupedSuggestions.Add(mergedSuggestion);

                // Mark all as processed
                processedSuggestions.Add(suggestion.Id);
                foreach (var similar in similarSuggestions)
                {
                    processedSuggestions.Add(similar.Id);
                }
            }
            else
            {
                dedupedSuggestions.Add(suggestion);
                processedSuggestions.Add(suggestion.Id);
            }
        }

        return dedupedSuggestions;
    }

    /// <summary>
    /// Prioritizes suggestions by impact using multiple factors
    /// </summary>
    private List<OptimizationSuggestion> PrioritizeSuggestionsByImpact(List<OptimizationSuggestion> suggestions)
    {
        return suggestions
            .OrderByDescending(s => CalculateImpactScore(s))
            .ThenByDescending(s => s.ConfidenceScore)
            .ThenBy(s => s.EstimatedEffortHours ?? double.MaxValue)
            .ToList();
    }

    /// <summary>
    /// Gets high-impact error patterns for analysis
    /// </summary>
    private async Task<List<ErrorPattern>> GetHighImpactErrorPatterns()
    {
        // Get patterns with high occurrence count and severity
        return await _errorPatternRepository.GetPatternsAsync(
            minOccurrenceCount: 3,
            minSeverityLevel: 3,
            minConfidenceScore: 0.6);
    }

    /// <summary>
    /// Applies advanced prioritization algorithm considering multiple factors
    /// </summary>
    private async Task<List<OptimizationSuggestion>> ApplyAdvancedPrioritization(List<OptimizationSuggestion> suggestions)
    {
        foreach (var suggestion in suggestions)
        {
            // Calculate comprehensive priority score
            var priorityScore = CalculateComprehensivePriorityScore(suggestion);
            suggestion.Priority = (int)Math.Ceiling(priorityScore * 5); // Scale to 1-5
        }

        return suggestions
            .OrderByDescending(s => s.Priority)
            .ThenByDescending(s => s.ConfidenceScore)
            .ToList();
    }

    /// <summary>
    /// Groups suggestions by optimization theme
    /// </summary>
    private Dictionary<string, List<OptimizationSuggestion>> GroupSuggestionsByTheme(List<OptimizationSuggestion> suggestions)
    {
        var themes = new Dictionary<string, List<OptimizationSuggestion>>();

        foreach (var suggestion in suggestions)
        {
            var theme = DetermineOptimizationTheme(suggestion);

            if (!themes.ContainsKey(theme))
            {
                themes[theme] = new List<OptimizationSuggestion>();
            }

            themes[theme].Add(suggestion);
        }

        return themes;
    }

    /// <summary>
    /// Creates an optimization campaign from a theme group
    /// </summary>
    private async Task<OptimizationCampaign> CreateOptimizationCampaign(string theme, List<OptimizationSuggestion> suggestions)
    {
        return new OptimizationCampaign
        {
            Name = $"{theme} Optimization Campaign",
            Description = $"Comprehensive optimization campaign focused on {theme.ToLower()} improvements",
            Theme = theme,
            Priority = suggestions.Max(s => s.Priority),
            EstimatedImpact = suggestions.Average(s => CalculateImpactScore(s)),
            ConfidenceScore = suggestions.Average(s => s.ConfidenceScore),
            EstimatedEffortHours = suggestions.Sum(s => s.EstimatedEffortHours ?? 0),
            Suggestions = suggestions,
            ExpectedOutcomes = GenerateExpectedOutcomes(theme, suggestions),
            SuccessMetrics = GenerateSuccessMetrics(theme, suggestions),
            ImplementationPhases = CreateImplementationPhases(suggestions)
        };
    }

    /// <summary>
    /// Optimizes campaign structure for better implementation planning
    /// </summary>
    private async Task<List<OptimizationCampaign>> OptimizeCampaignStructure(List<OptimizationCampaign> campaigns)
    {
        // Remove campaigns with too few suggestions
        campaigns = campaigns.Where(c => c.Suggestions.Count >= 2).ToList();

        // Merge very small campaigns into larger ones
        var optimizedCampaigns = new List<OptimizationCampaign>();

        foreach (var campaign in campaigns.OrderByDescending(c => c.EstimatedImpact))
        {
            if (campaign.Suggestions.Count >= 3 || campaign.EstimatedImpact > 0.7)
            {
                optimizedCampaigns.Add(campaign);
            }
            else
            {
                // Try to merge into existing similar campaign
                var similarCampaign = optimizedCampaigns
                    .FirstOrDefault(c => c.Theme == campaign.Theme && c.Suggestions.Count < 8);

                if (similarCampaign != null)
                {
                    MergeCampaigns(similarCampaign, campaign);
                }
                else
                {
                    optimizedCampaigns.Add(campaign);
                }
            }
        }

        return optimizedCampaigns;
    }

    /// <summary>
    /// Determines if two suggestions are similar enough to merge
    /// </summary>
    private bool AreSuggestionsStimilar(OptimizationSuggestion suggestion1, OptimizationSuggestion suggestion2)
    {
        if (suggestion1.Type != suggestion2.Type)
            return false;

        if (suggestion1.TargetComponent != suggestion2.TargetComponent)
            return false;

        // Calculate text similarity for title and description
        var titleSimilarity = CalculateStringSimilarity(suggestion1.Title, suggestion2.Title);
        var descriptionSimilarity = CalculateStringSimilarity(suggestion1.Description, suggestion2.Description);

        return titleSimilarity > 0.7 || descriptionSimilarity > 0.6;
    }

    /// <summary>
    /// Merges similar suggestions into a single comprehensive suggestion
    /// </summary>
    private OptimizationSuggestion MergeSimilarSuggestions(
        OptimizationSuggestion primary,
        List<OptimizationSuggestion> similar)
    {
        primary.ConfidenceScore = (new[] { primary.ConfidenceScore }
            .Concat(similar.Select(s => s.ConfidenceScore))).Average();

        primary.Priority = Math.Max(primary.Priority, similar.Max(s => s.Priority));

        primary.EstimatedEffortHours = (primary.EstimatedEffortHours ?? 0) +
            similar.Sum(s => s.EstimatedEffortHours ?? 0);

        primary.Description += $" (Consolidated from {similar.Count + 1} similar suggestions)";

        return primary;
    }

    /// <summary>
    /// Calculates impact score for prioritization
    /// </summary>
    private double CalculateImpactScore(OptimizationSuggestion suggestion)
    {
        var baseScore = suggestion.Priority / 5.0;
        var confidenceWeight = suggestion.ConfidenceScore;
        var effortPenalty = 1.0 - Math.Min(0.5, (suggestion.EstimatedEffortHours ?? 1) / 40.0);

        return baseScore * confidenceWeight * effortPenalty;
    }

    /// <summary>
    /// Calculates enhanced confidence score using pattern analysis
    /// </summary>
    private double CalculateEnhancedConfidenceScore(OptimizationSuggestion suggestion, IEnumerable<ErrorPattern> patterns)
    {
        var relatedPattern = patterns.FirstOrDefault(p => p.Id == suggestion.ErrorPatternId);
        if (relatedPattern == null)
            return suggestion.ConfidenceScore;

        var patternConfidence = relatedPattern.ConfidenceScore;
        var occurrenceWeight = Math.Min(1.0, relatedPattern.OccurrenceCount / 10.0);
        var severityWeight = relatedPattern.SeverityLevel / 5.0;

        return (suggestion.ConfidenceScore + patternConfidence + occurrenceWeight + severityWeight) / 4.0;
    }

    /// <summary>
    /// Estimates implementation effort in hours
    /// </summary>
    private double EstimateImplementationEffort(OptimizationSuggestion suggestion)
    {
        // Base effort by suggestion type
        var baseEffort = suggestion.Type switch
        {
            OptimizationType.TestCaseOptimization => 4.0,
            OptimizationType.ErrorHandlingImprovement => 8.0,
            OptimizationType.TimeoutOptimization => 2.0,
            OptimizationType.AssertionImprovement => 3.0,
            OptimizationType.ArchitecturalImprovement => 24.0,
            OptimizationType.PerformanceOptimization => 16.0,
            _ => 8.0
        };

        // Adjust by priority (higher priority might need more careful implementation)
        var priorityMultiplier = 1.0 + (suggestion.Priority - 3) * 0.2;

        return baseEffort * priorityMultiplier;
    }

    /// <summary>
    /// Generates intelligent tags for suggestions
    /// </summary>
    private string GenerateIntelligentTags(OptimizationSuggestion suggestion, IEnumerable<ErrorPattern> patterns)
    {
        var tags = new List<string>();

        // Add type-based tags
        tags.Add(suggestion.Type.ToString());

        // Add priority-based tags
        if (suggestion.Priority >= 4)
            tags.Add("HighPriority");
        else if (suggestion.Priority <= 2)
            tags.Add("LowPriority");

        // Add confidence-based tags
        if (suggestion.ConfidenceScore >= 0.8)
            tags.Add("HighConfidence");
        else if (suggestion.ConfidenceScore <= 0.5)
            tags.Add("LowConfidence");

        // Add effort-based tags
        if (suggestion.EstimatedEffortHours <= 4)
            tags.Add("QuickWin");
        else if (suggestion.EstimatedEffortHours >= 20)
            tags.Add("MajorProject");

        return string.Join(",", tags);
    }

    /// <summary>
    /// Calculates comprehensive priority score considering multiple factors
    /// </summary>
    private double CalculateComprehensivePriorityScore(OptimizationSuggestion suggestion)
    {
        var impactScore = CalculateImpactScore(suggestion);
        var urgencyScore = suggestion.Priority / 5.0;
        var feasibilityScore = 1.0 - Math.Min(0.8, (suggestion.EstimatedEffortHours ?? 1) / 50.0);
        var confidenceScore = suggestion.ConfidenceScore;

        // Weighted average: impact 40%, urgency 30%, feasibility 20%, confidence 10%
        return (impactScore * 0.4) + (urgencyScore * 0.3) + (feasibilityScore * 0.2) + (confidenceScore * 0.1);
    }

    /// <summary>
    /// Determines optimization theme for grouping
    /// </summary>
    private string DetermineOptimizationTheme(OptimizationSuggestion suggestion)
    {
        return suggestion.Type switch
        {
            OptimizationType.TestCaseOptimization => "Testing Quality",
            OptimizationType.ErrorHandlingImprovement => "Error Resilience",
            OptimizationType.TimeoutOptimization => "Performance",
            OptimizationType.AssertionImprovement => "Testing Quality",
            OptimizationType.ArchitecturalImprovement => "Architecture",
            OptimizationType.PerformanceOptimization => "Performance",
            OptimizationType.CodeQualityImprovement => "Code Quality",
            _ => "General Improvements"
        };
    }

    /// <summary>
    /// Generates expected outcomes for campaign
    /// </summary>
    private List<string> GenerateExpectedOutcomes(string theme, List<OptimizationSuggestion> suggestions)
    {
        var outcomes = new List<string>();

        switch (theme)
        {
            case "Testing Quality":
                outcomes.Add("Reduced test flakiness and false positives");
                outcomes.Add("Improved test coverage and reliability");
                outcomes.Add("Faster feedback loop for developers");
                break;
            case "Error Resilience":
                outcomes.Add("Reduced system downtime and error rates");
                outcomes.Add("Better error recovery and user experience");
                outcomes.Add("Improved system stability under load");
                break;
            case "Performance":
                outcomes.Add("Reduced response times and resource usage");
                outcomes.Add("Improved scalability and throughput");
                outcomes.Add("Better user experience and satisfaction");
                break;
            default:
                outcomes.Add($"Improved {theme.ToLower()} across the system");
                break;
        }

        return outcomes;
    }

    /// <summary>
    /// Generates success metrics for campaign
    /// </summary>
    private List<string> GenerateSuccessMetrics(string theme, List<OptimizationSuggestion> suggestions)
    {
        var metrics = new List<string>();

        switch (theme)
        {
            case "Testing Quality":
                metrics.Add("Test success rate > 95%");
                metrics.Add("Test execution time reduction > 20%");
                metrics.Add("False positive rate < 5%");
                break;
            case "Error Resilience":
                metrics.Add("Error rate reduction > 50%");
                metrics.Add("Mean time to recovery < 5 minutes");
                metrics.Add("System uptime > 99.9%");
                break;
            case "Performance":
                metrics.Add("Response time improvement > 30%");
                metrics.Add("CPU utilization reduction > 15%");
                metrics.Add("Memory usage optimization > 20%");
                break;
            default:
                metrics.Add($"{theme} improvement metrics to be defined");
                break;
        }

        return metrics;
    }

    /// <summary>
    /// Creates implementation phases for campaign
    /// </summary>
    private List<CampaignPhase> CreateImplementationPhases(List<OptimizationSuggestion> suggestions)
    {
        var phases = new List<CampaignPhase>();

        // Group suggestions by effort and dependencies
        var quickWins = suggestions.Where(s => (s.EstimatedEffortHours ?? 0) <= 4).ToList();
        var mediumTasks = suggestions.Where(s => (s.EstimatedEffortHours ?? 0) > 4 && (s.EstimatedEffortHours ?? 0) <= 16).ToList();
        var largeTasks = suggestions.Where(s => (s.EstimatedEffortHours ?? 0) > 16).ToList();

        if (quickWins.Any())
        {
            phases.Add(new CampaignPhase
            {
                Name = "Quick Wins",
                Description = "Low-effort, high-impact optimizations",
                SuggestionIds = quickWins.Select(s => s.Id).ToList(),
                EstimatedDuration = TimeSpan.FromDays(2),
                Deliverables = new List<string> { "Immediate improvements", "Foundation for larger optimizations" }
            });
        }

        if (mediumTasks.Any())
        {
            phases.Add(new CampaignPhase
            {
                Name = "Core Improvements",
                Description = "Medium-effort optimizations with significant impact",
                SuggestionIds = mediumTasks.Select(s => s.Id).ToList(),
                EstimatedDuration = TimeSpan.FromDays(7),
                Prerequisites = quickWins.Any() ? new List<string> { "Quick Wins" } : new List<string>(),
                Deliverables = new List<string> { "Core system improvements", "Performance enhancements" }
            });
        }

        if (largeTasks.Any())
        {
            phases.Add(new CampaignPhase
            {
                Name = "Architectural Enhancements",
                Description = "High-effort, transformational improvements",
                SuggestionIds = largeTasks.Select(s => s.Id).ToList(),
                EstimatedDuration = TimeSpan.FromDays(14),
                Prerequisites = phases.Select(p => p.Name).ToList(),
                Deliverables = new List<string> { "Architectural improvements", "Long-term stability enhancements" }
            });
        }

        return phases;
    }

    /// <summary>
    /// Merges two campaigns together
    /// </summary>
    private void MergeCampaigns(OptimizationCampaign target, OptimizationCampaign source)
    {
        target.Suggestions.AddRange(source.Suggestions);
        target.EstimatedEffortHours += source.EstimatedEffortHours;
        target.EstimatedImpact = (target.EstimatedImpact + source.EstimatedImpact) / 2;
        target.ConfidenceScore = (target.ConfidenceScore + source.ConfidenceScore) / 2;
        target.Priority = Math.Max(target.Priority, source.Priority);
        target.ExpectedOutcomes.AddRange(source.ExpectedOutcomes.Except(target.ExpectedOutcomes));
        target.SuccessMetrics.AddRange(source.SuccessMetrics.Except(target.SuccessMetrics));
    }

    /// <summary>
    /// Calculates string similarity for suggestion comparison
    /// </summary>
    private double CalculateStringSimilarity(string str1, string str2)
    {
        if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
            return 0.0;

        var longer = str1.Length > str2.Length ? str1 : str2;
        var shorter = str1.Length > str2.Length ? str2 : str1;

        if (longer.Length == 0)
            return 1.0;

        return (longer.Length - LevenshteinDistance(longer, shorter)) / (double)longer.Length;
    }

    /// <summary>
    /// Calculates Levenshtein distance between two strings
    /// </summary>
    private int LevenshteinDistance(string str1, string str2)
    {
        var matrix = new int[str1.Length + 1, str2.Length + 1];

        for (int i = 0; i <= str1.Length; i++)
            matrix[i, 0] = i;

        for (int j = 0; j <= str2.Length; j++)
            matrix[0, j] = j;

        for (int i = 1; i <= str1.Length; i++)
        {
            for (int j = 1; j <= str2.Length; j++)
            {
                var cost = str1[i - 1] == str2[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[str1.Length, str2.Length];
    }

    /// <summary>
    /// Analyzes effectiveness of individual suggestion
    /// </summary>
    private async Task<double> AnalyzeIndividualSuggestionEffectiveness(OptimizationSuggestion suggestion)
    {
        // Placeholder for effectiveness analysis
        // In real implementation, this would analyze error rate changes before/after implementation
        return 0.8; // Default effectiveness score
    }

    /// <summary>
    /// Updates suggestion confidence score based on effectiveness analysis
    /// </summary>
    private async Task UpdateSuggestionConfidenceScore(OptimizationSuggestion suggestion, double effectiveness)
    {
        // Update confidence score based on real-world effectiveness
        var newConfidenceScore = (suggestion.ConfidenceScore + effectiveness) / 2.0;

        await _optimizationSuggestionRepository.UpdateConfidenceScoreAsync(suggestion.Id, newConfidenceScore);
    }

    /// <summary>
    /// Checks if current context indicates high error rate
    /// </summary>
    private bool IsHighErrorRateContext(SystemContext context)
    {
        return context.ErrorTrends.CurrentErrorRate > context.ErrorTrends.ErrorRateOneDayAgo * 1.5;
    }

    /// <summary>
    /// Checks if current context indicates high system load
    /// </summary>
    private bool IsHighLoadContext(SystemContext context)
    {
        return context.SystemLoad.CpuUtilization > 80 ||
               context.SystemLoad.MemoryUtilization > 85 ||
               context.SystemLoad.AverageResponseTime > 2000;
    }

    /// <summary>
    /// Checks if current context is during maintenance window
    /// </summary>
    private bool IsMaintenanceWindowContext(SystemContext context)
    {
        return context.BusinessContext.IsMaintenanceWindow;
    }

    /// <summary>
    /// Generates urgent error reduction suggestions
    /// </summary>
    private async Task<List<OptimizationSuggestion>> GenerateUrgentErrorReductionSuggestions(SystemContext context)
    {
        var suggestions = new List<OptimizationSuggestion>();

        // Focus on most common recent error categories
        foreach (var errorCategory in context.ErrorTrends.RecentErrorCategories.Take(3))
        {
            var patterns = await _errorPatternRepository.GetPatternsByCategoryAsync(errorCategory.Key);
            foreach (var pattern in patterns.Take(2)) // Limit to top 2 patterns per category
            {
                var patternSuggestions = await _suggestionManagementService.GenerateOptimizationSuggestionsAsync(pattern.Id);

                // Mark as urgent
                foreach (var suggestion in patternSuggestions)
                {
                    suggestion.Priority = 5; // Maximum priority
                    suggestion.Tags = $"Urgent,{suggestion.Tags}";
                }

                suggestions.AddRange(patternSuggestions);
            }
        }

        return suggestions;
    }

    /// <summary>
    /// Generates performance optimization suggestions for high load
    /// </summary>
    private async Task<List<OptimizationSuggestion>> GeneratePerformanceOptimizationSuggestions(SystemContext context)
    {
        var suggestions = new List<OptimizationSuggestion>();

        // Get performance-related patterns
        var performancePatterns = await _errorPatternRepository.GetPatternsByCategoryAsync("Performance");

        foreach (var pattern in performancePatterns.Take(5))
        {
            var patternSuggestions = await _suggestionManagementService.GenerateOptimizationSuggestionsAsync(pattern.Id);

            // Filter for performance-related suggestions
            var performanceSuggestions = patternSuggestions
                .Where(s => s.Type == OptimizationType.PerformanceOptimization ||
                           s.Type == OptimizationType.TimeoutOptimization)
                .ToList();

            foreach (var suggestion in performanceSuggestions)
            {
                suggestion.Tags = $"Performance,HighLoad,{suggestion.Tags}";
            }

            suggestions.AddRange(performanceSuggestions);
        }

        return suggestions;
    }

    /// <summary>
    /// Generates maintenance window specific suggestions
    /// </summary>
    private async Task<List<OptimizationSuggestion>> GenerateMaintenanceWindowSuggestions(SystemContext context)
    {
        var suggestions = new List<OptimizationSuggestion>();

        // Get all patterns for comprehensive maintenance optimizations
        var allPatterns = await _errorPatternRepository.GetPatternsAsync();

        foreach (var pattern in allPatterns.Take(10))
        {
            var patternSuggestions = await _suggestionManagementService.GenerateOptimizationSuggestionsAsync(pattern.Id);

            // Focus on architectural and major improvements suitable for maintenance windows
            var maintenanceSuggestions = patternSuggestions
                .Where(s => s.Type == OptimizationType.ArchitecturalImprovement ||
                           (s.EstimatedEffortHours ?? 0) > 8) // Larger tasks suitable for maintenance
                .ToList();

            foreach (var suggestion in maintenanceSuggestions)
            {
                suggestion.Tags = $"MaintenanceWindow,{suggestion.Tags}";
            }

            suggestions.AddRange(maintenanceSuggestions);
        }

        return suggestions;
    }

    /// <summary>
    /// Filters suggestions by available resources
    /// </summary>
    private List<OptimizationSuggestion> FilterSuggestionsByResourceAvailability(
        List<OptimizationSuggestion> suggestions,
        ResourceAvailability resources)
    {
        return suggestions
            .Where(s => (s.EstimatedEffortHours ?? 0) <= resources.DevelopmentCapacity)
            .ToList();
    }

    #endregion
}