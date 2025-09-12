using DigitalMe.Services.ApplicationServices.Orchestrators;
using DigitalMe.Services.ApplicationServices.UseCases.FileProcessing;
using DigitalMe.Services.ApplicationServices.UseCases.WebNavigation;
using DigitalMe.Services.ApplicationServices.UseCases.ServiceAvailability;
using DigitalMe.Services.ApplicationServices.UseCases.HealthCheck;
using DigitalMe.Services.ApplicationServices.Workflows;
using DigitalMe.Infrastructure;
using DigitalMe.Infrastructure.Repositories;

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
        
        return services;
    }
}