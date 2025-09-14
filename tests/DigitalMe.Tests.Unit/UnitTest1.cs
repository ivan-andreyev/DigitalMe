using FluentAssertions;

namespace DigitalMe.Tests.Unit;

public class FrameworkValidationTests : TestBase
{
    [Fact]
    public void TestFramework_Should_BeConfiguredCorrectly()
    {
        // Arrange
        var expectedValue = "test";

        // Act
        var result = expectedValue;

        // Assert
        result.Should().Be("test");
    }

    [Fact]
    public void MockRepository_Should_BeAvailable()
    {
        // Arrange & Act
        var mock = this.MockRepository.Create<IDisposable>();

        // Assert
        mock.Should().NotBeNull();
    }
}
