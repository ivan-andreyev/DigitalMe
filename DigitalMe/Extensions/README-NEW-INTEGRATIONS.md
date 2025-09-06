# 🔧 How to Add New Integrations - Quick Start Guide

## 📋 Integration Registration Pattern

**File:** `Extensions/ServiceCollectionExtensions.cs`

### ✅ Step 1: Create Your Service Interface & Implementation
```csharp
// DigitalMe/Integrations/External/Slack/ISlackService.cs
public interface ISlackService
{
    Task<bool> SendMessageAsync(string channel, string message);
    Task<SlackFile> UploadFileAsync(string channel, Stream file, string filename);
}

// DigitalMe/Integrations/External/Slack/SlackService.cs  
public class SlackService : ISlackService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    
    public SlackService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }
    
    // Implementation...
}
```

### ✅ Step 2: Register Service in Extensions
```csharp
// In ServiceCollectionExtensions.AddNewIntegrations()
public static IServiceCollection AddNewIntegrations(this IServiceCollection services, IConfiguration configuration)
{
    // Add your new integration here:
    services.AddScoped<ISlackService, SlackService>();
    services.AddScoped<IClickUpService, ClickUpService>();
    
    return services;
}

// In ServiceCollectionExtensions.AddHttpClients()  
public static IServiceCollection AddHttpClients(this IServiceCollection services)
{
    // Add HTTP client for your integration:
    services.AddHttpClient<ISlackService, SlackService>();
    services.AddHttpClient<IClickUpService, ClickUpService>();
    
    return services;
}
```

### ✅ Step 3: Add Configuration Section
```json
// appsettings.json
{
  "Integrations": {
    "Slack": {
      "BotToken": "xoxb-your-token",
      "SigningSecret": "your-signing-secret"
    },
    "ClickUp": {
      "ApiToken": "pk_your-token",
      "TeamId": "your-team-id"
    }
  }
}
```

## 🎯 That's It! 
Your service is now:
- ✅ Registered in DI container
- ✅ Has HTTP client configured  
- ✅ Ready for configuration injection
- ✅ Available throughout the application

## 📝 Real Examples in Codebase:
- **Telegram**: `Integrations/External/Telegram/TelegramService.cs`
- **GitHub**: `Integrations/External/GitHub/GitHubService.cs`
- **Google Calendar**: `Integrations/External/Google/CalendarService.cs`

## 🚀 Benefits of This Pattern:
1. **Consistent Registration** - Same pattern for all services
2. **Easy Discovery** - All services in one Extensions file
3. **HTTP Client Management** - Automatic HttpClientFactory usage
4. **Configuration Binding** - Standard config injection
5. **Testing Ready** - Easy to mock and test

---
**Need help?** Check existing integrations for examples! 🎯