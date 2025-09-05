# Week 1: Foundation & Setup

**Родительский план**: [../00-MAIN_PLAN-Phase-Execution.md](../00-MAIN_PLAN-Phase-Execution.md)

> **Week 1 Focus**: Project infrastructure, database setup, core services, API controllers, security implementation

---

## 📅 **Daily Implementation Plan** *(5 дней)*

### **Day 1: Project Infrastructure Setup** *(3 hours)*
- **Tasks**:
  - Create .NET 8 multi-project solution structure
  - Configure NuGet packages and dependencies
  - Setup PostgreSQL database with Docker
  - Establish CI/CD pipeline basics
- **Deliverables**: Working solution with Swagger UI and health checks
- **Success Criteria**: `dotnet build` (0 errors), health endpoint accessible

### **Day 2: Database & Entity Framework** *(3 hours)*  
- **Tasks**:
  - Implement Entity Framework Core DbContext
  - Configure PostgreSQL with JSONB support
  - Create initial database migrations
  - Setup connection strings and retry policies
- **Deliverables**: Database schema with personality profiles and conversations
- **Success Criteria**: Migrations applied, connection verified in health check

### **Day 3: Core Domain Models** *(3 hours)*
- **Tasks**:
  - Design PersonalityProfile and Conversation entities
  - Implement core domain models with JSONB traits
  - Document architectural decisions (ADRs)
  - Setup audit trails and timestamping
- **Deliverables**: Complete entity models with trait storage
- **Success Criteria**: Models compile, support complex JSON serialization

### **Day 4: Repository Pattern & Services** *(4 hours)*
- **Tasks**:
  - Implement repository pattern with architecture stubs
  - Configure dependency injection container
  - Setup cross-cutting concerns (logging, caching)
  - Create basic CRUD operations
- **Deliverables**: Complete infrastructure setup with service layer
- **Success Criteria**: All services registered and resolvable via DI

### **Day 5: API Controllers & Security** *(3 hours)*
- **Tasks**:
  - Implement API controllers with basic CRUD
  - Setup JWT authentication and authorization
  - Configure CORS and API versioning
  - Add request/response validation
- **Deliverables**: Working API with JWT authentication
- **Success Criteria**: API endpoints accessible with proper authentication

---

## ✅ **Week 1 Success Criteria**

### **Technical Requirements**:
- [ ] **Compilation**: `dotnet build` (0 errors, 0 warnings)
- [ ] **Database**: Migrations applied, connection verified in health check
- [ ] **API Startup**: Service starts on port 5000, Swagger UI accessible
- [ ] **Health Monitoring**: `/health` endpoint returns comprehensive status
- [ ] **Authentication**: JWT token generation and validation working
- [ ] **CRUD Operations**: Basic Create, Read, Update, Delete for core entities

### **Architecture Requirements**:
- [ ] **Architecture Documentation**: ADRs created for major decisions
- [ ] **Repository Pattern**: Architecture stubs implemented with clear specifications
- [ ] **Dependency Injection**: All services registered and resolvable
- [ ] **Structured Logging**: Serilog configured with JSON output and correlation IDs
- [ ] **Error Handling**: Global exception handling middleware configured

### **Testing Requirements**:
- [ ] **Test Coverage**: ≥80% coverage for core business logic
- [ ] **Unit Tests**: Architecture stubs with comprehensive test plans
- [ ] **Integration Tests**: Basic database and API integration tests
- [ ] **Performance**: API response time <500ms for basic operations

---

## 🔧 **Technology Stack**

### **Backend Core**:
- **Framework**: ASP.NET Core 8
- **Database**: PostgreSQL 16 + Entity Framework Core 8.0.10
- **Authentication**: JWT tokens with configurable expiry
- **Logging**: Serilog with structured logging
- **API Documentation**: Swagger/OpenAPI 3.0

### **Configuration Requirements**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=digitalme;Username=postgres;Password=<password>"
  },
  "JwtSettings": {
    "SecretKey": "<256-bit-secret>",
    "Issuer": "DigitalMe.API",
    "Audience": "DigitalMe.Clients",
    "ExpiryMinutes": 60
  }
}
```

---

## 🚨 **Prerequisites & Dependencies**

### **Required Installations**:
- [ ] .NET 8 SDK installed
- [ ] PostgreSQL 16 installed/accessible
- [ ] Docker Desktop (for containerized PostgreSQL)
- [ ] Visual Studio 2022 or VS Code with C# extension

### **Environment Setup**:
- [ ] PostgreSQL user with database creation privileges
- [ ] Environment variables for connection strings
- [ ] Development certificates configured for HTTPS

---

## 📁 **Project Structure Created**

```
src/
├── DigitalMe.API/           # Web API project
├── DigitalMe.Core/          # Business logic and services  
├── DigitalMe.Data/          # Data access and repositories
└── DigitalMe.Shared/        # Common models and utilities

tests/
├── DigitalMe.Tests.Unit/    # Unit tests
└── DigitalMe.Tests.Integration/ # Integration tests
```

---

## 🔄 **Next Steps**

- **Week 2**: [MCP Integration & LLM Services](./Week-2-MCP-LLM.md)
- **Dependencies**: Week 1 must be completed before proceeding
- **Handoff Criteria**: All success criteria met, comprehensive test coverage achieved

---

**⏱️ Estimated Time**: 16 hours total (3-4 hours per day)  
**🎯 Key Milestone**: Functional API with authentication and database integration