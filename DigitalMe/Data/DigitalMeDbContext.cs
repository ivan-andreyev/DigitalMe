using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using DigitalMe.Models;
using DigitalMe.Data.Entities;
using DigitalMe.Data.ValueConverters;

namespace DigitalMe.Data;

public class DigitalMeDbContext : IdentityDbContext
{
    public DigitalMeDbContext(DbContextOptions<DigitalMeDbContext> options) : base(options)
    {
    }

    public DbSet<PersonalityProfile> PersonalityProfiles { get; set; }
    public DbSet<PersonalityTrait> PersonalityTraits { get; set; }
    public DbSet<TemporalBehaviorPattern> TemporalBehaviorPatterns { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<TelegramMessage> TelegramMessages { get; set; }
    public DbSet<CalendarEvent> CalendarEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // PostgreSQL GUID column configurations
        modelBuilder.Entity<PersonalityProfile>()
            .Property(e => e.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()");

        modelBuilder.Entity<PersonalityTrait>()
            .Property(e => e.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()");

        modelBuilder.Entity<PersonalityTrait>()
            .Property(e => e.PersonalityProfileId)
            .HasColumnType("uuid");

        // Configure other GUID columns for PostgreSQL
        modelBuilder.Entity<Conversation>()
            .Property(e => e.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()");

        // Fix PostgreSQL boolean field type
        modelBuilder.Entity<Conversation>()
            .Property(e => e.IsActive)
            .HasColumnType("boolean");

        // Fix PostgreSQL DateTime field types
        modelBuilder.Entity<Conversation>()
            .Property(e => e.StartedAt)
            .HasColumnType("timestamptz");

        modelBuilder.Entity<Conversation>()
            .Property(e => e.EndedAt)
            .HasColumnType("timestamptz");

        modelBuilder.Entity<Message>()
            .Property(e => e.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()");

        modelBuilder.Entity<Message>()
            .Property(e => e.ConversationId)
            .HasColumnType("uuid");

        modelBuilder.Entity<Message>()
            .Property(e => e.Timestamp)
            .HasColumnType("timestamptz");

        // Fix PersonalityProfile DateTime fields  
        modelBuilder.Entity<PersonalityProfile>()
            .Property(e => e.CreatedAt)
            .HasColumnType("timestamptz");

        modelBuilder.Entity<PersonalityProfile>()
            .Property(e => e.UpdatedAt)
            .HasColumnType("timestamptz");

        // Fix PersonalityTrait DateTime fields
        modelBuilder.Entity<PersonalityTrait>()
            .Property(e => e.CreatedAt)
            .HasColumnType("timestamptz");

        // Fix TelegramMessage DateTime fields
        modelBuilder.Entity<TelegramMessage>()
            .Property(e => e.CreatedAt)
            .HasColumnType("timestamptz");

        modelBuilder.Entity<TelegramMessage>()
            .Property(e => e.MessageDate)
            .HasColumnType("timestamptz");

        // Fix CalendarEvent DateTime fields  
        modelBuilder.Entity<CalendarEvent>()
            .Property(e => e.CreatedAt)
            .HasColumnType("timestamptz");

        modelBuilder.Entity<CalendarEvent>()
            .Property(e => e.LastSyncAt)
            .HasColumnType("timestamptz");

        modelBuilder.Entity<CalendarEvent>()
            .Property(e => e.StartTime)
            .HasColumnType("timestamptz");

        modelBuilder.Entity<CalendarEvent>()
            .Property(e => e.EndTime)
            .HasColumnType("timestamptz");

        // PersonalityProfile relationships
        modelBuilder.Entity<PersonalityTrait>()
            .HasOne(pt => pt.PersonalityProfile)
            .WithMany(pp => pp.Traits)
            .HasForeignKey(pt => pt.PersonalityProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // Conversation relationships
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        modelBuilder.Entity<Message>()
            .HasIndex(m => m.ConversationId);

        modelBuilder.Entity<Message>()
            .HasIndex(m => m.Timestamp);

        modelBuilder.Entity<TelegramMessage>()
            .HasIndex(t => t.TelegramMessageId)
            .IsUnique();

        modelBuilder.Entity<TelegramMessage>()
            .HasIndex(t => new { t.ChatId, t.MessageDate });

        modelBuilder.Entity<CalendarEvent>()
            .HasIndex(c => c.GoogleEventId)
            .IsUnique();

        modelBuilder.Entity<CalendarEvent>()
            .HasIndex(c => new { c.StartTime, c.EndTime });

        // JSON Value Converters for complex fields
        // Note: PersonalityProfile.Traits is a navigation property (ICollection<PersonalityTrait>), not a JSON string
        // It's configured through relationships, not as a direct property

        // Message.Metadata is already string - just set column type for PostgreSQL  
        modelBuilder.Entity<Message>()
            .Property(e => e.Metadata)
            .HasColumnType("jsonb");

        // Base Entity configurations - apply to all entities inheriting from BaseEntity/AuditableBaseEntity
        ConfigureBaseEntity(modelBuilder.Entity<PersonalityProfile>());
        ConfigureBaseEntity(modelBuilder.Entity<PersonalityTrait>());
        ConfigureBaseEntity(modelBuilder.Entity<TemporalBehaviorPattern>());
        ConfigureBaseEntity(modelBuilder.Entity<Conversation>());
        ConfigureBaseEntity(modelBuilder.Entity<Message>());

        // Enhanced indexing strategy for performance optimization
        ConfigurePerformanceIndexes(modelBuilder);
    }

    /// <summary>
    /// Configures base entity properties with PostgreSQL-specific optimizations.
    /// </summary>
    private void ConfigureBaseEntity<T>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<T> builder)
        where T : class, IEntity
    {
        builder.Property(e => e.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.CreatedAt)
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(e => e.UpdatedAt)
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Add index on CreatedAt for time-based queries
        builder.HasIndex(e => e.CreatedAt);
    }

    /// <summary>
    /// Configures performance-optimized indexes for core query patterns.
    /// </summary>
    private void ConfigurePerformanceIndexes(ModelBuilder modelBuilder)
    {
        // Conversation → Messages relationship optimization
        modelBuilder.Entity<Message>()
            .HasIndex(m => new { m.ConversationId, m.Timestamp })
            .HasDatabaseName("IX_Messages_ConversationId_Timestamp_Desc")
            .IsDescending(false, true); // ASC on ConversationId, DESC on Timestamp for latest messages first

        // Active conversations index
        modelBuilder.Entity<Conversation>()
            .HasIndex(c => new { c.UserId, c.IsActive, c.StartedAt })
            .HasDatabaseName("IX_Conversations_UserId_IsActive_StartedAt")
            .HasFilter("IsActive = true");

        // Personality trait search optimization
        modelBuilder.Entity<PersonalityTrait>()
            .HasIndex(pt => new { pt.PersonalityProfileId, pt.Category, pt.Weight })
            .HasDatabaseName("IX_PersonalityTraits_ProfileId_Category_Weight");

        // JSONB GIN indexes for metadata searches (PostgreSQL-specific)
        modelBuilder.Entity<Message>()
            .HasIndex(m => m.Metadata)
            .HasDatabaseName("IX_Messages_Metadata_GIN")
            .HasMethod("gin"); // PostgreSQL GIN index for JSONB queries

        // Note: PersonalityProfile.Traits is a navigation property, not a JSONB column
        // Indexes on navigation properties should be set on the foreign key instead
    }

    /// <summary>
    /// Override SaveChanges to automatically update audit fields.
    /// </summary>
    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    /// <summary>
    /// Override SaveChangesAsync to automatically update audit fields.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Automatically updates audit fields (UpdatedAt, UpdatedBy) before saving.
    /// </summary>
    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is IEntity &&
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            if (entry.Entity is IEntity entity)
            {
                entity.UpdatedAt = DateTime.UtcNow;
            }

            if (entry.Entity is IAuditableEntity auditableEntity && entry.State == EntityState.Added)
            {
                // TODO: Get current user from IHttpContextAccessor
                auditableEntity.CreatedBy ??= "system";
            }

            if (entry.Entity is IAuditableEntity auditableEntityUpdate && entry.State == EntityState.Modified)
            {
                // TODO: Get current user from IHttpContextAccessor  
                auditableEntityUpdate.UpdatedBy = "system";
            }
        }
    }
}
