using Microsoft.EntityFrameworkCore;
using DigitalMe.Web.Models;

namespace DigitalMe.Web.Data;

public class DigitalMeDbContext : DbContext
{
    public DigitalMeDbContext(DbContextOptions<DigitalMeDbContext> options) : base(options)
    {
    }

    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<ChatSession> ChatSessions { get; set; }
    public DbSet<ChatMessageEntity> ChatMessages { get; set; }
    public DbSet<SystemConfiguration> SystemConfigurations { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql();
        }
        
        // Connection pool settings
        optionsBuilder.EnableServiceProviderCaching();
        optionsBuilder.EnableSensitiveDataLogging(false);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure UserProfile with performance indexes
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.UserName).IsRequired().HasMaxLength(100);
            
            // Unique indexes for business logic
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.UserName).IsUnique();
            
            // Performance indexes for query optimization
            entity.HasIndex(e => new { e.IsActive, e.LastLoginAt })
                  .HasDatabaseName("IX_UserProfiles_IsActive_LastLoginAt");
            entity.HasIndex(e => new { e.Email, e.IsActive })
                  .HasDatabaseName("IX_UserProfiles_Email_IsActive");
        });

        // Configure ChatSession with optimized indexes
        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Title).HasMaxLength(500);
            entity.HasOne(e => e.UserProfile).WithMany().HasForeignKey(e => e.UserId);
            
            // Individual indexes for basic queries
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.UpdatedAt);
            
            // Composite indexes for optimized query performance
            entity.HasIndex(e => new { e.UserId, e.IsActive, e.CreatedAt })
                  .HasDatabaseName("IX_ChatSessions_UserId_IsActive_CreatedAt");
            entity.HasIndex(e => new { e.UserId, e.IsActive, e.UpdatedAt })
                  .HasDatabaseName("IX_ChatSessions_UserId_IsActive_UpdatedAt");
            entity.HasIndex(e => new { e.IsActive, e.CreatedAt })
                  .HasDatabaseName("IX_ChatSessions_IsActive_CreatedAt");
        });

        // Configure ChatMessageEntity with performance-optimized indexes
        modelBuilder.Entity<ChatMessageEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.MessageType).HasMaxLength(50);
            entity.HasOne(e => e.ChatSession).WithMany(s => s.Messages).HasForeignKey(e => e.SessionId);
            
            // Individual indexes for basic queries
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.CreatedAt);
            
            // Composite indexes for optimized pagination and ordering
            entity.HasIndex(e => new { e.SessionId, e.CreatedAt })
                  .HasDatabaseName("IX_ChatMessages_SessionId_CreatedAt");
            entity.HasIndex(e => new { e.SessionId, e.MessageType, e.CreatedAt })
                  .HasDatabaseName("IX_ChatMessages_SessionId_MessageType_CreatedAt");
            
            // Index for message counting and aggregation queries
            entity.HasIndex(e => new { e.SessionId, e.MessageType })
                  .HasDatabaseName("IX_ChatMessages_SessionId_MessageType");
        });

        // Configure SystemConfiguration with caching-optimized indexes
        modelBuilder.Entity<SystemConfiguration>(entity =>
        {
            entity.HasKey(e => e.Key);
            entity.Property(e => e.Key).HasMaxLength(200);
            entity.Property(e => e.Value).IsRequired();
            entity.Property(e => e.ValueType).HasMaxLength(50);
            
            // Index for bulk configuration queries by type
            entity.HasIndex(e => e.ValueType)
                  .HasDatabaseName("IX_SystemConfiguration_ValueType");
            entity.HasIndex(e => new { e.ValueType, e.Key })
                  .HasDatabaseName("IX_SystemConfiguration_ValueType_Key");
        });
    }
}