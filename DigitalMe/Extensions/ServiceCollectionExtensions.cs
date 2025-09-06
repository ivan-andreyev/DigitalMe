using Microsoft.EntityFrameworkCore;
using DigitalMe.Data;
using DigitalMe.Services;
using DigitalMe.Repositories;
using DigitalMe.Integrations.External.Telegram;
using DigitalMe.Integrations.External.Google;
using DigitalMe.Integrations.External.GitHub;
using DigitalMe.Integrations.MCP;
using DigitalMe.Configuration;

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
        services.AddScoped<IPersonalityService, PersonalityService>();
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IIvanPersonalityService, IvanPersonalityService>();
        services.AddScoped<IMessageProcessor, MessageProcessor>();
        services.AddScoped<IHealthChecker, HealthChecker>();
        
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
        
        // GitHub integration
        services.AddScoped<IGitHubService, GitHubService>();
        
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
        
        // ClickUp Integration - TODO
        // services.AddScoped<IClickUpService, ClickUpService>();
        
        return services;
    }
    
    /// <summary>
    /// Register all HTTP clients for external APIs
    /// </summary>
    public static IServiceCollection AddHttpClients(this IServiceCollection services)
    {
        // Standard HTTP clients for external integrations
        services.AddHttpClient<ITelegramService, TelegramService>();
        services.AddHttpClient<IGitHubService, GitHubService>();
        
        // Slack HTTP client - COMPLETED ✅
        services.AddHttpClient<DigitalMe.Integrations.External.Slack.ISlackService, DigitalMe.Integrations.External.Slack.SlackService>();
        
        // Future HTTP clients will be added here:
        // services.AddHttpClient<IClickUpService, ClickUpService>();
        
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