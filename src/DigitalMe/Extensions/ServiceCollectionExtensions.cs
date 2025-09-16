using DigitalMe.Configuration;
using DigitalMe.Data;
using DigitalMe.Integrations.External.GitHub;
using DigitalMe.Integrations.External.Google;
using DigitalMe.Integrations.External.Telegram;
using DigitalMe.Integrations.MCP;
using DigitalMe.Repositories;
using DigitalMe.Services;
using DigitalMe.Services.Performance;
using DigitalMe.Services.Resilience;
using DigitalMe.Services.Security;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;

namespace DigitalMe.Extensions;

/// <summary>
/// Extension methods for service registration to standardize DI pattern
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register all repositories
    /// </summary>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IPersonalityRepository, PersonalityRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();

        return services;
    }

    /// <summary>
    /// Register all business logic services
    /// </summary>
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        // MVP: Use simplified PersonalityService without repository pattern
        // Register both interfaces for ISP compliance
        services.AddScoped<MvpPersonalityService>();
        services.AddScoped<IPersonalityService>(provider => provider.GetRequiredService<MvpPersonalityService>());
        services.AddScoped<IMvpPersonalityService>(provider => provider.GetRequiredService<MvpPersonalityService>());
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IProfileDataParser, ProfileDataParser>();
        services.AddScoped<IMessageProcessor, MessageProcessor>();
        services.AddScoped<IMvpMessageProcessor, MvpMessageProcessor>();
        services.AddScoped<IHealthChecker, HealthChecker>();
        services.AddScoped<IIvanLevelHealthCheckService, PersonalLevelHealthCheckService>();
        services.AddScoped<IPersonalLevelHealthCheckService, PersonalLevelHealthCheckService>();

        // Resilience services
        services.AddSingleton<IResiliencePolicyService, ResiliencePolicyService>();

        // Performance optimization services moved to CleanArchitectureServiceCollectionExtensions
        // services.AddMemoryCache(); // Already added in CleanArchitectureServiceCollectionExtensions

        // Security services
        services.AddScoped<ISecurityValidationService, SecurityValidationService>();

        // Health check services
        services.AddScoped<DigitalMe.Services.Monitoring.IHealthCheckService, DigitalMe.Services.Monitoring.HealthCheckService>();

        // Performance metrics services moved to Program.cs for centralized monitoring setup

        // Ivan-Level capability services - Phase B Week 1
        services.AddScoped<DigitalMe.Services.FileProcessing.FileProcessingService>();
        services.AddScoped<DigitalMe.Services.FileProcessing.IFileProcessingService>(provider => 
            provider.GetRequiredService<DigitalMe.Services.FileProcessing.FileProcessingService>());
        
        // Focused file processing interfaces following ISP
        services.AddScoped<DigitalMe.Services.FileProcessing.IDocumentProcessor>(provider => 
            provider.GetRequiredService<DigitalMe.Services.FileProcessing.FileProcessingService>());
        services.AddScoped<DigitalMe.Services.FileProcessing.IFileUtilities>(provider => 
            provider.GetRequiredService<DigitalMe.Services.FileProcessing.FileProcessingService>());
        
        // Ivan-Level capability services - Phase B Week 2
        services.AddScoped<DigitalMe.Services.WebNavigation.WebNavigationService>();
        services.AddScoped<DigitalMe.Services.WebNavigation.IWebNavigationService>(provider => 
            provider.GetRequiredService<DigitalMe.Services.WebNavigation.WebNavigationService>());
        
        // Focused web navigation interfaces following ISP
        services.AddScoped<DigitalMe.Services.WebNavigation.IWebBrowserManager>(provider => 
            provider.GetRequiredService<DigitalMe.Services.WebNavigation.WebNavigationService>());
        services.AddScoped<DigitalMe.Services.WebNavigation.IWebNavigator>(provider => 
            provider.GetRequiredService<DigitalMe.Services.WebNavigation.WebNavigationService>());
        services.AddScoped<DigitalMe.Services.WebNavigation.IWebElementInteractor>(provider => 
            provider.GetRequiredService<DigitalMe.Services.WebNavigation.WebNavigationService>());
        services.AddScoped<DigitalMe.Services.WebNavigation.IWebContentExtractor>(provider => 
            provider.GetRequiredService<DigitalMe.Services.WebNavigation.WebNavigationService>());
        services.AddScoped<DigitalMe.Services.WebNavigation.IWebScriptExecutor>(provider => 
            provider.GetRequiredService<DigitalMe.Services.WebNavigation.WebNavigationService>());
        
        // Ivan-Level capability services - Phase B Week 3
        services.AddScoped<DigitalMe.Services.CaptchaSolving.CaptchaSolvingService>();
        services.AddScoped<DigitalMe.Services.CaptchaSolving.ICaptchaSolvingService>(provider => 
            provider.GetRequiredService<DigitalMe.Services.CaptchaSolving.CaptchaSolvingService>());
        
        // Focused CAPTCHA solving interfaces following ISP
        services.AddScoped<DigitalMe.Services.CaptchaSolving.ICaptchaImageSolver>(provider => 
            provider.GetRequiredService<DigitalMe.Services.CaptchaSolving.CaptchaSolvingService>());
        services.AddScoped<DigitalMe.Services.CaptchaSolving.ICaptchaInteractiveSolver>(provider => 
            provider.GetRequiredService<DigitalMe.Services.CaptchaSolving.CaptchaSolvingService>());
        services.AddScoped<DigitalMe.Services.CaptchaSolving.ICaptchaAccountManager>(provider => 
            provider.GetRequiredService<DigitalMe.Services.CaptchaSolving.CaptchaSolvingService>());
        
        services.AddScoped<DigitalMe.Services.Voice.VoiceService>();
        services.AddScoped<DigitalMe.Services.Voice.IVoiceService>(provider => 
            provider.GetRequiredService<DigitalMe.Services.Voice.VoiceService>());
        
        // Focused voice interfaces following ISP
        services.AddScoped<DigitalMe.Services.Voice.ITextToSpeechService>(provider => 
            provider.GetRequiredService<DigitalMe.Services.Voice.VoiceService>());
        services.AddScoped<DigitalMe.Services.Voice.ISpeechToTextService>(provider => 
            provider.GetRequiredService<DigitalMe.Services.Voice.VoiceService>());
        services.AddScoped<DigitalMe.Services.Voice.ISpeechToTextConfigurationService>(provider => 
            provider.GetRequiredService<DigitalMe.Services.Voice.VoiceService>());
        services.AddScoped<DigitalMe.Services.Voice.IVoiceServiceManager>(provider =>
            provider.GetRequiredService<DigitalMe.Services.Voice.VoiceService>());

        // Ivan-Level capability services - Phase B Week 4 - Email Integration
        services.AddScoped<DigitalMe.Services.Email.EmailService>();
        services.AddScoped<DigitalMe.Services.Email.IEmailService>(provider =>
            provider.GetRequiredService<DigitalMe.Services.Email.EmailService>());

        // Focused email interfaces following ISP
        services.AddScoped<DigitalMe.Services.Email.ISmtpService, DigitalMe.Services.Email.SmtpService>();
        services.AddScoped<DigitalMe.Services.Email.IImapService, DigitalMe.Services.Email.ImapService>();

        // Application Services layer - Clean Architecture compliance
        services.AddScoped<DigitalMe.Services.ApplicationServices.Workflows.IIvanLevelWorkflowService, DigitalMe.Services.ApplicationServices.Workflows.PersonalLevelWorkflowService>();

        // Orchestrators - Clean Architecture Application Services
        services.AddScoped<DigitalMe.Services.ApplicationServices.Orchestrators.IWorkflowOrchestrator, DigitalMe.Services.ApplicationServices.Orchestrators.WorkflowOrchestrator>();

        // Use Cases - Clean Architecture Application Services
        services.AddScoped<DigitalMe.Services.ApplicationServices.UseCases.FileProcessing.IFileProcessingUseCase, DigitalMe.Services.ApplicationServices.UseCases.FileProcessing.FileProcessingUseCase>();
        services.AddScoped<DigitalMe.Services.ApplicationServices.UseCases.WebNavigation.IWebNavigationUseCase, DigitalMe.Services.ApplicationServices.UseCases.WebNavigation.WebNavigationUseCase>();
        services.AddScoped<DigitalMe.Services.ApplicationServices.UseCases.ServiceAvailability.IServiceAvailabilityUseCase, DigitalMe.Services.ApplicationServices.UseCases.ServiceAvailability.ServiceAvailabilityUseCase>();
        services.AddScoped<DigitalMe.Services.ApplicationServices.UseCases.HealthCheck.IHealthCheckUseCase, DigitalMe.Services.ApplicationServices.UseCases.HealthCheck.HealthCheckUseCase>();
        services.AddScoped<DigitalMe.Services.ApplicationServices.UseCases.Email.IEmailUseCase, DigitalMe.Services.ApplicationServices.UseCases.Email.EmailUseCase>();

        // Backup Services - Refactored following SRP (Single Responsibility Principle)
        services.AddScoped<DigitalMe.Services.Backup.IBackupExecutor, DigitalMe.Services.Backup.BackupExecutor>();
        services.AddScoped<DigitalMe.Services.Backup.IBackupValidator, DigitalMe.Services.Backup.BackupValidator>();
        services.AddScoped<DigitalMe.Services.Backup.IBackupCleanup, DigitalMe.Services.Backup.BackupCleanup>();
        services.AddScoped<DigitalMe.Services.Backup.IDatabaseBackupService, DigitalMe.Services.Backup.BackupOrchestrator>();
        services.AddScoped<DigitalMe.Services.Backup.BackupSchedulerService>();

        // Learning Services - Moved to CleanArchitectureServiceCollectionExtensions for proper layering
        // Registrations are now in AddLearningInfrastructureServices() method

        return services;
    }

    /// <summary>
    /// Register existing external integrations with standardized configuration
    /// </summary>
    public static IServiceCollection AddExternalIntegrations(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure integration settings
        services.Configure<IntegrationSettings>(
            configuration.GetSection("Integrations"));

        // Telegram integration
        services.AddScoped<ITelegramService, TelegramService>();

        // Google services
        services.AddScoped<ICalendarService, CalendarService>();

        // GitHub integration - Enhanced version
        services.AddScoped<IGitHubService, GitHubService>();
        services.AddScoped<IGitHubEnhancedService, GitHubEnhancedService>();
        services.AddScoped<IGitHubWebhookService, GitHubWebhookService>();

        // MCP Integration
        services.Configure<AnthropicConfiguration>(
            configuration.GetSection("Anthropic"));
        services.AddScoped<IClaudeApiService, ClaudeApiService>();

        // Ivan-Level service configurations - Phase B Week 3
        services.Configure<DigitalMe.Services.Voice.VoiceServiceConfig>(configuration.GetSection("Voice"));
        services.Configure<DigitalMe.Services.CaptchaSolving.CaptchaSolvingServiceConfig>(configuration.GetSection("TwoCaptcha"));

        return services;
    }

    /// <summary>
    /// Register future integrations (Slack, ClickUp, etc.)
    /// Template for new service registration
    /// </summary>
    public static IServiceCollection AddNewIntegrations(this IServiceCollection services, IConfiguration configuration)
    {
        // Slack Integration - COMPLETED ✅
        services.AddScoped<DigitalMe.Integrations.External.Slack.ISlackService, DigitalMe.Integrations.External.Slack.SlackService>();
        services.AddScoped<DigitalMe.Integrations.External.Slack.ISlackWebhookService, DigitalMe.Integrations.External.Slack.SlackWebhookService>();

        // ClickUp Integration - COMPLETED ✅
        services.AddScoped<DigitalMe.Integrations.External.ClickUp.IClickUpService, DigitalMe.Integrations.External.ClickUp.ClickUpService>();
        services.AddScoped<DigitalMe.Integrations.External.ClickUp.IClickUpWebhookService, DigitalMe.Integrations.External.ClickUp.ClickUpWebhookService>();

        return services;
    }

    /// <summary>
    /// Register all HTTP clients for external APIs with resilience policies
    /// </summary>
    public static IServiceCollection AddHttpClients(this IServiceCollection services)
    {
        // Create service provider to get resilience policy service
        var serviceProvider = services.BuildServiceProvider();
        var resiliencePolicyService = serviceProvider.GetService<IResiliencePolicyService>();

        // Telegram HTTP client with resilience policies and pooling optimization
        services.AddHttpClient<ITelegramService, TelegramService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("User-Agent", "DigitalMe/1.0");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                MaxConnectionsPerServer = 10
            })
            .AddPolicyHandler((serviceProvider, request) =>
            {
                var resilienceService = serviceProvider.GetService<IResiliencePolicyService>();
                return resilienceService?.GetCombinedPolicy("telegram") ??
                       HttpPolicyExtensions.HandleTransientHttpError().WaitAndRetryAsync(3,
                           _ => TimeSpan.FromSeconds(2));
            });

        // GitHub HTTP clients with resilience policies and optimized pooling
        services.AddHttpClient<IGitHubService, GitHubService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("User-Agent", "DigitalMe/1.0");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                MaxConnectionsPerServer = 20 // Higher for GitHub due to better rate limits
            })
            .AddPolicyHandler((serviceProvider, request) =>
            {
                var resilienceService = serviceProvider.GetService<IResiliencePolicyService>();
                return resilienceService?.GetCombinedPolicy("github") ??
                       HttpPolicyExtensions.HandleTransientHttpError().WaitAndRetryAsync(3,
                           _ => TimeSpan.FromSeconds(2));
            });

        services.AddHttpClient<IGitHubEnhancedService, GitHubEnhancedService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("User-Agent", "DigitalMe/1.0");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                MaxConnectionsPerServer = 20,
            })
            .AddPolicyHandler((serviceProvider, request) =>
            {
                var resilienceService = serviceProvider.GetService<IResiliencePolicyService>();
                return resilienceService?.GetCombinedPolicy("github") ??
                       HttpPolicyExtensions.HandleTransientHttpError().WaitAndRetryAsync(3,
                           _ => TimeSpan.FromSeconds(2));
            });

        // Slack HTTP client with resilience policies and conservative pooling - COMPLETED ✅
        services.AddHttpClient<DigitalMe.Integrations.External.Slack.ISlackService, DigitalMe.Integrations.External.Slack.SlackService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("User-Agent", "DigitalMe/1.0");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                MaxConnectionsPerServer = 5, // Conservative for Slack rate limits
            })
            .AddPolicyHandler((serviceProvider, request) =>
            {
                var resilienceService = serviceProvider.GetService<IResiliencePolicyService>();
                return resilienceService?.GetCombinedPolicy("slack") ??
                       HttpPolicyExtensions.HandleTransientHttpError().WaitAndRetryAsync(3,
                           _ => TimeSpan.FromSeconds(2));
            });

        // ClickUp HTTP client with resilience policies and balanced pooling - COMPLETED ✅
        services.AddHttpClient<DigitalMe.Integrations.External.ClickUp.IClickUpService, DigitalMe.Integrations.External.ClickUp.ClickUpService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("User-Agent", "DigitalMe/1.0");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                MaxConnectionsPerServer = 15, // Balanced for ClickUp
            })
            .AddPolicyHandler((serviceProvider, request) =>
            {
                var resilienceService = serviceProvider.GetService<IResiliencePolicyService>();
                return resilienceService?.GetCombinedPolicy("clickup") ??
                       HttpPolicyExtensions.HandleTransientHttpError().WaitAndRetryAsync(3,
                           _ => TimeSpan.FromSeconds(2));
            });

        // CaptchaSolvingService HTTP client for 2captcha.com API - Phase B Week 3
        services.AddHttpClient<DigitalMe.Services.CaptchaSolving.ICaptchaSolvingService, DigitalMe.Services.CaptchaSolving.CaptchaSolvingService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(120); // CAPTCHA solving can take time
                client.DefaultRequestHeaders.Add("User-Agent", "DigitalMe/1.0");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                MaxConnectionsPerServer = 5, // Conservative for 2captcha rate limits
            })
            .AddPolicyHandler((serviceProvider, request) =>
            {
                var resilienceService = serviceProvider.GetService<IResiliencePolicyService>();
                return resilienceService?.GetCombinedPolicy("captcha") ??
                       HttpPolicyExtensions.HandleTransientHttpError().WaitAndRetryAsync(3,
                           _ => TimeSpan.FromSeconds(5)); // Longer delay for CAPTCHA retries
            });

        return services;
    }

    /// <summary>
    /// One-stop registration for all DigitalMe services
    /// </summary>
    public static IServiceCollection AddDigitalMeServices(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddRepositories()
            .AddBusinessServices()
            .AddExternalIntegrations(configuration)
            .AddNewIntegrations(configuration)
            .AddHttpClients();
    }
}
