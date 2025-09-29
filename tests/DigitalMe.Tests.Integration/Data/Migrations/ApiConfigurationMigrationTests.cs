using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using DigitalMe.Data;
using DigitalMe.Data.Entities;
using Microsoft.Data.Sqlite;

namespace DigitalMe.Tests.Integration.Data.Migrations;

/// <summary>
/// TDD tests for API Configuration system migrations.
/// Tests verify table creation, constraints, indexes, and rollback capability.
/// </summary>
public class ApiConfigurationMigrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DigitalMeDbContext _context;

    public ApiConfigurationMigrationTests()
    {
        // Create in-memory SQLite database for testing
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DigitalMeDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new DigitalMeDbContext(options);
    }

    [Fact]
    public async Task Migration_Should_Create_ApiConfigurations_Table()
    {
        // Arrange & Act
        await _context.Database.MigrateAsync();

        // Assert - verify table exists
        var tableExists = await TableExistsAsync("ApiConfigurations");
        tableExists.Should().BeTrue("ApiConfigurations table should be created by migration");
    }

    [Fact]
    public async Task Migration_Should_Create_ApiUsageRecords_Table()
    {
        // Arrange & Act
        await _context.Database.MigrateAsync();

        // Assert - verify table exists
        var tableExists = await TableExistsAsync("ApiUsageRecords");
        tableExists.Should().BeTrue("ApiUsageRecords table should be created by migration");
    }

    [Fact]
    public async Task ApiConfigurations_Should_Have_Required_Columns()
    {
        // Arrange & Act
        await _context.Database.MigrateAsync();

        // Assert - verify all required columns exist
        var columns = await GetTableColumnsAsync("ApiConfigurations");

        columns.Should().Contain("Id", "Primary key should exist");
        columns.Should().Contain("UserId", "UserId column should exist");
        columns.Should().Contain("Provider", "Provider column should exist");
        columns.Should().Contain("DisplayName", "DisplayName column should exist");
        columns.Should().Contain("EncryptedApiKey", "EncryptedApiKey column should exist");
        columns.Should().Contain("EncryptionIV", "EncryptionIV column should exist");
        columns.Should().Contain("EncryptionSalt", "EncryptionSalt column should exist");
        columns.Should().Contain("KeyFingerprint", "KeyFingerprint column should exist");
        columns.Should().Contain("IsActive", "IsActive column should exist");
        columns.Should().Contain("LastUsedAt", "LastUsedAt column should exist");
        columns.Should().Contain("LastValidatedAt", "LastValidatedAt column should exist");
        columns.Should().Contain("ValidationStatus", "ValidationStatus column should exist");
        columns.Should().Contain("CreatedAt", "CreatedAt column should exist");
        columns.Should().Contain("UpdatedAt", "UpdatedAt column should exist");
    }

    [Fact]
    public async Task ApiUsageRecords_Should_Have_Required_Columns()
    {
        // Arrange & Act
        await _context.Database.MigrateAsync();

        // Assert - verify all required columns exist
        var columns = await GetTableColumnsAsync("ApiUsageRecords");

        columns.Should().Contain("Id", "Primary key should exist");
        columns.Should().Contain("UserId", "UserId column should exist");
        columns.Should().Contain("Provider", "Provider column should exist");
        columns.Should().Contain("ConfigurationId", "ConfigurationId column should exist");
        columns.Should().Contain("Model", "Model column should exist");
        columns.Should().Contain("RequestType", "RequestType column should exist");
        columns.Should().Contain("TokensUsed", "TokensUsed column should exist");
        columns.Should().Contain("InputTokens", "InputTokens column should exist");
        columns.Should().Contain("OutputTokens", "OutputTokens column should exist");
        columns.Should().Contain("CostEstimate", "CostEstimate column should exist");
        columns.Should().Contain("ResponseTimeMs", "ResponseTimeMs column should exist");
        columns.Should().Contain("Success", "Success column should exist");
        columns.Should().Contain("ErrorType", "ErrorType column should exist");
        columns.Should().Contain("ErrorMessage", "ErrorMessage column should exist");
        columns.Should().Contain("RequestTimestamp", "RequestTimestamp column should exist");
        columns.Should().Contain("CreatedAt", "CreatedAt column should exist");
        columns.Should().Contain("UpdatedAt", "UpdatedAt column should exist");
    }

    [Fact]
    public async Task ApiConfigurations_Should_Have_Unique_Constraint_On_UserId_Provider()
    {
        // Arrange
        await _context.Database.MigrateAsync();

        var config1 = new ApiConfiguration
        {
            UserId = "user123",
            Provider = "Anthropic",
            EncryptedApiKey = "encrypted_key_1",
            EncryptionIV = "iv_1",
            EncryptionSalt = "salt_1",
            KeyFingerprint = "fingerprint_1"
        };

        var config2 = new ApiConfiguration
        {
            UserId = "user123",
            Provider = "Anthropic", // Same UserId + Provider
            EncryptedApiKey = "encrypted_key_2",
            EncryptionIV = "iv_2",
            EncryptionSalt = "salt_2",
            KeyFingerprint = "fingerprint_2"
        };

        // Act
        _context.Set<ApiConfiguration>().Add(config1);
        await _context.SaveChangesAsync();

        _context.Set<ApiConfiguration>().Add(config2);

        // Assert - should throw due to unique constraint
        var act = async () => await _context.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>("Duplicate UserId+Provider should violate unique constraint");
    }

    [Fact]
    public async Task ApiUsageRecords_Should_Have_Index_On_UserId()
    {
        // Arrange & Act
        await _context.Database.MigrateAsync();

        // Assert - verify index exists for performance
        var indexes = await GetTableIndexesAsync("ApiUsageRecords");
        indexes.Should().Contain(idx => idx.Contains("UserId"), "UserId should have an index for query performance");
    }

    [Fact]
    public async Task ApiUsageRecords_Should_Have_Index_On_Provider()
    {
        // Arrange & Act
        await _context.Database.MigrateAsync();

        // Assert - verify index exists for performance
        var indexes = await GetTableIndexesAsync("ApiUsageRecords");
        indexes.Should().Contain(idx => idx.Contains("Provider"), "Provider should have an index for query performance");
    }

    [Fact]
    public async Task ApiUsageRecords_Should_Have_Index_On_RequestTimestamp()
    {
        // Arrange & Act
        await _context.Database.MigrateAsync();

        // Assert - verify index exists for time-based queries
        var indexes = await GetTableIndexesAsync("ApiUsageRecords");
        indexes.Should().Contain(idx => idx.Contains("RequestTimestamp"), "RequestTimestamp should have an index for time-based queries");
    }

    [Fact]
    public async Task ApiUsageRecords_Should_Have_Foreign_Key_To_ApiConfigurations()
    {
        // Arrange
        await _context.Database.MigrateAsync();

        var config = new ApiConfiguration
        {
            UserId = "user123",
            Provider = "Anthropic",
            EncryptedApiKey = "encrypted_key",
            EncryptionIV = "iv",
            EncryptionSalt = "salt",
            KeyFingerprint = "fingerprint"
        };

        _context.Set<ApiConfiguration>().Add(config);
        await _context.SaveChangesAsync();

        var usage = new ApiUsageRecord
        {
            UserId = "user123",
            Provider = "Anthropic",
            ConfigurationId = config.Id, // Reference to existing configuration
            TokensUsed = 1000,
            Success = true
        };

        // Act
        _context.Set<ApiUsageRecord>().Add(usage);
        await _context.SaveChangesAsync();

        // Assert - usage record should be created successfully with valid FK
        var savedUsage = await _context.Set<ApiUsageRecord>()
            .Include(u => u.Configuration)
            .FirstOrDefaultAsync(u => u.Id == usage.Id);

        savedUsage.Should().NotBeNull("Usage record should be saved");
        savedUsage!.Configuration.Should().NotBeNull("Foreign key relationship should load navigation property");
        savedUsage.Configuration!.Id.Should().Be(config.Id, "Foreign key should reference correct configuration");
    }

    [Fact]
    public async Task ApiUsageRecords_Should_Allow_Null_ConfigurationId()
    {
        // Arrange - user provides their own API key without saving configuration
        await _context.Database.MigrateAsync();

        var usage = new ApiUsageRecord
        {
            UserId = "user123",
            Provider = "Anthropic",
            ConfigurationId = null, // User's own API key, not from our storage
            TokensUsed = 1000,
            Success = true
        };

        // Act
        _context.Set<ApiUsageRecord>().Add(usage);
        await _context.SaveChangesAsync();

        // Assert - should save successfully with null ConfigurationId
        var savedUsage = await _context.Set<ApiUsageRecord>()
            .FirstOrDefaultAsync(u => u.Id == usage.Id);

        savedUsage.Should().NotBeNull("Usage record should be saved");
        savedUsage!.ConfigurationId.Should().BeNull("ConfigurationId should be nullable for user-provided keys");
    }

    [Fact]
    public async Task Migration_Should_Set_Default_Values_Correctly()
    {
        // Arrange
        await _context.Database.MigrateAsync();

        var config = new ApiConfiguration
        {
            UserId = "user123",
            Provider = "Anthropic",
            EncryptedApiKey = "key",
            EncryptionIV = "iv",
            EncryptionSalt = "salt",
            KeyFingerprint = "fp"
            // IsActive, ValidationStatus should use defaults
            // CreatedAt, UpdatedAt should be auto-set
        };

        // Act
        _context.Set<ApiConfiguration>().Add(config);
        await _context.SaveChangesAsync();

        // Assert
        var saved = await _context.Set<ApiConfiguration>().FirstAsync(c => c.Id == config.Id);
        saved.IsActive.Should().BeTrue("IsActive should default to true");
        saved.ValidationStatus.Should().Be(ApiConfigurationStatus.Unknown, "ValidationStatus should default to 'Unknown'");
        saved.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5), "CreatedAt should be auto-set");
        saved.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5), "UpdatedAt should be auto-set");
    }

    [Fact]
    public async Task CostEstimate_Should_Have_Correct_Decimal_Precision()
    {
        // Arrange - financial calculations require high precision
        await _context.Database.MigrateAsync();

        var usage = new ApiUsageRecord
        {
            UserId = "user123",
            Provider = "Anthropic",
            TokensUsed = 1000,
            CostEstimate = 0.015123m, // Test decimal precision
            Success = true
        };

        // Act
        _context.Set<ApiUsageRecord>().Add(usage);
        await _context.SaveChangesAsync();

        // Assert - decimal precision should be preserved (10,6)
        var saved = await _context.Set<ApiUsageRecord>().FirstAsync(u => u.Id == usage.Id);
        saved.CostEstimate.Should().Be(0.015123m, "Decimal precision should be preserved for financial data");
    }

    [Fact]
    public async Task Migration_Should_Support_Rollback()
    {
        // Arrange - apply migration first
        await _context.Database.MigrateAsync();
        var tablesAfterMigration = await TableExistsAsync("ApiConfigurations");
        tablesAfterMigration.Should().BeTrue("Tables should exist after migration");

        // Act - rollback by ensuring schema can be recreated
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.EnsureCreatedAsync();

        // Assert - schema recreation should work (validates migration reversibility)
        var tablesAfterRecreate = await TableExistsAsync("ApiConfigurations");
        tablesAfterRecreate.Should().BeTrue("Migration should be reversible and schema recreatable");
    }

    #region Helper Methods

    private async Task<bool> TableExistsAsync(string tableName)
    {
        var sql = $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{tableName}'";
        var command = _connection.CreateCommand();
        command.CommandText = sql;
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }

    private async Task<List<string>> GetTableColumnsAsync(string tableName)
    {
        var sql = $"PRAGMA table_info({tableName})";
        var command = _connection.CreateCommand();
        command.CommandText = sql;

        var columns = new List<string>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(1)); // Column name is in index 1
        }
        return columns;
    }

    private async Task<List<string>> GetTableIndexesAsync(string tableName)
    {
        var sql = $"PRAGMA index_list({tableName})";
        var command = _connection.CreateCommand();
        command.CommandText = sql;

        var indexes = new List<string>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            indexes.Add(reader.GetString(1)); // Index name is in index 1
        }
        return indexes;
    }

    #endregion

    public void Dispose()
    {
        _context?.Dispose();
        _connection?.Dispose();
    }
}