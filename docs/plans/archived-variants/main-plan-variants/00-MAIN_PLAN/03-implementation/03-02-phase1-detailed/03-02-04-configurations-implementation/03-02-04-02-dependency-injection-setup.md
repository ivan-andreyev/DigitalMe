# Dependency Injection Setup 💉

> **Parent Plan**: [03-02-04-configurations-implementation.md](../03-02-04-configurations-implementation.md) | **Plan Type**: CONFIGURATION PLAN | **LLM Ready**: ✅ YES  
> **Prerequisites**: Service interfaces defined | **Execution Time**: 0.5 days

📍 **Architecture** → **Implementation** → **Configurations** → **DI Setup**

## Dependency Injection Architecture

### Service Registration Strategy
**Target File**: `DigitalMe/Program.cs`
**Lines**: 15-40 (service registration section)

### Core Service Registrations

#### Repository Layer Registration
```csharp
// File: Program.cs, Lines: 20-25
services.AddScoped<IPersonalityRepository, PersonalityRepository>();
services.AddScoped<IConversationRepository, ConversationRepository>();
services.AddScoped<IMessageRepository, MessageRepository>();
```

#### Service Layer Registration
```csharp
// File: Program.cs, Lines: 27-32
services.AddScoped<IPersonalityService, PersonalityService>();
services.AddScoped<IConversationService, ConversationService>();
services.AddScoped<IMcpService, McpService>();
```

#### HTTP Client Configuration
```csharp
// File: Program.cs, Lines: 35-45
services.AddHttpClient<IMcpService, McpService>(client =>
{
    var mcpConfig = builder.Configuration.GetSection("McpService").Get<McpClientConfiguration>();
    client.BaseAddress = new Uri(mcpConfig.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(mcpConfig.TimeoutSeconds);
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {mcpConfig.ApiKey}");
});
```

### Database Configuration
```csharp
// File: Program.cs, Lines: 10-18
services.AddDbContext<DigitalMeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

### Implementation Success Criteria

✅ **Service Registration**: All interfaces have concrete implementations registered
✅ **Scoped Lifetimes**: Correct lifetime management for services
✅ **HTTP Client**: Properly configured HTTP clients
✅ **Database Context**: Entity Framework configuration

---

## 📊 PLAN METADATA

- **Type**: CONFIGURATION PLAN
- **LLM Ready**: ✅ YES
- **Implementation Depth**: Service registration templates
- **Execution Time**: 0.5 days
- **Lines**: 55 (under 400 limit)