# Flow 1: Critical Path - Backend Core Development

> **Developer**: Developer A (Lead Backend Developer)  
> **Duration**: 18 дней  
> **Utilization**: 100%  
> **Role**: Ведущий архитектор, критический путь  
> **Parent Plan**: [../00-MAIN_PLAN-PARALLEL-EXECUTION.md](../00-MAIN_PLAN-PARALLEL-EXECUTION.md)

---

## 🎯 OVERVIEW

**Цель Flow 1**: Разработка критического пути Backend системы - от базы данных до LLM интеграции.

**Критическая важность**: Этот flow определяет общее время проекта. Все остальные flows зависят от milestone checkpoints этого потока.

**Ответственность**: Developer A несет полную ответственность за архитектурные решения и техническое качество системы.

---

## 📋 TASK BREAKDOWN

### **Week 1: Foundation Infrastructure (Days 1-5)**

#### **Day 1: Project Structure Setup**
**Time**: 8 часов  
**Dependencies**: НЕТ (стартует немедленно)  
**Blocks**: Database Context (Day 2)

**Tasks**:
- [ ] Create .NET 8 Solution structure
  - `DigitalMe.sln`, `src/DigitalMe.API/`, `src/DigitalMe.Core/`, `src/DigitalMe.Data/`
- [ ] Setup Package References (EF Core, PostgreSQL, Serilog)
- [ ] Configure basic Program.cs with DI container
- [ ] Create initial appsettings.json configuration

**Acceptance Criteria**:
- ✅ `dotnet build` executes successfully (0 errors)
- ✅ `dotnet run --project src/DigitalMe.API` starts on port 5000
- ✅ Health check endpoint `/health` returns HTTP 200
- ✅ Swagger UI accessible at `/swagger`

**Implementation Details**: [../00-MAIN_PLAN/03-implementation/03-02-phase1-detailed.md](../00-MAIN_PLAN/03-implementation/03-02-phase1-detailed.md#day-1-project-infrastructure-8-hours)

#### **Day 2-3: Database Foundation**
**Time**: 16 часов (2 дня)  
**Dependencies**: Project Structure (Day 1)  
**Blocks**: Entity Models (Day 4)

**Tasks**:
- [ ] Implement DigitalMeContext with all DbSet configurations
- [ ] Create Entity Models: PersonalityProfile, Conversation, Message
- [ ] Configure PostgreSQL connection with retry policies
- [ ] Create and apply initial EF Core migrations

**Acceptance Criteria**:
- ✅ Migration creates all tables with proper indexes and constraints
- ✅ Database connection verified through health check
- ✅ JSONB columns configured for PostgreSQL personality traits
- ✅ Foreign key relationships established correctly

**Critical Dependencies**: PostgreSQL database instance must be available

#### **Day 4-5: Repository Layer**
**Time**: 16 часов (2 дня)  
**Dependencies**: Entity Models (Day 3)  
**Blocks**: Core Services (Week 2)

**Tasks**:
- [ ] Implement PersonalityRepository with CRUD operations
- [ ] Create ConversationRepository with message history support
- [ ] Setup Generic Repository pattern with base functionality
- [ ] Add structured logging with Serilog throughout repositories

**Acceptance Criteria**:
- ✅ Repository methods execute without exceptions
- ✅ Include() relationships load correctly (PersonalityProfile → Traits)
- ✅ Unit tests pass for all repository operations
- ✅ Logging captures all database operations with correlation IDs

**Performance Target**: Repository queries execute <100ms for 95% of operations

### **Week 2: Core Services Layer (Days 6-10)**

#### **Day 6-7: Dependency Injection & Core Services**
**Time**: 16 часов (2 дня)  
**Dependencies**: Repository Layer (Day 5)  
**Blocks**: API Controllers (Day 8)

**Tasks**:
- [ ] Complete DI container registration in Program.cs
- [ ] Implement PersonalityService with system prompt generation
- [ ] Create ConversationService for chat history management
- [ ] Add MemoryCache configuration for personality profiles

**Acceptance Criteria**:
- ✅ All services resolve through DI without exceptions
- ✅ PersonalityService generates system prompts with Ivan's traits
- ✅ ConversationService manages chat sessions correctly
- ✅ Cache layer improves personality profile load times

**Code Quality**: Service layer должен иметь >90% unit test coverage

#### **Day 8-10: API Controllers & Authentication**
**Time**: 24 часа (3 дня)  
**Dependencies**: Core Services (Day 7)  
**Blocks**: **MILESTONE 1** (Day 10)

**Tasks**:
- [ ] Implement PersonalityController with full CRUD endpoints
- [ ] Create ChatController for conversation management
- [ ] Add JWT Authentication with proper token validation
- [ ] Implement request/response DTOs with validation attributes

**Acceptance Criteria**:
- ✅ All API endpoints respond with proper HTTP status codes
- ✅ JWT authentication protects sensitive endpoints
- ✅ API documentation complete in OpenAPI/Swagger
- ✅ Integration tests pass for all controller methods

**🎯 MILESTONE 1 TRIGGER**: API Foundation Ready (Day 10)

### **Week 3: LLM Integration (Days 11-18)**

#### **Day 11-13: MCP Integration Setup**
**Time**: 24 часа (3 дня)  
**Dependencies**: **MILESTONE 1** (API Foundation)  
**Blocks**: LLM Services (Day 14)

**Tasks**:
- [ ] Configure Microsoft.SemanticKernel with Anthropic connector
- [ ] Implement MCP service wrapper with connection management
- [ ] Create personality prompt templates with mood calculations
- [ ] Add error handling and retry policies for MCP connections

**Acceptance Criteria**:
- ✅ MCP client successfully connects to Claude API
- ✅ Personality prompts include calculated trait values (directness: 0.90/1.0)
- ✅ Error handling gracefully manages connection failures
- ✅ Retry policies handle transient failures automatically

**Critical**: MCP integration является самым рискованным компонентом

#### **Day 14-16: LLM Services & Personality Engine**
**Time**: 24 часа (3 дня)  
**Dependencies**: MCP Integration (Day 13)  
**Blocks**: Agent Behavior Engine (Day 17)

**Tasks**:
- [ ] Implement PersonalityPromptEngine with mathematical trait calculations
- [ ] Create LLM response service with context-aware generation
- [ ] Add mood analysis service using NLP patterns
- [ ] Implement conversation context management

**Acceptance Criteria**:
- ✅ System prompts dynamically adjust traits based on mood
- ✅ LLM responses reflect Ivan's personality style consistently
- ✅ Conversation context maintained across multiple exchanges
- ✅ Response time <2 seconds for 95% of personality-aware queries

**🎯 MILESTONE 2 TRIGGER**: MCP Integration Complete (Day 16)

#### **Day 17-18: Agent Behavior Engine**
**Time**: 16 часов (2 дня)  
**Dependencies**: LLM Services (Day 16)  
**Blocks**: **MILESTONE 4** (Production Ready)

**Tasks**:
- [ ] Create behavioral decision engine with context awareness
- [ ] Implement response filtering and quality validation
- [ ] Add learning mechanism for personality refinement
- [ ] Setup WebSocket support for real-time chat

**Acceptance Criteria**:
- ✅ Agent responds in Ivan's style >90% of the time
- ✅ WebSocket connections support real-time bidirectional chat
- ✅ Response quality filtering catches inappropriate content
- ✅ Learning mechanism improves personality accuracy over time

**🎯 MILESTONE 4 TRIGGER**: Production Ready (Day 18)

---

## 🔄 DEPENDENCIES & SYNCHRONIZATION

### **Incoming Dependencies**
- **Day 1**: НЕТ зависимостей (критический путь стартует немедленно)
- **Database**: PostgreSQL instance должен быть доступен на Day 2

### **Outgoing Dependencies (что этот flow разблокирует)**
- **Day 10 → MILESTONE 1**: Разблокирует Flow 2 (External Integrations) и Flow 3 (Frontend)
- **Day 16 → MILESTONE 2**: Разблокирует продвинутые функции в других flows
- **Day 18 → MILESTONE 4**: Финальная интеграция всех flows

### **Cross-Flow Communication**
- **Daily standups**: Sync с Developer B и C по прогрессу и интерфейсам
- **Interface contracts**: API specification должна быть готова к Day 7
- **Code reviews**: Cross-flow reviews для обеспечения качества

---

## ⚠️ RISK MANAGEMENT

### **High-Priority Risks**

1. **MCP Integration Complexity**
   - **Probability**: Medium
   - **Impact**: High (может задержать весь проект)
   - **Mitigation**: HTTP REST API fallback, дополнительные 2 дня buffer времени
   - **Trigger**: Если MCP setup занимает >3 дней

2. **PostgreSQL Performance Issues**
   - **Probability**: Low
   - **Impact**: Medium
   - **Mitigation**: Database optimization, connection pooling, query optimization
   - **Trigger**: Если repository queries >200ms

3. **JWT Authentication Complexity**
   - **Probability**: Low
   - **Impact**: Medium
   - **Mitigation**: Use standard Microsoft.AspNetCore.Authentication.JwtBearer
   - **Trigger**: Если authentication не работает к Day 9

### **Mitigation Strategies**
- **Buffer time**: +1 день встроен в каждую неделю
- **Fallback options**: Готовые для каждого критического компонента
- **Daily monitoring**: Прогресс отслеживается ежедневно против плана

---

## 📊 PROGRESS TRACKING

### **Daily Progress Metrics**
```
Day 1:  ████░░░░░░░░░░░░░░░░░░░░░░░░ 1/18  (Project Setup)
Day 5:  ████████████░░░░░░░░░░░░░░░░ 5/18  (Foundation Complete)
Day 10: ████████████████████████░░░░ 10/18 (MILESTONE 1)
Day 16: ████████████████████████████ 16/18 (MILESTONE 2)
Day 18: ████████████████████████████ 18/18 (Complete)
```

### **Quality Gates**
- **Day 5**: Foundation quality review (code, tests, documentation)
- **Day 10**: API integration testing and MILESTONE 1 validation  
- **Day 16**: MCP integration verification and MILESTONE 2 validation
- **Day 18**: End-to-end system testing and production readiness

### **Success Metrics**
- **Code Quality**: >90% test coverage, zero critical bugs
- **Performance**: <2s response time, <100ms database queries
- **Architecture**: Clean separation of concerns, SOLID principles
- **Documentation**: Complete API docs, architectural decisions recorded

---

## 🎯 DELIVERABLES

### **Week 1 Deliverables**
- [ ] Complete .NET 8 project structure
- [ ] PostgreSQL database with all tables and relationships
- [ ] Repository layer with full CRUD operations
- [ ] Unit test suite with >80% coverage

### **Week 2 Deliverables**
- [ ] Core services with dependency injection
- [ ] API controllers with full CRUD endpoints
- [ ] JWT authentication and authorization
- [ ] **MILESTONE 1: API Foundation Ready**

### **Week 3 Deliverables**
- [ ] MCP integration with Claude API
- [ ] Personality-aware LLM services
- [ ] Agent behavior engine with real-time chat
- [ ] **MILESTONE 4: Production-ready backend system**

---

## 🔗 NAVIGATION

- **← Parent Plan**: [Parallel Execution Plan](../00-MAIN_PLAN-PARALLEL-EXECUTION.md)
- **→ Parallel Flows**: [Flow 2](../Parallel-Flow-2/) | [Flow 3](../Parallel-Flow-3/)
- **→ Sync Points**: [Milestone 1](../Sync-Points/Milestone-1-API-Foundation.md) | [Milestone 2](../Sync-Points/Milestone-2-MCP-Complete.md)
- **→ Original Plan**: [Main Plan](../00-MAIN_PLAN.md) (reference)

---

**⚡ CRITICAL PATH**: Этот flow определяет общее время проекта. Любая задержка здесь влияет на все остальные flows. Priority: **МАКСИМАЛЬНЫЙ**.

**🎯 SUCCESS CRITERIA**: Flow 1 считается успешным если все milestones достигнуты в срок и backend система полностью функциональна с LLM интеграцией.