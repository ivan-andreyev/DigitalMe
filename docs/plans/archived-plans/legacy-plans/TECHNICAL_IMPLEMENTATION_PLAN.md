# Технический план реализации персонального агента

## Обзор системы

Персональный агент Ивана - система интеграции с внешними сервисами для автоматизации личных и рабочих процессов через LLM интерфейс.

### Ключевые требования:
- **Интеграции:** Telegram, Slack, Google Calendar, GitHub, контакты, Claude Code API
- **Данные:** PostgreSQL с EF Core, Code-First подход
- **LLM:** OpenAI, Anthropic с возможностью расширения
- **Технологии:** C#/.NET как основной стек
- **Протоколы:** MCP для интеграции с агентскими системами

## Рекомендуемый технологический стек

### Основные технологии
- **.NET 8.0** - последняя LTS версия
- **ASP.NET Core Web API** - для REST API и веб-интерфейса
- **Entity Framework Core 8** - ORM с Code-First миграциями
- **PostgreSQL** - основная база данных
- **Microsoft Semantic Kernel** - агентский фреймворк

### Агентский фреймворк
**Рекомендация: Microsoft Semantic Kernel**

**Причины выбора:**
- Native .NET интеграция
- Production-ready состояние в 2024-2025
- Официальная поддержка OpenAI и Anthropic
- Встроенная поддержка MCP протокола
- Планируемая конвергенция с AutoGen в 2025

**NuGet пакеты:**
```xml
<PackageReference Include="Microsoft.SemanticKernel" Version="1.28.0" />
<PackageReference Include="Microsoft.SemanticKernel.Connectors.OpenAI" Version="1.28.0" />
<PackageReference Include="Microsoft.Extensions.AI" Version="9.0.0" />
```

## Интеграции с внешними сервисами

### 1. Telegram Integration
**Основная библиотека:** `Telegram.Bot` v22.6.0
```xml
<PackageReference Include="Telegram.Bot" Version="22.6.0" />
```

**Функциональность:**
- Чтение всех сообщений пользователя
- Отправка сообщений от имени пользователя
- Управление чатами и группами

**Альтернатива для расширенных возможностей:** `WTelegramClient`
- Доступ к MTProto API для полного функционала

### 2. Google Services Integration
**Calendar API:**
```xml
<PackageReference Include="Google.Apis.Calendar.v3" Version="1.69.0.3746" />
<PackageReference Include="Google.Apis.Auth" Version="1.69.0.3746" />
```

**Google Workspace (Drive, Docs, Sheets):**
```xml
<PackageReference Include="Google.Apis.Drive.v3" Version="1.69.0.3746" />
<PackageReference Include="Google.Apis.Docs.v1" Version="1.69.0.3746" />
<PackageReference Include="Google.Apis.Sheets.v4" Version="1.69.0.3746" />
```

### 3. GitHub Integration
```xml
<PackageReference Include="Octokit" Version="13.0.1" />
```

**Функциональность:**
- Доступ к репозиториям
- Управление issues и PR
- Анализ коммитов и активности

### 4. Slack Integration
```xml
<PackageReference Include="SlackAPI" Version="1.1.5" />
```

### 5. MCP Protocol Support
**Официальный Microsoft SDK для MCP:**
```xml
<PackageReference Include="Microsoft.Extensions.AI.MCP" Version="9.0.0" />
```

## База данных и хранение

### PostgreSQL с Code-First подходом

**EF Core пакеты:**
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.10" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.10" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.10" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.10" />
```

**Модель данных:**
```csharp
public class PersonalAgentContext : DbContext
{
    public DbSet<Contact> Contacts { get; set; }
    public DbSet<CalendarEvent> CalendarEvents { get; set; }
    public DbSet<TelegramMessage> TelegramMessages { get; set; }
    public DbSet<GitHubRepository> GitHubRepositories { get; set; }
    public DbSet<PersonalityProfile> PersonalityProfiles { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<AgentAction> AgentActions { get; set; }
}
```

### Варианты хостинга БД

#### Вариант 1: Supabase (рекомендуемый)
- **Стоимость:** $25/месяц (Pro план)
- **Преимущества:** 
  - Полная поддержка PostgreSQL
  - REST API из коробки
  - Real-time subscriptions
  - Встроенная аутентификация
- **Миграции:** Полная поддержка EF Core миграций через connection string

#### Вариант 2: Azure Database for PostgreSQL
- **Стоимость:** $50-100/месяц
- **Преимущества:** 
  - Интеграция с Azure экосистемой
  - Высокая доступность
  - Автоматические бэкапы

## LLM Провайдеры

### OpenAI Integration
```xml
<PackageReference Include="OpenAI-DotNet" Version="8.7.2" />
```

### Anthropic Integration
```xml
<PackageReference Include="Anthropic.SDK" Version="2.0.0" />
```

**Абстракция провайдеров:**
```csharp
public interface ILLMProvider
{
    Task<string> GenerateResponseAsync(string prompt, PersonalityContext context);
    Task<string> GenerateWithToolsAsync(string prompt, IEnumerable<ITool> tools);
}

public class OpenAIProvider : ILLMProvider { }
public class AnthropicProvider : ILLMProvider { }
```

## Архитектура системы

### Компонентная диаграмма
```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Web API       │    │  Semantic Kernel │    │   PostgreSQL    │
│   Controllers   │◄──►│   Agent Core     │◄──►│   Database      │
└─────────────────┘    └──────────────────┘    └─────────────────┘
         ▲                        ▲
         │                        │
         ▼                        ▼
┌─────────────────┐    ┌──────────────────┐
│   External      │    │   Personality    │
│   Integrations  │    │   Profile Store  │
│                 │    │                  │
│ • Telegram      │    │ • Profile Data   │
│ • Google        │    │ • Conversation   │
│ • GitHub        │    │   History        │
│ • Slack         │    │ • Learning Log   │
└─────────────────┘    └──────────────────┘
```

### Модули системы

#### 1. Core Agent Module
- **Personality Engine** - загрузка и применение профиля Ивана
- **Decision Making** - логика принятия решений
- **Memory Management** - краткосрочная и долгосрочная память

#### 2. Integration Module  
- **Service Connectors** - подключения к внешним API
- **Data Synchronization** - синхронизация данных
- **Event Processing** - обработка событий из внешних систем

#### 3. Data Module
- **Entity Framework Context** - доступ к данным
- **Repository Pattern** - абстракция данных
- **Migration System** - управление схемой БД

#### 4. API Module
- **REST Controllers** - веб API
- **Authentication** - безопасность
- **Rate Limiting** - защита от злоупотреблений

## План реализации MVP

### Фаза 1: Core MVP (4-6 недель)
**Цель:** Базовый функционал агента

**Компоненты:**
- ✅ Web API на ASP.NET Core
- ✅ PostgreSQL с EF Core
- ✅ Semantic Kernel integration
- ✅ Telegram Bot (базовые команды)
- ✅ Google Calendar (чтение событий)
- ✅ Простейший personality engine

**Критерии готовности:**
- Агент отвечает в Telegram в стиле Ивана
- Может читать и создавать события в календаре
- Сохраняет историю диалогов

### Фаза 2: Enhanced Integration (3-4 недели)
**Цель:** Расширенные интеграции

**Компоненты:**
- ✅ Claude API integration (через MCP)
- ✅ GitHub repositories access
- ✅ Contact management
- ✅ Enhanced personality model
- ✅ Multi-turn conversations

**Критерии готовности:**
- Доступ к GitHub репозиториям
- Управление контактами
- Более точное моделирование личности

### Фаза 3: Advanced Features (4-5 недель)
**Цель:** Продвинутые возможности

**Компоненты:**
- ✅ MCP Server implementation
- ✅ Google Workspace full integration
- ✅ Slack integration
- ✅ Multi-agent scenarios
- ✅ Advanced memory management

### Фаза 4: Production Ready (2-3 недели)
**Цель:** Готовность к продакшену

**Компоненты:**
- ✅ Cloud deployment
- ✅ Monitoring & logging
- ✅ Security enhancements
- ✅ Performance optimization
- ✅ Documentation

## Хостинг и развертывание

### Рекомендуемое решение: Azure Container Apps

**Преимущества:**
- Scale-to-zero для экономии (до 80% экономии)
- Простое развертывание контейнеров
- Автоматическое HTTPS
- Встроенный мониторинг

**Стоимость:** $20-50/месяц в зависимости от нагрузки

### Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["DigitalMe.csproj", "."]
RUN dotnet restore "DigitalMe.csproj"
COPY . .
RUN dotnet build "DigitalMe.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DigitalMe.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DigitalMe.dll"]
```

## Безопасность

### Аутентификация и авторизация
- **JWT токены** для API доступа
- **OAuth 2.0** для внешних сервисов
- **API Keys** в Azure Key Vault

### Секреты и конфигурация
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "{{SUPABASE_CONNECTION_STRING}}"
  },
  "ExternalApis": {
    "OpenAI": {
      "ApiKey": "{{OPENAI_API_KEY}}"
    },
    "Anthropic": {
      "ApiKey": "{{ANTHROPIC_API_KEY}}"
    },
    "Telegram": {
      "BotToken": "{{TELEGRAM_BOT_TOKEN}}",
      "UserSession": "{{TELEGRAM_USER_SESSION}}"
    }
  }
}
```

## Мониторинг и логирование

### Application Insights
```xml
<PackageReference Include="Microsoft.ApplicationInsights.AspNetCore" Version="2.22.0" />
```

### Структурированное логирование
```xml
<PackageReference Include="Serilog.AspNetCore" Version="8.0.2" />
<PackageReference Include="Serilog.Sinks.ApplicationInsights" Version="4.0.0" />
```

## Оценка ресурсов

### Временные затраты
- **Core MVP:** 4-6 недель (1 senior developer)
- **Full System:** 13-18 недель (1 senior developer)
- **Team approach:** 8-12 недель (2-3 developers)

### Операционные расходы
- **Supabase Pro:** $25/месяц
- **Azure Container Apps:** $20-50/месяц
- **OpenAI API:** $50-200/месяц (в зависимости от использования)
- **Anthropic API:** $50-150/месяц
- **Azure Key Vault:** $5/месяц
- **Application Insights:** $10-30/месяц

**Итого:** $145-445/месяц

### Разработческие расходы
- **Senior .NET Developer:** $60-100/час
- **Total MVP cost:** $9,600-24,000
- **Full system cost:** $31,200-72,000

## Риски и митигации

### Технические риски
1. **API лимиты внешних сервисов**
   - *Митигация:* Implement caching and batch requests
2. **LLM hallucinations**
   - *Митигация:* Validation layers and confidence scoring
3. **Data privacy concerns**
   - *Митигация:* End-to-end encryption and minimal data retention

### Бизнес риски
1. **High operational costs**
   - *Митигация:* Scale-to-zero hosting and usage optimization
2. **External API changes**
   - *Митигация:* Abstraction layers and version management

## Следующие шаги

1. **Immediate (1-2 дня):** Setup project structure and core dependencies
2. **Week 1:** Database schema and EF Core setup
3. **Week 2:** Basic Semantic Kernel integration
4. **Week 3:** Telegram Bot MVP
5. **Week 4:** Google Calendar integration
6. **Month 2:** Enhanced features and personality engine
7. **Month 3:** Advanced integrations and production deployment

---
*Технический план создан: 2025-08-27*  
*Статус: Готов к реализации*