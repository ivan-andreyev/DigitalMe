# ⚡ Digital Clone Development: Quick Start Guide

**Родительский план**: [00-MAIN_PLAN.md](./00-MAIN_PLAN.md)

> **Единственная точка входа** для быстрого старта и навигации по планам разработки Digital Clone Agent

---

## ⚡ QUICK START

### Хочешь сразу кодить? (3 минуты)
1. **Читай план**: [Phase 1 Implementation](00-MAIN_PLAN/03-implementation/03-02-phase1-detailed.md) 
2. **Проверь окружение**: .NET 8 SDK, PostgreSQL, Claude Code
3. **Создавай проект**: `dotnet new webapi -n DigitalMe.Api`

### Хочешь понять архитектуру? (45 минут)
1. **Старт**: [Architecture Overview](00-MAIN_PLAN/00-ARCHITECTURE_OVERVIEW.md)
2. **Концепты**: [System Overview](00-MAIN_PLAN/01-conceptual/01-01-system-overview.md)
3. **Техника**: [Database Design](00-MAIN_PLAN/02-technical/02-01-database-design.md)

### Проблемы?
- **Ссылки не работают?** → Проверь структуру каталогов `docs/plans/00-MAIN_PLAN/`
- **Файлы не найдены?** → Все планы лежат в `docs/plans/`
- **Не понятно что делать?** → Начни с [Architecture Overview](00-MAIN_PLAN/00-ARCHITECTURE_OVERVIEW.md)

---

## 📍 НАВИГАЦИЯ ПО ПЛАНАМ

### ⚡ Для немедленной разработки
**EXECUTE NOW** → [Phase 1 Implementation](00-MAIN_PLAN/03-implementation/03-02-phase1-detailed.md)
- **LLM Ready**: ✅ YES (готов к выполнению)
- **Time**: 3-4 недели реализации
- **Status**: Детализированный план с file:line ссылками

### 🎯 Для понимания архитектуры (45 мин)
**START HERE** → [Architecture Overview](00-MAIN_PLAN/00-ARCHITECTURE_OVERVIEW.md)
- Системная архитектура .NET 8 + MCP  
- Интеграции с Telegram, Google, GitHub
- Blazor + MAUI фронтенды

### 📚 Дополнительные планы (по необходимости)
- **[Profile Enhancement Plan](PROFILE_ENHANCEMENT_PLAN.md)** - улучшение профиля личности Ивана
- **[Temporal Modeling Strategy](TEMPORAL_MODELING_STRATEGY.md)** - моделирование временных аспектов
- **[Legacy Plans](archived-plans/legacy-plans/)** - архивные планы (для истории)

---

## 🗺️ Карта планов

### 🏗️ АКТИВНЫЕ ПЛАНЫ
```  
📁 00-MAIN_PLAN/                       🚀 ОСНОВНЫЕ ПЛАНЫ (ACTIVE)
├── 00-ARCHITECTURE_OVERVIEW.md        🗺️ Навигационный центр архитектуры
├── 01-conceptual.md                   📋 Координатор (25 мин понимания)
├── 02-technical.md                    📋 Координатор (85 мин подготовки)  
├── 03-implementation.md               📋 Координатор (⚡ ГОТОВО К ВЫПОЛНЕНИЮ)
├── 04-reference.md                    📋 Координатор (справочные материалы)
├── 01-conceptual/                     📁 Концептуальные планы
├── 02-technical/                      📁 Технические планы  
├── 03-implementation/                 📁 Планы реализации
└── 04-reference/                      📁 Справочные материалы

📄 PROFILE_ENHANCEMENT_PLAN.md          ✅ АКТУАЛЬНЫЙ ПЛАН
📄 TEMPORAL_MODELING_STRATEGY.md        ✅ АКТУАЛЬНЫЙ ПЛАН
```

### 📚 АРХИВНЫЕ ПЛАНЫ  
```
📁 archived-plans/legacy-plans/         📚 LEGACY (для истории)
├── DIGITAL_CLONE_DEVELOPMENT_PLAN.md  ⚠️ УСТАРЕЛ (архив)
├── ALL_DOTNET_ARCHITECTURE_PLAN.md    ⚠️ УСТАРЕЛ → см. architecture/
├── TECHNICAL_IMPLEMENTATION_PLAN.md   ⚠️ УСТАРЕЛ → см. architecture/
└── README.md                          📖 Объяснение legacy планов
```

---

## 🚨 Критическая информация

### Личность агента
**Профиль**: `data/profile/IVAN_PROFILE_DATA.md`
- **Иван, 34 года, Head of R&D**  
- **Философия**: "всем похуй", "сила в правде", "живи и дай жить другим"
- **Стиль**: Прямой, технически компетентный, иногда резкий

### 🔧 Технические спецификации (LLM-Ready)

**Основной стек**:
- **Backend**: ASP.NET Core 8 + Microsoft.Extensions.AI v8.0.0 + Microsoft.SemanticKernel v1.26.0
- **База данных**: PostgreSQL 16 + EF Core v8.0.10 + Code-First migrations  
- **LLM**: Claude 3.5 Sonnet via Microsoft.SemanticKernel.Connectors.Anthropic
- **Аутентификация**: JWT (Microsoft.AspNetCore.Authentication.JwtBearer v8.0.10)

**Внешние интеграции**:
- **Google APIs**: Google.Apis.Gmail.v1 v1.68.0.3482 + Google.Apis.Calendar.v3 v1.68.0.3344
- **GitHub**: Octokit v13.0.1 с Personal Access Token
- **Telegram**: Telegram.Bot library

**Математика личности**:
```csharp
// Коррекция черт по настроению
var adjustedTrait = Math.Min(1.0, baseTrait * moodModifier);
// Модификаторы: Irritated=1.3, Tired=0.6, Focused=1.15, Calm=1.0
```

**Настройка конфигурации**:
- Anthropic:ApiKey, Anthropic:BaseUrl, Anthropic:DefaultModel  
- JWT secret (256-bit), issuer, audience, expiry
- Google OAuth2 ClientId/ClientSecret
- GitHub PersonalAccessToken
- PostgreSQL connection string с retry policy

**Метрики производительности**:
- Ответ LLM: <2s для 95% запросов
- Покрытие тестами: 85%+
- API error rate: <1%
- WebSocket latency: <500ms

### Технологический стек
- **Фронтенды**: Blazor Server + MAUI Cross-Platform + Telegram Bot
- **Контейнеризация**: Docker multi-stage build + docker-compose.yml
- **Hosting**: Railway (рекомендуемая платформа)
- **Мониторинг**: Health checks + структурированное логирование (Serilog)

---

**НАЧНИ ЗДЕСЬ** → [Architecture Overview](00-MAIN_PLAN/00-ARCHITECTURE_OVERVIEW.md) *(понимание)* или [Phase 1 Implementation](00-MAIN_PLAN/03-implementation/03-02-phase1-detailed.md) *(выполнение)*