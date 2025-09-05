# Database Design Specification 💾

> **Plan Type**: TECHNICAL | **LLM Ready**: PARTIAL | **Reading Time**: 25 мин  
> **Prerequisites**: `../01-conceptual/01-02-technical-foundation.md` | **Next**: `02-02-mcp-integration.md`

## EF Core DbContext

**Файл**: `src/DigitalMe.Data/Context/DigitalMeContext.cs`

```csharp
public class DigitalMeContext : DbContext
{
    // Личность и профиль
    public DbSet<PersonalityProfile> PersonalityProfiles { get; set; }
    public DbSet<PersonalityTrait> PersonalityTraits { get; set; }
    
    // Коммуникация
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Message> Messages { get; set; }
    
    // Интеграции
    public DbSet<TelegramMessage> TelegramMessages { get; set; }
    public DbSet<CalendarEvent> CalendarEvents { get; set; }
    public DbSet<Contact> Contacts { get; set; }
    public DbSet<GitHubRepository> GitHubRepositories { get; set; }
    public DbSet<EmailMessage> EmailMessages { get; set; }
    
    // Система
    public DbSet<AgentAction> AgentActions { get; set; }
    public DbSet<IntegrationLog> IntegrationLogs { get; set; }
}
```

## Основные сущности

### PersonalityProfile
**Цель**: Хранение профиля личности Ивана
```csharp
public class PersonalityProfile
{
    public Guid Id { get; set; }
    public string Name { get; set; }              // "Ivan"
    public string Description { get; set; }       // Биография
    public Dictionary<string, object> Traits { get; set; }  // JSON
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public ICollection<PersonalityTrait> PersonalityTraits { get; set; }
}
```

### Conversation & Message
**Цель**: История всех диалогов с агентом
```csharp
public class Conversation
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Platform { get; set; }          // "Telegram", "Web", "Mobile"
    public string UserId { get; set; }            // ID пользователя на платформе
    public DateTime StartedAt { get; set; }
    
    public ICollection<Message> Messages { get; set; }
}

public class Message
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public string Role { get; set; }              // "user", "assistant"
    public string Content { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
    public DateTime Timestamp { get; set; }
    
    public Conversation Conversation { get; set; }
}
```

### TelegramMessage
**Цель**: Синхронизация сообщений Telegram
```csharp
public class TelegramMessage
{
    public Guid Id { get; set; }
    public long TelegramMessageId { get; set; }   // ID в Telegram API
    public long ChatId { get; set; }
    public string FromUsername { get; set; }
    public string Text { get; set; }
    public DateTime MessageDate { get; set; }
    public bool IsFromIvan { get; set; }          // Отправлено Иваном
    public bool ProcessedByAgent { get; set; }    // Обработано агентом
}
```

### CalendarEvent
**Цель**: Синхронизация событий Google Calendar
```csharp
public class CalendarEvent
{
    public Guid Id { get; set; }
    public string GoogleEventId { get; set; }     // ID в Google Calendar
    public string CalendarId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Location { get; set; }
    public DateTime LastSyncAt { get; set; }
}
```

## Индексы и производительность

**Файл**: `src/DigitalMe.Data/Context/DigitalMeContext.cs:OnModelCreating`

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Индексы для поиска сообщений
    modelBuilder.Entity<Message>()
        .HasIndex(m => m.ConversationId);
    
    modelBuilder.Entity<Message>()
        .HasIndex(m => m.Timestamp);
        
    // Индексы для Telegram интеграции
    modelBuilder.Entity<TelegramMessage>()
        .HasIndex(t => t.TelegramMessageId)
        .IsUnique();
        
    modelBuilder.Entity<TelegramMessage>()
        .HasIndex(t => new { t.ChatId, t.MessageDate });
        
    // Индексы для календарных событий
    modelBuilder.Entity<CalendarEvent>()
        .HasIndex(c => c.GoogleEventId)
        .IsUnique();
        
    modelBuilder.Entity<CalendarEvent>()
        .HasIndex(c => new { c.StartTime, c.EndTime });
}
```

## Миграции

**Команды создания миграций**:
```bash
# Создание начальной миграции
dotnet ef migrations add InitialCreate --project src/DigitalMe.Data --startup-project src/DigitalMe.API

# Обновление базы данных
dotnet ef database update --project src/DigitalMe.Data --startup-project src/DigitalMe.API
```

**Файл миграции**: `src/DigitalMe.Data/Migrations/20250827000000_InitialCreate.cs`

## Connection String

**Файл**: `src/DigitalMe.API/appsettings.json:ConnectionStrings`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host={{DB_HOST}};Database=digitalme;Username={{DB_USER}};Password={{DB_PASSWORD}};SSL Mode=Require;"
  }
}
```

## Репозиторий паттерн

**Файл**: `src/DigitalMe.Data/Repositories/IPersonalityRepository.cs`
```csharp
public interface IPersonalityRepository
{
    Task<PersonalityProfile> GetProfileAsync(string name);
    Task<PersonalityProfile> UpdateProfileAsync(PersonalityProfile profile);
    Task<IEnumerable<PersonalityTrait>> GetTraitsAsync(Guid profileId);
}
```

---

### 🔙 Navigation
- **← Parent Plan**: [Main Plan](../../00-MAIN_PLAN.md)
- **← Technical Coordinator**: [../02-technical.md](../02-technical.md)
- **← Architecture Overview**: [../00-ARCHITECTURE_OVERVIEW.md](../00-ARCHITECTURE_OVERVIEW.md)

---

**Следующий план**: [MCP интеграция](02-02-mcp-integration.md)