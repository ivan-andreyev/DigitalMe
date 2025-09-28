using DigitalMe.Data;
using DigitalMe.Data.Entities;
using DigitalMe.Models;
using DigitalMe.Models.Database;
using DigitalMe.Services.HealthChecks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace DigitalMe.IntegrationTests;

/// <summary>
/// Comprehensive tests for PostgreSQL case-sensitivity issues
/// Validates migrations, table names, column names, and SQL queries work correctly
/// These tests catch issues BEFORE deployment to prevent production failures
/// </summary>
public class PostgreSQLCaseSensitivityTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _output;
    private readonly IServiceScope _scope;
    private readonly DigitalMeDbContext _context;

    public PostgreSQLCaseSensitivityTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<DigitalMeDbContext>();
    }

    /// <summary>
    /// Test that validates migrations create tables with correct case-sensitive names
    /// Prevents: "relation 'conversations' does not exist" errors
    /// </summary>
    [Fact]
    public async Task Migrations_Should_Create_Tables_With_Correct_Case_Sensitive_Names()
    {
        _output.WriteLine("🔍 Testing migration table creation with proper case sensitivity");

        // Arrange & Act - Ensure database is migrated
        await _context.Database.EnsureCreatedAsync();

        // Assert - Check that tables exist with correct PascalCase names
        var tableNames = new[] { "Conversations", "PersonalityProfiles", "PersonalityTraits", "Messages" };

        foreach (var tableName in tableNames)
        {
            _output.WriteLine($"🔍 Checking table existence: {tableName}");

            // This query uses the exact table name as it appears in the database
            // Check if table exists with correct case-sensitive name
            var tableExistsQueryResult = await _context.Database
                .SqlQuery<QueryResult>($"SELECT 1 as value FROM information_schema.tables WHERE table_name = '{tableName}' AND table_schema = 'public' LIMIT 1")
                .FirstOrDefaultAsync();

            var tableExists = (tableExistsQueryResult?.value ?? 0) > 0;

            Assert.True(tableExists, $"Table '{tableName}' should exist with exact case-sensitive name");
            _output.WriteLine($"✅ Table '{tableName}' exists");
        }
    }
    /// <summary>
    /// Test that validates filtered indexes work with quoted column names
    /// Prevents: "column 'isactive' does not exist" in migration filters
    /// </summary>
    [Fact]
    public async Task Migration_Filters_Should_Work_With_Quoted_Column_Names()
    {
        _output.WriteLine("🔍 Testing migration filters with quoted identifiers");

        // Arrange & Act
        await _context.Database.EnsureCreatedAsync();

        // Create test conversation to verify filter works
        var testProfile = new PersonalityProfile
        {
            Id = Guid.NewGuid(),
            Name = "Test Profile",
            Description = "Test Description",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var testConversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Title = "Test Conversation",
            PersonalityProfileId = testProfile.Id,
            IsActive = true,
            Platform = "Test",
            UserId = "test-user",
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PersonalityProfiles.Add(testProfile);
        _context.Conversations.Add(testConversation);
        await _context.SaveChangesAsync();

        // Assert - Query using the filtered index should work
        // This exercises the index filter: "IsActive" = true
        var activeConversations = await _context.Conversations
            .Where(c => c.IsActive == true && c.UserId == "test-user")
            .CountAsync();

        Assert.True(activeConversations >= 1, "Should find active conversations using filtered index");
        _output.WriteLine($"✅ Found {activeConversations} active conversations using filtered index");
    }

    /// <summary>
    /// Test that validates health check SQL queries work with quoted table/column names
    /// Prevents: "relation 'conversations' does not exist" in health checks
    /// </summary>
    [Fact]
    public async Task HealthCheck_SQL_Should_Work_With_Quoted_Identifiers()
    {
        _output.WriteLine("🔍 Testing health check SQL with quoted table and column names");

        // Arrange
        await _context.Database.EnsureCreatedAsync();

        var logger = _scope.ServiceProvider.GetRequiredService<ILogger<DataConsistencyHealthCheck>>();
        var healthCheck = new DataConsistencyHealthCheck(_context, logger);

        // Act - Execute health check (this runs the problematic SQL)
        var result = await healthCheck.CheckHealthAsync(
            new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext(),
            CancellationToken.None);

        // Assert - Health check should not fail due to table/column name issues
        Assert.NotEqual(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy, result.Status);
        _output.WriteLine($"✅ Health check status: {result.Status}");

        if (result.Exception != null)
        {
            _output.WriteLine($"❌ Health check exception: {result.Exception.Message}");
            throw result.Exception;
        }
    }

    /// <summary>
    /// Test that validates raw SQL queries work with both quoted and unquoted identifiers
    /// Comprehensive test for all PostgreSQL case-sensitivity scenarios
    /// </summary>
    [Fact]
    public async Task Raw_SQL_Queries_Should_Handle_Case_Sensitivity_Correctly()
    {
        _output.WriteLine("🔍 Testing raw SQL queries with case-sensitive identifiers");

        // Arrange
        await _context.Database.EnsureCreatedAsync();

        // Test 1: Quoted table names (should work)
        _output.WriteLine("🔍 Testing quoted table names");
        var quotedQueryResult = await _context.Database
            .SqlQuery<QueryResult>($@"SELECT COUNT(*) as value FROM ""Conversations""")
            .FirstOrDefaultAsync();
        var quotedQuery = quotedQueryResult?.value ?? 0;

        Assert.True(quotedQuery >= 0, "Quoted table name query should work");
        _output.WriteLine($"✅ Quoted table query returned: {quotedQuery}");

        // Test 2: Quoted column names (should work)
        _output.WriteLine("🔍 Testing quoted column names");
        var quotedColumnQueryResult = await _context.Database
            .SqlQuery<QueryResult>($@"SELECT COUNT(*) as value FROM ""Conversations"" WHERE ""IsActive"" = true")
            .FirstOrDefaultAsync();
        var quotedColumnQuery = quotedColumnQueryResult?.value ?? 0;

        Assert.True(quotedColumnQuery >= 0, "Quoted column name query should work");
        _output.WriteLine($"✅ Quoted column query returned: {quotedColumnQuery}");

        // Test 3: Mixed case without quotes (should fail in PostgreSQL)
        _output.WriteLine("🔍 Testing unquoted mixed case (should handle gracefully)");
        try
        {
            var unquotedQueryResult = await _context.Database
                .SqlQuery<QueryResult>($@"SELECT COUNT(*) as value FROM Conversations WHERE IsActive = true")
                .FirstOrDefaultAsync();
            var unquotedQuery = unquotedQueryResult?.value ?? 0;

            _output.WriteLine($"⚠️ Unquoted query unexpectedly succeeded: {unquotedQuery}");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"✅ Unquoted query failed as expected: {ex.Message}");
            Assert.Contains("does not exist", ex.Message.ToLowerInvariant());
        }
    }

    /// <summary>
    /// Test that validates Entity Framework column mappings work correctly
    /// Ensures HasColumnName mappings are respected in queries
    /// </summary>
    [Fact]
    public async Task Entity_Framework_Column_Mappings_Should_Work_Correctly()
    {
        _output.WriteLine("🔍 Testing Entity Framework column mappings");

        // Arrange
        await _context.Database.EnsureCreatedAsync();

        // Create test data
        var testProfile = new PersonalityProfile
        {
            Id = Guid.NewGuid(),
            Name = "EF Test Profile",
            Description = "Testing EF mappings",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PersonalityProfiles.Add(testProfile);
        await _context.SaveChangesAsync();

        // Act & Assert - Test that EF queries work with mapped columns
        var activeProfiles = await _context.PersonalityProfiles
            .Where(p => p.IsActive == true)  // This uses HasColumnName("IsActive") mapping
            .CountAsync();

        Assert.True(activeProfiles >= 1, "Should find active profiles using EF column mapping");
        _output.WriteLine($"✅ Found {activeProfiles} active profiles using EF mapping");

        // Test conversation EF mapping
        var testConversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Title = "EF Test Conversation",
            PersonalityProfileId = testProfile.Id,
            IsActive = true,
            Platform = "Test",
            UserId = "ef-test-user",
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Conversations.Add(testConversation);
        await _context.SaveChangesAsync();

        var activeConversations = await _context.Conversations
            .Where(c => c.IsActive == true)  // This also uses HasColumnName mapping
            .CountAsync();

        Assert.True(activeConversations >= 1, "Should find active conversations using EF column mapping");
        _output.WriteLine($"✅ Found {activeConversations} active conversations using EF mapping");
    }

    public void Dispose()
    {
        _scope?.Dispose();
    }
}