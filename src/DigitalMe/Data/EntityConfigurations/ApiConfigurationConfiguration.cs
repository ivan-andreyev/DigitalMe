using DigitalMe.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalMe.Data.EntityConfigurations;

/// <summary>
/// Entity configuration for ApiConfiguration with security, performance optimizations, and constraints.
/// </summary>
public class ApiConfigurationConfiguration : IEntityTypeConfiguration<ApiConfiguration>
{
    /// <summary>
    /// Configures the ApiConfiguration entity with PostgreSQL-specific settings, indexes, and constraints.
    /// </summary>
    /// <param name="entity">The entity type builder for ApiConfiguration.</param>
    public void Configure(EntityTypeBuilder<ApiConfiguration> entity)
    {
        entity.HasKey(e => e.Id);

        // Unique constraint: one configuration per user per provider
        entity.HasIndex(e => new { e.UserId, e.Provider })
            .IsUnique()
            .HasDatabaseName("IX_ApiConfigurations_UserId_Provider_Unique");

        // Performance indexes for common queries
        entity.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_ApiConfigurations_UserId");

        entity.HasIndex(e => e.Provider)
            .HasDatabaseName("IX_ApiConfigurations_Provider");

        entity.HasIndex(e => new { e.UserId, e.IsActive })
            .HasDatabaseName("IX_ApiConfigurations_UserId_IsActive")
            .HasFilter("IsActive = true");

        entity.HasIndex(e => e.LastUsedAt)
            .HasDatabaseName("IX_ApiConfigurations_LastUsedAt");

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

        entity.Property(e => e.LastUsedAt)
            .HasColumnType("timestamptz");

        entity.Property(e => e.LastValidatedAt)
            .HasColumnType("timestamptz");

        entity.Property(e => e.IsActive)
            .HasColumnType("boolean")
            .HasDefaultValue(true);

        // String length constraints for security
        entity.Property(e => e.UserId)
            .IsRequired()
            .HasMaxLength(450);

        entity.Property(e => e.Provider)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(e => e.DisplayName)
            .HasMaxLength(200);

        entity.Property(e => e.EncryptedApiKey)
            .IsRequired();

        entity.Property(e => e.EncryptionIV)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(e => e.EncryptionSalt)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(e => e.KeyFingerprint)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(e => e.ValidationStatus)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(ApiConfigurationStatus.Unknown);
    }
}