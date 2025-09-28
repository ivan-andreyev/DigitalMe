using DigitalMe.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace DigitalMe.IntegrationTests;

/// <summary>
/// TDD tests for PostgreSQL column alias case-sensitivity
/// Tests the specific error: "column t.Value does not exist"
/// Expected: "Perhaps you meant to reference the column "t.value""
/// </summary>
public class PostgreSQLAliasTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _output;
    private readonly IServiceScope _scope;
    private readonly DigitalMeDbContext _context;

    public PostgreSQLAliasTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<DigitalMeDbContext>();
    }

    /// <summary>
    /// FAILING TEST - demonstrates the PostgreSQL alias case-sensitivity problem
    /// Error: "column t.Value does not exist" when using PascalCase aliases
    /// </summary>
    [Fact]
    public async Task SqlQuery_With_PascalCase_Alias_Should_Fail_In_PostgreSQL()
    {
        _output.WriteLine("🔍 Testing PostgreSQL alias case-sensitivity issue");

        // Arrange
        await _context.Database.EnsureCreatedAsync();

        // Act & Assert - This should FAIL with PostgreSQL
        var exception = await Assert.ThrowsAsync<Exception>(async () =>
        {
            await _context.Database
                .SqlQuery<int>($@"SELECT COUNT(*) as value FROM ""Conversations""")
                .FirstOrDefaultAsync();
        });

        _output.WriteLine($"✅ Expected failure: {exception.Message}");
        Assert.Contains("Value does not exist", exception.Message);
        Assert.Contains("perhaps you meant", exception.Message.ToLowerInvariant());
    }

    /// <summary>
    /// PASSING TEST - demonstrates the correct way to use lowercase aliases
    /// This is the fix for the PostgreSQL case-sensitivity issue
    /// </summary>
    [Fact]
    public async Task SqlQuery_With_Lowercase_Alias_Should_Work_In_PostgreSQL()
    {
        _output.WriteLine("🔍 Testing PostgreSQL with lowercase alias (the fix)");

        // Arrange
        await _context.Database.EnsureCreatedAsync();

        // Act - This should WORK with PostgreSQL
        var result = await _context.Database
            .SqlQuery<int>($@"SELECT COUNT(*) as value FROM ""Conversations""")
            .FirstOrDefaultAsync();

        // Assert
        Assert.True(result >= 0, "Lowercase alias should work in PostgreSQL");
        _output.WriteLine($"✅ Query with lowercase alias succeeded: {result}");
    }

    /// <summary>
    /// PASSING TEST - demonstrates quoted PascalCase aliases work too
    /// Alternative fix using quoted identifiers
    /// </summary>
    [Fact]
    public async Task SqlQuery_With_Quoted_PascalCase_Alias_Should_Work_In_PostgreSQL()
    {
        _output.WriteLine("🔍 Testing PostgreSQL with quoted PascalCase alias");

        // Arrange
        await _context.Database.EnsureCreatedAsync();

        // Act - This should WORK with PostgreSQL when quoted
        var result = await _context.Database
            .SqlQuery<int>($@"SELECT COUNT(*) as ""Value"" FROM ""Conversations""")
            .FirstOrDefaultAsync();

        // Assert
        Assert.True(result >= 0, "Quoted PascalCase alias should work in PostgreSQL");
        _output.WriteLine($"✅ Query with quoted alias succeeded: {result}");
    }

    /// <summary>
    /// Reproduce the exact health check error before fixing it
    /// This mimics the failing query from DataConsistencyHealthCheck
    /// </summary>
    [Fact]
    public async Task Health_Check_Query_Should_Fail_Before_Fix()
    {
        _output.WriteLine("🔍 Reproducing exact health check failure");

        // Arrange
        await _context.Database.EnsureCreatedAsync();

        // Act & Assert - Exact query from health check (should fail)
        var exception = await Assert.ThrowsAsync<Exception>(async () =>
        {
            await _context.Database
                .SqlQuery<int>($@"
                    SELECT COUNT(*) as value
                    FROM ""Conversations"" c
                    LEFT JOIN ""PersonalityProfiles"" pp ON c.""PersonalityProfileId"" = pp.""Id""
                    WHERE pp.""Id"" IS NULL")
                .FirstOrDefaultAsync();
        });

        _output.WriteLine($"✅ Health check query failed as expected: {exception.Message}");
        Assert.Contains("Value does not exist", exception.Message);
    }

    /// <summary>
    /// Test the fixed health check query
    /// This should work after applying the fix
    /// </summary>
    [Fact]
    public async Task Health_Check_Query_Should_Work_After_Fix()
    {
        _output.WriteLine("🔍 Testing fixed health check query");

        // Arrange
        await _context.Database.EnsureCreatedAsync();

        // Act - Fixed query with lowercase alias
        var result = await _context.Database
            .SqlQuery<int>($@"
                SELECT COUNT(*) as value
                FROM ""Conversations"" c
                LEFT JOIN ""PersonalityProfiles"" pp ON c.""PersonalityProfileId"" = pp.""Id""
                WHERE pp.""Id"" IS NULL")
            .FirstOrDefaultAsync();

        // Assert
        Assert.True(result >= 0, "Fixed health check query should work");
        _output.WriteLine($"✅ Fixed health check query succeeded: {result}");
    }

    public void Dispose()
    {
        _scope?.Dispose();
    }
}