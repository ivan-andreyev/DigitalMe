# 🤖 DigitalMe - Цифровой клон Ивана

**Персонализированный AI-агент с поддержкой Claude API, MCP протокола и полной автоматизацией CI/CD**

[![Build Status](https://github.com/your-username/DigitalMe/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/your-username/DigitalMe/actions)
[![Security Scan](https://github.com/your-username/DigitalMe/actions/workflows/security-scan.yml/badge.svg)](https://github.com/your-username/DigitalMe/actions)
[![Docker](https://ghcr-badge.egpl.dev/your-username/digitalme/digitalme/latest_tag?color=%2344cc11&ignore=latest&label=docker&trim=)](https://github.com/your-username/DigitalMe/pkgs/container/digitalme%2Fdigitalme)

## 🚀 Быстрый старт

### Локальный запуск

```bash
# Клонирование репозитория
git clone https://github.com/your-username/DigitalMe.git
cd DigitalMe

# Восстановление зависимостей
dotnet restore DigitalMe.CI.sln

# Запуск приложения
dotnet run --project DigitalMe
```

### Docker запуск

```bash
# Простой запуск
docker run -p 8080:8080 ghcr.io/your-username/digitalme/digitalme:latest

# С настройками
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="your-db-string" \
  -e Anthropic__ApiKey="your-claude-key" \
  ghcr.io/your-username/digitalme/digitalme:latest
```

## 📋 Возможности

### 🧠 AI & Personal Assistant
- **Персональный AI клон** - Максимально точное моделирование личности пользователя
- **Claude API интеграция** - Продвинутые языковые возможности через Anthropic
- **MCP протокол** - Model Context Protocol для расширенного взаимодействия
- **Tool Strategy Pattern** - Гибкая система инструментов и автоматизации

### 🔧 Enterprise Architecture
- **Clean Architecture** - Четкое разделение слоев (Domain, Application, Infrastructure)
- **SOLID принципы** - Масштабируемая и поддерживаемая кодовая база
- **Repository Pattern** - Абстракция доступа к данным
- **Dependency Injection** - Слабая связанность компонентов

### 🛡️ Production Ready
- **Health Checks** - Мониторинг состояния приложения (`/health`, `/health/ready`, `/health/live`)
- **Structured Logging** - Serilog с консолью и файлами
- **Database Support** - PostgreSQL + SQLite с миграциями EF Core
- **Performance Metrics** - Встроенная телеметрия и мониторинг

### 🔄 DevOps & CI/CD
- **GitHub Actions** - Полностью автоматизированный CI/CD pipeline
- **Multi-stage Docker** - Оптимизированные производственные образы
- **Security Scanning** - CodeQL, DevSkim, dependency scanning
- **Automated Releases** - Семантическое версионирование и Docker registry

## 🏗️ Архитектура

```
DigitalMe/
├── 🎯 DigitalMe/              # Core API application
│   ├── Controllers/           # REST API endpoints  
│   ├── Services/             # Business logic
│   ├── Data/                 # EF Core, entities, repositories
│   └── Integrations/         # External services (Anthropic, MCP)
├── 🌐 src/DigitalMe.Web/     # Blazor Web UI
├── 📱 src/DigitalMe.MAUI/    # Cross-platform mobile app
├── 🧪 tests/                 # Unit & Integration tests
├── 🐳 Dockerfile             # Production container
└── 📋 .github/workflows/     # CI/CD automation
```

### Core Components

- **🤖 AgentBehaviorEngine** - Центральный движок обработки сообщений
- **🔧 ToolRegistry** - Динамическая система инструментов 
- **💾 PersonalityService** - Управление профилями личности
- **🔗 MCPService** - Интеграция с Model Context Protocol
- **📊 HealthCheckService** - Мониторинг системы

## 🛠️ Технологический стек

### Backend
- **.NET 8.0** - Latest LTS framework
- **ASP.NET Core** - Web API and SignalR
- **Entity Framework Core** - ORM с PostgreSQL/SQLite
- **Anthropic SDK** - Claude API integration
- **Serilog** - Structured logging

### Frontend  
- **Blazor Server** - Interactive web UI
- **.NET MAUI** - Cross-platform mobile

### DevOps & Infrastructure
- **GitHub Actions** - CI/CD automation
- **Docker** - Контейнеризация
- **PostgreSQL** - Production database
- **SQLite** - Development database

## 📊 Мониторинг и Observability

### Health Checks
```bash
curl http://localhost:8080/health          # Общее состояние
curl http://localhost:8080/health/ready    # Готовность к работе
curl http://localhost:8080/health/live     # Проверка жизнеспособности
```

### Metrics
```bash
curl http://localhost:8080/metrics         # Метрики производительности
curl http://localhost:8080/runtime/gc      # GC статистика
curl http://localhost:8080/runtime/threadpool # ThreadPool статистика
```

## 🚀 Development

### Локальная разработка

```bash
# Тестирование CI/CD pipeline локально
./scripts/test-ci-locally.sh

# Запуск с горячей перезагрузкой
dotnet watch run --project DigitalMe

# Запуск тестов
dotnet test DigitalMe.CI.sln --configuration Release
```

### Структура проектов

- **`DigitalMe.sln`** - Полный solution (включая MAUI)
- **`DigitalMe.CI.sln`** - CI/CD solution (без MAUI для стабильности)

### Database Setup

```bash
# Создание миграции
dotnet ef migrations add "MigrationName" --project DigitalMe

# Применение миграций
dotnet ef database update --project DigitalMe
```

## 🔐 Конфигурация

### Environment Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Среда выполнения | `Production` |
| `ConnectionStrings__DefaultConnection` | Строка подключения к БД | `Host=localhost;Database=digitalme;...` |
| `Anthropic__ApiKey` | Claude API ключ | `sk-ant-...` |
| `MCP__ServerUrl` | URL MCP сервера | `http://localhost:3000/mcp` |

### appsettings.json

```json
{
  "Anthropic": {
    "ApiKey": "your-claude-api-key",
    "DefaultModel": "claude-3-5-sonnet-20241022",
    "MaxTokens": 4000
  },
  "MCP": {
    "ServerUrl": "http://localhost:3000/mcp"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=digitalme.db"
  }
}
```

## 🔄 CI/CD Pipeline

### Автоматические процессы

- **✅ Build & Test** - На каждый push/PR
- **🔒 Security Scan** - Ежедневно в 02:00 UTC  
- **📦 Dependency Update** - Еженедельно по понедельникам
- **🚀 Release** - При создании тегов `v*.*.*`

### GitHub Actions Workflows

1. **`ci-cd.yml`** - Основной pipeline (build, test, deploy)
2. **`security-scan.yml`** - Сканирование безопасности
3. **`dependency-update.yml`** - Обновление зависимостей
4. **`release.yml`** - Релизный pipeline

### Deployment

```bash
# Production deployment
docker-compose -f docker-compose.prod.yml up -d

# Kubernetes deployment
kubectl apply -f k8s/digitalme-deployment.yaml
```

## 📝 API Documentation

После запуска приложения доступен Swagger UI:
- **Local**: http://localhost:5000/swagger
- **Docker**: http://localhost:8080/swagger

### Основные endpoints

- `GET /api/chat/status` - Статус системы
- `POST /api/chat/send` - Отправка сообщения
- `GET /api/personality/{name}` - Получение профиля личности
- `POST /api/personality` - Создание профиля
- `GET /health` - Health check

## 🧪 Testing

### Запуск тестов

```bash
# Все тесты
dotnet test DigitalMe.CI.sln

# Unit тесты
dotnet test tests/DigitalMe.Tests.Unit

# Integration тесты  
dotnet test tests/DigitalMe.Tests.Integration
```

### Test Coverage

Тесты покрывают:
- ✅ Repository patterns
- ✅ Service layer logic  
- ✅ API controllers
- ✅ Database compatibility
- ⚠️ Integration тесты (требуют обновления)

## 🤝 Contributing

1. Fork проект
2. Создайте feature branch (`git checkout -b feature/amazing-feature`)
3. Commit изменения (`git commit -m 'Add amazing feature'`)
4. Push в branch (`git push origin feature/amazing-feature`)
5. Создайте Pull Request

### Code Style

- Используйте `.editorconfig` настройки
- Следуйте SOLID принципам
- Покрывайте код тестами
- Документируйте публичные API

## 📚 Documentation

- **[CI/CD Setup](./README-CI-CD.md)** - Подробная настройка pipeline
- **[Architecture](./docs/architecture/)** - Архитектурная документация
- **[API Reference](./docs/api/)** - Справочник API
- **[Deployment Guide](./docs/deployment/)** - Руководство по развертыванию

## 🏆 Status

- ✅ **Core API** - Готов к production
- ✅ **CI/CD Pipeline** - Полностью автоматизирован  
- ✅ **Docker Images** - Оптимизированы для production
- ✅ **Health Monitoring** - Встроенная телеметрия
- ⚠️ **Tests** - Требуют рефакторинга после архитектурных изменений
- 🔄 **Web UI** - В разработке
- 🔄 **Mobile App** - В разработке

## 📞 Support & Links

- 🐛 **Issues**: [GitHub Issues](https://github.com/your-username/DigitalMe/issues)
- 💬 **Discussions**: [GitHub Discussions](https://github.com/your-username/DigitalMe/discussions)
- 📖 **Documentation**: [Wiki](https://github.com/your-username/DigitalMe/wiki)
- 🚀 **Releases**: [GitHub Releases](https://github.com/your-username/DigitalMe/releases)

---

**🤖 Made with ❤️ by Ivan & Claude**

*DigitalMe - Where AI meets personality*
