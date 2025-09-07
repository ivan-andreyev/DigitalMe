using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using FluentAssertions;
using System.Net;

namespace DigitalMe.Tests.Integration.Frontend;

public class SmokeTests : IntegrationTestBase
{
    public SmokeTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task HomePage_ShouldReturnSuccessStatusCode()
    {
        // Act
        var response = await Client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("DigitalMe"); // Basic content check
    }

    [Fact]
    public async Task ChatPage_ShouldReturnSuccessStatusCode()
    {
        // Act
        var response = await Client.GetAsync("/chat");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Чат с Иваном"); // Basic content check
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnHealthy()
    {
        // Act - Use simple health endpoint for basic smoke test
        var response = await Client.GetAsync("/health/simple");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }

    [Fact]
    public async Task ApiPersonality_ShouldReturnSuccessStatusCode()
    {
        // Act
        var response = await Client.GetAsync("/api/personality");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/chat")]
    [InlineData("/personality")]
    public async Task Pages_ShouldNotReturnServerErrors(string url)
    {
        // Act
        var response = await Client.GetAsync(url);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.Should().NotBe(HttpStatusCode.BadGateway);
        response.StatusCode.Should().NotBe(HttpStatusCode.ServiceUnavailable);
    }
}