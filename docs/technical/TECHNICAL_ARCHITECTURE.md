# Техническая архитектура персонального агента Ивана

## Обзор системы

Персональный агент Ивана — это интеллектуальная система на базе C#/.NET, предоставляющая комплексную автоматизацию личных и профессиональных задач через интеграцию с различными сервисами и LLM провайдерами.

## 1. Технологический стек

### 1.1 Основной фреймворк
- **.NET 8.0+** - Основная платформа разработки
- **ASP.NET Core 8.0** - Web API и хостинг
- **Entity Framework Core 8.0** - ORM для работы с БД
- **C# 12** - Язык программации

### 1.2 Агентские фреймворки
**Рекомендуемый выбор: Microsoft Semantic Kernel**
- **Microsoft Semantic Kernel** - Production-ready агентский фреймворк для enterprise приложений
- **AutoGen 0.4** - Для экспериментальных multi-agent сценариев
- **Интеграция**: Плавный переход AutoGen → Semantic Kernel (Early 2025)

**Ключевые возможности:**
- Enterprise-grade language support (C#, Python, Java)
- Process Framework для stateful, long-running processes
- Azure AI Foundry Agents Service integration
- Dapr и Microsoft Orleans support для масштабирования

### 1.3 Хранение данных
**База данных:**
- **PostgreSQL 15+** - Основная реляционная БД
- **Entity Framework Core Code-First** - Миграции и схема БД
- **Npgsql** - PostgreSQL provider для .NET

**Хостинг БД:**
- **Основной: Supabase** - PostgreSQL as a Service с дополнительными возможностями
- **Альтернатива: Azure Database for PostgreSQL** - Managed PostgreSQL в Azure

### 1.4 Интеграции и SDK

#### LLM Провайдеры
1. **OpenAI Integration:**
   - `OpenAI-DotNet` - Официальная библиотека OpenAI
   - `Microsoft.Extensions.AI` - Унифицированный интерфейс для LLM

2. **Anthropic Claude Integration:**
   - `Anthropic.SDK` by tghamm - Полнофункциональный unofficial SDK
   - `Claudia` by Cysharp - Производительная альтернатива
   - **Будущее:** Официальный Microsoft SDK для Anthropic (в разработке)

#### Внешние сервисы
1. **Telegram Integration:**
   - `Telegram.Bot` v22.6.0 - Основная библиотека для bot API
   - `WTelegramClient` - Для advanced сценариев (MTProto)

2. **Google Services:**
   - `Google.Apis.Calendar.v3` - Google Calendar API
   - `Google.Apis.Drive.v3` - Google Drive integration
   - `Google.Apis.Docs.v1` - Google Docs API
   - `Google.Apis.Sheets.v4` - Google Sheets API

3. **GitHub Integration:**
   - `Octokit.net` - GitHub REST API client
   - `Octokit.GraphQL.net` - GitHub GraphQL client

4. **Model Context Protocol (MCP):**
   - `ModelContextProtocol` - Официальный Microsoft SDK
   - Поддержка спецификации 2025-06-18
   - Elicitation support, structured tool output

### 1.5 Хостинг приложения

**Рекомендуемый вариант: Azure Container Apps**
- **Преимущества:**
  - Scale-to-zero capability (экономия 60-80% при переменной нагрузке)
  - Serverless архитектура
  - Per-second billing
  - Поддержка .NET containers
  - Azure Savings Plans (до 17% экономии)

**Альтернатива: Azure App Service**
- Для приложений с предсказуемой нагрузкой
- Проще в настройке и управлении
- Встроенная поддержка .NET

## 2. Архитектура системы

### 2.1 Высокоуровневая архитектура

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   User Clients  │    │   Agent Core     │    │   Integrations  │
│                 │    │                  │    │                 │
│ • Telegram Bot  │◄──►│ • Semantic       │◄──►│ • Google APIs   │
│ • Web Interface │    │   Kernel         │    │ • GitHub API    │
│ • Claude Code   │    │ • MCP Server     │    │ • LLM Providers │
│ • API Clients   │    │ • Task Engine    │    │ • Contact APIs  │
└─────────────────┘    └──────────────────┘    └─────────────────┘
         │                       │                       │
         │              ┌────────▼────────┐              │
         │              │   Data Layer    │              │
         │              │                 │              │
         └──────────────►│ • PostgreSQL   │◄─────────────┘
                        │ • EF Core       │
                        │ • Supabase      │
                        └─────────────────┘
```

### 2.2 Модульная архитектура

#### Core Modules
1. **PersonalAgent.Core**
   - Агентский движок (Semantic Kernel)
   - Orchestration и workflow management
   - Context management и memory

2. **PersonalAgent.Data**
   - Entity Framework Core models
   - Repository pattern
   - Data access abstractions

3. **PersonalAgent.Api**
   - ASP.NET Core Web API
   - Authentication & Authorization
   - Rate limiting и caching

#### Integration Modules
4. **PersonalAgent.Integrations.Telegram**
   - Telegram Bot implementation
   - Message processing и routing

5. **PersonalAgent.Integrations.Google**
   - Google Calendar, Drive, Docs, Sheets
   - OAuth2 flow management

6. **PersonalAgent.Integrations.GitHub**
   - Repository management
   - Issue tracking integration

7. **PersonalAgent.Integrations.LLM**
   - Multi-provider LLM abstraction
   - Model routing и failover

#### MCP Support
8. **PersonalAgent.MCP**
   - MCP server implementation
   - Tool и resource exposure
   - Claude Code integration

### 2.3 База данных - схема

```sql
-- Users и Authentication
CREATE TABLE Users (
    Id UUID PRIMARY KEY,
    TelegramUserId BIGINT UNIQUE,
    Username VARCHAR(100),
    Email VARCHAR(255),
    CreatedAt TIMESTAMP,
    UpdatedAt TIMESTAMP
);

-- Контакты
CREATE TABLE Contacts (
    Id UUID PRIMARY KEY,
    UserId UUID REFERENCES Users(Id),
    Name VARCHAR(200),
    Phone VARCHAR(50),
    Email VARCHAR(255),
    TelegramHandle VARCHAR(100),
    SlackUserId VARCHAR(100),
    Source VARCHAR(50), -- 'phone', 'telegram', 'slack', 'manual'
    CreatedAt TIMESTAMP
);

-- Задачи и События
CREATE TABLE Tasks (
    Id UUID PRIMARY KEY,
    UserId UUID REFERENCES Users(Id),
    Title VARCHAR(500),
    Description TEXT,
    Status VARCHAR(50), -- 'pending', 'in_progress', 'completed', 'cancelled'
    Priority VARCHAR(20), -- 'low', 'medium', 'high', 'urgent'
    DueDate TIMESTAMP,
    GoogleCalendarEventId VARCHAR(255),
    CreatedAt TIMESTAMP,
    UpdatedAt TIMESTAMP
);

-- Интеграции
CREATE TABLE UserIntegrations (
    Id UUID PRIMARY KEY,
    UserId UUID REFERENCES Users(Id),
    ServiceType VARCHAR(50), -- 'google', 'github', 'telegram', 'slack'
    AccessToken TEXT, -- Encrypted
    RefreshToken TEXT, -- Encrypted
    ExpiresAt TIMESTAMP,
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP
);

-- Агентские сессии
CREATE TABLE AgentSessions (
    Id UUID PRIMARY KEY,
    UserId UUID REFERENCES Users(Id),
    SessionType VARCHAR(50), -- 'telegram', 'api', 'mcp'
    ContextData JSONB,
    Status VARCHAR(50),
    StartedAt TIMESTAMP,
    EndedAt TIMESTAMP
);

-- MCP Tools и Resources
CREATE TABLE MCPTools (
    Id UUID PRIMARY KEY,
    Name VARCHAR(100),
    Description TEXT,
    Schema JSONB,
    IsEnabled BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP
);
```

## 3. Ключевые компоненты

### 3.1 Agent Core (Semantic Kernel)

```csharp
public class PersonalAgentKernel
{
    private readonly Kernel _kernel;
    private readonly IServiceProvider _serviceProvider;
    
    public PersonalAgentKernel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        
        var builder = Kernel.CreateBuilder()
            .AddOpenAIChatCompletion("gpt-4o", Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
            .AddAnthropicChatCompletion("claude-3-sonnet", Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"));
            
        // Добавляем plugins для интеграций
        builder.Plugins.AddFromType<CalendarPlugin>();
        builder.Plugins.AddFromType<ContactsPlugin>();
        builder.Plugins.AddFromType<GitHubPlugin>();
        builder.Plugins.AddFromType<TelegramPlugin>();
        
        _kernel = builder.Build();
    }
    
    public async Task<string> ProcessRequestAsync(string userRequest, AgentContext context)
    {
        var prompt = $"""
            Ты персональный агент Ивана. Твоя задача: {userRequest}
            
            Контекст:
            - Текущее время: {DateTime.Now}
            - Пользователь: {context.User.Username}
            - Доступные инструменты: {string.Join(", ", GetAvailableTools(context))}
            
            Проанализируй запрос и выполни необходимые действия.
            """;
            
        var result = await _kernel.InvokePromptAsync(prompt);
        return result.ToString();
    }
}
```

### 3.2 MCP Server Integration

```csharp
public class PersonalAgentMCPServer : IMCPServer
{
    private readonly IServiceProvider _services;
    
    public PersonalAgentMCPServer(IServiceProvider services)
    {
        _services = services;
    }
    
    [MCPTool("get_calendar_events")]
    public async Task<CalendarEvent[]> GetCalendarEventsAsync(
        DateTime startDate,
        DateTime endDate,
        [Description("Optional calendar ID")] string? calendarId = null)
    {
        var calendarService = _services.GetRequiredService<ICalendarService>();
        return await calendarService.GetEventsAsync(startDate, endDate, calendarId);
    }
    
    [MCPTool("send_telegram_message")]
    public async Task<bool> SendTelegramMessageAsync(
        [Description("Telegram user ID or chat ID")] long chatId,
        [Description("Message text to send")] string message)
    {
        var telegramService = _services.GetRequiredService<ITelegramService>();
        return await telegramService.SendMessageAsync(chatId, message);
    }
    
    [MCPTool("create_github_issue")]
    public async Task<GitHubIssue> CreateGitHubIssueAsync(
        [Description("Repository owner/name")] string repository,
        [Description("Issue title")] string title,
        [Description("Issue description")] string body)
    {
        var githubService = _services.GetRequiredService<IGitHubService>();
        return await githubService.CreateIssueAsync(repository, title, body);
    }
}
```

### 3.3 Telegram Bot Integration

```csharp
public class PersonalAgentTelegramBot
{
    private readonly ITelegramBotClient _botClient;
    private readonly PersonalAgentKernel _agentKernel;
    private readonly IUserService _userService;
    
    public PersonalAgentTelegramBot(
        ITelegramBotClient botClient,
        PersonalAgentKernel agentKernel,
        IUserService userService)
    {
        _botClient = botClient;
        _agentKernel = agentKernel;
        _userService = userService;
    }
    
    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        if (update.Message is { } message && message.Text is { } messageText)
        {
            var user = await _userService.GetOrCreateUserAsync(message.From.Id);
            var context = new AgentContext(user, message.Chat.Id);
            
            var response = await _agentKernel.ProcessRequestAsync(messageText, context);
            
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: response,
                cancellationToken: cancellationToken);
        }
    }
}
```

## 4. MVP Компоненты и план поэтапной реализации

### 4.1 Фаза 1: Core MVP (4-6 недель)

**Компоненты MVP:**
1. **Базовая архитектура**
   - ASP.NET Core API
   - PostgreSQL + Entity Framework Core
   - Базовая аутентификация

2. **Semantic Kernel Integration**
   - Базовая настройка с OpenAI GPT-4o
   - Простые plugins для календаря и контактов

3. **Telegram Bot**
   - Базовый бот для текстовых сообщений
   - Простые команды (/start, /help, /calendar)

4. **Google Calendar Integration**
   - Основные операции: чтение, создание, изменение событий
   - OAuth2 авторизация

**Временная оценка:** 4-6 недель (1 разработчик)

### 4.2 Фаза 2: Enhanced Features (3-4 недели)

**Дополнительные возможности:**
1. **Anthropic Claude Integration**
   - Добавление Claude как альтернативного LLM
   - A/B тестирование моделей

2. **GitHub Integration**
   - Управление репозиториями
   - Создание и отслеживание issues

3. **Contact Management**
   - Синхронизация с телефонными контактами
   - Управление через Telegram

4. **Enhanced Telegram Features**
   - Inline keyboards
   - File handling
   - Voice message transcription

**Временная оценка:** 3-4 недели

### 4.3 Фаза 3: Advanced Integration (4-5 недель)

**Продвинутые функции:**
1. **MCP Server Implementation**
   - Полноценный MCP server
   - Claude Code integration

2. **Google Workspace Full Integration**
   - Google Drive, Docs, Sheets
   - Document processing и создание

3. **Multi-Agent Architecture**
   - AutoGen integration для сложных workflows
   - Специализированные агенты

4. **Slack Integration**
   - Bot для Slack workspace
   - Cross-platform messaging

**Временная оценка:** 4-5 недели

### 4.4 Фаза 4: Production & Scale (2-3 недели)

**Production-ready features:**
1. **Hosting & Deployment**
   - Azure Container Apps deployment
   - Supabase production setup
   - CI/CD pipeline

2. **Monitoring & Observability**
   - Application Insights
   - Logging и metrics
   - Error tracking

3. **Security & Compliance**
   - Encryption at rest
   - API rate limiting
   - GDPR compliance

**Временная оценка:** 2-3 недели

## 5. Оценка сложности и ресурсов

### 5.1 Техническая сложность

**Уровень сложности: Средне-высокий**

**Ключевые технические вызовы:**
1. **Интеграция множественных API** (Google, GitHub, Telegram, LLM)
2. **Управление состоянием агента** и контекстом между сессиями
3. **Обработка real-time событий** от различных источников
4. **Безопасность** при работе с персональными данными
5. **Масштабирование** MCP server для Claude Code integration

### 5.2 Временные затраты

**Общая оценка: 13-18 недель (1 разработчик)**

| Фаза | Длительность | Описание |
|------|-------------|----------|
| Фаза 1 | 4-6 недель | Core MVP |
| Фаза 2 | 3-4 недели | Enhanced Features |
| Фаза 3 | 4-5 недель | Advanced Integration |
| Фаза 4 | 2-3 недели | Production Ready |
| **Итого** | **13-18 недель** | **Full System** |

### 5.3 Команда разработки

**Рекомендуемый состав:**
1. **Senior .NET Developer** (Fulltime) - архитектура, core development
2. **DevOps Engineer** (Part-time) - infrastructure, CI/CD
3. **QA Engineer** (Part-time) - тестирование интеграций

**Альтернативный подход:** Один senior full-stack разработчик + консультации по DevOps

### 5.4 Ежемесячные операционные расходы (USD)

| Сервис | Стоимость | Примечание |
|--------|-----------|------------|
| Supabase Pro | $25/месяц | PostgreSQL hosting |
| Azure Container Apps | $20-50/месяц | При низкой нагрузке (scale-to-zero) |
| OpenAI API | $50-200/месяц | В зависимости от использования |
| Anthropic API | $50-150/месяц | Альтернативный LLM |
| Google Workspace APIs | $0-20/месяц | Бесплатные квоты покрывают MVP |
| **Итого** | **$145-445/месяц** | Зависит от масштаба использования |

## 6. Рекомендации и следующие шаги

### 6.1 Приоритеты разработки

1. **Начать с Фазы 1** для создания работающего MVP
2. **Использовать Semantic Kernel** как основной агентский фреймворк
3. **Supabase** для быстрого старта с PostgreSQL
4. **Azure Container Apps** для cost-effective hosting

### 6.2 Архитектурные решения

1. **Модульная архитектура** для независимого развития компонентов
2. **Repository pattern** для абстракции доступа к данным
3. **Plugin architecture** для новых интеграций
4. **Event-driven architecture** для real-time обновлений

### 6.3 Риски и митигация

**Основные риски:**
1. **API rate limits** - реализовать intelligent caching и request batching
2. **LLM costs** - мониторинг использования и оптимизация prompts
3. **Security** - encryption, secure token storage, audit logs
4. **Scalability** - использовать Azure auto-scaling capabilities

Данная архитектура обеспечивает solid foundation для персонального агента Ивана с возможностью поэтапного развития и масштабирования.