# DigitalMe CI/CD Pipeline

🚀 **Автоматизированная система сборки, тестирования и развертывания**

## 📋 Обзор

DigitalMe использует GitHub Actions для полностью автоматизированного CI/CD pipeline с поддержкой:

- ✅ Автоматическая сборка и тестирование
- 🔒 Сканирование безопасности 
- 🐳 Docker контейнеризация
- 📦 Автоматические релизы
- 🔄 Обновление зависимостей
- 🚀 Развертывание в staging/production

## 🏗️ Структура Pipeline

### 1. Основной CI/CD Pipeline (`ci-cd.yml`)

**Триггеры:**
- Push в `main`, `master`, `develop` 
- Pull Request в `main`, `master`

**Задачи:**
- 🔄 Сборка проекта (`DigitalMe.CI.sln`)
- 🧪 Unit тесты
- 🔗 Integration тесты с PostgreSQL
- 📊 Генерация отчетов
- 🐳 Docker образы (для main/master)
- 🚀 Развертывание

### 2. Безопасность (`security-scan.yml`)

**Триггеры:**
- Ежедневно в 02:00 UTC
- Ручной запуск

**Проверки:**
- 🔍 CodeQL анализ
- 🛡️ DevSkim сканирование
- 📋 Поиск уязвимостей в зависимостях

### 3. Обновления (`dependency-update.yml`)

**Триггеры:**
- Еженедельно по понедельникам в 09:00 UTC
- Ручной запуск

**Задачи:**
- 📦 Проверка устаревших пакетов
- 🔄 Автоматическое обновление minor/patch версий
- 🧪 Тестирование после обновлений
- 📝 Создание Pull Request

### 4. Релизы (`release.yml`)

**Триггеры:**
- Push тегов `v*.*.*`
- Ручной запуск с параметрами

**Процесс:**
- 🏷️ Создание GitHub Release
- 📦 Сборка артефактов
- 🐳 Docker образы с версионными тегами
- 🚀 Развертывание в production

## 🛠️ Настройка проекта

### Структура файлов

```
DigitalMe/
├── .github/workflows/          # GitHub Actions workflows
│   ├── ci-cd.yml              # Основной CI/CD pipeline
│   ├── security-scan.yml      # Сканирование безопасности
│   ├── dependency-update.yml  # Обновление зависимостей
│   └── release.yml           # Релизный pipeline
├── DigitalMe.CI.sln          # Solution для CI (без MAUI)
├── DigitalMe.sln             # Полный solution (с MAUI)
├── Dockerfile                # Production Docker образ
└── .dockerignore             # Исключения для Docker
```

### Solution файлы

- **`DigitalMe.CI.sln`** - Используется в CI/CD (исключает MAUI для стабильности)
- **`DigitalMe.sln`** - Полный solution для локальной разработки

## 🚀 Развертывание

### Docker Deployment

```bash
# Pull последнего образа
docker pull ghcr.io/your-username/digitalme/digitalme:latest

# Запуск с базовой конфигурацией
docker run -d \
  --name digitalme \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="your-db-connection" \
  ghcr.io/your-username/digitalme/digitalme:latest
```

### Docker Compose

```yaml
version: '3.8'
services:
  digitalme:
    image: ghcr.io/your-username/digitalme/digitalme:latest
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=digitalme;Username=postgres;Password=${DB_PASSWORD}
    depends_on:
      - postgres
      
  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: digitalme
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  postgres_data:
```

### Kubernetes Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: digitalme
spec:
  replicas: 3
  selector:
    matchLabels:
      app: digitalme
  template:
    metadata:
      labels:
        app: digitalme
    spec:
      containers:
      - name: digitalme
        image: ghcr.io/your-username/digitalme/digitalme:latest
        ports:
        - containerPort: 8080
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: digitalme-secrets
              key: database-connection
```

## ⚙️ Конфигурация

### Переменные окружения

| Переменная | Описание | Пример |
|------------|----------|---------|
| `ASPNETCORE_ENVIRONMENT` | Среда выполнения | `Production` |
| `ConnectionStrings__DefaultConnection` | База данных | `Host=localhost;Database=digitalme;...` |
| `Anthropic__ApiKey` | API ключ Claude | `sk-ant-...` |
| `MCP__ServerUrl` | URL MCP сервера | `http://localhost:3000/mcp` |

### Secrets в GitHub

Настройте следующие secrets в репозитории:

- `ANTHROPIC_API_KEY` - API ключ для Claude
- `DATABASE_PASSWORD` - Пароль базы данных
- `DOCKER_REGISTRY_TOKEN` - Токен для Docker registry

## 📊 Мониторинг

### Health Checks

Приложение предоставляет health check endpoints:

- `/health` - Общий статус
- `/health/ready` - Готовность к обслуживанию  
- `/health/live` - Статус жизнеспособности

### Metrics

Docker образ настроен для экспорта метрик:

- Prometheus metrics endpoint: `/metrics`
- Application Insights (если настроен)
- Structured logging

## 🔧 Локальная разработка

### Запуск CI проверок локально

```bash
# Сборка CI solution
dotnet build DigitalMe.CI.sln --configuration Release

# Запуск тестов как в CI
dotnet test DigitalMe.CI.sln --configuration Release --logger trx

# Сборка Docker образа
docker build -t digitalme-local .

# Запуск контейнера
docker run -d -p 8080:8080 digitalme-local
```

### Проверка безопасности

```bash
# Поиск уязвимостей в зависимостях
dotnet list package --vulnerable --include-transitive

# Обновление пакетов
dotnet outdated
```

## 🚨 Troubleshooting

### Ошибки сборки

1. **MAUI ошибки** - CI использует `DigitalMe.CI.sln` без MAUI
2. **PostgreSQL connection** - Проверьте строку подключения
3. **Missing packages** - Запустите `dotnet restore`

### Docker проблемы

1. **Image pull errors** - Убедитесь в доступе к GitHub Container Registry
2. **Permission denied** - Контейнер запускается под non-root пользователем
3. **Health check fails** - Проверьте `/health` endpoint

### Развертывание

1. **Environment variables** - Проверьте все необходимые переменные
2. **Database migrations** - Запустите миграции перед развертыванием
3. **Port conflicts** - Убедитесь в доступности портов 8080/8081

## 📞 Поддержка

- 📚 [Документация](./docs/)
- 🐛 [Issues](https://github.com/your-username/DigitalMe/issues)
- 💬 [Discussions](https://github.com/your-username/DigitalMe/discussions)

---

🤖 **Автоматизированная система CI/CD для DigitalMe**