using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalMe.Services.Learning.ErrorLearning.Models;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Learning.ErrorLearning;

/// <summary>
/// Error Learning Service orchestrator - coordinates focused services following SRP
/// Acts as a facade for error learning operations while delegating to specialized services
/// Implements composition pattern to avoid God Class anti-pattern
/// </summary>
public partial class ErrorLearningService : IErrorLearningService
{
    private readonly ILogger<ErrorLearningService> _logger;
    private readonly IErrorRecordingService _errorRecordingService;
    private readonly IPatternAnalysisService _patternAnalysisService;
    private readonly IOptimizationSuggestionManagementService _optimizationSuggestionService;
    private readonly ILearningStatisticsService _statisticsService;

    public ErrorLearningService(
        ILogger<ErrorLearningService> logger,
        IErrorRecordingService errorRecordingService,
        IPatternAnalysisService patternAnalysisService,
        IOptimizationSuggestionManagementService optimizationSuggestionService,
        ILearningStatisticsService statisticsService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _errorRecordingService = errorRecordingService ?? throw new ArgumentNullException(nameof(errorRecordingService));
        _patternAnalysisService = patternAnalysisService ?? throw new ArgumentNullException(nameof(patternAnalysisService));
        _optimizationSuggestionService = optimizationSuggestionService ?? throw new ArgumentNullException(nameof(optimizationSuggestionService));
        _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
    }

    /// <inheritdoc />
    public async Task<LearningHistoryEntry> RecordErrorAsync(
        string source, 
        string errorMessage, 
        string? testCaseName = null,
        string? apiName = null,
        string? httpMethod = null,
        string? apiEndpoint = null,
        int? httpStatusCode = null,
        string? requestDetails = null,
        string? responseDetails = null,
        string? stackTrace = null,
        string? environmentContext = null)
    {
        // Create request object to avoid God Method anti-pattern
        var request = new ErrorRecordingRequest
        {
            Source = source,
            ErrorMessage = errorMessage,
            TestCaseName = testCaseName,
            ApiName = apiName,
            HttpMethod = httpMethod,
            ApiEndpoint = apiEndpoint,
            HttpStatusCode = httpStatusCode,
            RequestDetails = requestDetails,
            ResponseDetails = responseDetails,
            StackTrace = stackTrace,
            EnvironmentContext = environmentContext
        };

        // Delegate to focused service following SRP
        return await _errorRecordingService.RecordErrorAsync(request);
    }

    /// <inheritdoc />
    public async Task<int> AnalyzeErrorPatternsAsync(int batchSize = 100)
    {
        // Delegate to pattern analysis service
        return await _patternAnalysisService.AnalyzeErrorPatternsAsync(batchSize);
    }

    /// <inheritdoc />
    public async Task<List<ErrorPattern>> GetErrorPatternsAsync(
        string? category = null,
        string? apiEndpoint = null,
        int? minOccurrenceCount = null,
        int? minSeverityLevel = null,
        double? minConfidenceScore = null)
    {
        // Delegate to pattern analysis service
        return await _patternAnalysisService.GetErrorPatternsAsync(
            category, apiEndpoint, minOccurrenceCount, minSeverityLevel, minConfidenceScore);
    }

    /// <inheritdoc />
    public async Task<List<LearningHistoryEntry>> GetLearningHistoryAsync(int errorPatternId, int limit = 50)
    {
        // Delegate to pattern analysis service
        return await _patternAnalysisService.GetLearningHistoryAsync(errorPatternId, limit);
    }

    /// <inheritdoc />
    public async Task<List<OptimizationSuggestion>> GenerateOptimizationSuggestionsAsync(int errorPatternId)
    {
        // Delegate to optimization suggestion service
        return await _optimizationSuggestionService.GenerateOptimizationSuggestionsAsync(errorPatternId);
    }

    /// <inheritdoc />
    public async Task<List<OptimizationSuggestion>> GetOptimizationSuggestionsAsync(
        OptimizationType? type = null,
        SuggestionStatus? status = null,
        int? minPriority = null,
        double? minConfidenceScore = null)
    {
        // Delegate to optimization suggestion service
        return await _optimizationSuggestionService.GetOptimizationSuggestionsAsync(
            type, status, minPriority, minConfidenceScore);
    }

    /// <inheritdoc />
    public async Task<OptimizationSuggestion> UpdateSuggestionStatusAsync(
        int suggestionId, 
        SuggestionStatus status, 
        string? reviewerNotes = null)
    {
        // Delegate to optimization suggestion service
        return await _optimizationSuggestionService.UpdateSuggestionStatusAsync(
            suggestionId, status, reviewerNotes);
    }

    /// <inheritdoc />
    public async Task<LearningStatistics> GetLearningStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        // Delegate to statistics service
        return await _statisticsService.GetLearningStatisticsAsync(fromDate, toDate);
    }
}