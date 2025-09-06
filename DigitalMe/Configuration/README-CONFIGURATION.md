# ⚙️ Configuration Management Guide

## 📋 Standard Configuration Pattern

All integrations follow the same configuration structure for consistency and ease of management.

### ✅ Configuration Structure

```json
{
  "Integrations": {
    "ServiceName": {
      "Enabled": true,
      "ApiToken": "your-token",
      "BaseUrl": "https://api.service.com",
      "TimeoutSeconds": 30,
      "MaxRetries": 3,
      "RateLimitPerMinute": 60
    }
  }
}
```

### 🔧 Using Configuration in Services

```csharp
public class SlackService : ISlackService
{
    private readonly SlackSettings _settings;
    private readonly HttpClient _httpClient;
    
    public SlackService(
        IOptions<IntegrationSettings> integrationSettings, 
        HttpClient httpClient)
    {
        _settings = integrationSettings.Value.Slack;
        _httpClient = httpClient;
        
        // Check if service is enabled
        if (!_settings.Enabled)
            throw new InvalidOperationException("Slack integration is disabled");
            
        // Configure HTTP client with settings
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl ?? "https://slack.com/api/");
    }
}
```

### 🎯 Available Integration Settings

#### Base Settings (All Integrations)
- **`Enabled`**: Whether integration is active
- **`BaseUrl`**: API endpoint override 
- **`TimeoutSeconds`**: Request timeout (default: 30)
- **`MaxRetries`**: Retry attempts (default: 3)
- **`RateLimitPerMinute`**: Rate limiting (default: 60)

#### Service-Specific Settings

**Slack:**
- `BotToken`, `SigningSecret`, `WorkspaceId`

**ClickUp:**  
- `ApiToken`, `TeamId`, `WorkspaceId`

**GitHub:**
- `PersonalAccessToken`, `WebhookSecret`, `Organization`

**Telegram:**
- `BotToken`, `WebhookUrl`

**Google:**
- `ClientId`, `ClientSecret`, `RedirectUri`

## 🚀 Environment Variables

Production secrets can be overridden with environment variables:

```bash
# Override configuration values
INTEGRATIONS__SLACK__BOTTOKEN="xoxb-your-prod-token"
INTEGRATIONS__CLICKUP__APITOKEN="pk_your-prod-token"
INTEGRATIONS__GITHUB__PERSONALACCESSTOKEN="ghp_your-token"
```

## ✅ Benefits

1. **Consistent Structure** - Same pattern across all integrations
2. **Easy Enablement** - Toggle services on/off with `Enabled` flag
3. **Performance Control** - Timeout and retry settings
4. **Rate Limiting** - Built-in rate limit configuration
5. **Environment Override** - Production secrets via env vars
6. **Type Safety** - Strong typing with C# configuration classes

---
**Configuration classes:** `Configuration/IntegrationSettings.cs` 🎯