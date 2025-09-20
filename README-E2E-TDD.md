# E2E и TDD Workflow для DigitalMe

## Быстрый старт для TDD

### 1. Локальная среда с PostgreSQL
```bash
# Запуск тестовой среды (PostgreSQL + API + Web)
docker-compose -f docker-compose.test.yml up -d

# Проверка что всё запустилось
curl http://localhost:5001/health/simple
curl http://localhost:8081/

# TDD цикл - Unit тесты (быстрые)
dotnet watch test tests/DigitalMe.Tests.Unit --filter "Category=Unit"

# Integration тесты с реальной PostgreSQL
dotnet test tests/DigitalMe.Tests.Integration --filter "Category=Integration"

# E2E тесты против локальной среды
TEST_ENV=local dotnet test tests/DigitalMe.Tests.E2E --filter "Category=E2E"
```

### 2. Остановка среды
```bash
# Остановка и очистка
docker-compose -f docker-compose.test.yml down -v
```

## E2E тестирование

### Конфигурируемые окружения
- **local**: `http://localhost:5001` (docker-compose.test.yml)
- **production**: `https://digitalme-api-llig7ks2ca-uc.a.run.app`
- **staging**: (планируется)

### Запуск E2E тестов

```bash
# Против локального окружения
TEST_ENV=local dotnet test tests/DigitalMe.Tests.E2E

# Против production (осторожно!)
TEST_ENV=production dotnet test tests/DigitalMe.Tests.E2E

# Только критические health checks
dotnet test tests/DigitalMe.Tests.E2E --filter "Category=E2E&Environment=All"

# Только production-specific тесты
dotnet test tests/DigitalMe.Tests.E2E --filter "Environment=Production"
```

## CI/CD Pipeline

### Deploy workflow теперь включает:
1. **Unit Tests** - быстрые тесты логики
2. **Integration Tests** - тесты с in-memory DB
3. **Deploy** - развертывание в Cloud Run
4. **E2E Tests** - проверка реального production

### Результат E2E тестов:
- ✅ **SUCCESS**: Production работает правильно
- ❌ **FAIL**: Есть проблемы в production, требуется диагностика

## TDD Workflow

### 1. Red-Green-Refactor цикл
```bash
# 1. RED - написать failing test
# 2. GREEN - минимальный код для прохождения теста
dotnet watch test tests/DigitalMe.Tests.Unit

# 3. REFACTOR - улучшить код
# 4. Integration test
dotnet test tests/DigitalMe.Tests.Integration

# 5. E2E test (локально)
TEST_ENV=local dotnet test tests/DigitalMe.Tests.E2E
```

### 2. Диагностика production проблем
```bash
# Сначала воспроизвести локально
docker-compose -f docker-compose.test.yml up -d
TEST_ENV=local dotnet test tests/DigitalMe.Tests.E2E

# Затем написать failing E2E test для бага
# Исправить bug
# Проверить что тест проходит локально
# Deploy и проверить production
```

## Структура тестов

```
tests/
├── DigitalMe.Tests.Unit/          # Быстрые unit tests
├── DigitalMe.Tests.Integration/   # Integration tests (in-memory DB)
└── DigitalMe.Tests.E2E/          # E2E tests (реальные HTTP вызовы)
    ├── HealthCheckE2ETests.cs    # Базовая доступность
    ├── AuthenticationE2ETests.cs # Auth flow
    └── E2ETestConfig.cs          # Конфигурация окружений
```

## Переменные окружения

### Для локальной разработки:
- `TEST_ENV=local` - использовать docker-compose.test.yml URLs
- `TEST_ENV=production` - использовать production URLs

### Для CI/CD:
- Автоматически устанавливается `TEST_ENV=production` после deploy

## Troubleshooting

### Локальные E2E тесты не проходят:
```bash
# Проверить что среда запущена
docker-compose -f docker-compose.test.yml ps

# Проверить логи
docker-compose -f docker-compose.test.yml logs digitalme-api-test

# Пересоздать среду
docker-compose -f docker-compose.test.yml down -v
docker-compose -f docker-compose.test.yml up -d
```

### Production E2E тесты не проходят:
- Проверить статус в Cloud Run console
- Проверить логи в Google Cloud Logging
- Возможна проблема с cold start - тесты включают retry logic