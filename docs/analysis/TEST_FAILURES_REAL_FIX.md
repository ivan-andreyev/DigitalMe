# Настоящее решение проблем с тестами

## Расследование

### 1. WebNavigationServiceTests - РЕАЛЬНАЯ проблема

**НЕ ПРОБЛЕМА:** Тесты используют реальный Playwright
**РЕАЛЬНАЯ ПРОБЛЕМА:** В CI/CD не установлены Playwright браузеры!

WebNavigationService - это интеграционный компонент, который ДОЛЖЕН тестироваться с реальным браузером. Это не юнит-тесты, это интеграционные тесты, и они ДОЛЖНЫ использовать реальный Playwright.

#### Доказательства:
1. Microsoft.Playwright установлен как NuGet пакет (версия 1.48.0)
2. Тесты инициализируют реальный браузер: `await _service.InitializeBrowserAsync()`
3. Тесты висят потому что браузеры НЕ УСТАНОВЛЕНЫ в CI/CD окружении

#### Настоящее решение:
Добавить установку Playwright браузеров в GitHub Actions workflow:

```yaml
- name: 🎭 Install Playwright Browsers
  run: dotnet tool install --global Microsoft.Playwright.CLI && playwright install chromium
```

### 2. TelegramService DI конфликт

**Проблема:** В Program.cs строка 244 регистрирует HttpClient для TelegramService:
```csharp
builder.Services.AddHttpClient<TelegramService>();
```

Но конструктор TelegramService принимает только ILogger<TelegramService>, а не HttpClient!

**Решение:** Удалить строку 244 из Program.cs, оставить только правильную регистрацию:
```csharp
builder.Services.AddScoped<ITelegramService, TelegramService>();
```

## План ЧЕСТНОГО исправления

### 1. Исправить GitHub Actions workflows
Добавить установку Playwright браузеров после restore dependencies:
- develop-ci.yml
- master-ci.yml
- pull-request.yml

### 2. Исправить Program.cs
Удалить конфликтующую строку 244 с AddHttpClient<TelegramService>()

### 3. Проверить локально
```bash
# Установить браузеры локально если не установлены
dotnet tool install --global Microsoft.Playwright.CLI
playwright install chromium

# Запустить тесты
dotnet test
```

## Почему это ЧЕСТНОЕ решение

1. **Не маскируем проблему** - не заменяем интеграционные тесты на моки
2. **Решаем корневую причину** - устанавливаем недостающие зависимости
3. **Сохраняем покрытие** - все тесты остаются как есть
4. **Никаких Skip атрибутов** - все тесты работают
5. **Никаких заглушек** - реальное тестирование с реальным браузером

## Ожидаемый результат

После этих исправлений:
- ✅ WebNavigationServiceTests будут работать с реальным Chromium
- ✅ TelegramService DI конфликт будет решен
- ✅ Все тесты будут зелеными
- ✅ CI/CD pipeline будет проходить успешно