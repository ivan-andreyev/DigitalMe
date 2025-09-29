using DigitalMe.Data.Entities;
using DigitalMe.Data.ValueConverters;
using DigitalMe.Models;
using DigitalMe.Services.Learning.ErrorLearning.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DigitalMe.Data;

/// <summary>
/// Application database context for DigitalMe, handling personal profile data, conversations, and API configurations.
/// Extends IdentityDbContext to provide ASP.NET Core Identity integration with custom entities.
/// </summary>
public class DigitalMeDbContext : IdentityDbContext
{
    /// <summary>
    /// Initializes a new instance of the DigitalMeDbContext with specified options.
    /// </summary>
    /// <param name="options">Database context configuration options.</param>
    public DigitalMeDbContext(DbContextOptions<DigitalMeDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the personality profiles collection.
    /// </summary>
    public DbSet<PersonalityProfile> PersonalityProfiles { get; set; }

    /// <summary>
    /// Gets or sets the personality traits collection.
    /// </summary>
    public DbSet<PersonalityTrait> PersonalityTraits { get; set; }

    /// <summary>
    /// Gets or sets the temporal behavior patterns collection.
    /// </summary>
    public DbSet<TemporalBehaviorPattern> TemporalBehaviorPatterns { get; set; }

    /// <summary>
    /// Gets or sets the conversations collection.
    /// </summary>
    public DbSet<Conversation> Conversations { get; set; }

    /// <summary>
    /// Gets or sets the messages collection.
    /// </summary>
    public DbSet<Message> Messages { get; set; }

    /// <summary>
    /// Gets or sets the Telegram messages collection.
    /// </summary>
    public DbSet<TelegramMessage> TelegramMessages { get; set; }

    /// <summary>
    /// Gets or sets the calendar events collection.
    /// </summary>
    public DbSet<CalendarEvent> CalendarEvents { get; set; }

    /// <summary>
    /// Gets or sets the error patterns collection for the Error Learning System.
    /// </summary>
    public DbSet<ErrorPattern> ErrorPatterns { get; set; }

    /// <summary>
    /// Gets or sets the learning history entries collection for the Error Learning System.
    /// </summary>
    public DbSet<LearningHistoryEntry> LearningHistoryEntries { get; set; }

    /// <summary>
    /// Gets or sets the optimization suggestions collection for the Error Learning System.
    /// </summary>
    public DbSet<OptimizationSuggestion> OptimizationSuggestions { get; set; }

    /// <summary>
    /// Gets or sets the API configurations collection for secure API key management.
    /// </summary>
    public DbSet<ApiConfiguration> ApiConfigurations { get; set; }

    /// <summary>
    /// Gets or sets the API usage records collection for analytics and monitoring.
    /// </summary>
    public DbSet<ApiUsageRecord> ApiUsageRecords { get; set; }

    /// <summary>
    /// Configures the database model and applies entity configurations including PostgreSQL-specific optimizations.
    /// </summary>
    /// <param name="modelBuilder">The model builder used to configure entities.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration implementations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DigitalMeDbContext).Assembly);

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

        // Error Learning System configuration
        ConfigureErrorLearningSystem(modelBuilder);
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
    /// Automatically updates audit fields (UpdatedAt, UpdatedBy, CreatedBy) before saving.
    /// Note: Currently uses "system" as the default user. User context tracking will be implemented
    /// when IHttpContextAccessor is integrated into the audit infrastructure.
    /// </summary>
    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is IEntity &&
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            // Update timestamp for all entities
            if (entry.Entity is not IEntity entity)
                continue;

            entity.UpdatedAt = DateTime.UtcNow;

            // Set CreatedBy for new auditable entities
            if (entry.Entity is IAuditableEntity auditableEntity && entry.State == EntityState.Added)
            {
                auditableEntity.CreatedBy ??= "system";
                continue;
            }

            // Set UpdatedBy for modified auditable entities
            if (entry.Entity is IAuditableEntity auditableEntityUpdate && entry.State == EntityState.Modified)
            {
                auditableEntityUpdate.UpdatedBy = "system";
            }
        }
    }

    /// <summary>
    /// Configures Error Learning System entities with proper relationships and PostgreSQL optimizations
    /// </summary>
    private void ConfigureErrorLearningSystem(ModelBuilder modelBuilder)
    {
        // ErrorPattern configuration
        modelBuilder.Entity<ErrorPattern>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Ensure PatternHash is unique for pattern deduplication
            entity.HasIndex(e => e.PatternHash)
                .IsUnique();
            
            // Performance indexes
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => new { e.Category, e.Subcategory });
            entity.HasIndex(e => e.ApiEndpoint);
            entity.HasIndex(e => e.LastObserved);
            entity.HasIndex(e => e.SeverityLevel);
            entity.HasIndex(e => e.OccurrenceCount);
            
            // PostgreSQL specific configurations
            entity.Property(e => e.FirstObserved)
                .HasColumnType("timestamptz");
            entity.Property(e => e.LastObserved)
                .HasColumnType("timestamptz");
            entity.Property(e => e.Context)
                .HasColumnType("jsonb");
            entity.Property(e => e.SuggestedSolutions)
                .HasColumnType("jsonb");
        });

        // LearningHistoryEntry configuration
        modelBuilder.Entity<LearningHistoryEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Foreign key relationship
            entity.HasOne(e => e.ErrorPattern)
                .WithMany(ep => ep.LearningHistory)
                .HasForeignKey(e => e.ErrorPatternId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Performance indexes
            entity.HasIndex(e => e.ErrorPatternId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.Source);
            entity.HasIndex(e => e.ApiName);
            entity.HasIndex(e => new { e.IsAnalyzed, e.ContributedToPattern });
            
            // PostgreSQL specific configurations
            entity.Property(e => e.Timestamp)
                .HasColumnType("timestamptz");
            entity.Property(e => e.RequestDetails)
                .HasColumnType("jsonb");
            entity.Property(e => e.ResponseDetails)
                .HasColumnType("jsonb");
            entity.Property(e => e.EnvironmentContext)
                .HasColumnType("jsonb");
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb");
        });

        // OptimizationSuggestion configuration
        modelBuilder.Entity<OptimizationSuggestion>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Foreign key relationship
            entity.HasOne(e => e.ErrorPattern)
                .WithMany(ep => ep.OptimizationSuggestions)
                .HasForeignKey(e => e.ErrorPatternId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Performance indexes
            entity.HasIndex(e => e.ErrorPatternId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.GeneratedAt);
            entity.HasIndex(e => new { e.IsReviewed, e.Status });
            
            // PostgreSQL specific configurations
            entity.Property(e => e.GeneratedAt)
                .HasColumnType("timestamptz");
            entity.Property(e => e.ReviewedAt)
                .HasColumnType("timestamptz");
            entity.Property(e => e.ImplementationDetails)
                .HasColumnType("jsonb");
        });
    }
}
