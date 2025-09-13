using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalMe.Services.Learning.ErrorLearning.Models;
using DigitalMe.Services.Learning.ErrorLearning.SuggestionEngine.Models;

namespace DigitalMe.Services.Learning.ErrorLearning.SuggestionEngine;

/// <summary>
/// Advanced interface for intelligent optimization suggestion generation
/// Provides sophisticated algorithms for generating actionable recommendations
/// Following ISP with focused responsibility for suggestion intelligence
/// </summary>
public interface IAdvancedSuggestionEngine
{
    /// <summary>
    /// Generates comprehensive optimization suggestions for multiple error patterns
    /// Uses advanced algorithms to analyze patterns and generate intelligent recommendations
    /// </summary>
    /// <param name="patterns">Collection of error patterns to analyze</param>
    /// <returns>Prioritized list of optimization suggestions</returns>
    Task<List<OptimizationSuggestion>> GenerateComprehensiveSuggestionsAsync(IEnumerable<ErrorPattern> patterns);

    /// <summary>
    /// Generates prioritized suggestions based on system-wide analysis
    /// Identifies most impactful optimizations across all error patterns
    /// </summary>
    /// <param name="maxSuggestions">Maximum number of suggestions to return</param>
    /// <returns>Top priority optimization suggestions</returns>
    Task<List<OptimizationSuggestion>> GeneratePrioritizedSuggestionsAsync(int maxSuggestions = 20);

    /// <summary>
    /// Groups related suggestions into optimization campaigns
    /// Identifies synergistic optimizations that should be implemented together
    /// </summary>
    /// <param name="suggestions">Individual suggestions to group</param>
    /// <returns>Grouped suggestions organized by optimization themes</returns>
    Task<List<OptimizationCampaign>> GroupSuggestionsIntoCampaignsAsync(IEnumerable<OptimizationSuggestion> suggestions);

    /// <summary>
    /// Analyzes suggestion effectiveness and updates confidence scores
    /// Uses feedback data to improve future suggestion quality
    /// </summary>
    /// <returns>Number of suggestions analyzed and updated</returns>
    Task<int> AnalyzeSuggestionEffectivenessAsync();

    /// <summary>
    /// Generates contextual suggestions based on current system state
    /// Provides recommendations tailored to specific time, environment, or usage patterns
    /// </summary>
    /// <param name="context">Current system context for contextual suggestions</param>
    /// <returns>Context-aware optimization suggestions</returns>
    Task<List<OptimizationSuggestion>> GenerateContextualSuggestionsAsync(SystemContext context);
}