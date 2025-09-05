# AppSettings Configuration 🔧

> **Parent Plan**: [03-02-04-configurations-implementation.md](../03-02-04-configurations-implementation.md) | **Plan Type**: CONFIGURATION PLAN | **LLM Ready**: ✅ YES  
> **Prerequisites**: None | **Execution Time**: 0.5 days

📍 **Architecture** → **Implementation** → **Configurations** → **AppSettings**

## AppSettings Configuration Architecture

### Configuration File Structure
**Target File**: `DigitalMe/appsettings.json`
**Environment Files**: `appsettings.Development.json`, `appsettings.Production.json`

### Core Configuration Sections

#### Database Configuration
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=DigitalMeDb;Trusted_Connection=true;MultipleActiveResultSets=true",
    "RedisConnection": "localhost:6379"
  }
}
```

#### MCP Service Configuration
```json
{
  "McpService": {
    "BaseUrl": "https://api.mcp-service.com",
    "ApiKey": "your-mcp-api-key",
    "TimeoutSeconds": 30,
    "MaxRetries": 3,
    "DefaultTemperature": 0.7,
    "MaxTokens": 2000
  }
}
```

#### Logging Configuration
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "DigitalMe": "Debug"
    }
  }
}
```

#### CORS Configuration
```json
{
  "AllowedHosts": "*",
  "CorsSettings": {
    "AllowedOrigins": ["http://localhost:3000", "https://digitalme.app"],
    "AllowedMethods": ["GET", "POST", "PUT", "DELETE"],
    "AllowedHeaders": ["*"]
  }
}
```

### Implementation Success Criteria

✅ **File Creation**: appsettings.json with all required sections
✅ **Environment Variants**: Development and Production configurations
✅ **Security**: Sensitive data in user secrets for development
✅ **Validation**: Configuration validation at startup

---

## 📊 PLAN METADATA

- **Type**: CONFIGURATION PLAN
- **LLM Ready**: ✅ YES
- **Implementation Depth**: Configuration templates and structure
- **Execution Time**: 0.5 days
- **Lines**: 60 (under 400 limit)