# Database Migration Plan: digitalme_clean → digitalme_fixed

## Проблема
EnsureCreated() в Entity Framework Core игнорирует Fluent API mappings (HasColumnName), создавая столбцы по именам C# properties:
- `PersonalityProfile.IsActive` → столбец `isactive` (неправильно)
- Вместо правильного: `IsActive` (как указано в HasColumnName)

## Решение
1. ✅ **Исправлен DatabaseMigrationService**: Заменен EnsureCreated() на MigrateAsync() для PostgreSQL
2. 🔄 **Создана новая БД**: `digitalme_fixed` с правильными именами столбцов
3. 🔄 **Подготовлен план миграции** данных из старой БД в новую

## Этапы выполнения

### Этап 1: Деплоймент исправления ✅
- [x] Исправлен DatabaseMigrationService.cs
- [x] Обновлен connection string на digitalme_fixed
- [x] Запущен деплоймент с исправлением

### Этап 2: Проверка работоспособности 🔄
- [ ] Дождаться завершения деплоймента
- [ ] Проверить health endpoint: `curl https://digitalme-api-llig7ks2ca-uc.a.run.app/health`
- [ ] Ожидаемый результат: `Healthy` (вместо `Unhealthy`)
- [ ] Проверить логи на успешное создание таблиц с правильными именами

### Этап 3: Перенос данных (если нужно)
- [ ] Оценить объем данных в digitalme_clean
- [ ] Если есть важные данные:
  - [ ] Выполнить SQL скрипт `scripts/migrate-data-to-fixed-db.sql`
  - [ ] Проверить целостность данных
- [ ] Если данных нет или они тестовые:
  - [ ] Пропустить миграцию, использовать свежую БД

### Этап 4: Финальное тестирование
- [ ] Проверить registration endpoint
- [ ] Проверить login endpoint
- [ ] Проверить frontend authentication flow
- [ ] Проверить создание conversations
- [ ] Проверить personality profile functionality

### Этап 5: Производственное переключение
- [ ] Убедиться что digitalme_fixed работает стабильно
- [ ] Обновить документацию с новым именем БД
- [ ] Опционально: Удалить digitalme_clean после периода наблюдения

## Техническая детализация

### Исправление в коде
```csharp
// ❌ Старый код (неправильно)
await context.Database.EnsureCreatedAsync();

// ✅ Новый код (правильно)
await context.Database.MigrateAsync();
```

### Connection Strings
```
// Старая БД (с неправильными столбцами)
Database=digitalme_clean

// Новая БД (с правильными столбцами)
Database=digitalme_fixed
```

### Проверка исправления
```sql
-- Этот запрос должен работать в digitalme_fixed
SELECT COUNT(*) FROM "PersonalityProfiles" WHERE "IsActive" = true;

-- И НЕ работать в digitalme_clean (ошибка: column "isactive" does not exist)
```

## Риски и митигация

### Риск: Потеря данных
**Митигация**:
- Создаем новую БД вместо изменения старой
- Подготовлен SQL скрипт для переноса данных
- Старая БД остается нетронутой до подтверждения

### Риск: Простой приложения
**Митигация**:
- Переключение через environment variable
- Быстрый rollback возможен изменением connection string

### Риск: Несовместимость данных
**Митигация**:
- Тестирование на integration tests
- Поэтапная проверка функциональности

## Критерии успеха

### ✅ Успешное исправление:
1. Health endpoint возвращает `Healthy`
2. В логах нет ошибок `column "isactive" does not exist`
3. PersonalityProfiles queries работают корректно
4. Identity system функционирует
5. Frontend может подключиться к API

### ❌ Если исправление не сработало:
1. Rollback на digitalme_clean
2. Дополнительная диагностика проблемы
3. Альтернативные подходы к исправлению

## Мониторинг

### Логи для отслеживания:
- `🔧 CRITICAL FIX: EnsureCreated ignores HasColumnName mappings`
- `✅ PostgreSQL database migrated successfully with correct column names`
- Health check статус изменения
- Migration success/failure events

### Метрики:
- Health endpoint response time
- API error rates
- Database connection успешность
- Migration completion time

## Контакты и эскалация

При проблемах с миграцией:
1. Проверить Cloud Run logs
2. Проверить Cloud SQL connections
3. При необходимости - rollback на предыдущую конфигурацию
4. Документировать проблемы для дальнейшего анализа