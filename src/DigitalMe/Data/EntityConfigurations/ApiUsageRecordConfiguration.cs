using DigitalMe.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalMe.Data.EntityConfigurations;

/// <summary>
/// Entity configuration for ApiUsageRecord with performance indexes and analytics optimizations.
/// </summary>
public class ApiUsageRecordConfiguration : IEntityTypeConfiguration<ApiUsageRecord>
{
    /// <summary>
    /// Configures the ApiUsageRecord entity with PostgreSQL-specific settings and analytics-optimized indexes.
    /// </summary>
    /// <param name="entity">The entity type builder for ApiUsageRecord.</param>
    public void Configure(EntityTypeBuilder<ApiUsageRecord> entity)
    {
        entity.HasKey(e => e.Id);

        // Foreign key relationship with ApiConfiguration (nullable)
        entity.HasOne(e => e.Configuration)
            .WithMany()
            .HasForeignKey(e => e.ConfigurationId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // Performance indexes for analytics queries
        entity.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_ApiUsageRecords_UserId");

        entity.HasIndex(e => e.Provider)
            .HasDatabaseName("IX_ApiUsageRecords_Provider");

        entity.HasIndex(e => e.ConfigurationId)
            .HasDatabaseName("IX_ApiUsageRecords_ConfigurationId");

        entity.HasIndex(e => e.RequestTimestamp)
            .HasDatabaseName("IX_ApiUsageRecords_RequestTimestamp");

        entity.HasIndex(e => new { e.UserId, e.RequestTimestamp })
            .HasDatabaseName("IX_ApiUsageRecords_UserId_RequestTimestamp")
            .IsDescending(false, true); // DESC on timestamp for latest first

        entity.HasIndex(e => new { e.Provider, e.RequestTimestamp })
            .HasDatabaseName("IX_ApiUsageRecords_Provider_RequestTimestamp");

        entity.HasIndex(e => new { e.UserId, e.Provider, e.Success })
            .HasDatabaseName("IX_ApiUsageRecords_UserId_Provider_Success");

        // PostgreSQL specific configurations
        entity.Property(e => e.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()");

        entity.Property(e => e.CreatedAt)
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        entity.Property(e => e.UpdatedAt)
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        entity.Property(e => e.RequestTimestamp)
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        entity.Property(e => e.Success)
            .HasColumnType("boolean")
            .HasDefaultValue(false);

        // High-precision decimal for financial calculations
        entity.Property(e => e.CostEstimate)
            .HasColumnType("decimal(10,6)")
            .HasDefaultValue(0m);

        // String length constraints
        entity.Property(e => e.UserId)
            .IsRequired()
            .HasMaxLength(450);

        entity.Property(e => e.Provider)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(e => e.Model)
            .HasMaxLength(100);

        entity.Property(e => e.RequestType)
            .HasMaxLength(100);

        entity.Property(e => e.ErrorType)
            .HasMaxLength(100);

        entity.Property(e => e.ErrorMessage)
            .HasMaxLength(1000);

        // Default values for token/performance metrics
        entity.Property(e => e.TokensUsed)
            .HasDefaultValue(0);

        entity.Property(e => e.InputTokens)
            .HasDefaultValue(0);

        entity.Property(e => e.OutputTokens)
            .HasDefaultValue(0);

        entity.Property(e => e.ResponseTimeMs)
            .HasDefaultValue(0);
    }
}