# Technical Foundation Architecture 🔧

> **Plan Type**: CONCEPTUAL | **LLM Ready**: YES | **Reading Time**: 15 мин  
> **Prerequisites**: `01-01-system-overview.md` | **Next**: `../02-technical/02-01-database-design.md`

## Технологический стек

### Платформа
- **.NET 8.0** (LTS) - основная платформа
- **C# 12** - язык разработки
- **Target Frameworks**: net8.0

### Backend Core
- **ASP.NET Core 8.0** - Web API и хост
- **Microsoft.SemanticKernel 1.28.0** - агентский фреймворк  
- **Microsoft.Extensions.AI.MCP 9.0.0** - MCP протокол
- **SignalR** - real-time коммуникация

### База данных
- **PostgreSQL 16+** - основное хранилище
- **Entity Framework Core 8.0.10** - ORM
- **Npgsql.EntityFrameworkCore.PostgreSQL 8.0.10** - драйвер
- **Code-First подход** с миграциями

### LLM Интеграция
- **Claude Code** (основной мозг) через MCP
- **Anthropic.SDK 2.0.0** - резервная интеграция
- **OpenAI-DotNet 8.7.2** - дополнительный провайдер

### Внешние интеграции
```xml
<PackageReference Include="Telegram.Bot" Version="22.6.0" />
<PackageReference Include="Google.Apis.Calendar.v3" Version="1.69.0.3746" />
<PackageReference Include="Google.Apis.Gmail.v1" Version="1.69.0.3746" />
<PackageReference Include="Octokit" Version="13.0.1" />
<PackageReference Include="SlackAPI" Version="1.1.5" />
```

### Фронтенд технологии
- **Blazor Server** - веб-интерфейс
- **Blazor WebAssembly** - автономный режим
- **.NET MAUI** - мобильные и десктоп приложения
- **Telegram.Bot** - Telegram интерфейс

## Системная архитектура

### Слои приложения
```
┌─────────────────────────────────────────────────┐
│                 Presentation                    │
│ • Blazor Components  • MAUI Views  • Bot API   │
├─────────────────────────────────────────────────┤
│                   API Layer                     │
│ • REST Controllers   • SignalR Hubs            │
├─────────────────────────────────────────────────┤
│                Business Logic                   │
│ • Semantic Kernel   • Personality Engine       │
├─────────────────────────────────────────────────┤
│                Integration Layer                │
│ • MCP Client     • External APIs               │
├─────────────────────────────────────────────────┤
│                 Data Access                     │
│ • EF Core DbContext  • Repositories            │
├─────────────────────────────────────────────────┤
│                   Database                      │
│ • PostgreSQL     • Migrations                  │
└─────────────────────────────────────────────────┘
```

### Core Services
- **IPersonalityService** - моделирование личности Ивана
- **IMCPService** - коммуникация с Claude Code
- **IMemoryService** - управление памятью агента
- **IIntegrationService** - внешние API
- **IConversationService** - управление диалогами

## Файловая структура проекта

```
DigitalMe/
├── src/
│   ├── DigitalMe.API/              # Web API проект
│   │   ├── Controllers/
│   │   ├── Hubs/                   # SignalR хабы
│   │   ├── Program.cs
│   │   └── appsettings.json
│   ├── DigitalMe.Core/             # Бизнес-логика
│   │   ├── Services/
│   │   ├── Models/
│   │   └── Interfaces/
│   ├── DigitalMe.Data/             # Доступ к данным
│   │   ├── Context/
│   │   ├── Entities/
│   │   ├── Migrations/
│   │   └── Repositories/
│   ├── DigitalMe.Integrations/     # Внешние API
│   │   ├── Telegram/
│   │   ├── Google/
│   │   ├── GitHub/
│   │   └── MCP/
│   ├── DigitalMe.Web/              # Blazor веб-app
│   │   ├── Components/
│   │   ├── Pages/
│   │   └── wwwroot/
│   └── DigitalMe.Mobile/           # MAUI приложение
│       ├── Platforms/
│       ├── Views/
│       └── MauiProgram.cs
├── tests/
│   ├── DigitalMe.Tests.Unit/
│   ├── DigitalMe.Tests.Integration/
│   └── DigitalMe.Tests.E2E/
├── docs/
│   └── api/                        # API документация
└── docker/
    ├── Dockerfile
    └── docker-compose.yml
```

---

### 🔙 Navigation
- **← Parent Plan**: [Main Plan](../../00-MAIN_PLAN.md)
- **← Conceptual Coordinator**: [../01-conceptual.md](../01-conceptual.md)
- **← Architecture Overview**: [../00-ARCHITECTURE_OVERVIEW.md](../00-ARCHITECTURE_OVERVIEW.md)

---

## 🔗 NAVIGATION & DEPENDENCIES

### Prerequisites (что должно быть прочитано до этого)
- **REQUIRED**: `01-01-system-overview.md` - понимание общей архитектуры

### Next Steps (что читать дальше)  
- **RECOMMENDED**: `../02-technical/02-01-database-design.md` - детальная схема БД
- **ALTERNATIVE**: `../02-technical/02-02-mcp-integration.md` - интеграция с Claude

### Related Plans
- **Parent**: `01-01-system-overview.md` (системный обзор)
- **Children**: Все планы в `../02-technical/` (детальные спецификации)

---

## 📊 PLAN METADATA

- **Type**: CONCEPTUAL
- **LLM Ready**: YES  
- **Estimated Reading**: 15 минут
- **Prerequisites**: System Overview
- **Status**: Technical foundation defined
- **Created**: 2025-08-27
- **Last Updated**: 2025-08-27

**🎯 NEXT ACTION**: Переходи к `../02-technical/02-01-database-design.md` для детального понимания БД