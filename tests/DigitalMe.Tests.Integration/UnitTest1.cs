using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;

namespace DigitalMe.Tests.Integration;

public class ApplicationIntegrationTests : IntegrationTestBase
{
    public ApplicationIntegrationTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task Application_Should_StartSuccessfully()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/health");
        
        // Assert
        response.Should().NotBeNull();
    }
}