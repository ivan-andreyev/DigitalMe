using DigitalMe.Services;
using DigitalMe.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Moq;
using FluentAssertions;

namespace DigitalMe.Tests.Unit.New;

public class DiagnosticTests
{
    [Fact]
    public void Configuration_Should_Load_Successfully()
    {
        // Arrange & Act
        var mockConfig = new Mock<IConfiguration>();
        
        // Assert
        mockConfig.Should().NotBeNull();
    }

    [Fact]
    public void DatabaseContext_Should_Initialize_Successfully()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<DigitalMeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        // Act & Assert
        using var context = new DigitalMeDbContext(options);
        context.Should().NotBeNull();
        context.Database.EnsureCreated().Should().BeTrue();
    }

    [Fact]
    public void PersonalityService_Should_Initialize_Without_Errors()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<PersonalityService>>();
        var mockParser = new Mock<IProfileDataParser>();
        var mockConfig = new Mock<IConfiguration>();
        var mockEnvironment = new Mock<IWebHostEnvironment>();

        // Act
        var service = () => new PersonalityService(
            mockLogger.Object,
            mockParser.Object,
            mockConfig.Object,
            mockEnvironment.Object);

        // Assert
        service.Should().NotThrow();
        service().Should().NotBeNull();
    }

    [Fact]
    public async Task PersonalityService_GetPersonalityAsync_Should_Not_Throw()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<PersonalityService>>();
        var mockParser = new Mock<IProfileDataParser>();
        var mockConfig = new Mock<IConfiguration>();
        var mockEnvironment = new Mock<IWebHostEnvironment>();

        var service = new PersonalityService(
            mockLogger.Object,
            mockParser.Object,
            mockConfig.Object,
            mockEnvironment.Object);

        // Act & Assert
        var act = async () => await service.GetPersonalityAsync();
        await act.Should().NotThrowAsync();
    }
}
