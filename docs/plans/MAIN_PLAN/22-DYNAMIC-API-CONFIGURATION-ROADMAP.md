# 🔧 Dynamic API Configuration System - Implementation Plan

**⬅️ Back to:** [MAIN_PLAN.md](../MAIN_PLAN.md) - Central entry point for all plans
**📋 Related:** [09-CONSOLIDATED-EXECUTION-PLAN.md](09-CONSOLIDATED-EXECUTION-PLAN.md) - Previous plan

**Plan ID:** 09.5
**Status:** READY FOR EXECUTION
**Priority:** HIGH VALUE OPTIONAL FEATURE
**Created:** 2025-09-29
**Type:** Feature Enhancement Plan

---

## 🎯 EXECUTIVE SUMMARY

**CONCEPT:** Implement a dynamic API configuration system that allows seamless transition from system-provided demo keys to user-configured API keys across all integrations, with enterprise-grade security and user-friendly management interface.

**BUSINESS VALUE:**
- **Demo Experience:** New users get instant access with system keys
- **User Autonomy:** Seamless transition to user-owned API keys
- **Cost Control:** System demo quotas prevent abuse while enabling trials
- **Security First:** Enterprise-grade encryption and key management
- **Scalability:** Support for unlimited integrations without code changes

**TECHNICAL APPROACH:**
- Backward-compatible extension of existing appsettings.json configuration
- Encrypted user key storage with per-user isolation
- Dynamic service configuration loading with fallback mechanisms
- Comprehensive usage tracking and quota management system

---

## 🏗️ SYSTEM ARCHITECTURE OVERVIEW

### Current State Analysis
**Existing Services to Enhance:**
- `PersonalityService` → Anthropic Claude API integration
- `AnthropicService` → Direct API communication
- `SlackApiClient` → Slack integration services
- `McpService` → MCP server configuration
- Various integration services in `src/DigitalMe/Integrations/External/`

**Current Configuration Pattern:**
```json
{
  "DigitalMe": {
    "ApiBaseUrl": "http://localhost:5003",
    "Authentication": { ... },
    // Static API keys in appsettings
    "ApiKeys": {
      "Anthropic": "sk-ant-...",
      "OpenAI": "sk-..."
    }
  }
}
```

**Target Architecture:**
```json
{
  "DigitalMe": {
    "ApiConfiguration": {
      "SystemKeys": {
        "Anthropic": { "Key": "sk-ant-...", "DemoQuota": 100 },
        "OpenAI": { "Key": "sk-...", "DemoQuota": 50 }
      },
      "DefaultQuotas": {
        "DailyRequests": 1000,
        "MonthlyTokens": 50000
      }
    }
  }
}
```

### New Components Architecture

**1. Configuration Management Layer**
```
ApiConfigurationService
├── SystemKeyProvider (demo/fallback keys)
├── UserKeyProvider (encrypted user keys)
├── QuotaManager (usage tracking)
└── ConfigurationResolver (dynamic key resolution)
```

**2. Security Layer**
```
KeyEncryptionService
├── AES-256-GCM encryption
├── Per-user encryption keys
├── Secure key derivation (PBKDF2)
└── Zero-knowledge storage
```

**3. Usage Tracking Layer**
```
ApiUsageTracker
├── Request counting
├── Token consumption tracking
├── Quota enforcement
└── Analytics and reporting
```

**4. UI Management Layer**
```
Blazor Configuration Components
├── ApiKeyManagementComponent
├── IntegrationTestComponent
├── UsageDisplayComponent
└── QuotaManagementComponent
```

---

## 📋 PHASE-BASED IMPLEMENTATION PLAN

## PHASE 1: Foundation Infrastructure (Week 1-2)

### P1.1: Database Schema Design
**Estimated Time:** 2 days
**Dependencies:** None

**Task:** Create encrypted API key storage schema
```sql
-- UserApiConfigurations table
CREATE TABLE UserApiConfigurations (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL,
    Provider NVARCHAR(100) NOT NULL, -- 'Anthropic', 'OpenAI', etc.
    EncryptedApiKey NVARCHAR(MAX) NOT NULL,
    EncryptionIV NVARCHAR(100) NOT NULL,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    LastUsedAt DATETIME2,
    UNIQUE(UserId, Provider)
);

-- ApiUsageTracking table
CREATE TABLE ApiUsageTracking (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL,
    Provider NVARCHAR(100) NOT NULL,
    RequestCount INT DEFAULT 0,
    TokensUsed BIGINT DEFAULT 0,
    LastRequestAt DATETIME2,
    QuotaPeriodStart DATETIME2,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);
```

**Deliverables:**
- `Data/Entities/UserApiConfiguration.cs`
- `Data/Entities/ApiUsageRecord.cs`
- Migration files for new tables
- Repository interfaces and implementations

**Acceptance Criteria:**
- Database tables created with proper indexing
- Entity Framework models configured
- Repository pattern implemented
- Unit tests for data layer (90%+ coverage)

### P1.2: Core Service Architecture
**Estimated Time:** 3 days
**Dependencies:** P1.1 completed

**Task:** Implement core configuration services

**Key Components:**
```csharp
public interface IApiConfigurationService
{
    Task<string?> GetApiKeyAsync(string provider, string userId = null);
    Task SetUserApiKeyAsync(string userId, string provider, string apiKey);
    Task<bool> TestConnectionAsync(string provider, string apiKey);
    Task<ApiUsageInfo> GetUsageInfoAsync(string userId, string provider);
}

public interface IKeyEncryptionService
{
    Task<EncryptedKeyInfo> EncryptApiKeyAsync(string apiKey, string userId);
    Task<string> DecryptApiKeyAsync(EncryptedKeyInfo encryptedInfo, string userId);
}

public interface IQuotaManager
{
    Task<bool> CheckQuotaAsync(string userId, string provider);
    Task RecordUsageAsync(string userId, string provider, int tokens);
    Task<QuotaStatus> GetQuotaStatusAsync(string userId, string provider);
}
```

**Deliverables:**
- Core service interfaces
- Implementation classes with proper DI registration
- Configuration resolution logic (system → user fallback)
- Usage tracking implementation
- Comprehensive unit tests

**Acceptance Criteria:**
- Services properly registered in DI container
- Fallback mechanism working (system → user keys)
- Encryption/decryption tested with multiple users
- Quota tracking functional
- 95%+ test coverage for core services

### P1.3: Integration with Existing Services
**Estimated Time:** 2 days
**Dependencies:** P1.2 completed

**Task:** Integrate dynamic configuration with existing services

**Modifications Required:**
```csharp
// Current: PersonalityService with static config
public class PersonalityService
{
    private readonly string _apiKey; // From appsettings.json

    // New: Dynamic configuration
    private readonly IApiConfigurationService _configService;

    public async Task<string> ProcessAsync(string input, string userId)
    {
        var apiKey = await _configService.GetApiKeyAsync("Anthropic", userId);
        // Use dynamic key for API call
    }
}
```

**Services to Modify:**
- `PersonalityService.cs`
- `AnthropicService.cs` (if exists)
- `SlackApiClient.cs`
- All services in `Integrations/External/`

**Deliverables:**
- Modified service classes
- Backward compatibility maintained
- Integration tests
- Performance impact assessment

**Acceptance Criteria:**
- All existing functionality preserved
- Dynamic key resolution working
- No performance degradation
- Integration tests passing
- Backward compatibility verified

## PHASE 2: Security Implementation (Week 3)

### P2.1: Advanced Encryption System
**Estimated Time:** 3 days
**Dependencies:** P1 completed

**Task:** Implement enterprise-grade encryption for API keys

**Security Requirements:**
```csharp
public class KeyEncryptionService : IKeyEncryptionService
{
    // AES-256-GCM encryption
    // Per-user derived keys using PBKDF2
    // Secure random IV generation
    // Zero-knowledge storage (keys never logged)

    public async Task<EncryptedKeyInfo> EncryptApiKeyAsync(string apiKey, string userId)
    {
        var userSalt = await GetOrCreateUserSaltAsync(userId);
        var derivedKey = DeriveEncryptionKey(userId, userSalt);
        var iv = GenerateSecureIV();

        using var aes = new AesCcm(derivedKey);
        // Encrypt with authentication
        return new EncryptedKeyInfo(encryptedData, iv, authTag);
    }
}
```

**Security Features:**
- **AES-256-GCM**: Authenticated encryption preventing tampering
- **PBKDF2**: Secure key derivation with user-specific salts
- **Zero-logging**: API keys never appear in logs or exceptions
- **Memory protection**: Secure key handling in memory
- **Audit trail**: Key creation/modification logging (not content)

**Deliverables:**
- Production-ready encryption service
- Security audit documentation
- Performance benchmarks
- Memory leak verification
- Comprehensive security tests

**Acceptance Criteria:**
- Security review passed
- Performance under 10ms per operation
- Memory usage optimized
- No key leakage in logs/exceptions
- Encryption strength verified

### P2.2: API Key Validation & Testing
**Estimated Time:** 2 days
**Dependencies:** P2.1 completed

**Task:** Implement secure API key validation system

**Validation Components:**
```csharp
public class ApiKeyValidator
{
    public async Task<ValidationResult> ValidateAnthropicKeyAsync(string apiKey)
    {
        // Test API call with minimal usage
        // Return detailed validation result
    }

    public async Task<ValidationResult> ValidateOpenAIKeyAsync(string apiKey)
    {
        // Provider-specific validation
    }
}
```

**Validation Strategy:**
- **Minimal impact testing**: Use cheapest possible API calls
- **Provider-specific**: Different validation per API provider
- **Error categorization**: Invalid format vs invalid key vs quota exceeded
- **Security logging**: Log validation attempts (success/failure only)

**Deliverables:**
- Validation service for all supported providers
- Test connection functionality
- Error handling and user feedback
- Security audit for validation process

**Acceptance Criteria:**
- All API providers validated correctly
- Minimal cost impact (< $0.01 per validation)
- Clear error messages for users
- No key exposure in error messages
- Validation results properly cached

## PHASE 3: User Interface Development (Week 4)

### P3.1: Blazor Configuration Components
**Estimated Time:** 4 days
**Dependencies:** P2 completed

**Task:** Create user-friendly configuration interface

**UI Components Structure:**
```
Pages/Settings/
├── ApiConfiguration.razor        # Main configuration page
├── Components/
│   ├── ApiKeyInput.razor         # Secure key input component
│   ├── ProviderCard.razor        # Individual provider configuration
│   ├── UsageDisplay.razor        # Usage statistics
│   ├── QuotaIndicator.razor      # Quota status indicator
│   └── ConnectionTest.razor      # Test connection functionality
└── Shared/
    ├── SecureInput.razor         # Secure input with masking
    └── StatusIndicator.razor     # Status icons and messages
```

**Key UI Features:**
```razor
@* ApiConfiguration.razor *@
<h3>API Configuration</h3>

<div class="provider-grid">
    @foreach (var provider in ApiProviders)
    {
        <ProviderCard Provider="@provider"
                      OnKeyUpdated="HandleKeyUpdate"
                      OnTestConnection="TestConnection" />
    }
</div>

<UsageDisplay UserId="@CurrentUserId" />
```

**UX Requirements:**
- **Secure input masking**: Keys shown as ••••••••
- **Real-time validation**: Immediate feedback on key format
- **Test connection**: One-click connectivity testing
- **Usage visualization**: Clear quota and usage display
- **Responsive design**: Mobile-friendly interface
- **Progressive disclosure**: Show advanced settings on demand

**Deliverables:**
- Complete Blazor component suite
- Responsive CSS styling
- JavaScript interop for secure input handling
- Component documentation
- UI/UX testing completed

**Acceptance Criteria:**
- All UI components functional
- Secure key masking working
- Test connection functionality
- Mobile-responsive design
- Accessibility compliance (WCAG 2.1)
- User testing feedback incorporated

### P3.2: Settings Integration
**Estimated Time:** 2 days
**Dependencies:** P3.1 completed

**Task:** Integrate API configuration into existing settings system

**Integration Points:**
- Add to existing settings navigation
- Integrate with user authentication
- Connect to notification system
- Add help documentation

**Settings Page Structure:**
```
Settings/
├── Profile                 # Existing
├── Preferences            # Existing
├── API Configuration      # NEW
├── Usage & Billing        # NEW
└── Security              # Enhanced
```

**Deliverables:**
- Integrated settings navigation
- User role-based access control
- Help documentation
- Settings persistence
- Navigation breadcrumbs

**Acceptance Criteria:**
- Seamless integration with existing UI
- Proper access control implemented
- Help documentation accessible
- Settings saved persistently
- Navigation intuitive

## PHASE 4: Advanced Features (Week 5)

### P4.1: Usage Analytics & Monitoring
**Estimated Time:** 3 days
**Dependencies:** P3 completed

**Task:** Implement comprehensive usage tracking and analytics

**Analytics Components:**
```csharp
public class UsageAnalyticsService
{
    public async Task<UsageReport> GenerateUsageReportAsync(
        string userId,
        DateRange period,
        string[] providers)
    {
        // Generate detailed usage analytics
        return new UsageReport
        {
            TotalRequests = requests,
            TotalTokens = tokens,
            CostEstimate = cost,
            ProviderBreakdown = breakdown,
            TrendAnalysis = trends
        };
    }
}
```

**Analytics Features:**
- **Real-time usage tracking**: Live updates of API consumption
- **Cost estimation**: Approximate cost calculation per provider
- **Trend analysis**: Usage patterns and predictions
- **Quota warnings**: Proactive notifications before limits
- **Detailed breakdowns**: Per-provider, per-day analytics
- **Export functionality**: CSV/JSON export for external analysis

**Deliverables:**
- Analytics service implementation
- Usage visualization components
- Export functionality
- Real-time notification system
- Performance optimization

**Acceptance Criteria:**
- Real-time usage updates working
- Cost estimates within 5% accuracy
- Export functionality tested
- Performance impact < 2% overhead
- User notifications timely and relevant

### P4.2: Advanced Quota Management
**Estimated Time:** 2 days
**Dependencies:** P4.1 completed

**Task:** Implement sophisticated quota management system

**Quota Management Features:**
```csharp
public class AdvancedQuotaManager
{
    // Multi-tier quota system
    public async Task<QuotaAllocation> CalculateUserQuotaAsync(string userId)
    {
        // Consider user tier, usage history, payment status
        // Dynamic quota adjustment based on behavior
    }

    // Smart throttling
    public async Task<ThrottleDecision> ShouldThrottleAsync(string userId, string provider)
    {
        // Intelligent throttling based on usage patterns
    }
}
```

**Advanced Features:**
- **Dynamic quota adjustment**: Based on user behavior and tier
- **Smart throttling**: Gradual slowdown instead of hard cutoffs
- **Quota sharing**: Family/team account quota sharing
- **Rollover allowances**: Unused quota carryover
- **Emergency buffers**: Critical request allowances
- **Provider-specific limits**: Different limits per API provider

**Deliverables:**
- Advanced quota management service
- Throttling implementation
- Admin interface for quota management
- Quota sharing functionality
- Comprehensive testing

**Acceptance Criteria:**
- Dynamic quota adjustment functional
- Smart throttling prevents hard cutoffs
- Quota sharing working correctly
- Admin controls properly secured
- Edge cases handled gracefully

## PHASE 5: Enterprise Features & Deployment (Week 6)

### P5.1: Admin Dashboard & Management
**Estimated Time:** 3 days
**Dependencies:** P4 completed

**Task:** Create administrative interface for system management

**Admin Dashboard Features:**
```razor
@* AdminDashboard.razor *@
<AdminLayout>
    <SystemOverview />
    <UserManagement />
    <QuotaManagement />
    <UsageAnalytics />
    <SecurityAudit />
    <SystemConfiguration />
</AdminLayout>
```

**Management Capabilities:**
- **System-wide usage monitoring**: All users, all providers
- **Quota management**: Set system and user quotas
- **Security monitoring**: Key usage auditing
- **System configuration**: Manage system API keys
- **User support tools**: Help troubleshoot user issues
- **Cost monitoring**: Track system API costs

**Role-based Access:**
- **Super Admin**: Full system access
- **Admin**: User and quota management
- **Support**: Read-only troubleshooting access
- **User**: Own configuration only

**Deliverables:**
- Admin dashboard implementation
- Role-based access control
- Security audit functionality
- System monitoring tools
- Admin documentation

**Acceptance Criteria:**
- Admin dashboard fully functional
- Proper role-based access implemented
- Security audit trails complete
- System monitoring accurate
- Documentation comprehensive

### P5.2: Production Deployment & Security Hardening
**Estimated Time:** 2 days
**Dependencies:** P5.1 completed

**Task:** Prepare system for production deployment

**Security Hardening Checklist:**
- **Encryption verification**: All keys properly encrypted
- **Access control audit**: All endpoints secured
- **Logging security**: No sensitive data in logs
- **Input validation**: All user inputs sanitized
- **Rate limiting**: API abuse prevention
- **Monitoring alerts**: Security event notifications

**Production Configuration:**
```json
{
  "DigitalMe": {
    "ApiConfiguration": {
      "EncryptionSettings": {
        "KeyDerivationIterations": 100000,
        "EncryptionAlgorithm": "AES-256-GCM"
      },
      "SecuritySettings": {
        "MaxFailedValidations": 3,
        "ValidationCooldownMinutes": 15,
        "AuditLogRetentionDays": 90
      },
      "QuotaSettings": {
        "DefaultDemoQuota": 50,
        "MaxUserQuota": 10000,
        "QuotaResetSchedule": "0 0 * * *"
      }
    }
  }
}
```

**Deployment Steps:**
1. **Database migration**: Apply new schema to production
2. **Service deployment**: Deploy with zero-downtime
3. **Configuration migration**: Migrate existing API keys
4. **Monitoring setup**: Configure alerts and dashboards
5. **Security validation**: Final security review
6. **User communication**: Notify users of new features

**Deliverables:**
- Production-ready configuration
- Deployment scripts and procedures
- Security hardening documentation
- Monitoring and alerting setup
- User migration tools

**Acceptance Criteria:**
- Zero-downtime deployment successful
- All security hardening implemented
- Monitoring and alerts functional
- Existing users migrated successfully
- Documentation complete and accurate

---

## 🔧 TECHNICAL IMPLEMENTATION DETAILS

### API Provider Support Matrix

| Provider | Authentication | Validation Method | Usage Tracking | Cost Estimation |
|----------|----------------|-------------------|----------------|-----------------|
| **Anthropic Claude** | API Key | `/messages` test call | Token counting | $0.015/1K tokens |
| **OpenAI GPT** | API Key | `/models` endpoint | Token counting | Variable by model |
| **MCP Servers** | Various | Server ping | Request counting | Custom/Free |
| **TwoCaptcha** | API Key | Balance check | Request counting | $0.001/captcha |
| **Email SMTP/IMAP** | Username/Password | Connection test | Message counting | Usually free |
| **Slack** | OAuth2/Bot Token | `/auth.test` | API calls | Rate limited |
| **ClickUp** | API Key | `/user` endpoint | API calls | Rate limited |
| **GitHub** | OAuth2/Token | `/user` endpoint | API calls | Rate limited |
| **Telegram** | Bot Token | `/getMe` | API calls | Free |
| **Google APIs** | OAuth2/Service Account | Various | API calls | Variable |

### Database Schema Details

```sql
-- Complete schema with relationships and constraints
CREATE TABLE UserApiConfigurations (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId NVARCHAR(450) NOT NULL,
    Provider NVARCHAR(100) NOT NULL,
    DisplayName NVARCHAR(200) NULL,
    EncryptedApiKey NVARCHAR(MAX) NOT NULL,
    EncryptionIV NVARCHAR(100) NOT NULL,
    EncryptionSalt NVARCHAR(100) NOT NULL,
    KeyFingerprint NVARCHAR(100) NOT NULL, -- For key identification without decryption
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
    LastUsedAt DATETIME2 NULL,
    LastValidatedAt DATETIME2 NULL,
    ValidationStatus NVARCHAR(50) DEFAULT 'Unknown', -- Valid, Invalid, Expired, etc.

    CONSTRAINT FK_UserApiConfigurations_UserId FOREIGN KEY (UserId)
        REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_UserApiConfigurations_UserProvider
        UNIQUE(UserId, Provider),

    INDEX IX_UserApiConfigurations_UserId (UserId),
    INDEX IX_UserApiConfigurations_Provider (Provider),
    INDEX IX_UserApiConfigurations_LastUsed (LastUsedAt DESC)
);

CREATE TABLE ApiUsageRecords (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId NVARCHAR(450) NOT NULL,
    ConfigurationId UNIQUEIDENTIFIER NOT NULL,
    Provider NVARCHAR(100) NOT NULL,
    RequestType NVARCHAR(100) NOT NULL, -- 'chat', 'completion', 'validation', etc.
    TokensUsed INT DEFAULT 0,
    CostEstimate DECIMAL(18,6) DEFAULT 0,
    RequestTimestamp DATETIME2 DEFAULT GETUTCDATE(),
    ResponseTime INT NULL, -- milliseconds
    Success BIT DEFAULT 1,
    ErrorType NVARCHAR(100) NULL,

    CONSTRAINT FK_ApiUsageRecords_Configuration
        FOREIGN KEY (ConfigurationId)
        REFERENCES UserApiConfigurations(Id) ON DELETE CASCADE,

    INDEX IX_ApiUsageRecords_UserId_Date (UserId, RequestTimestamp DESC),
    INDEX IX_ApiUsageRecords_Provider_Date (Provider, RequestTimestamp DESC),
    INDEX IX_ApiUsageRecords_Configuration (ConfigurationId)
);

CREATE TABLE QuotaAllocations (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId NVARCHAR(450) NOT NULL,
    Provider NVARCHAR(100) NOT NULL,
    QuotaType NVARCHAR(50) NOT NULL, -- 'Daily', 'Monthly', 'Demo'
    AllowedRequests INT DEFAULT 0,
    AllowedTokens BIGINT DEFAULT 0,
    AllowedCost DECIMAL(18,2) DEFAULT 0,
    PeriodStart DATETIME2 NOT NULL,
    PeriodEnd DATETIME2 NOT NULL,
    UsedRequests INT DEFAULT 0,
    UsedTokens BIGINT DEFAULT 0,
    UsedCost DECIMAL(18,6) DEFAULT 0,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),

    CONSTRAINT UQ_QuotaAllocations_UserProviderPeriod
        UNIQUE(UserId, Provider, QuotaType, PeriodStart),

    INDEX IX_QuotaAllocations_UserId (UserId),
    INDEX IX_QuotaAllocations_Period (PeriodStart, PeriodEnd)
);

CREATE TABLE SystemConfiguration (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ConfigKey NVARCHAR(200) NOT NULL UNIQUE,
    ConfigValue NVARCHAR(MAX) NOT NULL,
    Description NVARCHAR(500) NULL,
    Category NVARCHAR(100) NOT NULL,
    IsEncrypted BIT DEFAULT 0,
    LastUpdated DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedBy NVARCHAR(450) NULL,

    INDEX IX_SystemConfiguration_Category (Category),
    INDEX IX_SystemConfiguration_Key (ConfigKey)
);
```

### Security Implementation

```csharp
public class KeyEncryptionService : IKeyEncryptionService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<KeyEncryptionService> _logger;
    private const int KeyDerivationIterations = 100000;
    private const int SaltSize = 32;
    private const int IVSize = 12; // For GCM mode
    private const int TagSize = 16; // Authentication tag size

    public async Task<EncryptedKeyInfo> EncryptApiKeyAsync(string apiKey, string userId)
    {
        try
        {
            // Generate unique salt for this encryption
            var salt = new byte[SaltSize];
            RandomNumberGenerator.Fill(salt);

            // Derive encryption key from user ID and salt
            var derivedKey = DeriveKeyFromUser(userId, salt);

            // Generate random IV for GCM mode
            var iv = new byte[IVSize];
            RandomNumberGenerator.Fill(iv);

            // Encrypt with AES-256-GCM for authenticated encryption
            var plaintext = Encoding.UTF8.GetBytes(apiKey);
            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[TagSize];

            using var aes = new AesGcm(derivedKey);
            aes.Encrypt(iv, plaintext, ciphertext, tag);

            // Create fingerprint for key identification (non-reversible)
            var fingerprint = CreateKeyFingerprint(apiKey);

            return new EncryptedKeyInfo
            {
                EncryptedData = Convert.ToBase64String(ciphertext),
                IV = Convert.ToBase64String(iv),
                Salt = Convert.ToBase64String(salt),
                AuthTag = Convert.ToBase64String(tag),
                Fingerprint = fingerprint
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt API key for user {UserId}",
                userId.Substring(0, 8) + "***"); // Log partial user ID only
            throw new SecurityException("Encryption failed", ex);
        }
        finally
        {
            // Ensure sensitive data is cleared from memory
            GC.Collect();
        }
    }

    private byte[] DeriveKeyFromUser(string userId, byte[] salt)
    {
        // Use PBKDF2 with user ID as password and unique salt
        using var pbkdf2 = new Rfc2898DeriveBytes(
            userId,
            salt,
            KeyDerivationIterations,
            HashAlgorithmName.SHA256);

        return pbkdf2.GetBytes(32); // 256-bit key
    }

    private string CreateKeyFingerprint(string apiKey)
    {
        // Create non-reversible fingerprint for key identification
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToBase64String(hash)[..16]; // First 16 chars
    }
}
```

### Performance Considerations

**Expected Performance Metrics:**
- **Key Encryption/Decryption**: < 10ms per operation
- **Database Queries**: < 50ms for configuration lookup
- **API Key Validation**: < 2000ms per provider
- **Usage Tracking**: < 5ms per request
- **Memory Usage**: < 50MB additional for the entire system

**Optimization Strategies:**
- **Caching**: Redis cache for decrypted keys (short TTL)
- **Connection Pooling**: Optimized database connections
- **Lazy Loading**: Load configurations only when needed
- **Batch Processing**: Bulk usage record insertion
- **Indexed Queries**: Proper database indexing strategy

### Error Handling Strategy

```csharp
public enum ApiConfigurationError
{
    KeyNotFound,
    DecryptionFailed,
    QuotaExceeded,
    ProviderUnavailable,
    InvalidKeyFormat,
    ValidationTimeout,
    SecurityViolation
}

public class ApiConfigurationException : Exception
{
    public ApiConfigurationError ErrorType { get; }
    public string Provider { get; }
    public bool IsUserFacing { get; }

    public ApiConfigurationException(
        ApiConfigurationError errorType,
        string provider,
        string message,
        bool isUserFacing = true)
        : base(message)
    {
        ErrorType = errorType;
        Provider = provider;
        IsUserFacing = isUserFacing;
    }
}
```

---

## 🎯 SUCCESS METRICS & ACCEPTANCE CRITERIA

### Functional Requirements
- [ ] **Dynamic Key Resolution**: System correctly chooses between system and user keys
- [ ] **Encryption Security**: All user keys encrypted with AES-256-GCM
- [ ] **UI Functionality**: All configuration UI components working correctly
- [ ] **Provider Support**: All 10 specified API providers supported
- [ ] **Usage Tracking**: Accurate tracking of API usage and costs
- [ ] **Quota Management**: Proper quota enforcement and notifications

### Performance Requirements
- [ ] **Response Time**: Configuration lookup < 50ms (95th percentile)
- [ ] **Encryption Performance**: Key operations < 10ms each
- [ ] **Memory Usage**: < 50MB additional system memory
- [ ] **Database Performance**: All queries < 100ms
- [ ] **UI Responsiveness**: Page load times < 2 seconds

### Security Requirements
- [ ] **Zero Key Exposure**: No API keys in logs, exceptions, or UI
- [ ] **Proper Encryption**: Security audit passed
- [ ] **Access Control**: Role-based permissions working
- [ ] **Input Validation**: All user inputs properly sanitized
- [ ] **Audit Trail**: All configuration changes logged

### Usability Requirements
- [ ] **Intuitive UI**: User testing shows 90%+ task completion
- [ ] **Error Messages**: Clear, actionable error messages
- [ ] **Help Documentation**: Comprehensive user guide available
- [ ] **Mobile Support**: Responsive design on mobile devices
- [ ] **Accessibility**: WCAG 2.1 AA compliance

### Integration Requirements
- [ ] **Backward Compatibility**: Existing functionality preserved
- [ ] **Service Integration**: All existing services work with dynamic keys
- [ ] **Deployment**: Zero-downtime production deployment
- [ ] **Migration**: Existing configurations migrated successfully
- [ ] **Monitoring**: Comprehensive monitoring and alerting

---

## 🚀 DEPLOYMENT & ROLLOUT STRATEGY

### Phase 1: Internal Testing (Week 7)
- Deploy to development environment
- Internal team testing
- Security audit and penetration testing
- Performance benchmarking
- Bug fixes and optimizations

### Phase 2: Beta Release (Week 8)
- Deploy to staging environment
- Limited beta user group (10-20 users)
- User feedback collection
- UI/UX refinements
- Documentation updates

### Phase 3: Gradual Rollout (Week 9)
- Deploy to production with feature flag
- Gradual user rollout (10% → 50% → 100%)
- Monitor system performance and stability
- User support and issue resolution
- Full documentation release

### Rollback Plan
- Database migration rollback scripts prepared
- Feature flag for instant disable
- Previous version deployment ready
- User data backup and recovery procedures
- Communication plan for users

---

## 💰 COST-BENEFIT ANALYSIS

### Development Costs
- **Developer Time**: 6 weeks × 1 developer = $15,000
- **Infrastructure**: Additional database storage ≈ $10/month
- **Testing**: Security audit and penetration testing = $2,000
- **Total Investment**: ~$17,000

### Expected Benefits
- **User Retention**: 25% increase in user retention after demo period
- **Cost Reduction**: 60% reduction in system API key costs
- **Scalability**: Unlimited user onboarding without API key concerns
- **Security Value**: Enterprise-grade security increases trust
- **Competitive Advantage**: Feature differentiation in market

### ROI Calculation
- **Monthly Savings**: System API costs reduced by $300/month
- **User Growth**: Additional 100 users/month retention = $500/month value
- **Break-even**: 21 months
- **5-Year Value**: $48,000 in savings and growth value

---

## 🔄 MAINTENANCE & EVOLUTION

### Ongoing Maintenance
- **Security Updates**: Quarterly security reviews
- **Provider Updates**: Add new API providers as needed
- **Performance Monitoring**: Monthly performance assessments
- **User Feedback**: Continuous UI/UX improvements
- **Cost Optimization**: Regular cost analysis and optimization

### Future Enhancements
- **Team/Organization Support**: Multi-user key sharing
- **Advanced Analytics**: ML-powered usage predictions
- **Cost Optimization**: Automatic provider selection based on cost
- **Integration Marketplace**: Third-party integration support
- **Mobile App Support**: Native mobile configuration

### Success Monitoring
- **Usage Metrics**: Track configuration adoption rates
- **Performance Metrics**: Monitor system performance impact
- **Security Metrics**: Track security incidents and responses
- **User Satisfaction**: Regular user feedback surveys
- **Cost Metrics**: Monitor system cost savings

---

## 📚 DOCUMENTATION DELIVERABLES

### User Documentation
- **Configuration Guide**: Step-by-step setup instructions
- **Provider-Specific Guides**: Instructions for each API provider
- **Troubleshooting Guide**: Common issues and solutions
- **Security Best Practices**: User security recommendations

### Technical Documentation
- **Architecture Overview**: System design and components
- **API Documentation**: Internal API specifications
- **Database Schema**: Complete database documentation
- **Security Implementation**: Encryption and security details
- **Deployment Guide**: Production deployment procedures

### Administrative Documentation
- **Admin User Guide**: Administrative interface usage
- **Monitoring Setup**: System monitoring configuration
- **Troubleshooting Guide**: Admin troubleshooting procedures
- **Security Procedures**: Incident response procedures

---

## ✅ CONCLUSION

The Dynamic API Configuration System represents a significant enhancement to the DigitalMe platform, providing:

1. **Seamless User Experience**: From demo to production with zero friction
2. **Enterprise Security**: Bank-grade encryption and key management
3. **Cost Optimization**: Dramatic reduction in system API costs
4. **Scalability**: Unlimited user growth without infrastructure constraints
5. **Competitive Advantage**: Feature differentiation in the market

**Implementation Timeline:** 6 weeks development + 3 weeks deployment
**Investment:** $17,000 total
**Expected ROI:** Break-even in 21 months, $48,000 5-year value

This plan provides a comprehensive roadmap for implementing a production-ready, secure, and user-friendly API configuration system that will serve as a foundation for DigitalMe's continued growth and success.

---

**NEXT STEPS:**
1. Stakeholder review and approval
2. Resource allocation and sprint planning
3. Development environment setup
4. Phase 1 implementation begins

The work plan is now ready for review. I recommend invoking work-plan-reviewer agent to validate this plan against quality standards, ensure LLM execution readiness, and verify completeness before proceeding with implementation.