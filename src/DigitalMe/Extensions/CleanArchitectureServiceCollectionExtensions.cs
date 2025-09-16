using DigitalMe.Infrastructure;
using DigitalMe.Infrastructure.Repositories;
using DigitalMe.Services.ApplicationServices.Orchestrators;
using DigitalMe.Services.ApplicationServices.UseCases.FileProcessing;
using DigitalMe.Services.ApplicationServices.UseCases.HealthCheck;
using DigitalMe.Services.ApplicationServices.UseCases.ServiceAvailability;
using DigitalMe.Services.ApplicationServices.UseCases.WebNavigation;
using DigitalMe.Services.ApplicationServices.Workflows;
using DigitalMe.Services.PersonalityEngine;
using DigitalMe.Services.Strategies;

namespace DigitalMe.Extensions;

/// <summary>
/// Extension methods for registering Clean Architecture services.
/// Ensures proper dependency registration following architectural principles.
/// </summary>
public static class CleanArchitectureServiceCollectionExtensions
{
    /// <summary>
    /// Registers Clean Architecture services with proper layering and dependencies.
    /// </summary>
    public static IServiceCollection AddCleanArchitectureServices(this IServiceCollection services)
    {
        // Infrastructure Layer - Repository implementations
        services.AddSingleton<IFileRepository, FileSystemFileRepository>();
        
        // Application Layer - Use Cases (single responsibility)
        services.AddScoped<IFileProcessingUseCase, FileProcessingUseCase>();
        services.AddScoped<IWebNavigationUseCase, WebNavigationUseCase>();
        services.AddScoped<IServiceAvailabilityUseCase, ServiceAvailabilityUseCase>();
        services.AddScoped<IHealthCheckUseCase, HealthCheckUseCase>();
        
        // Application Layer - Orchestrators (composition only)
        services.AddScoped<IWorkflowOrchestrator, WorkflowOrchestrator>();
        
        // Application Layer - Workflow Services (TRUE integration)
        services.AddScoped<IWebNavigationWorkflowService, WebNavigationWorkflowService>();
        services.AddScoped<ICaptchaWorkflowService, CaptchaWorkflowService>();
        services.AddScoped<IIvanLevelWorkflowService, IvanLevelWorkflowService>();

        // Personality Engine Services - SOLID Refactored Architecture
        services.AddScoped<DigitalMe.Services.IPersonalityService, DigitalMe.Services.PersonalityService>();
        // Legacy compatibility
        services.AddScoped<DigitalMe.Services.IIvanPersonalityService>(provider =>
            provider.GetService<DigitalMe.Services.IPersonalityService>()!);
        services.AddScoped<DigitalMe.Services.IPersonalityBehaviorMapper, DigitalMe.Services.PersonalityBehaviorMapper>();
        services.AddScoped<DigitalMe.Services.IProfileDataParser, DigitalMe.Services.ProfileDataParser>();

        // Ivan-Specific Use Cases and Integration
        services.AddScoped<DigitalMe.Services.ApplicationServices.UseCases.Ivan.IIvanPersonalityUseCase,
                          DigitalMe.Services.ApplicationServices.UseCases.Ivan.IvanPersonalityUseCase>();

        // Personal Response Styling Services - Generic and Reusable
        services.AddScoped<DigitalMe.Services.ApplicationServices.ResponseStyling.IPersonalVocabularyService,
                          DigitalMe.Services.ApplicationServices.ResponseStyling.PersonalVocabularyService>();
        services.AddScoped<DigitalMe.Services.ApplicationServices.ResponseStyling.IPersonalLinguisticPatternService,
                          DigitalMe.Services.ApplicationServices.ResponseStyling.PersonalLinguisticPatternService>();
        services.AddScoped<DigitalMe.Services.ApplicationServices.ResponseStyling.IPersonalContextAnalyzer,
                          DigitalMe.Services.ApplicationServices.ResponseStyling.PersonalContextAnalyzer>();
        services.AddScoped<DigitalMe.Services.ApplicationServices.ResponseStyling.IPersonalResponseStylingService,
                          DigitalMe.Services.ApplicationServices.ResponseStyling.PersonalResponseStylingService>();

        // Legacy Ivan Response Styling Services - Backward Compatibility
        services.AddScoped<DigitalMe.Services.ApplicationServices.ResponseStyling.IIvanVocabularyService,
                          DigitalMe.Services.ApplicationServices.ResponseStyling.IvanVocabularyService>();
        services.AddScoped<DigitalMe.Services.ApplicationServices.ResponseStyling.IIvanLinguisticPatternService,
                          DigitalMe.Services.ApplicationServices.ResponseStyling.IvanLinguisticPatternService>();
        services.AddScoped<DigitalMe.Services.ApplicationServices.ResponseStyling.IIvanContextAnalyzer,
                          DigitalMe.Services.ApplicationServices.ResponseStyling.IvanContextAnalyzer>();
        services.AddScoped<DigitalMe.Services.ApplicationServices.ResponseStyling.IIvanResponseStylingService,
                          DigitalMe.Services.ApplicationServices.ResponseStyling.IvanResponseStylingServiceRefactored>();

        // Performance Optimization Services - ISP Compliant
        services.AddSingleton<DigitalMe.Services.Performance.ICachingService,
                           DigitalMe.Services.Performance.CachingService>();
        services.AddSingleton<DigitalMe.Services.Performance.IPerformanceOptimizationService,
                           DigitalMe.Services.Performance.PerformanceOptimizationService>();
        services.AddMemoryCache(); // Add memory cache support

        // Security Services
        services.AddScoped<DigitalMe.Services.Security.ISecurityValidationService,
                          DigitalMe.Services.Security.SecurityValidationService>();

        // Configuration Service
        services.AddSingleton<IPersonalityConfigurationService, PersonalityConfigurationService>();

        // Strategy Pattern Services
        services.AddScoped<IPersonalityStrategyFactory, PersonalityStrategyFactory>();
        services.AddScoped<IPersonalityAdapterStrategy, IvanPersonalityStrategy>();
        services.AddScoped<IPersonalityAdapterStrategy, GenericPersonalityStrategy>();

        // Specialized Analyzer Services (SRP compliance)
        services.AddScoped<IPersonalityContextAdapter, PersonalityContextAdapter>();
        services.AddScoped<IStressBehaviorAnalyzer, StressBehaviorAnalyzer>();
        services.AddScoped<IExpertiseConfidenceAnalyzer, ExpertiseConfidenceAnalyzer>();
        services.AddScoped<ICommunicationStyleAnalyzer, CommunicationStyleAnalyzer>();
        services.AddScoped<IContextAnalyzer, ContextAnalyzer>();

        // Main Orchestrator (now delegates to specialized services)
        services.AddScoped<DigitalMe.Services.IContextualPersonalityEngine, DigitalMe.Services.ContextualPersonalityEngine>();
        
        // Learning Infrastructure Services - Phase 1.1 refactored architecture
        services.AddLearningInfrastructureServices();
        
        return services;
    }

    /// <summary>
    /// Registers Learning Infrastructure services following Clean Architecture principles.
    /// All learning-related services with proper dependency injection.
    /// </summary>
    public static IServiceCollection AddLearningInfrastructureServices(this IServiceCollection services)
    {
        // Learning Infrastructure Services - Core interfaces
        services.AddTransient<DigitalMe.Services.Learning.IAutoDocumentationParser, DigitalMe.Services.Learning.AutoDocumentationParser>();
        services.AddTransient<DigitalMe.Services.Learning.ISelfTestingFramework, DigitalMe.Services.Learning.SelfTestingFramework>();
        
        // AutoDocumentationParser SOLID refactored services - Phase 2
        services.AddTransient<DigitalMe.Services.Learning.Documentation.HttpContentFetching.IDocumentationFetcher, DigitalMe.Services.Learning.Documentation.HttpContentFetching.DocumentationFetcher>();
        services.AddTransient<DigitalMe.Services.Learning.Documentation.ContentParsing.IDocumentationParser, DigitalMe.Services.Learning.Documentation.ContentParsing.DocumentationParser>();
        services.AddTransient<DigitalMe.Services.Learning.Documentation.PatternAnalysis.IUsagePatternAnalyzer, DigitalMe.Services.Learning.Documentation.PatternAnalysis.UsagePatternAnalyzer>();
        services.AddTransient<DigitalMe.Services.Learning.Documentation.TestGeneration.IApiTestCaseGenerator, DigitalMe.Services.Learning.Documentation.TestGeneration.ApiTestCaseGenerator>();
        
        // Test Infrastructure Components - T2.7 additions (extracted from SelfTestingFramework god class)
        services.AddTransient<DigitalMe.Services.Learning.Testing.TestGeneration.ITestCaseGenerator, DigitalMe.Services.Learning.Testing.TestGeneration.TestCaseGenerator>();
        
        // FIXED: Circular dependency resolved by separating single vs parallel execution
        services.AddTransient<DigitalMe.Services.Learning.Testing.TestExecution.ISingleTestExecutor, DigitalMe.Services.Learning.Testing.TestExecution.SingleTestExecutor>();
        services.AddTransient<DigitalMe.Services.Learning.Testing.TestExecution.ITestExecutor, DigitalMe.Services.Learning.Testing.TestExecution.TestExecutor>();
        services.AddTransient<DigitalMe.Services.Learning.Testing.ParallelProcessing.IParallelTestRunner, DigitalMe.Services.Learning.Testing.ParallelProcessing.ParallelTestRunner>();
        
        services.AddTransient<DigitalMe.Services.Learning.Testing.ResultsAnalysis.IResultsAnalyzer, DigitalMe.Services.Learning.Testing.ResultsAnalysis.ResultsAnalyzer>();
        services.AddTransient<DigitalMe.Services.Learning.Testing.Statistics.IStatisticalAnalyzer, DigitalMe.Services.Learning.Testing.Statistics.StatisticalAnalyzer>();

        // Focused ISP-compliant interfaces for orchestrator pattern
        services.AddTransient<DigitalMe.Services.Learning.Testing.ITestOrchestrator, DigitalMe.Services.Learning.Testing.TestOrchestratorService>();
        services.AddTransient<DigitalMe.Services.Learning.Testing.ICapabilityValidator, DigitalMe.Services.Learning.Testing.CapabilityValidatorService>();
        services.AddTransient<DigitalMe.Services.Learning.Testing.ITestAnalyzer, DigitalMe.Services.Learning.Testing.TestAnalyzerService>();

        // Error Learning System - Phase 3 (T3.1-T3.3)
        services.AddErrorLearningSystem();

        return services;
    }
}