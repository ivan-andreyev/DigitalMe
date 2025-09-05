# Architecture Reference & Technical Documentation

> **Parent Plan**: [MAIN_PLAN.md](../MAIN_PLAN.md)  
> **Section**: Architectural Documentation  
> **Purpose**: Comprehensive system architecture, design decisions, and technical specifications

---

## 🏗️ SYSTEM ARCHITECTURE OVERVIEW

### **High-Level Architecture Diagram**
```
┌─────────────────────────────────────────────────────────────────┐
│                        FRONTEND LAYER                          │
├─────────────────┬─────────────────┬─────────────────────────────┤
│   Blazor Web    │   MAUI Mobile   │     Telegram Bot API       │
├─────────────────┴─────────────────┴─────────────────────────────┤
│                     API GATEWAY LAYER                          │
├─────────────────────────────────────────────────────────────────┤
│                    PERSONALITY ENGINE                          │
├─────────────┬─────────────────┬─────────────────┬─────────────────┤
│ Profile     │ System Prompt   │ Claude API      │ Message         │
│ Service     │ Generator       │ Service         │ Processor       │
├─────────────┴─────────────────┴─────────────────┴─────────────────┤
│                       SERVICE LAYER                            │
├─────────────┬─────────────────┬─────────────────┬─────────────────┤
│ Profile     │ External APIs   │ User            │ Repository      │
│ Seeder      │ (Tg/Gh/Google)  │ Management      │ Layer           │
├─────────────┴─────────────────┴─────────────────┴─────────────────┤
│                       DATA LAYER                               │
├─────────────┬─────────────────┬─────────────────┬─────────────────┤
│ PostgreSQL  │ SQLite (Dev)    │ Entity          │ Migration       │
│ (Production)│ Database        │ Framework       │ Management      │
└─────────────┴─────────────────┴─────────────────┴─────────────────┘
```

---

## 🔧 TECHNOLOGY STACK (FINALIZED)

### **Core Framework**
- **Backend**: ASP.NET Core 8.0
- **ORM**: Entity Framework Core 8.0
- **Database**: PostgreSQL (production), SQLite (development)
- **Cache**: In-memory caching (Redis planned for Phase 3)

### **AI/LLM Integration**
- **Primary**: Claude API via **Anthropic.SDK v5.5.1**
- **Decision**: Direct Anthropic.SDK integration (NOT SemanticKernel)
- **Rationale**: Simpler integration, fewer abstractions, better control

### **External APIs**
- **Telegram**: Telegram.Bot v22.6.2
- **Google**: Google APIs client libraries
- **GitHub**: Octokit v14.0.0

### **Infrastructure**
- **Logging**: Serilog with Console and File sinks
- **HTTP**: Polly for retry policies and resilience
- **Configuration**: ASP.NET Core Configuration system
- **Deployment**: Docker containers, Cloud Run target

---

## 🎯 CRITICAL DESIGN DECISIONS

### **1. Anthropic.SDK Direct Integration**
```csharp
// CHOSEN APPROACH: Direct Anthropic.SDK usage
public class ClaudeApiService
{
    private readonly AnthropicClient _client;
    
    public async Task<string> GenerateResponseAsync(string systemPrompt, string userMessage)
    {
        var request = new MessageRequest
        {
            Model = "claude-3-sonnet-20240229",
            MaxTokens = 4096,
            Messages = new List<Message>
            {
                new() { Role = "system", Content = systemPrompt },
                new() { Role = "user", Content = userMessage }
            }
        };
        
        var response = await _client.Messages.CreateAsync(request);
        return response.Content.FirstOrDefault()?.Text ?? string.Empty;
    }
}
```

**Benefits**:
- Direct control over API calls
- No additional abstraction layers
- Better error handling and debugging
- Consistent with existing project dependencies

### **2. Entity-First Personality Modeling**
```csharp
// CORE ENTITIES DESIGN
public class PersonalityProfile : BaseEntity
{
    // Core identity properties
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Profession { get; set; } = string.Empty;
    public string CorePhilosophy { get; set; } = string.Empty;
    public string CommunicationStyle { get; set; } = string.Empty;
    public string TechnicalPreferences { get; set; } = string.Empty;
    
    // Temporal modeling
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<PersonalityTrait> Traits { get; set; } = new List<PersonalityTrait>();
}

public class PersonalityTrait : BaseEntity
{
    public Guid PersonalityProfileId { get; set; }
    public virtual PersonalityProfile PersonalityProfile { get; set; } = null!;
    
    // Trait definition
    public string Category { get; set; } = string.Empty; // "Values", "Behavior", "Communication"
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Weight { get; set; } = 1.0; // 0.1-2.0 importance multiplier
    public bool IsActive { get; set; } = true;
    
    // Temporal behavior patterns
    public string? TemporalPattern { get; set; } // JSON: {"morning": "active", "evening": "reflective"}
    public string? ContextualModifiers { get; set; } // JSON: context-specific behavior adjustments
}
```

### **3. Data Flow Architecture**
```
IVAN_PROFILE_DATA.md → MarkdownParser → ProfileSeederService → Database Entities
                                                                       ↓
User Message → MessageProcessor → PersonalityService → SystemPrompt Generator
                                                           ↓
SystemPrompt + UserMessage → ClaudeApiService → Claude API → Response
```

---

## 📊 COMPONENT SPECIFICATIONS

### **Personality Engine Components**

#### **PersonalityService**
- **Responsibility**: Core personality logic and system prompt generation
- **Key Methods**: 
  - `GenerateSystemPromptAsync(PersonalityProfile profile)`
  - `GetPersonalityContextAsync(Guid profileId)`
  - `ApplyTemporalModifiersAsync(PersonalityTrait[] traits, DateTime context)`

#### **ClaudeApiService** ✅ **IMPLEMENTED**
- **Responsibility**: Claude API integration and response generation
- **Status**: Production-ready (303+ lines)
- **Features**: Error handling, retry policies, response formatting

#### **MessageProcessor** (P2.2 Target)
- **Responsibility**: End-to-end conversation flow orchestration
- **Key Methods**:
  - `ProcessConversationAsync(string userMessage, Guid profileId)`
  - `HandleContextualResponseAsync(ConversationContext context)`

#### **ProfileSeederService** (P2.3 Target)
- **Responsibility**: Load IVAN_PROFILE_DATA.md into database entities
- **Features**: Markdown parsing, data validation, idempotent loading

---

## 🔐 SECURITY & PRIVACY

### **API Key Management**
- Claude API keys stored in secure configuration
- Environment-specific key rotation
- No API keys in source code or logs

### **Personal Data Protection**
- PersonalityProfile data encrypted at rest
- Conversation history with retention policies  
- GDPR compliance considerations

### **Authentication & Authorization**
- JWT-based authentication for API endpoints
- Role-based access control for admin functions
- Secure external API integrations

---

## 📈 SCALABILITY CONSIDERATIONS

### **Performance Targets**
- **Response Time**: <2s for personality-aware responses
- **Throughput**: >100 requests/minute
- **Concurrent Users**: 50+ simultaneous conversations

### **Optimization Strategies**
- In-memory caching for personality profiles
- Connection pooling for database operations
- Async/await throughout the pipeline
- Claude API rate limiting and queuing

---

## 🔄 DATA CONSISTENCY

### **Entity Relationships**
```csharp
// One-to-Many: PersonalityProfile → PersonalityTraits
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<PersonalityTrait>()
        .HasOne(t => t.PersonalityProfile)
        .WithMany(p => p.Traits)
        .HasForeignKey(t => t.PersonalityProfileId)
        .OnDelete(DeleteBehavior.Cascade);
}
```

### **Temporal Data Handling**
- Trait activation/deactivation based on context
- Time-aware personality adjustments
- Historical behavior pattern analysis

---

**Referenced by**: [MAIN_PLAN.md](../MAIN_PLAN.md) - Architectural Documentation section  
**Last Updated**: 2025-09-05