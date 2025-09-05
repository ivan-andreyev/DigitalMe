# Day 2: Database Context Setup & Migrations

**Родительский план**: [../03-02-01-week1-foundation.md](../03-02-01-week1-foundation.md)

## Day 2: Database Context Setup (3 hours)

**Файл**: `src/DigitalMe.Data/Context/DigitalMeContext.cs:1-60`
```csharp
using Microsoft.EntityFrameworkCore;
using DigitalMe.Data.Entities;

namespace DigitalMe.Data.Context;

public class DigitalMeContext : DbContext
{
    public DigitalMeContext(DbContextOptions<DigitalMeContext> options) : base(options)
    {
    }
    
    // Core entities
    public DbSet<PersonalityProfile> PersonalityProfiles { get; set; } = default!;
    public DbSet<PersonalityTrait> PersonalityTraits { get; set; } = default!;
    
    // Communication
    public DbSet<Conversation> Conversations { get; set; } = default!;
    public DbSet<Message> Messages { get; set; } = default!;
    
    // Integrations
    public DbSet<TelegramMessage> TelegramMessages { get; set; } = default!;
    public DbSet<CalendarEvent> CalendarEvents { get; set; } = default!;
    public DbSet<Contact> Contacts { get; set; } = default!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // PersonalityProfile configuration - line 28-35
        modelBuilder.Entity<PersonalityProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
        
        // PersonalityTrait configuration - line 36-45
        modelBuilder.Entity<PersonalityTrait>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PersonalityProfileId, e.Name }).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Value).HasColumnType("jsonb");
            entity.HasOne(e => e.PersonalityProfile)
                  .WithMany(e => e.PersonalityTraits)
                  .HasForeignKey(e => e.PersonalityProfileId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Message indexes for performance - line 46-55
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasIndex(e => e.ConversationId);
            entity.HasIndex(e => e.Timestamp);
            entity.Property(e => e.Metadata).HasColumnType("jsonb");
            entity.HasOne(e => e.Conversation)
                  .WithMany(e => e.Messages)
                  .HasForeignKey(e => e.ConversationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
```

**Команды создания миграции**:
```bash
dotnet ef migrations add InitialCreate --project src/DigitalMe.Data --startup-project src/DigitalMe.API --verbose
dotnet ef migrations list --project src/DigitalMe.Data --startup-project src/DigitalMe.API
```

**Критерии успеха (измеримые)**:
- ✅ Миграция создается без ошибок  
- ✅ SQL скрипт содержит все таблицы (5+ таблиц)
- ✅ Индексы созданы корректно (`\d+ personality_profiles` в psql)
- ✅ Foreign key constraints настроены

## Navigation
- [Previous: Day 1: Solution Setup](03-02-01-01-solution-setup.md)
- [Next: Day 3: Entity Models Implementation](03-02-01-03-entity-models.md)
- [Overview: Week 1 Foundation](../03-02-01-week1-foundation.md)