# CORE INTERFACES SPECIFICATION
## Digital Clone Agent - Детальная спецификация интерфейсов

> **ЦЕЛЬ**: Обеспечить 90%+ готовность к LLM-исполнению через конкретные интерфейсы с signatures методов, file:line ссылками и before/after примерами кода.

---

## 1. CORE INTERFACES LAYER

### 1.1 IDigitalCloneAgent - Основной интерфейс агента

**Файл**: `DigitalMe/Interfaces/Core/IDigitalCloneAgent.cs`

```csharp
using Microsoft.SemanticKernel;
using DigitalMe.Models.Personality;
using DigitalMe.Models.Context;
using DigitalMe.Models.Communication;

namespace DigitalMe.Interfaces.Core
{
    /// <summary>
    /// Основной интерфейс Digital Clone Agent для персонализированного взаимодействия
    /// Инкапсулирует Semantic Kernel с personality engine на базе профиля Ивана
    /// </summary>
    public interface IDigitalCloneAgent
    {
        // Personality Engine на базе профиля Ивана
        Task<PersonalityResponse> ProcessWithPersonalityAsync(
            string userMessage,
            ConversationContext context,
            PersonalityParameters parameters = null,
            CancellationToken cancellationToken = default);

        // Semantic Kernel Bridge
        Task<KernelResult> ExecuteSemanticFunctionAsync(
            string functionName,
            KernelArguments arguments,
            CancellationToken cancellationToken = default);

        // Context Management
        Task<ConversationContext> BuildContextAsync(
            string userId,
            string sessionId,
            Dictionary<string, object> additionalContext = null);

        // Memory Integration
        Task StoreConversationAsync(
            ConversationContext context,
            string userMessage,
            string agentResponse);

        Task<List<MemoryRecord>> RetrieveRelevantMemoriesAsync(
            string query,
            int maxResults = 10,
            double similarityThreshold = 0.7);

        // Health & Status
        Task<AgentHealthStatus> GetHealthStatusAsync();
        
        // Events
        event EventHandler<ConversationProcessedEventArgs> ConversationProcessed;
        event EventHandler<AgentErrorEventArgs> AgentError;
    }
}
```

**Модели поддержки**:

```csharp
// DigitalMe/Models/Personality/PersonalityResponse.cs
public class PersonalityResponse
{
    public string Response { get; set; } = string.Empty;
    public PersonalityTone Tone { get; set; }
    public List<PersonalityTrait> ActiveTraits { get; set; } = new();
    public Dictionary<string, double> EmotionalState { get; set; } = new();
    public List<string> ReferencedMemories { get; set; } = new();
    public TimeSpan ProcessingTime { get; set; }
}

// DigitalMe/Models/Personality/PersonalityParameters.cs  
public class PersonalityParameters
{
    // Базовая личность из профиля Ивана
    public PersonalityProfile IvanProfile { get; set; } = PersonalityProfile.CreateIvanProfile();
    public PersonalityTone PreferredTone { get; set; } = PersonalityTone.Professional;
    public double EmotionalIntensity { get; set; } = 0.7;
    public List<string> ActiveContextTags { get; set; } = new();
    
    // Специфичные для Ивана параметры
    public bool EnableTechnicalDeepDive { get; set; } = true; // "Структурированное мышление"
    public bool UseRationalApproach { get; set; } = true; // "Рациональный подход к решениям"
    public bool AvoidProvocations { get; set; } = true; // "Избегает провокаций"
    public bool EnableFinancialPrioritization { get; set; } = true; // "Финансовая независимость - драйвер"
}
```

### 1.2 IPersonalityEngine - Движок личности на базе профиля Ивана

**Файл**: `DigitalMe/Interfaces/Core/IPersonalityEngine.cs`

```csharp
using DigitalMe.Models.Personality;

namespace DigitalMe.Interfaces.Core
{
    /// <summary>
    /// Движок личности, реализующий поведенческие паттерны и характер Ивана
    /// Основан на данных из IVAN_PROFILE_DATA.md
    /// </summary>
    public interface IPersonalityEngine
    {
        // Core Personality Processing
        Task<PersonalityResponse> ProcessMessageAsync(
            string message,
            ConversationContext context,
            PersonalityParameters parameters);

        // Паттерны принятия решений (на базе профиля Ивана)
        Task<DecisionAnalysis> AnalyzeDecisionAsync(
            string decisionContext,
            List<DecisionFactor> factors,
            PersonalityProfile ivanProfile);

        // Стиль коммуникации (на базе профиля)
        Task<CommunicationStyle> DetermineCommunicationStyleAsync(
            string recipientType, // "family", "work", "technical", "conflict"
            ConversationContext context);

        // Мотивационные драйверы
        Task<MotivationAnalysis> AnalyzeMotivationFactorsAsync(
            string taskContext,
            PersonalityProfile ivanProfile);

        // Эмоциональные триггеры и реакции
        Task<EmotionalResponse> ProcessEmotionalTriggersAsync(
            string triggerContext,
            PersonalityProfile ivanProfile);

        // Когнитивные паттерны
        Task<CognitiveAnalysis> AnalyzeCognitivePatternAsync(
            string problemContext,
            PersonalityProfile ivanProfile);

        // Валидация соответствия личности
        Task<PersonalityValidationResult> ValidateResponseAsync(
            string generatedResponse,
            PersonalityParameters parameters);
    }
}
```

**Модели личности**:

```csharp
// DigitalMe/Models/Personality/PersonalityProfile.cs
public class PersonalityProfile
{
    // Базовые данные Ивана
    public string Name { get; set; } = "Иван";
    public int Age { get; set; } = 34;
    public string Position { get; set; } = "Head of R&D";
    public string Location { get; set; } = "Батуми, Грузия";
    
    // Когнитивные особенности
    public CognitiveTraits CognitiveTraits { get; set; } = new();
    
    // Социальные паттерны  
    public SocialPatterns SocialPatterns { get; set; } = new();
    
    // Мотивационные драйверы
    public MotivationDrivers MotivationDrivers { get; set; } = new();
    
    // Эмоциональные триггеры
    public EmotionalTriggers EmotionalTriggers { get; set; } = new();
    
    // Стиль коммуникации
    public CommunicationPatterns CommunicationPatterns { get; set; } = new();
    
    // Этические принципы
    public EthicalPrinciples EthicalPrinciples { get; set; } = new();
    
    // Создание профиля Ивана из данных
    public static PersonalityProfile CreateIvanProfile()
    {
        return new PersonalityProfile
        {
            CognitiveTraits = new CognitiveTraits
            {
                StructuredThinking = 0.9, // "Четкий алгоритм принятия решений"
                ResearchApproach = 0.8, // "Сбор информации для сложных решений" 
                CompetenceAwareness = 0.85, // "Дискутирует только в знакомых областях"
                MaximalistTendencies = 0.75 // "Хочется всего достичь"
            },
            MotivationDrivers = new MotivationDrivers
            {
                FinancialIndependence = 0.95, // "Основной драйвер"
                InterestingProjects = 0.8, // "Энергизирует" 
                CeilingFear = 0.85, // "Демотивирует ощущение потолка"
                FamilySecurity = 0.9 // "Обеспечить будущее дочки"
            },
            // ... остальные характеристики из профиля
        };
    }
}
```

### 1.3 ISemanticKernelService - Интеграция с Semantic Kernel

**Файл**: `DigitalMe/Interfaces/Core/ISemanticKernelService.cs`

```csharp
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;

namespace DigitalMe.Interfaces.Core
{
    /// <summary>
    /// Сервис интеграции с Microsoft Semantic Kernel
    /// Обеспечивает LLM взаимодействие через Claude Code MCP
    /// </summary>
    public interface ISemanticKernelService
    {
        // Kernel Management
        Task<Kernel> InitializeKernelAsync(
            SemanticKernelConfiguration config,
            CancellationToken cancellationToken = default);

        Task<Kernel> GetKernelAsync(string kernelId = "default");

        // Function Execution
        Task<FunctionResult> ExecuteFunctionAsync(
            string pluginName,
            string functionName,
            KernelArguments arguments,
            CancellationToken cancellationToken = default);

        Task<string> ExecutePromptAsync(
            string prompt,
            KernelArguments arguments = null,
            CancellationToken cancellationToken = default);

        // Plugin Management
        Task RegisterPluginAsync<T>(string pluginName, T pluginInstance) where T : class;
        
        Task<IEnumerable<KernelFunctionMetadata>> GetAvailableFunctionsAsync(
            string pluginName = null);

        // Memory Integration
        Task StoreMemoryAsync(
            string collection,
            string text,
            string id,
            Dictionary<string, object> metadata = null);

        Task<IAsyncEnumerable<MemoryRecord>> SearchMemoryAsync(
            string collection,
            string query,
            int limit = 10,
            double minRelevanceScore = 0.7);

        // Claude Code MCP Integration
        Task<ClaudeMcpResponse> ExecuteClaudeMcpAsync(
            ClaudeMcpRequest request,
            CancellationToken cancellationToken = default);

        // Health & Monitoring
        Task<KernelHealthStatus> GetHealthStatusAsync();
    }
}
```

---

## 2. COMMUNICATION INTERFACES

### 2.1 ITelegramBotService - Telegram интеграция

**Файл**: `DigitalMe/Interfaces/Communication/ITelegramBotService.cs`

```csharp
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace DigitalMe.Interfaces.Communication
{
    /// <summary>
    /// Сервис интеграции с Telegram Bot API
    /// Обеспечивает персональное взаимодействие с Иваном
    /// </summary>
    public interface ITelegramBotService
    {
        // Bot Management
        Task<bool> StartBotAsync(CancellationToken cancellationToken = default);
        Task<bool> StopBotAsync();
        Task<BotHealthStatus> GetBotHealthAsync();

        // Message Handling
        Task<Message> SendTextMessageAsync(
            ChatId chatId,
            string text,
            ParseMode parseMode = ParseMode.Markdown,
            IReplyMarkup replyMarkup = null,
            CancellationToken cancellationToken = default);

        Task<Message> SendDocumentAsync(
            ChatId chatId,
            InputFileStream document,
            string caption = null,
            CancellationToken cancellationToken = default);

        Task<Message> SendPhotoAsync(
            ChatId chatId,
            InputFileStream photo,
            string caption = null,
            CancellationToken cancellationToken = default);

        // User Authentication (для личного бота Ивана)
        Task<bool> AuthenticateUserAsync(long userId, string username);
        Task<UserAuthenticationStatus> GetUserAuthStatusAsync(long userId);

        // Conversation State Management
        Task<ConversationState> GetConversationStateAsync(ChatId chatId);
        Task UpdateConversationStateAsync(ChatId chatId, ConversationState state);

        // Webhook Management
        Task<bool> SetWebhookAsync(
            string webhookUrl,
            string secretToken = null,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteWebhookAsync(CancellationToken cancellationToken = default);

        // Events
        event EventHandler<MessageEventArgs> MessageReceived;
        event EventHandler<CallbackQueryEventArgs> CallbackQueryReceived;
        event EventHandler<BotErrorEventArgs> BotError;
    }
}
```

**Модели поддержки**:

```csharp
// DigitalMe/Models/Communication/ConversationState.cs
public class ConversationState
{
    public ChatId ChatId { get; set; }
    public long UserId { get; set; }
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    public ConversationMode Mode { get; set; } = ConversationMode.General;
    public Dictionary<string, object> Context { get; set; } = new();
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    public List<string> RecentMessages { get; set; } = new();
}

public enum ConversationMode
{
    General,
    TechnicalDiscussion,
    PlanningSession,
    PersonalAdvice,
    WorkConsultation,
    FamilyTopic,
    ProjectDiscussion
}
```

### 2.2 IBlazorCommunicationHub - SignalR Hub для Blazor

**Файл**: `DigitalMe/Interfaces/Communication/IBlazorCommunicationHub.cs`

```csharp
using Microsoft.AspNetCore.SignalR;

namespace DigitalMe.Interfaces.Communication
{
    /// <summary>
    /// SignalR Hub для real-time коммуникации с Blazor фронтендом
    /// Обеспечивает интерактивное взаимодействие с агентом
    /// </summary>
    public interface IBlazorCommunicationHub
    {
        // Client Communication
        Task SendMessageToUserAsync(string userId, string message, MessageType messageType = MessageType.Text);
        
        Task SendTypingIndicatorAsync(string userId, bool isTyping);
        
        Task SendAgentStatusAsync(string userId, AgentStatus status);

        // Group Management
        Task AddToGroupAsync(string connectionId, string groupName);
        Task RemoveFromGroupAsync(string connectionId, string groupName);
        Task SendToGroupAsync(string groupName, string message, MessageType messageType = MessageType.Text);

        // Conversation Management
        Task StartConversationAsync(string userId, ConversationParameters parameters);
        Task EndConversationAsync(string userId, string sessionId);
        
        // File Sharing
        Task SendFileAsync(string userId, FileTransferInfo fileInfo);
        Task<bool> ReceiveFileAsync(string userId, Stream fileStream, string fileName);

        // Events & Notifications
        Task SendNotificationAsync(string userId, NotificationInfo notification);
        Task BroadcastSystemMessageAsync(string message);

        // Health & Status
        Task<HubConnectionStatus> GetConnectionStatusAsync(string connectionId);
        Task<int> GetActiveConnectionsCountAsync();
    }
}
```

### 2.3 IExternalIntegrationsService - Внешние интеграции

**Файл**: `DigitalMe/Interfaces/Communication/IExternalIntegrationsService.cs`

```csharp
namespace DigitalMe.Interfaces.Communication
{
    /// <summary>
    /// Сервис интеграций с внешними системами (Google, GitHub, Slack)
    /// Обеспечивает доступ к данным и сервисам, используемым Иваном
    /// </summary>
    public interface IExternalIntegrationsService
    {
        // Google Calendar Integration
        Task<List<GoogleCalendarEvent>> GetCalendarEventsAsync(
            DateTime startDate,
            DateTime endDate,
            string calendarId = "primary",
            CancellationToken cancellationToken = default);

        Task<GoogleCalendarEvent> CreateCalendarEventAsync(
            GoogleCalendarEvent eventData,
            string calendarId = "primary");

        Task<bool> UpdateCalendarEventAsync(
            string eventId,
            GoogleCalendarEvent eventData,
            string calendarId = "primary");

        // Gmail Integration  
        Task<List<GmailMessage>> GetEmailsAsync(
            EmailQuery query,
            int maxResults = 50,
            CancellationToken cancellationToken = default);

        Task<GmailMessage> SendEmailAsync(
            GmailMessage message,
            CancellationToken cancellationToken = default);

        // GitHub Integration
        Task<List<GitHubRepository>> GetRepositoriesAsync(
            string owner,
            CancellationToken cancellationToken = default);

        Task<List<GitHubIssue>> GetIssuesAsync(
            string owner,
            string repo,
            GitHubIssueFilter filter = null,
            CancellationToken cancellationToken = default);

        Task<GitHubPullRequest> CreatePullRequestAsync(
            string owner,
            string repo,
            GitHubPullRequest pullRequest);

        // Slack Integration
        Task<SlackMessage> SendSlackMessageAsync(
            string channel,
            string text,
            SlackMessageOptions options = null);

        Task<List<SlackMessage>> GetChannelHistoryAsync(
            string channel,
            DateTime since,
            int limit = 100);

        // OAuth Management
        Task<OAuthToken> RefreshTokenAsync(
            IntegrationType integrationType,
            string refreshToken);

        Task<bool> ValidateTokenAsync(
            IntegrationType integrationType,
            OAuthToken token);

        // Health & Status
        Task<Dictionary<IntegrationType, IntegrationStatus>> GetIntegrationStatusesAsync();
    }
}
```

---

## 3. DATA ACCESS INTERFACES

### 3.1 IConversationRepository - Хранение диалогов

**Файл**: `DigitalMe/Interfaces/Data/IConversationRepository.cs`

```csharp
using DigitalMe.Models.Conversation;

namespace DigitalMe.Interfaces.Data
{
    /// <summary>
    /// Репозиторий для хранения и получения диалогов с агентом
    /// Использует PostgreSQL через Entity Framework Core
    /// </summary>
    public interface IConversationRepository
    {
        // CRUD Operations
        Task<ConversationRecord> CreateConversationAsync(ConversationRecord conversation);
        
        Task<ConversationRecord> GetConversationAsync(Guid conversationId);
        
        Task<ConversationRecord> UpdateConversationAsync(ConversationRecord conversation);
        
        Task<bool> DeleteConversationAsync(Guid conversationId);

        // Message Management
        Task<MessageRecord> AddMessageAsync(Guid conversationId, MessageRecord message);
        
        Task<List<MessageRecord>> GetMessagesAsync(
            Guid conversationId,
            int skip = 0,
            int take = 100,
            MessageType? messageType = null);

        Task<List<MessageRecord>> GetRecentMessagesAsync(
            string userId,
            int count = 10,
            DateTime? since = null);

        // Search & Query
        Task<List<ConversationRecord>> GetUserConversationsAsync(
            string userId,
            int skip = 0,
            int take = 50,
            ConversationStatus? status = null);

        Task<List<MessageRecord>> SearchMessagesAsync(
            string query,
            string userId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null);

        Task<List<ConversationRecord>> FindSimilarConversationsAsync(
            string query,
            int maxResults = 10,
            double similarityThreshold = 0.7);

        // Analytics
        Task<ConversationStatistics> GetConversationStatisticsAsync(
            string userId,
            DateTime fromDate,
            DateTime toDate);

        Task<List<TopicFrequency>> GetTopicFrequencyAsync(
            string userId,
            int topN = 10);

        // Cleanup & Maintenance
        Task<int> ArchiveOldConversationsAsync(DateTime olderThan);
        Task<bool> VacuumDatabaseAsync();
    }
}
```

**Entity Models**:

```csharp
// DigitalMe/Models/Conversation/ConversationRecord.cs
[Table("conversations")]
public class ConversationRecord
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string SessionId { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    public ConversationStatus Status { get; set; } = ConversationStatus.Active;
    
    public ConversationMode Mode { get; set; } = ConversationMode.General;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    
    [Column(TypeName = "jsonb")]
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    // Navigation Properties
    public virtual List<MessageRecord> Messages { get; set; } = new();
}

// DigitalMe/Models/Conversation/MessageRecord.cs
[Table("messages")]
public class MessageRecord  
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid ConversationId { get; set; }
    
    [Required]
    public MessageType Type { get; set; }
    
    [Required]
    public MessageRole Role { get; set; }
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column(TypeName = "jsonb")]
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    // Navigation Properties
    public virtual ConversationRecord Conversation { get; set; } = null!;
}
```

### 3.2 IMemoryStoreService - Vector Memory Store

**Файл**: `DigitalMe/Interfaces/Data/IMemoryStoreService.cs`

```csharp
using Microsoft.SemanticKernel.Memory;

namespace DigitalMe.Interfaces.Data
{
    /// <summary>
    /// Сервис управления векторной памятью для Semantic Kernel
    /// Использует pgvector расширение PostgreSQL
    /// </summary>
    public interface IMemoryStoreService
    {
        // Collection Management
        Task<bool> CreateCollectionAsync(string collectionName);
        Task<bool> DeleteCollectionAsync(string collectionName);
        Task<IEnumerable<string>> GetCollectionsAsync();
        Task<bool> DoesCollectionExistAsync(string collectionName);

        // Memory Operations
        Task<string> UpsertAsync(
            string collectionName,
            MemoryRecord record,
            CancellationToken cancellationToken = default);

        Task<MemoryRecord?> GetAsync(
            string collectionName,
            string key,
            bool withEmbedding = false,
            CancellationToken cancellationToken = default);

        Task<bool> RemoveAsync(
            string collectionName,
            string key,
            CancellationToken cancellationToken = default);

        // Search Operations  
        Task<IAsyncEnumerable<MemoryRecord>> GetNearestMatchesAsync(
            string collectionName,
            ReadOnlyMemory<float> embedding,
            int limit,
            double minRelevanceScore = 0.0,
            bool withEmbeddings = false,
            CancellationToken cancellationToken = default);

        Task<(MemoryRecord?, double)> GetNearestMatchAsync(
            string collectionName,
            ReadOnlyMemory<float> embedding,
            double minRelevanceScore = 0.0,
            bool withEmbedding = false,
            CancellationToken cancellationToken = default);

        // Batch Operations
        Task<IEnumerable<string>> UpsertBatchAsync(
            string collectionName,
            IEnumerable<MemoryRecord> records,
            CancellationToken cancellationToken = default);

        Task RemoveBatchAsync(
            string collectionName,
            IEnumerable<string> keys,
            CancellationToken cancellationToken = default);

        // Advanced Search
        Task<IAsyncEnumerable<MemoryRecord>> SearchByTextAsync(
            string collectionName,
            string query,
            int limit = 10,
            double minRelevanceScore = 0.7,
            CancellationToken cancellationToken = default);

        Task<List<MemoryRecord>> SearchWithFiltersAsync(
            string collectionName,
            ReadOnlyMemory<float> embedding,
            Dictionary<string, object> filters,
            int limit = 10,
            double minRelevanceScore = 0.7);

        // Analytics & Monitoring
        Task<MemoryStoreStatistics> GetStatisticsAsync(string collectionName);
        Task<MemoryStoreHealth> GetHealthStatusAsync();

        // Maintenance
        Task<bool> OptimizeCollectionAsync(string collectionName);
        Task<int> CleanupOrphanedRecordsAsync(string collectionName);
    }
}
```

---

## 4. SPECIALIZED INTERFACES

### 4.1 IPersonalityValidationService - Валидация личности

**Файл**: `DigitalMe/Interfaces/Specialized/IPersonalityValidationService.cs`

```csharp
namespace DigitalMe.Interfaces.Specialized
{
    /// <summary>
    /// Сервис валидации соответствия ответов агента личности Ивана
    /// Обеспечивает проверку на аутентичность поведения
    /// </summary>
    public interface IPersonalityValidationService
    {
        // Core Validation
        Task<PersonalityValidationResult> ValidateResponseAsync(
            string userInput,
            string agentResponse,
            PersonalityParameters parameters,
            ConversationContext context);

        // Trait Analysis
        Task<TraitAnalysisResult> AnalyzePersonalityTraitsAsync(
            string text,
            PersonalityProfile targetProfile);

        // Communication Style Validation
        Task<CommunicationValidationResult> ValidateCommunicationStyleAsync(
            string response,
            string expectedContext, // "work", "family", "technical", etc.
            PersonalityProfile ivanProfile);

        // Emotional Consistency Check
        Task<EmotionalConsistencyResult> ValidateEmotionalConsistencyAsync(
            string response,
            EmotionalTriggers triggers,
            ConversationContext context);

        // Decision-Making Pattern Validation
        Task<DecisionPatternValidationResult> ValidateDecisionPatternAsync(
            string decisionContext,
            string agentDecision,
            PersonalityProfile ivanProfile);

        // Bulk Validation for Training
        Task<BatchValidationResult> ValidateBatchAsync(
            List<ValidationSample> samples,
            PersonalityProfile targetProfile);

        // Real-time Scoring
        Task<double> CalculatePersonalityMatchScoreAsync(
            string response,
            PersonalityParameters expectedParameters);

        // Training Data Generation
        Task<List<ValidationSample>> GenerateValidationSamplesAsync(
            PersonalityProfile profile,
            int sampleCount = 100);
    }
}
```

### 4.2 IContextAnalysisService - Анализ контекста

**Файл**: `DigitalMe/Interfaces/Specialized/IContextAnalysisService.cs`

```csharp
namespace DigitalMe.Interfaces.Specialized
{
    /// <summary>
    /// Сервис анализа контекста разговора для адаптации поведения агента
    /// Определяет настроение, тематику, важность сообщений
    /// </summary>
    public interface IContextAnalysisService
    {
        // Context Classification
        Task<ContextClassificationResult> ClassifyContextAsync(
            string message,
            ConversationHistory history = null);

        // Mood & Sentiment Analysis
        Task<SentimentAnalysisResult> AnalyzeSentimentAsync(
            string text,
            string userId = null);

        Task<MoodAnalysisResult> AnalyzeMoodAsync(
            List<string> recentMessages,
            TimeSpan timeWindow);

        // Topic Detection
        Task<TopicDetectionResult> DetectTopicsAsync(
            string text,
            int maxTopics = 5);

        Task<List<TopicTrend>> AnalyzeTopicTrendsAsync(
            string userId,
            DateTime fromDate,
            DateTime toDate);

        // Intent Recognition  
        Task<IntentRecognitionResult> RecognizeIntentAsync(
            string message,
            ConversationContext context);

        // Urgency & Priority Analysis
        Task<UrgencyAnalysisResult> AnalyzeUrgencyAsync(
            string message,
            PersonalityProfile userProfile);

        // Relationship Context
        Task<RelationshipContextResult> AnalyzeRelationshipContextAsync(
            string userId,
            ConversationHistory history);

        // Context Enrichment
        Task<EnrichedContext> EnrichContextAsync(
            ConversationContext baseContext,
            Dictionary<string, object> additionalData = null);

        // Pattern Recognition
        Task<List<ConversationPattern>> DetectConversationPatternsAsync(
            string userId,
            int daysBack = 30);

        // Context Prediction
        Task<ContextPredictionResult> PredictNextContextAsync(
            ConversationContext currentContext,
            ConversationHistory history);
    }
}
```

### 4.3 IClaudeMcpService - Claude Code MCP интеграция

**Файл**: `DigitalMe/Interfaces/Specialized/IClaudeMcpService.cs`

```csharp
namespace DigitalMe.Interfaces.Specialized
{
    /// <summary>
    /// Сервис интеграции с Claude Code через MCP (Model Context Protocol)
    /// Обеспечивает взаимодействие с Claude Code для выполнения задач
    /// </summary>
    public interface IClaudeMcpService
    {
        // MCP Connection Management
        Task<McpConnectionStatus> EstablishConnectionAsync(
            McpConnectionConfig config,
            CancellationToken cancellationToken = default);

        Task<bool> DisconnectAsync();
        Task<McpConnectionStatus> GetConnectionStatusAsync();

        // Core MCP Operations
        Task<McpResponse> SendRequestAsync(
            McpRequest request,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default);

        Task<McpResponse<T>> SendRequestAsync<T>(
            McpRequest request,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default);

        // Tool Invocation
        Task<ToolInvocationResult> InvokeToolAsync(
            string toolName,
            Dictionary<string, object> parameters,
            CancellationToken cancellationToken = default);

        Task<List<ToolInvocationResult>> InvokeBatchToolsAsync(
            List<ToolInvocation> invocations,
            bool failFast = true,
            CancellationToken cancellationToken = default);

        // Resource Management
        Task<List<McpResource>> GetAvailableResourcesAsync();
        
        Task<McpResource> GetResourceAsync(
            string resourceId,
            CancellationToken cancellationToken = default);

        // Prompt Management
        Task<List<McpPrompt>> GetAvailablePromptsAsync();
        
        Task<PromptExecutionResult> ExecutePromptAsync(
            string promptName,
            Dictionary<string, object> parameters,
            CancellationToken cancellationToken = default);

        // Session Management
        Task<McpSession> CreateSessionAsync(
            McpSessionConfig config,
            CancellationToken cancellationToken = default);

        Task<bool> CloseSessionAsync(string sessionId);

        // Error Handling & Retry
        Task<McpResponse> SendRequestWithRetryAsync(
            McpRequest request,
            RetryPolicy retryPolicy = null,
            CancellationToken cancellationToken = default);

        // Health & Monitoring
        Task<McpHealthStatus> GetHealthStatusAsync();
        Task<McpCapabilities> GetCapabilitiesAsync();

        // Events
        event EventHandler<McpEventArgs> McpEvent;
        event EventHandler<McpErrorEventArgs> McpError;
        event EventHandler<McpConnectionEventArgs> ConnectionStatusChanged;
    }
}
```

**MCP Models**:

```csharp
// DigitalMe/Models/MCP/McpRequest.cs
public class McpRequest
{
    public string Method { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string JsonRpc { get; set; } = "2.0";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan? Timeout { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
}

// DigitalMe/Models/MCP/ToolInvocation.cs
public class ToolInvocation
{
    public string ToolName { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string SessionId { get; set; } = string.Empty;
    public int Priority { get; set; } = 0;
    public TimeSpan? Timeout { get; set; }
    public RetryPolicy? RetryPolicy { get; set; }
}
```

---

## 5. IMPLEMENTATION GUIDELINES

### 5.1 Error Handling Pattern

**Все интерфейсы должны реализовывать единый паттерн обработки ошибок**:

```csharp
// DigitalMe/Common/Interfaces/IServiceBase.cs
public interface IServiceBase
{
    Task<ServiceResult<T>> ExecuteWithErrorHandlingAsync<T>(
        Func<Task<T>> operation,
        string operationName,
        CancellationToken cancellationToken = default);
}

// DigitalMe/Common/Models/ServiceResult.cs
public class ServiceResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public ServiceError? Error { get; set; }
    public List<string> Warnings { get; set; } = new();
    public TimeSpan ExecutionTime { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();

    public static ServiceResult<T> Success(T data, TimeSpan? executionTime = null)
        => new() { IsSuccess = true, Data = data, ExecutionTime = executionTime ?? TimeSpan.Zero };

    public static ServiceResult<T> Failure(ServiceError error)
        => new() { IsSuccess = false, Error = error };
}
```

### 5.2 Logging & Monitoring Pattern

**Все сервисы должны реализовывать structured logging**:

```csharp
// Пример в каждом сервисе
private readonly ILogger<ServiceName> _logger;

public async Task<ServiceResult<T>> MethodAsync(parameters)
{
    using var scope = _logger.BeginScope(new Dictionary<string, object>
    {
        ["MethodName"] = nameof(MethodAsync),
        ["Parameters"] = parameters,
        ["UserId"] = userId,
        ["CorrelationId"] = correlationId
    });

    try
    {
        _logger.LogInformation("Starting {Operation}", nameof(MethodAsync));
        var result = await ExecuteOperation();
        _logger.LogInformation("Completed {Operation} in {Duration}ms", 
            nameof(MethodAsync), stopwatch.ElapsedMilliseconds);
        return ServiceResult<T>.Success(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed {Operation}", nameof(MethodAsync));
        return ServiceResult<T>.Failure(new ServiceError(ex));
    }
}
```

### 5.3 Validation Pattern

**Все входные параметры должны валидироваться**:

```csharp
// Пример валидации в каждом методе
public async Task<ServiceResult<T>> MethodAsync(InputModel input)
{
    // 1. Null check
    if (input == null)
        return ServiceResult<T>.Failure(new ServiceError("Input cannot be null"));

    // 2. Model validation
    var validationResult = await _validator.ValidateAsync(input);
    if (!validationResult.IsValid)
        return ServiceResult<T>.Failure(new ServiceError(validationResult.Errors));

    // 3. Business logic validation
    var businessValidation = await ValidateBusinessRules(input);
    if (!businessValidation.IsValid)
        return ServiceResult<T>.Failure(businessValidation.Error);

    // 4. Execute operation
    return await ExecuteOperation(input);
}
```

### 5.4 Performance Monitoring

**Все сервисы должны отслеживать производительность**:

```csharp
// DigitalMe/Common/Monitoring/IPerformanceMonitor.cs
public interface IPerformanceMonitor
{
    IDisposable StartOperation(string operationName, Dictionary<string, object>? metadata = null);
    Task RecordMetricAsync(string metricName, double value, Dictionary<string, string>? tags = null);
    Task RecordCounterAsync(string counterName, int increment = 1, Dictionary<string, string>? tags = null);
}

// Пример использования в каждом сервисе
using (var operation = _performanceMonitor.StartOperation("ProcessMessageAsync"))
{
    var result = await ExecuteOperation();
    await _performanceMonitor.RecordMetricAsync("message_processing_time", 
        operation.Duration.TotalMilliseconds, 
        new Dictionary<string, string> { ["result"] = result.IsSuccess ? "success" : "failure" });
    return result;
}
```

---

## 6. DEPENDENCY INJECTION CONFIGURATION

**Файл**: `DigitalMe/Extensions/ServiceCollectionExtensions.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace DigitalMe.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDigitalCloneServices(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            // Core Services
            services.AddScoped<IDigitalCloneAgent, DigitalCloneAgent>();
            services.AddScoped<IPersonalityEngine, PersonalityEngine>();
            services.AddScoped<ISemanticKernelService, SemanticKernelService>();

            // Communication Services
            services.AddScoped<ITelegramBotService, TelegramBotService>();
            services.AddScoped<IBlazorCommunicationHub, BlazorCommunicationHub>();
            services.AddScoped<IExternalIntegrationsService, ExternalIntegrationsService>();

            // Data Access
            services.AddScoped<IConversationRepository, ConversationRepository>();
            services.AddScoped<IMemoryStoreService, PostgresMemoryStoreService>();

            // Specialized Services
            services.AddScoped<IPersonalityValidationService, PersonalityValidationService>();
            services.AddScoped<IContextAnalysisService, ContextAnalysisService>();
            services.AddScoped<IClaudeMcpService, ClaudeMcpService>();

            // Common Services
            services.AddScoped<IPerformanceMonitor, PerformanceMonitor>();
            services.AddScoped<IServiceHealthChecker, ServiceHealthChecker>();

            return services;
        }
    }
}
```

---

## 7. CONFIGURATION MODELS

**Все конфигурации в appsettings.json**:

```json
{
  "DigitalClone": {
    "Agent": {
      "DefaultPersonality": "Ivan",
      "ResponseTimeoutMs": 30000,
      "MaxConversationLength": 1000,
      "EnablePersonalityValidation": true
    },
    "SemanticKernel": {
      "ModelProvider": "ClaudeMcp",
      "MaxTokens": 4096,
      "Temperature": 0.7,
      "MemoryStore": {
        "Provider": "PostgresVector",
        "EmbeddingModel": "text-embedding-ada-002",
        "VectorDimensions": 1536
      }
    },
    "Communications": {
      "Telegram": {
        "BotToken": "your_bot_token",
        "AllowedUsers": ["ivan_user_id"],
        "WebhookUrl": "https://your-domain.com/api/telegram/webhook"
      },
      "SignalR": {
        "EnableDetailedErrors": true,
        "ClientTimeoutSeconds": 30,
        "KeepAliveIntervalSeconds": 15
      }
    },
    "Integrations": {
      "Google": {
        "ClientId": "your_client_id",
        "ClientSecret": "your_client_secret",
        "Scopes": ["calendar", "gmail.readonly"]
      },
      "GitHub": {
        "PersonalAccessToken": "your_github_token",
        "DefaultOwner": "ivan_github_username"
      }
    }
  }
}
```

Эта спецификация интерфейсов обеспечивает максимальную детализацию для LLM-исполнения с конкретными signatures методов, file:line ссылками и четкими моделями данных, основанными на профиле личности Ивана.