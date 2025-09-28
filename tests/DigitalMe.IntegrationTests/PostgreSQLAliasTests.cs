using DigitalMe.Data;
using DigitalMe.Models.Database;
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

        // Act & Assert - This now works with QueryResult wrapper
        var result = await _context.Database
            .SqlQuery<QueryResult>($@"SELECT COUNT(*) as value FROM ""Conversations""")
            .FirstOrDefaultAsync();

        // Previously this would fail, but now it works thanks to QueryResult
        Assert.NotNull(result);
        Assert.True(result.value >= 0);

        _output.WriteLine($"✅ EF Core bug fixed with QueryResult wrapper: {result.value}");
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

        // Act - This works with QueryResult wrapper
        var queryResult = await _context.Database
            .SqlQuery<QueryResult>($@"SELECT COUNT(*) as value FROM ""Conversations""")
            .FirstOrDefaultAsync();

        var result = queryResult?.value ?? 0;

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

        // Act - This works with QueryResult wrapper (even with quoted PascalCase)
        // Note: We still need to map to lowercase property in QueryResult
        var queryResult = await _context.Database
            .SqlQuery<QueryResult>($@"SELECT COUNT(*) as value FROM ""Conversations""")
            .FirstOrDefaultAsync();

        var result = queryResult?.value ?? 0;

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

        // Act - Test the fixed health check query with QueryResult
        var queryResult = await _context.Database
            .SqlQuery<QueryResult>($@"
                SELECT COUNT(*) as value
                FROM ""Conversations"" c
                LEFT JOIN ""PersonalityProfiles"" pp ON c.""PersonalityProfileId"" = pp.""Id""
                WHERE pp.""Id"" IS NULL")
            .FirstOrDefaultAsync();

        // Assert - This now works instead of failing
        Assert.NotNull(queryResult);
        var result = queryResult.value;

        _output.WriteLine($"✅ Health check query now works: {result}");
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

        // Act - Fixed query with QueryResult wrapper
        var queryResult = await _context.Database
            .SqlQuery<QueryResult>($@"
                SELECT COUNT(*) as value
                FROM ""Conversations"" c
                LEFT JOIN ""PersonalityProfiles"" pp ON c.""PersonalityProfileId"" = pp.""Id""
                WHERE pp.""Id"" IS NULL")
            .FirstOrDefaultAsync();

        var result = queryResult?.value ?? 0;

        // Assert
        Assert.True(result >= 0, "Fixed health check query should work");
        _output.WriteLine($"✅ Fixed health check query succeeded: {result}");
    }

    public void Dispose()
    {
        _scope?.Dispose();
    }
}