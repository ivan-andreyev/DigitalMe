# Technical Setup & Configuration

> **Parent Plan**: [MAIN_PLAN.md](../MAIN_PLAN.md)  
> **Section**: Technical Reference  
> **Purpose**: Detailed development environment, configuration, and setup instructions

---

## 🛠️ DEVELOPMENT ENVIRONMENT

### **Prerequisites**
```bash
# Required Software
dotnet --version  # 8.0+
docker --version  # Latest
postgres --version  # 14+
git --version     # 2.0+
```

### **Project Setup**
```bash
# Clone and Setup
git clone <repository>
cd DigitalMe
cp .env.example .env.development
# Configure CLAUDE_API_KEY, DATABASE_URL in .env.development
dotnet restore
dotnet ef database update
dotnet run
```

---

## ⚙️ CONFIGURATION

### **Application Configuration Keys**
```json
{
  "Claude": {
    "ApiKey": "claude-api-key-here",
    "Model": "claude-3-sonnet-20240229", 
    "MaxTokens": 4096,
    "RetryPolicy": {
      "MaxRetries": 3,
      "BaseDelay": "00:00:02"
    }
  },
  "Database": {
    "ConnectionString": "postgresql://username:password@localhost:5432/digitalme",
    "EnableRetryOnFailure": true,
    "MaxRetryCount": 3,
    "CommandTimeout": 30
  },
  "PersonalityEngine": {
    "ProfilePath": "data/profile/IVAN_PROFILE_DATA.md",
    "DefaultAccuracy": 0.85,
    "TemporalModelingEnabled": true,
    "CacheTimeoutMinutes": 30
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Warning",
      "DigitalMe": "Debug"
    }
  }
}
```

### **Environment Variables**
```bash
# .env.development
CLAUDE_API_KEY=claude-api-key-here
DATABASE_URL=postgresql://username:password@localhost:5432/digitalme
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=https://localhost:5001;http://localhost:5000
```

---

## 🏗️ PROJECT STRUCTURE

### **Critical File Locations**
```
DigitalMe/
├── Data/
│   ├── Entities/
│   │   ├── PersonalityProfile.cs     ✅ IMPLEMENTED (150+ lines)
│   │   └── PersonalityTrait.cs       ✅ IMPLEMENTED (172+ lines)
│   ├── ApplicationDbContext.cs       📋 TO BE UPDATED
│   └── Configurations/               📋 P2.1 TARGET
├── Services/
│   ├── PersonalityService.cs         ✅ EXISTS (needs entity integration)
│   ├── DataLoading/
│   │   └── ProfileSeederService.cs   📋 P2.3 TARGET
│   └── MessageProcessor.cs           📋 P2.2 TARGET
├── Integrations/
│   └── MCP/
│       └── ClaudeApiService.cs       ✅ IMPLEMENTED (303+ lines)
└── data/
    └── profile/
        └── IVAN_PROFILE_DATA.md      ✅ EXISTS (350+ lines)
```

---

## 🔧 EXISTING ASSETS STATUS

### **✅ PRODUCTION-READY COMPONENTS**
- **PersonalityProfile.cs**: Complete entity with all required properties
- **PersonalityTrait.cs**: Full implementation with relationships, temporal patterns
- **ClaudeApiService.cs**: Complete Anthropic.SDK integration with error handling
- **IVAN_PROFILE_DATA.md**: Comprehensive personality data for Ivan

### **📋 IMPLEMENTATION TARGETS**
- **ApplicationDbContext**: Needs DbSets and entity configurations added
- **ProfileSeederService**: Markdown parser + entity population logic
- **MessageProcessor**: Orchestrate PersonalityService → ClaudeApiService flow

---

## 🧪 TESTING SETUP

### **Test Configuration**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "DataSource=:memory:",
    "TestDatabase": "Server=localhost;Database=digitalme_test;Integrated Security=true;"
  },
  "Claude": {
    "ApiKey": "test-api-key",
    "Model": "claude-3-haiku-20240307",
    "MaxTokens": 1000
  }
}
```

### **Test Categories**
- **Unit Tests**: Entity validation, service logic, API integrations
- **Integration Tests**: Database operations, API calls, end-to-end flows  
- **Performance Tests**: Response times, throughput, memory usage

---

## 🚀 DEPLOYMENT PREPARATION

### **Development to Production Checklist**
- [ ] Environment-specific configuration files
- [ ] Database migration scripts
- [ ] Health check endpoints
- [ ] Logging and monitoring setup
- [ ] Security configuration (HTTPS, API keys)
- [ ] Performance optimization
- [ ] Docker containerization
- [ ] Cloud deployment scripts

---

**Referenced by**: [MAIN_PLAN.md](../MAIN_PLAN.md) - Technical Reference section  
**Last Updated**: 2025-09-05