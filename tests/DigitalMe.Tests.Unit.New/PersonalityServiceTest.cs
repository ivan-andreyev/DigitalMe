using DigitalMe.Services;
using DigitalMe.Data;
using DigitalMe.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Moq;
using FluentAssertions;

namespace DigitalMe.Tests.Unit.New;

public class PersonalityServiceTest
{
    [Fact]
    public async Task GetPersonalityAsync_ShouldReturnSuccess()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<DigitalMeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new DigitalMeDbContext(options);
        context.Database.EnsureCreated();

        var mockLogger = new Mock<ILogger<PersonalityService>>();
        var mockParser = new Mock<IProfileDataParser>();
        var mockConfig = new Mock<IConfiguration>();
        var mockEnvironment = new Mock<IWebHostEnvironment>();

        var service = new PersonalityService(
            mockLogger.Object,
            mockParser.Object,
            mockConfig.Object,
            mockEnvironment.Object);

        // Act
        var result = await service.GetPersonalityAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
    }
}
