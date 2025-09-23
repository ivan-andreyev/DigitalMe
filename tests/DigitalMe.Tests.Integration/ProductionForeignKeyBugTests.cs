using System.Net;
using System.Text;
using System.Text.Json;
using DigitalMe.Data;
using DigitalMe.Data.Entities;
using DigitalMe.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace DigitalMe.Tests.Integration;

/// <summary>
/// Dedicated tests to reproduce and fix the exact Foreign Key constraint bug from production.
/// These tests use TDD methodology: failing test → fix → prevent regression.
/// </summary>
public class ProductionForeignKeyBugTests : IClassFixture<ProductionForeignKeyBugTests.ProductionBugWebApplicationFactory>
{
    private readonly ProductionBugWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProductionForeignKeyBugTests(ProductionBugWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    /// <summary>
    /// TDD RED PHASE: This test MUST FAIL initially to reproduce production bug.
    ///
    /// Production scenario: Directly test ConversationService with invalid PersonalityProfileId
    /// This simulates the exact situation where PersonalityProfile exists in service but FK fails
    /// </summary>
    [Fact]
    [Trait("Category", "TDD")]
    [Trait("Phase", "Red")]
    [Trait("Bug", "ProductionForeignKey")]
    public async Task ConversationService_WithNonExistentPersonalityProfileId_ShouldFailWithForeignKeyError()
    {
        // Arrange - Initialize database with FK constraints enabled
        await _factory.InitializeDatabaseAsync();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DigitalMeDbContext>();
        var conversationService = scope.ServiceProvider.GetRequiredService<IConversationService>();

        // Create a PersonalityProfile and get its ID, then delete it to simulate FK constraint issue
        var tempProfile = new PersonalityProfile
        {
            Id = Guid.NewGuid(),
            Name = "TempProfile",
            Description = "Temporary profile for FK testing"
        };

        context.PersonalityProfiles.Add(tempProfile);
        await context.SaveChangesAsync();

        var orphanedProfileId = tempProfile.Id;

        // Delete the profile but keep its ID - this will cause FK constraint to fail
        context.PersonalityProfiles.Remove(tempProfile);
        await context.SaveChangesAsync();

        // Act & Assert - This should FAIL with DbUpdateException containing Foreign Key constraint
        var exception = await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            // Manually try to create a conversation with non-existent PersonalityProfileId
            var conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                Title = "Test Conversation",
                PersonalityProfileId = orphanedProfileId, // This ID doesn't exist anymore
                Platform = "test",
                UserId = "test-user",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Conversations.Add(conversation);
            await context.SaveChangesAsync(); // This should throw Foreign Key constraint error
        });

        // Verify we get the exact production error
        exception.Should().NotBeNull();
        exception.InnerException.Should().NotBeNull();
        exception.InnerException!.Message.Should().Contain("FOREIGN KEY constraint failed");
    }

    /// <summary>
    /// TDD RED PHASE: Test full chat flow with FK constraint error
    /// This should FAIL because ConversationService doesn't handle FK errors gracefully
    /// </summary>
    [Fact]
    [Trait("Category", "TDD")]
    [Trait("Phase", "Red")]
    [Trait("Bug", "ProductionForeignKey")]
    public async Task ChatFlow_WithForeignKeyConstraintError_ShouldFailGracefully()
    {
        // Arrange - Initialize database
        await _factory.InitializeDatabaseAsync();

        // Create valid PersonalityProfile first
        var validProfile = await CreateValidPersonalityProfileAsync();

        // Test chat request
        var chatRequest = new
        {
            message = "Hello, this should work first",
            platform = "test",
            userId = "test-user-fk"
        };

        var json = JsonSerializer.Serialize(chatRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // First request should work
        var firstResponse = await _client.PostAsync("/api/chat/send", content);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Now corrupt the data by deleting PersonalityProfile while conversation references it
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DigitalMeDbContext>();

        // Remove PersonalityProfile to create FK constraint situation
        var profileToDelete = await context.PersonalityProfiles.FirstAsync(p => p.Id == validProfile.Id);
        context.PersonalityProfiles.Remove(profileToDelete);
        await context.SaveChangesAsync();

        // Act - Second request should fail gracefully, not crash
        var secondResponse = await _client.PostAsync("/api/chat/send", content);

        // Assert - This is what we want AFTER fixing the code (TDD Green phase)
        // Currently this will FAIL because code doesn't handle FK gracefully
        secondResponse.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError,
            "Code should handle Foreign Key constraint failures gracefully, not crash with 500 error");

        secondResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,                  // Fallback behavior works
            HttpStatusCode.BadRequest,          // Proper validation error
            HttpStatusCode.ServiceUnavailable   // Graceful degradation
        );

        if (!secondResponse.IsSuccessStatusCode)
        {
            var errorContent = await secondResponse.Content.ReadAsStringAsync();
            errorContent.Should().NotContain("Foreign Key constraint failed",
                "Error messages should not expose internal database constraint details");
            errorContent.Should().NotBeEmpty("Should provide meaningful error message to user");
        }
    }

    /// <summary>
    /// TDD validation: Test that normal happy path still works after FK fix
    /// </summary>
    [Fact]
    [Trait("Category", "TDD")]
    [Trait("Phase", "Green")]
    public async Task ChatSend_WithValidPersonalityProfile_ShouldWorkNormally()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();
        var validProfile = await CreateValidPersonalityProfileAsync();

        var chatRequest = new
        {
            message = "Normal chat message",
            platform = "test",
            userId = "normal-user"
        };

        var json = JsonSerializer.Serialize(chatRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/chat/send", content);

        // Assert - This should work normally
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeEmpty();
    }

    private async Task<PersonalityProfile> CreateValidPersonalityProfileAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DigitalMeDbContext>();

        var profile = new PersonalityProfile
        {
            Id = Guid.NewGuid(),
            Name = "Ivan",
            Description = "Test Ivan profile for FK testing",
            Traits = new List<PersonalityTrait>
            {
                new()
                {
                    Category = "Communication",
                    Name = "Direct",
                    Description = "Direct communication style",
                    Weight = 0.8
                },
                new()
                {
                    Category = "Technical",
                    Name = "Pragmatic",
                    Description = "Practical technical approach",
                    Weight = 0.9
                }
            }
        };

        context.PersonalityProfiles.Add(profile);
        await context.SaveChangesAsync();

        return profile;
    }

    public class ProductionBugWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbPath = $"test_fk_bug_{Guid.NewGuid()}.db";
        private IMvpPersonalityService? _mockPersonalityService;

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

                // Use SQLite with FK constraints enabled (like production)
                services.AddDbContext<DigitalMeDbContext>(options =>
                {
                    options.UseSqlite($"Data Source={_dbPath}");
                    options.EnableSensitiveDataLogging();
                });

                // Replace MVPPersonalityService if mock is provided
                if (_mockPersonalityService != null)
                {
                    var personalityServiceDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMvpPersonalityService));
                    if (personalityServiceDescriptor != null)
                    {
                        services.Remove(personalityServiceDescriptor);
                    }
                    services.AddSingleton(_mockPersonalityService);
                }
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

            await context.Database.EnsureDeletedAsync();
            await context.Database.MigrateAsync();

            // Enable FK constraints explicitly for production-like behavior
            await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON;");
        }

        public void ReplaceMVPPersonalityService(IMvpPersonalityService mockService)
        {
            _mockPersonalityService = mockService;
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
}