# План: Lazy Secrets Loading + Caching (Уровень 2 оптимизации)

**Статус**: 📋 TODO (Technical Debt)
**Приоритет**: P2 (Medium) - Не блокирует, но улучшит производительность
**Время**: ~20-30 минут
**Зависимости**: Уровень 1 (Non-blocking validation) - ✅ COMPLETE

---

## 🎯 Цель

Дальнейшая оптимизация SecretsManagementService:
- **Lazy Loading**: Загружать секреты только при первом использовании
- **Caching**: Кэшировать с TTL чтобы не перепроверять каждый раз
- **Runtime Reload**: Подхватывать изменения без перезапуска (опционально)

---

## 📊 Текущее состояние (после Уровня 1)

**✅ Достигнуто:**
- Test environment: валидация пропускается (<1 сек startup)
- Production: background валидация (не блокирует startup)
- Убраны Process.Start (powershell/cmd) - ускорение 20000x

**❌ Что ещё можно улучшить:**
- GetSecret() вызывается для КАЖДОГО запроса к секрету
- Нет кэширования - каждый раз Environment.GetEnvironmentVariable()
- ValidateSecrets() проверяет ВСЕ секреты сразу (даже если не используются)

---

## 🔧 Уровень 2: Lazy Loading + Caching

### 2.1 Добавить in-memory кэш с TTL (10 мин)

**Файл**: `src/DigitalMe/Services/Configuration/SecretsManagementService.cs`

```csharp
using System.Collections.Concurrent;

public class SecretsManagementService : ISecretsManagementService
{
    // Add cache fields
    private readonly ConcurrentDictionary<string, CachedSecret> _cache = new();
    private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(5);

    private record CachedSecret(string? Value, DateTime CachedAt);

    public string? GetSecret(string secretKey, string? environmentVariableName = null)
    {
        // Check cache first
        if (_cache.TryGetValue(secretKey, out var cached))
        {
            var age = DateTime.UtcNow - cached.CachedAt;
            if (age < _cacheTtl)
            {
                _logger.LogDebug("Secret '{SecretKey}' retrieved from cache (age: {Age})",
                    secretKey, age);
                return cached.Value;
            }
            else
            {
                _logger.LogDebug("Secret '{SecretKey}' cache expired (age: {Age} > TTL: {TTL})",
                    secretKey, age, _cacheTtl);
            }
        }

        // Load secret from sources (existing logic)
        var secret = LoadSecretFromSources(secretKey, environmentVariableName);

        // Cache result (even if null - avoids repeated lookups)
        _cache[secretKey] = new CachedSecret(secret, DateTime.UtcNow);

        return secret;
    }

    private string? LoadSecretFromSources(string secretKey, string? environmentVariableName)
    {
        // Move existing GetSecret() logic here
        // Priority 1: Configuration
        var configValue = _configuration[secretKey];
        if (!string.IsNullOrWhiteSpace(configValue) && !IsPlaceholderValue(configValue))
            return configValue;

        // Priority 2: Environment variable
        if (!string.IsNullOrEmpty(environmentVariableName))
        {
            var envValue = GetEnvironmentVariableCrossPlatform(environmentVariableName);
            if (!string.IsNullOrWhiteSpace(envValue))
                return envValue;
        }

        // Priority 3: Auto-detect env var
        var autoEnvVar = ConvertToEnvironmentVariableName(secretKey);
        var autoEnvValue = GetEnvironmentVariableCrossPlatform(autoEnvVar);
        if (!string.IsNullOrWhiteSpace(autoEnvValue))
            return autoEnvValue;

        return null;
    }

    // Add cache invalidation method
    public void InvalidateCache()
    {
        _cache.Clear();
        _logger.LogInformation("Secrets cache invalidated");
    }

    public void InvalidateSecret(string secretKey)
    {
        _cache.TryRemove(secretKey, out _);
        _logger.LogDebug("Secret '{SecretKey}' removed from cache", secretKey);
    }
}
```

**Результат**:
- Первый вызов GetSecret(): ~1 мс (Environment.GetEnvironmentVariable)
- Последующие вызовы: <0.01 мс (cache lookup)
- Cache TTL 5 минут - баланс между актуальностью и производительностью

---

### 2.2 Lazy секреты - загружать только при использовании (10 мин)

**Проблема**: ValidateSecrets() проверяет ВСЕ секреты (critical + recommended)

**Решение**: Разделить на уровни:

```csharp
public SecretsValidationResult ValidateCriticalSecrets()
{
    // Validate only critical secrets (Anthropic:ApiKey, JWT:Key)
    var criticalSecrets = new Dictionary<string, string>
    {
        { "Anthropic:ApiKey", "ANTHROPIC_API_KEY" },
        { "JWT:Key", "JWT_KEY" }
    };

    var result = new SecretsValidationResult { IsValid = true };

    foreach (var (key, envVar) in criticalSecrets)
    {
        var secret = GetSecret(key, envVar); // Uses cache
        if (string.IsNullOrWhiteSpace(secret))
        {
            result.MissingSecrets.Add($"{key} (or {envVar})");
            result.IsValid = false;
        }
    }

    return result;
}

public SecretsValidationResult ValidateOptionalSecrets()
{
    // Validate optional secrets only when needed
    var optionalSecrets = new Dictionary<string, string>
    {
        { "Integrations:GitHub:PersonalAccessToken", "GITHUB_TOKEN" },
        { "Integrations:Telegram:BotToken", "TELEGRAM_BOT_TOKEN" },
        { "Integrations:Google:ClientSecret", "GOOGLE_CLIENT_SECRET" }
    };

    var result = new SecretsValidationResult { IsValid = true };

    foreach (var (key, envVar) in optionalSecrets)
    {
        var secret = GetSecret(key, envVar); // Uses cache
        if (string.IsNullOrWhiteSpace(secret))
        {
            result.Warnings.Add($"Optional secret '{key}' not configured");
        }
    }

    return result;
}

// Keep existing ValidateSecrets() for backward compatibility
public SecretsValidationResult ValidateSecrets()
{
    var critical = ValidateCriticalSecrets();
    var optional = ValidateOptionalSecrets();

    // Merge results
    return new SecretsValidationResult
    {
        IsValid = critical.IsValid,
        MissingSecrets = critical.MissingSecrets,
        Warnings = critical.Warnings.Concat(optional.Warnings).ToList(),
        // ...
    };
}
```

**Результат**:
- Background validation проверяет только critical секреты (2 instead of 6)
- Optional секреты проверяются lazy (когда нужны)
- Ускорение background validation: 3x

---

### 2.3 Runtime Reload (опционально, 10 мин)

**Задача**: Подхватывать изменения env variables без перезапуска

```csharp
public void StartBackgroundReload(IHostApplicationLifetime lifetime)
{
    lifetime.ApplicationStarted.Register(() =>
    {
        _ = Task.Run(async () =>
        {
            _logger.LogInformation("🔄 Background secrets reload started (interval: 5 min)");

            while (!lifetime.ApplicationStopping.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), lifetime.ApplicationStopping);

                    if (lifetime.ApplicationStopping.IsCancellationRequested)
                        break;

                    _logger.LogDebug("🔄 Reloading secrets cache...");
                    InvalidateCache();

                    // Re-validate critical secrets
                    var validation = ValidateCriticalSecrets();
                    if (!validation.IsValid)
                    {
                        _logger.LogWarning("⚠️ Critical secrets validation failed after reload");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Background secrets reload failed");
                }
            }

            _logger.LogInformation("🛑 Background secrets reload stopped");
        }, lifetime.ApplicationStopping);
    });
}
```

**Регистрация в Program.cs**:
```csharp
// After app.Build()
var secretsService = app.Services.GetRequiredService<ISecretsManagementService>();
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
secretsService.StartBackgroundReload(lifetime);
```

**Результат**:
- Env variables изменяются → через 5 минут подхватываются
- Не нужен перезапуск приложения
- Graceful shutdown (cancellation token)

---

## 📈 Ожидаемые улучшения

| Операция | Before (Уровень 1) | After (Уровень 2) | Improvement |
|----------|-------------------|------------------|-------------|
| **GetSecret() (first call)** | ~1 ms | ~1 ms | Same |
| **GetSecret() (cached)** | ~1 ms | <0.01 ms | **100x** |
| **ValidateSecrets() (all)** | 6 секретов | 2 critical + 4 lazy | **3x faster** |
| **Runtime reload** | Manual restart | Auto every 5 min | ♾️ |

---

## 🧪 Тестирование

### Unit Tests (15 мин)

**Файл**: `tests/DigitalMe.Tests.Unit/Services/SecretsManagementServiceCachingTests.cs`

```csharp
[Fact]
public void GetSecret_Should_Cache_Results()
{
    // Arrange
    var service = CreateService();

    // Act - First call
    var value1 = service.GetSecret("TestKey");
    var value2 = service.GetSecret("TestKey");

    // Assert - Same value returned without re-querying
    value1.Should().Be(value2);
    // Verify Environment.GetEnvironmentVariable called only once
}

[Fact]
public void GetSecret_Should_Expire_Cache_After_TTL()
{
    // Arrange
    var service = CreateService(cacheTtl: TimeSpan.FromMilliseconds(100));

    // Act
    var value1 = service.GetSecret("TestKey");
    Thread.Sleep(150); // Wait for TTL expiration
    var value2 = service.GetSecret("TestKey");

    // Assert - Cache expired, value re-queried
}

[Fact]
public void InvalidateCache_Should_Clear_All_Cached_Secrets()
{
    // Arrange
    var service = CreateService();
    service.GetSecret("Key1");
    service.GetSecret("Key2");

    // Act
    service.InvalidateCache();

    // Assert - Next calls re-query env variables
}
```

---

## 🚀 Deployment Strategy

**Backward Compatibility**:
- Существующий GetSecret() API не меняется
- Caching прозрачен для вызывающего кода
- ValidateSecrets() сохраняет существующее поведение

**Rollout**:
1. Unit tests для caching logic
2. Merge в develop
3. Monitoring в production (cache hit rate logs)
4. Опционально: Runtime reload можно включить через feature flag

---

## 📋 Чеклист

- [ ] Добавить ConcurrentDictionary cache с TTL
- [ ] Рефакторить GetSecret() → LoadSecretFromSources() + caching
- [ ] Добавить InvalidateCache() / InvalidateSecret() методы
- [ ] Разделить ValidateSecrets() → ValidateCriticalSecrets() + ValidateOptionalSecrets()
- [ ] Добавить StartBackgroundReload() (опционально)
- [ ] Unit tests для caching logic
- [ ] Update ISecretsManagementService interface (если нужны новые методы)
- [ ] Документация в README (cache behavior)
- [ ] Commit & merge в develop

---

## 💡 Future Enhancements (Уровень 3+)

- **Distributed Cache**: Redis для multi-instance scenarios
- **Secret Rotation Detection**: Автоматическое определение изменений
- **Health Check Integration**: Expose cache stats в /health endpoint
- **Metrics**: Prometheus metrics для cache hit rate, TTL, etc.

---

**Created**: 2025-09-30
**Status**: 📋 TODO (после расследования WebApplicationFactory)