# 🤖 Telegram Bot Setup Guide

**Parent Plan**: [MAIN_PLAN.md](../../../MAIN_PLAN.md)

## 📋 Overview

This guide provides comprehensive instructions for setting up and deploying the DigitalMe Telegram Bot integration. The bot serves as one of the primary interfaces for interacting with Ivan's digital personality clone.

## 🎯 Prerequisites

### Development Environment
- .NET 8 SDK installed
- Valid Telegram Bot Token from @BotFather
- PostgreSQL database (local or cloud)
- Claude API key configured

### Production Environment
- Docker environment
- Cloud hosting platform (Google Cloud Run, Railway, etc.)
- SSL certificate for webhook endpoints
- Domain name for webhook URL

## 🔧 Bot Registration & Configuration

### 1. Create Telegram Bot

1. **Start conversation with @BotFather on Telegram**
2. **Create new bot**:
   ```
   /newbot
   Choose a name for your bot: DigitalMe Ivan Clone
   Choose a username for your bot: digitalme_ivan_bot
   ```
3. **Save the Bot Token** - format: `123456789:ABCdefGHijklMNopQRstUVwxyz`

### 2. Configure Bot Settings

```bash
# Set bot description
/setdescription
@your_bot_username
I'm Ivan's digital personality clone. Chat with me to experience Ivan's thoughts, technical expertise, and communication style.

# Set bot commands
/setcommands
@your_bot_username
start - Initialize conversation with Ivan's clone
status - Show bot status and personality metrics
settings - Configure interaction preferences
reset - Reset conversation context
help - Show available commands and usage
```

### 3. Environment Configuration

Create or update `.env.development` and `.env.production`:

```bash
# Telegram Bot Configuration
TELEGRAM_BOT_TOKEN=your_bot_token_here
TELEGRAM_WEBHOOK_URL=https://yourdomain.com/api/telegram/webhook
TELEGRAM_WEBHOOK_SECRET=your_secure_webhook_secret

# Bot Behavior Settings
TELEGRAM_MAX_MESSAGE_LENGTH=4096
TELEGRAM_RESPONSE_TIMEOUT_MS=30000
TELEGRAM_RATE_LIMIT_PER_MINUTE=30
TELEGRAM_ENABLE_PERSONALITY_CONTEXT=true

# Integration Settings
CLAUDE_API_KEY=your_claude_api_key
DATABASE_CONNECTION_STRING=your_database_connection
PERSONALITY_PROFILE_ID=ivan_profile_id
```

## 💻 Development Setup

### 1. Install Dependencies

The following packages are required (should already be in project):

```xml
<PackageReference Include="Telegram.Bot" Version="22.6.2" />
<PackageReference Include="Telegram.Bot.Extensions.Polling" Version="1.2.0" />
<PackageReference Include="Microsoft.AspNetCore.SignalR" Version="8.0.0" />
```

### 2. Local Development Configuration

```csharp
// appsettings.Development.json
{
  "TelegramBot": {
    "Token": "your_development_bot_token",
    "WebhookUrl": "https://localhost:7001/api/telegram/webhook",
    "UsePolling": true,  // Use polling for local development
    "EnableLogging": true
  },
  "PersonalityEngine": {
    "ProfileId": "ivan_development_profile",
    "EnableDebugMode": true,
    "ResponseTimeoutMs": 15000
  }
}
```

### 3. Start Local Bot

```bash
# Navigate to project directory
cd DigitalMe

# Set development environment
export ASPNETCORE_ENVIRONMENT=Development

# Run application
dotnet run

# Bot should start polling for messages
# Test by messaging @your_bot_username
```

## 🚀 Production Deployment

### 1. Webhook Setup

#### Configure Webhook Endpoint

```csharp
// Controllers/TelegramWebhookController.cs
[ApiController]
[Route("api/telegram")]
public class TelegramWebhookController : ControllerBase
{
    private readonly ITelegramBotService _botService;
    private readonly IPersonalityService _personalityService;
    
    // TODO: Implement webhook message handling
    [HttpPost("webhook")]
    public async Task<IActionResult> HandleWebhook([FromBody] Update update)
    {
        // TODO: Process incoming Telegram updates
        // TODO: Generate personality-aware responses
        // TODO: Send responses back to user
        return Ok();
    }
}
```

#### Set Telegram Webhook

```bash
# Set webhook URL (run once after deployment)
curl -X POST "https://api.telegram.org/bot<YOUR_BOT_TOKEN>/setWebhook" \
     -H "Content-Type: application/json" \
     -d '{"url":"https://yourdomain.com/api/telegram/webhook","secret_token":"your_webhook_secret"}'

# Verify webhook is set
curl "https://api.telegram.org/bot<YOUR_BOT_TOKEN>/getWebhookInfo"
```

### 2. Docker Deployment

#### Dockerfile Configuration

```dockerfile
# Use in production deployment
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["DigitalMe/DigitalMe.csproj", "DigitalMe/"]
RUN dotnet restore "DigitalMe/DigitalMe.csproj"
COPY . .
WORKDIR "/src/DigitalMe"
RUN dotnet build "DigitalMe.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "DigitalMe.csproj" -c Release -o /app/publish

# Runtime stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DigitalMe.dll"]
```

#### Docker Compose (Optional)

```yaml
version: '3.8'
services:
  digitalme-bot:
    build: .
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - TELEGRAM_BOT_TOKEN=${TELEGRAM_BOT_TOKEN}
      - CLAUDE_API_KEY=${CLAUDE_API_KEY}
      - DATABASE_CONNECTION_STRING=${DATABASE_CONNECTION_STRING}
    depends_on:
      - postgres
    
  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: digitalme
      POSTGRES_USER: ${DB_USER}
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  postgres_data:
```

### 3. Cloud Deployment (Google Cloud Run)

```bash
# Build and push Docker image
docker build -t gcr.io/your-project/digitalme-bot .
docker push gcr.io/your-project/digitalme-bot

# Deploy to Cloud Run
gcloud run deploy digitalme-bot \
  --image gcr.io/your-project/digitalme-bot \
  --platform managed \
  --region us-central1 \
  --allow-unauthenticated \
  --set-env-vars="TELEGRAM_BOT_TOKEN=${TELEGRAM_BOT_TOKEN}" \
  --set-env-vars="CLAUDE_API_KEY=${CLAUDE_API_KEY}" \
  --memory 512Mi \
  --cpu 1 \
  --timeout 300s \
  --max-instances 10

# Update webhook URL to Cloud Run endpoint
curl -X POST "https://api.telegram.org/bot<YOUR_BOT_TOKEN>/setWebhook" \
     -H "Content-Type: application/json" \
     -d '{"url":"https://your-cloudrun-service.run.app/api/telegram/webhook"}'
```

## 🧪 Testing & Validation

### 1. Bot Response Testing

```bash
# Test basic bot functionality
# Send these messages to your bot:

/start
# Expected: Welcome message with Ivan's personality introduction

Hello, how are you?
# Expected: Response reflecting Ivan's communication style

What's your technical background?
# Expected: Information about Ivan's R&D role and preferences

/status
# Expected: Bot status, response time, personality metrics
```

### 2. Integration Testing

```csharp
// Example integration test structure
[Fact]
public async Task TelegramBot_ShouldRespondWithPersonality()
{
    // TODO: Test that bot responses include Ivan's personality traits
    // TODO: Verify Claude API integration works through Telegram
    // TODO: Validate response time and accuracy metrics
}

[Fact]
public async Task TelegramWebhook_ShouldHandleCommands()
{
    // TODO: Test /start, /status, /settings commands
    // TODO: Verify command parsing and execution
    // TODO: Validate error handling for invalid commands
}
```

### 3. Performance Monitoring

```csharp
// Add to appsettings.json
{
  "Monitoring": {
    "EnableTelegramMetrics": true,
    "LogResponseTimes": true,
    "TrackPersonalityAccuracy": true,
    "AlertOnSlowResponses": true,
    "MaxResponseTimeMs": 5000
  }
}
```

## 🔒 Security Considerations

### 1. Webhook Security

- **Use HTTPS only** for webhook endpoints
- **Implement webhook secret validation**
- **Validate Telegram server IP ranges**
- **Rate limiting** to prevent spam/abuse
- **Input sanitization** for all user messages

### 2. Data Protection

```csharp
// Data retention and privacy
public class TelegramPrivacyService
{
    // TODO: Implement conversation data retention policies
    // TODO: User data deletion on request
    // TODO: Anonymization of stored messages
    // TODO: GDPR compliance measures
}
```

### 3. API Key Management

- Store API keys in secure environment variables
- Rotate keys regularly (monthly recommended)
- Use different keys for development/production
- Monitor API usage for anomalies

## 📊 Monitoring & Maintenance

### 1. Health Checks

```csharp
// Add to Startup.cs
services.AddHealthChecks()
    .AddCheck<TelegramBotHealthCheck>("telegram_bot")
    .AddCheck<ClaudeApiHealthCheck>("claude_api")
    .AddCheck<PersonalityEngineHealthCheck>("personality_engine");
```

### 2. Logging Configuration

```json
{
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/telegram-bot-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7
        }
      }
    ],
    "Properties": {
      "Application": "DigitalMe.TelegramBot"
    }
  }
}
```

### 3. Metrics to Track

- **Response time** (target: <3 seconds)
- **Message throughput** (messages/minute)
- **Personality accuracy** (subjective user feedback)
- **Error rates** (API failures, timeouts)
- **User engagement** (messages per session, retention)

## 🚨 Troubleshooting

### Common Issues

#### Bot Not Responding
1. Check bot token is valid: `curl "https://api.telegram.org/bot<TOKEN>/getMe"`
2. Verify webhook URL is accessible
3. Check application logs for errors
4. Validate environment variables

#### Slow Response Times
1. Check Claude API response times
2. Verify database connection performance
3. Monitor memory/CPU usage
4. Consider caching frequently used personality data

#### Webhook Failures
1. Verify SSL certificate is valid
2. Check webhook secret configuration
3. Monitor webhook endpoint uptime
4. Validate payload format handling

### Debug Commands

```bash
# Check bot info
curl "https://api.telegram.org/bot<TOKEN>/getMe"

# Get webhook info
curl "https://api.telegram.org/bot<TOKEN>/getWebhookInfo"

# Delete webhook (for local development)
curl -X POST "https://api.telegram.org/bot<TOKEN>/deleteWebhook"

# Send test message
curl -X POST "https://api.telegram.org/bot<TOKEN>/sendMessage" \
     -H "Content-Type: application/json" \
     -d '{"chat_id":"YOUR_CHAT_ID","text":"Test message"}'
```

## 📚 Additional Resources

- [Telegram Bot API Documentation](https://core.telegram.org/bots/api)
- [Telegram.Bot Library Documentation](https://github.com/TelegramBots/Telegram.Bot)
- [ASP.NET Core Webhooks Guide](https://docs.microsoft.com/en-us/aspnet/webhooks/)
- [Docker Deployment Best Practices](https://docs.docker.com/develop/dev-best-practices/)

## ✅ Deployment Checklist

### Pre-Deployment
- [ ] Bot token obtained and secured
- [ ] Environment variables configured
- [ ] Database connection tested
- [ ] Claude API integration verified
- [ ] SSL certificate installed
- [ ] Domain name configured

### Deployment
- [ ] Application deployed to cloud platform
- [ ] Webhook URL set with Telegram
- [ ] Health checks passing
- [ ] Logging configured and working
- [ ] Monitoring alerts set up

### Post-Deployment
- [ ] Bot responds to test messages
- [ ] Commands work correctly
- [ ] Personality responses accurate
- [ ] Performance metrics baseline established
- [ ] Security scanning completed

---

**Created**: 2025-09-04  
**Status**: Production deployment guide ready  
**Version**: 1.0.0  
**Next Update**: After P2.4 Telegram integration completion