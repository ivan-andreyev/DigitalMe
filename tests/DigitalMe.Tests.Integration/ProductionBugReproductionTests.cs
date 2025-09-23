using System.Net;
using System.Text;
using System.Text.Json;
using DigitalMe.Data;
using DigitalMe.Data.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DigitalMe.Tests.Integration;

/// <summary>
/// Tests that reproduce actual production bugs to prevent regression.
/// These tests should FAIL initially, then be fixed through TDD.
/// </summary>
public class ProductionBugReproductionTests : IClassFixture<ProductionBugReproductionTests.ProductionLikeWebApplicationFactory>
{
    private readonly ProductionLikeWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProductionBugReproductionTests(ProductionLikeWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    /// <summary>
    /// REPRODUCTION TEST: Foreign Key constraint failed in production
    /// This test should reproduce the exact scenario that failed in production.
    ///
    /// Production error: SQLite Error 19: 'FOREIGN KEY constraint failed'
    /// When: PersonalityProfile exists in code but ID doesn't match DB records
    /// </summary>
    [Fact]
    [Trait("Category", "ProductionBug")]
    [Trait("Bug", "ForeignKeyConstraint")]
    public async Task ChatSend_WithOrphanedPersonalityProfile_ShouldNotFailWithForeignKeyError()
    {
        // Arrange - Initialize database with migrations
        await _factory.InitializeDatabaseAsync();

        // Create production-like scenario: PersonalityProfile with broken FK relationship
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DigitalMeDbContext>();

        // Create PersonalityProfile that MVPPersonalityService will find (Name = "Ivan")
        // but disable FK constraints to test what happens when FK constraint fails
        var ivanProfile = new PersonalityProfile
        {
            Id = Guid.NewGuid(), // This will be used in Conversation.PersonalityProfileId
            Name = "Ivan", // MVPPersonalityService searches by this
            Description = "Test Ivan profile for FK constraint testing",
            Traits = new List<PersonalityTrait>
            {
                new() { Category = "Approach", Name = "Pragmatic", Description = "Highly practical approach", Weight = 0.9 },
                new() { Category = "Skills", Name = "Technical", Description = "Strong technical focus", Weight = 0.8 }
            }
        };

        context.PersonalityProfiles.Add(ivanProfile);
        await context.SaveChangesAsync();

        // Simulate FK constraint issue by breaking the PersonalityProfile table
        // This could happen if migrations are inconsistent or data gets corrupted
        await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");

        // Remove the PersonalityProfile AFTER it's been loaded by the service
        // This simulates production scenario: service finds profile, but FK constraint fails
        context.PersonalityProfiles.Remove(ivanProfile);
        await context.SaveChangesAsync();

        var chatRequest = new
        {
            message = "Hello test",
            platform = "test",
            userId = "test-user"
        };

        var json = JsonSerializer.Serialize(chatRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/chat/send", content);

        // Assert - Should NOT fail with 500 Internal Server Error due to Foreign Key constraint
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
            "Chat should handle missing PersonalityProfile gracefully, not crash with Foreign Key error");

        // Should either:
        // 1. Create PersonalityProfile on-demand, OR
        // 2. Return proper error message (4xx), OR
        // 3. Use fallback mechanism
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,              // Success with fallback
            HttpStatusCode.BadRequest,      // Proper validation error
            HttpStatusCode.ServiceUnavailable // Graceful degradation
        );

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().NotBeEmpty();
            responseContent.Should().Contain("message", "Should return some response content");
        }
    }

    /// <summary>
    /// E2E Test: Complete auth + chat flow that should work end-to-end
    /// This tests the exact user journey that was broken in production
    /// </summary>
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Flow", "AuthToChat")]
    public async Task CompleteFlow_AuthThenChat_ShouldWorkEndToEnd()
    {
        // Arrange - Initialize database with migrations
        await _factory.InitializeDatabaseAsync();

        // Step 1: Register user
        var registerRequest = new
        {
            email = "e2e.test@example.com",
            password = "Test123@",
            confirmPassword = "Test123@"
        };

        var registerJson = JsonSerializer.Serialize(registerRequest);
        var registerContent = new StringContent(registerJson, Encoding.UTF8, "application/json");
        var registerResponse = await _client.PostAsync("/api/auth/register", registerContent);

        // Step 2: Should get valid JWT token
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var registerResponseContent = await registerResponse.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<AuthResponse>(registerResponseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        authResponse.Should().NotBeNull();
        authResponse!.Success.Should().BeTrue();
        authResponse.Token.Should().NotBeEmpty();

        // Step 3: Use token to send chat message (this was failing in production)
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.Token);

        var chatRequest = new
        {
            message = "Hello from authenticated user",
            platform = "web",
            userId = "e2e-test-user"
        };

        var chatJson = JsonSerializer.Serialize(chatRequest);
        var chatContent = new StringContent(chatJson, Encoding.UTF8, "application/json");

        // Act - This is where production was failing
        var chatResponse = await _client.PostAsync("/api/chat/send", chatContent);

        // Assert - Complete flow should work
        chatResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "Authenticated user should be able to send chat messages without Foreign Key errors");

        var chatResponseContent = await chatResponse.Content.ReadAsStringAsync();
        chatResponseContent.Should().NotBeEmpty();
        chatResponseContent.Should().ContainAny("conversationId", "content", "response");
    }

    public class ProductionLikeWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbPath = $"test_production_bug_{Guid.NewGuid()}.db";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext registrations
                var descriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<DigitalMeDbContext>) ||
                    d.ServiceType == typeof(DigitalMeDbContext) ||
                    d.ImplementationType == typeof(DigitalMeDbContext)).ToList();

                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                // Use SQLite (like production) not InMemory
                services.AddDbContext<DigitalMeDbContext>(options =>
                {
                    options.UseSqlite($"Data Source={_dbPath}");
                    options.EnableSensitiveDataLogging();
                });
            });

            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JWT:Key"] = "test-super-secret-key-12345678901234567890123456789012",
                    ["JWT:Issuer"] = "TestIssuer",
                    ["JWT:Audience"] = "TestAudience",
                    ["JWT:ExpireHours"] = "24"
                });
            });
        }

        public async Task InitializeDatabaseAsync()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DigitalMeDbContext>();

            // Create database and apply migrations
            await context.Database.EnsureDeletedAsync();
            await context.Database.MigrateAsync();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && File.Exists(_dbPath))
            {
                try
                {
                    File.Delete(_dbPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
            base.Dispose(disposing);
        }
    }

    private class AuthResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}