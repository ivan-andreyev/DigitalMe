# Manual Refactoring Specifications

**⬅️ Back to:** [MAIN_PLAN.md](../../MAIN_PLAN.md) - Central entry point for all plans

**📋 Parent Plan:** [08-HYBRID-CODE-QUALITY-RECOVERY-PLAN-Architecture.md](../08-HYBRID-CODE-QUALITY-RECOVERY-PLAN-Architecture.md) - Architecture diagram

**📋 Related Files:**
- [01-automated-tooling-config.md](01-automated-tooling-config.md) - Automated tooling config
- [03-validation-checklist.md](03-validation-checklist.md) - Validation checklist

## Цель раздела
Выполнить архитектурные рефакторинги для исправления 11 нарушений SOLID принципов, разделения больших файлов и реорганизации тестовой структуры.

## Входные зависимости
- [ ] Phase 1 автоматические исправления завершены
- [ ] StyleCop violations снижены до ≤10
- [ ] 154/154 тестов проходят после автоматических изменений

## Задача 1: CustomWebApplicationFactory рефакторинг

### Текущие проблемы (SRP/DIP нарушения)
```csharp
// ТЕКУЩЕЕ СОСТОЯНИЕ - нарушает SRP
public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    // 1. Конфигурация БД (отдельная ответственность)
    protected override void ConfigureWebHost(IWebHostBuilder builder) { }
    
    // 2. Настройка DI (отдельная ответственность)  
    private void ConfigureServices(IServiceCollection services) { }
    
    // 3. Инициализация тестовых данных (отдельная ответственность)
    private void SeedTestData() { }
    
    // 4. Управление жизненным циклом тестов (отдельная ответственность)
    public override async ValueTask DisposeAsync() { }
}
```

### Целевая архитектура (SOLID-compliant)
```csharp
// НОВАЯ АРХИТЕКТУРА - разделение ответственностей

// TODO: Интерфейс для конфигурации тестовой БД
public interface ITestDatabaseConfiguration  
{
    void ConfigureDatabase(IServiceCollection services);
    Task InitializeDatabaseAsync();
    Task CleanupDatabaseAsync();
}

// TODO: Интерфейс для настройки DI в тестах
public interface ITestServiceConfiguration
{
    void ConfigureServices(IServiceCollection services);
    void RegisterMockServices(IServiceCollection services);
}

// TODO: Интерфейс для управления тестовыми данными
public interface ITestDataSeeder
{
    Task SeedAsync();
    Task CleanupAsync();
}

// TODO: Интерфейс для управления тестовой средой
public interface ITestEnvironmentManager
{
    Task SetupAsync();
    Task TeardownAsync();
}

// TODO: Рефакторинг основного класса
public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    private readonly ITestDatabaseConfiguration _dbConfig;
    private readonly ITestServiceConfiguration _serviceConfig;
    private readonly ITestDataSeeder _dataSeeder;
    private readonly ITestEnvironmentManager _environmentManager;
    
    public CustomWebApplicationFactory(
        ITestDatabaseConfiguration dbConfig,
        ITestServiceConfiguration serviceConfig, 
        ITestDataSeeder dataSeeder,
        ITestEnvironmentManager environmentManager)
    {
        _dbConfig = dbConfig;
        _serviceConfig = serviceConfig;
        _dataSeeder = dataSeeder;
        _environmentManager = environmentManager;
    }
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            _dbConfig.ConfigureDatabase(services);
            _serviceConfig.ConfigureServices(services);
        });
    }
    
    // TODO: Lifecycle management через dependency injection
}
```

### Реализация разделенных классов
```csharp
// TODO: Конкретная реализация конфигурации БД
public class SqliteTestDatabaseConfiguration : ITestDatabaseConfiguration
{
    // TODO: SQLite in-memory database setup
    // TODO: Entity Framework configuration
    // TODO: Database migration application
}

// TODO: Конкретная реализация настройки сервисов
public class StandardTestServiceConfiguration : ITestServiceConfiguration  
{
    // TODO: Standard service registrations
    // TODO: Mock service replacements
    // TODO: Test-specific configurations
}

// TODO: Конкретная реализация seeding данных
public class DefaultTestDataSeeder : ITestDataSeeder
{
    // TODO: User data seeding
    // TODO: Profile data seeding  
    // TODO: Interview data seeding
}
```

## Задача 2: Идентификация и разделение больших файлов

### Поиск файлов превышающих лимиты
```bash
# TODO: Выполнить анализ размеров файлов
find src/ -name "*.cs" | xargs wc -l | sort -nr | head -20

# Критерии для разделения:
# - >500 строк = mandatory split
# - >300 строк = review for logical split
# - Multiple responsibilities in one file = split regardless of size
```

### Стратегия разделения по типам
```csharp
// TODO: Пример - если найден большой контроллер
public class LargeApiController : ControllerBase
{
    // TODO: Разделить на:
    // - UserManagementController (user CRUD operations)
    // - ProfileController (profile-specific operations)  
    // - InterviewController (interview management)
    // - AnalyticsController (analytics and reporting)
}

// TODO: Пример - если найден большой сервис
public class LargeService 
{
    // TODO: Разделить на:
    // - CoreBusinessService (main business logic)
    // - ValidationService (validation logic)
    // - TransformationService (data transformation)
    // - NotificationService (notifications)
}
```

### Принципы разделения
```csharp
// TODO: Каждый новый класс должен следовать SRP
// - Одна четко определенная ответственность
// - Minimal public interface
// - Clear dependencies via constructor injection
// - Maximum 10 public methods per class

// TODO: Пример правильного разделения
public interface IUserProfileService
{
    Task<UserProfile> GetProfileAsync(int userId);
    Task UpdateProfileAsync(UserProfile profile);
    Task<bool> ValidateProfileAsync(UserProfile profile);
}

public class UserProfileService : IUserProfileService
{
    private readonly IUserRepository _userRepository;
    private readonly IProfileValidator _validator;
    
    public UserProfileService(IUserRepository userRepository, IProfileValidator validator)
    {
        _userRepository = userRepository;
        _validator = validator;
    }
    
    // TODO: Implement methods with single responsibility
}
```

## Задача 3: Реорганизация тестовой структуры

### Текущие проблемы тестовой архитектуры
```
Tests/
├── SomeIntegrationTests.cs    (混杂 integration tests)
├── SomeUnitTests.cs          (混杂 unit tests)  
├── CustomWebApplicationFactory.cs (тестовая инфраструктура смешана с тестами)
└── TestUtilities.cs          (общие утилиты в корне)
```

### Целевая структура тестов
```
Tests/
├── Infrastructure/           
│   ├── CustomWebApplicationFactory.cs
│   ├── TestDatabaseConfiguration.cs
│   ├── TestServiceConfiguration.cs  
│   ├── TestDataSeeder.cs
│   └── BaseTestClasses/
│       ├── BaseIntegrationTest.cs
│       ├── BaseUnitTest.cs
│       └── BaseWebTest.cs
├── Integration/
│   ├── Controllers/
│   │   ├── UserControllerTests.cs
│   │   ├── ProfileControllerTests.cs
│   │   └── InterviewControllerTests.cs
│   ├── Services/
│   │   ├── UserServiceIntegrationTests.cs
│   │   └── ProfileServiceIntegrationTests.cs
│   └── Database/
│       ├── UserRepositoryTests.cs
│       └── ProfileRepositoryTests.cs
├── Unit/
│   ├── Controllers/
│   │   ├── UserControllerUnitTests.cs
│   │   └── ProfileControllerUnitTests.cs
│   ├── Services/
│   │   ├── UserServiceUnitTests.cs
│   │   └── ProfileServiceUnitTests.cs
│   └── Models/
│       ├── UserProfileModelTests.cs
│       └── InterviewDataModelTests.cs
└── Utilities/
    ├── TestDataFactory.cs
    ├── MockServiceFactory.cs
    └── AssertionHelpers.cs
```

### Базовые классы для тестов
```csharp
// TODO: Базовый класс для интеграционных тестов
public abstract class BaseIntegrationTest : IClassFixture<CustomWebApplicationFactory<Program>>
{
    protected readonly CustomWebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;
    
    protected BaseIntegrationTest(CustomWebApplicationFactory<Program> factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }
    
    // TODO: Common setup methods
    // TODO: Common assertion helpers
    // TODO: Common data cleanup
}

// TODO: Базовый класс для unit тестов
public abstract class BaseUnitTest
{
    protected readonly Mock<ILogger> MockLogger;
    // TODO: Common mocks setup
    // TODO: Common test utilities
}

// TODO: Базовый класс для web тестов
public abstract class BaseWebTest : BaseIntegrationTest
{
    // TODO: HTTP client utilities
    // TODO: Authentication helpers
    // TODO: Response assertion helpers
}
```

### Фабрики тестовых данных
```csharp
// TODO: Централизованное создание тестовых данных
public static class TestDataFactory
{
    public static UserProfile CreateValidUserProfile() 
    {
        // TODO: Create consistent test data
    }
    
    public static InterviewData CreateSampleInterviewData()
    {
        // TODO: Create sample interview responses
    }
    
    public static List<T> CreateCollection<T>(int count, Func<int, T> factory)
    {
        // TODO: Generic collection creation
    }
}

// TODO: Централизованное создание mock объектов
public static class MockServiceFactory  
{
    public static Mock<IUserRepository> CreateUserRepositoryMock()
    {
        // TODO: Consistent mock setup
    }
    
    public static Mock<IProfileService> CreateProfileServiceMock()
    {
        // TODO: Consistent mock behavior
    }
}
```

## Критерии завершения всех задач

### Архитектурные критерии
- [ ] CustomWebApplicationFactory разделен на 4+ специализированных класса
- [ ] Все классы следуют Single Responsibility Principle  
- [ ] Dependencies injected through interfaces (DIP compliance)
- [ ] Нет файлов >500 строк кода
- [ ] Циклическая сложность <10 для всех методов

### Тестовые критерии  
- [ ] Тесты организованы по логическим категориям
- [ ] Базовые классы устраняют дублирование кода
- [ ] Тестовые данные создаются через фабрики
- [ ] 154/154 тестов продолжают проходить
- [ ] Время выполнения тестов не увеличилось >20%

### Качественные критерии
- [ ] Code review готов (readable, maintainable)
- [ ] Документация обновлена для новых классов
- [ ] Git history сохраняет логические коммиты
- [ ] Нет breaking changes в public API

## Временная оценка
**Общее время**: 1-2 дня
- **Задача 1** (CustomWebApplicationFactory): 4-6 часов
- **Задача 2** (Large files splitting): 2-4 часа  
- **Задача 3** (Test restructuring): 4-6 часов
- **Testing & validation**: 2-3 часа

## План выполнения
**День 1**: Задача 1 + частично Задача 2
**День 2**: Завершение Задачи 2 + Задача 3 + валидация