using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using FluentAssertions;
using System.Net;
using System.Text;
using System.Text.Json;

namespace DigitalMe.Tests.Integration.Frontend;

public class AuthSmokeTests : IntegrationTestBase
{
    public AuthSmokeTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task AuthLogin_WithDemoCredentials_ShouldNotFail()
    {
        // Arrange
        var loginRequest = new
        {
            Email = "demo@digitalme.ai",
            Password = "Ivan2024!"
        };

        var json = JsonSerializer.Serialize(loginRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/auth/login", content);

        // Assert - Should not return server error (demo mode should handle this gracefully)
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.Should().NotBe(HttpStatusCode.BadGateway);
        
        // In demo mode, should return success OR proper error message (not crash)
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().NotBeEmpty();
        }
    }

    [Fact]
    public async Task AuthValidate_ShouldNotCrash()
    {
        // Act
        var response = await Client.GetAsync("/api/auth/validate");

        // Assert - Should handle missing/invalid tokens gracefully
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.Should().NotBe(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task SignalRHub_ShouldBeAccessible()
    {
        // Act - Try to access SignalR hub endpoint
        var response = await Client.GetAsync("/chathub");

        // Assert - Should not return server error (hub should be configured)
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
        
        // SignalR endpoints typically return 404 for GET requests, which is expected
        // We just want to ensure the server doesn't crash
    }
}