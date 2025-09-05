# Digital Clone System Overview 🏗️

> **Plan Type**: CONCEPTUAL | **LLM Ready**: YES | **Reading Time**: 10 мин  
> **Prerequisites**: None (entry point) | **Next**: `01-02-technical-foundation.md`

## Краткий обзор

Персональный агент Ивана на базе .NET 8 с интеграцией Claude Code через MCP протокол.

### Ключевые компоненты
- **Backend**: ASP.NET Core Web API + Semantic Kernel
- **База данных**: PostgreSQL + EF Core Code-First
- **LLM мозг**: Claude Code через MCP
- **Фронтенды**: Blazor, MAUI, Telegram Bot

### Архитектура
```
┌─────────────┐    MCP     ┌─────────────┐    EF Core    ┌─────────────┐
│ Claude Code │◄──────────►│   Backend   │◄─────────────►│ PostgreSQL  │
│   (Brain)   │  Protocol  │ Orchestrator│   Migrations  │  Database   │
└─────────────┘            └─────────────┘               └─────────────┘
                                   ▲
                           Multiple Frontends
                           ┌─────────────────┐
                           │ • Blazor Web    │
                           │ • MAUI Mobile   │
                           │ • MAUI Desktop  │
                           │ • Telegram Bot  │
                           │ • Voice (Future)│
                           └─────────────────┘
```

### Детальные планы
- [Техническая архитектура](../01-conceptual/01-02-technical-foundation.md)
- [Структура базы данных](../02-technical/02-01-database-design.md) 
- [MCP интеграция](../02-technical/02-02-mcp-integration.md)
- [Фронтенд архитектура](../02-technical/02-03-frontend-specs.md)
- [План разработки](../03-implementation/03-01-development-phases.md)
- [Хостинг и развертывание](../04-reference/04-01-deployment.md)

### Готовность к выполнению
- [x] Технологический стек определен
- [x] Архитектурные решения приняты
- [x] Структура планов каталогизирована
- [ ] Конкретные спецификации созданы
- [ ] Измеримые критерии определены

---

---

### 🔙 Navigation
- **← Parent Plan**: [Main Plan](../../00-MAIN_PLAN.md)
- **← Conceptual Coordinator**: [../01-conceptual.md](../01-conceptual.md)
- **← Architecture Overview**: [../00-ARCHITECTURE_OVERVIEW.md](../00-ARCHITECTURE_OVERVIEW.md)

---

## 🔗 NAVIGATION & DEPENDENCIES

### Prerequisites (что должно быть сделано до этого)
- Нет (это точка входа в архитектуру)

### Next Steps (что читать дальше)
- **RECOMMENDED**: `01-02-technical-foundation.md` - техническое понимание
- **ALTERNATIVE**: `02-01-database-design.md` - если нужна схема БД

### Related Plans
- **Parent**: `../00-ARCHITECTURE_OVERVIEW.md` (навигационный хаб)
- **Children**: Все планы в каталогах 02-technical/ и 03-implementation/

---

## 📊 PLAN METADATA

- **Type**: CONCEPTUAL  
- **LLM Ready**: YES
- **Estimated Reading**: 10 минут
- **Prerequisites**: Нет
- **Status**: Готов к детализации  
- **Created**: 2025-08-27
- **Last Updated**: 2025-08-27

**🎯 NEXT ACTION**: Читай `01-02-technical-foundation.md` для технических деталей