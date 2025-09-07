using Microsoft.EntityFrameworkCore;
using DigitalMe.Data;
using DigitalMe.Services;
using DigitalMe.Services.Resilience;
using DigitalMe.Services.Performance;
using DigitalMe.Services.Security;
using DigitalMe.Repositories;
using DigitalMe.Integrations.External.Telegram;
using DigitalMe.Integrations.External.Google;
using DigitalMe.Integrations.External.GitHub;
using DigitalMe.Integrations.MCP;
using DigitalMe.Configuration;
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
        services.AddScoped<MVPPersonalityService>();
        services.AddScoped<IPersonalityService>(provider => provider.GetRequiredService<MVPPersonalityService>());
        services.AddScoped<IMVPPersonalityService>(provider => provider.GetRequiredService<MVPPersonalityService>());
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IIvanPersonalityService, IvanPersonalityService>();
        services.AddScoped<IMessageProcessor, MessageProcessor>();
        services.AddScoped<IMVPMessageProcessor, MVPMessageProcessor>();
        services.AddScoped<IHealthChecker, HealthChecker>();
        
        // Resilience services
        services.AddSingleton<IResiliencePolicyService, ResiliencePolicyService>();
        
        // Performance optimization services
        services.AddSingleton<IPerformanceOptimizationService, PerformanceOptimizationService>();
        services.AddMemoryCache(); // Required for response caching
        
        // Security services
        services.AddScoped<ISecurityValidationService, SecurityValidationService>();
        
        // Health check services
        services.AddScoped<DigitalMe.Services.Monitoring.IHealthCheckService, DigitalMe.Services.Monitoring.HealthCheckService>();
        
        // Performance metrics services
        services.AddScoped<DigitalMe.Services.Monitoring.IPerformanceMetricsService, DigitalMe.Services.Monitoring.PerformanceMetricsService>();
        
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