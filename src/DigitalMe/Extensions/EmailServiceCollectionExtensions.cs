using DigitalMe.Services.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DigitalMe.Extensions;

/// <summary>
/// Extension methods for configuring email services in dependency injection container
/// Provides Ivan-Level email capabilities registration
/// Following Clean Architecture patterns with proper service lifetime management
/// </summary>
public static class EmailServiceCollectionExtensions
{
    /// <summary>
    /// Add email services to the DI container
    /// </summary>
    public static IServiceCollection AddEmailServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailServiceConfig>(configuration.GetSection("EmailService"));
        return services.RegisterCoreEmailServices();
    }

    /// <summary>
    /// Add email services with custom configuration
    /// </summary>
    public static IServiceCollection AddEmailServices(this IServiceCollection services, Action<EmailServiceConfig> configureOptions)
    {
        services.Configure(configureOptions);
        return services.RegisterCoreEmailServices();
    }

    /// <summary>
    /// Add email services with Gmail configuration
    /// </summary>
    public static IServiceCollection AddGmailServices(this IServiceCollection services, string username, string password)
    {
        services.Configure<EmailServiceConfig>(config =>
        {
            config.Smtp.Host = "smtp.gmail.com";
            config.Smtp.Port = 587;
            config.Smtp.Username = username;
            config.Smtp.Password = password;
            config.Smtp.EnableSsl = true;

            config.Imap.Host = "imap.gmail.com";
            config.Imap.Port = 993;
            config.Imap.Username = username;
            config.Imap.Password = password;
            config.Imap.EnableSsl = true;
        });

        return services.RegisterCoreEmailServices();
    }

    /// <summary>
    /// Add email services with Outlook configuration
    /// </summary>
    public static IServiceCollection AddOutlookServices(this IServiceCollection services, string username, string password)
    {
        services.Configure<EmailServiceConfig>(config =>
        {
            config.Smtp.Host = "smtp.outlook.com";
            config.Smtp.Port = 587;
            config.Smtp.Username = username;
            config.Smtp.Password = password;
            config.Smtp.EnableSsl = true;

            config.Imap.Host = "imap.outlook.com";
            config.Imap.Port = 993;
            config.Imap.Username = username;
            config.Imap.Password = password;
            config.Imap.EnableSsl = true;
        });

        return services.RegisterCoreEmailServices();
    }

    /// <summary>
    /// Register core email services with proper lifetimes
    /// </summary>
    private static IServiceCollection RegisterCoreEmailServices(this IServiceCollection services)
    {
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ISmtpService, SmtpService>();
        services.AddScoped<IImapService, ImapService>();
        services.AddScoped<DigitalMe.Services.ApplicationServices.UseCases.Email.IEmailUseCase, DigitalMe.Services.ApplicationServices.UseCases.Email.EmailUseCase>();
        return services;
    }
}