using DigitalMe.Services.Learning.ErrorLearning;
using DigitalMe.Services.Learning.ErrorLearning.Integration;
using DigitalMe.Services.Learning.ErrorLearning.Repositories;
using DigitalMe.Services.Learning.ErrorLearning.SuggestionEngine;
using DigitalMe.Services.Learning.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalMe.Extensions;

/// <summary>
/// Extension methods for registering Error Learning System services
/// </summary>
public static class ErrorLearningServiceCollectionExtensions
{
    /// <summary>
    /// Registers Error Learning System services and repositories
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddErrorLearningSystem(this IServiceCollection services)
    {
        // Register repositories
        services.AddScoped<IErrorPatternRepository, ErrorPatternRepository>();
        services.AddScoped<ILearningHistoryRepository, LearningHistoryRepository>();
        services.AddScoped<IOptimizationSuggestionRepository, OptimizationSuggestionRepository>();

        // Register focused services following SRP
        services.AddScoped<IErrorRecordingService, ErrorRecordingService>();
        services.AddScoped<IPatternAnalysisService, PatternAnalysisService>();
        services.AddScoped<IOptimizationSuggestionManagementService, OptimizationSuggestionManagementService>();
        services.AddScoped<ILearningStatisticsService, LearningStatisticsService>();

        // Register main orchestrator service
        services.AddScoped<IErrorLearningService, ErrorLearningService>();

        // Register integration services for SelfTestingFramework
        services.AddScoped<ITestFailureCapture, TestFailureCaptureService>();

        // Register learning-enabled test orchestrator as decorator
        services.AddScoped<LearningEnabledTestOrchestrator>();

        // Register orchestrator factory
        services.AddScoped<TestOrchestratorFactory>();

        // Register advanced suggestion engine
        services.AddScoped<IAdvancedSuggestionEngine, AdvancedSuggestionEngine>();

        return services;
    }
}