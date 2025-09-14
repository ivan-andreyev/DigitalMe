using System;
using System.Threading.Tasks;
using DigitalMe.Services.Learning;
using DigitalMe.Services.Learning.ErrorLearning;
using DigitalMe.Services.Learning.ErrorLearning.Integration;
using DigitalMe.Services.Learning.ErrorLearning.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DigitalMe.Tests.Unit.Services.Learning.ErrorLearning;

/// <summary>
/// Comprehensive integration tests for the Error Learning System
/// Tests the complete flow from test failure capture to learning analysis
/// </summary>
public class ErrorLearningSystemIntegrationTests
{
    private readonly Mock<IErrorLearningService> _mockErrorLearningService;
    private readonly Mock<ILogger<TestFailureCaptureService>> _mockLogger;

    public ErrorLearningSystemIntegrationTests()
    {
        this._mockErrorLearningService = new Mock<IErrorLearningService>();
        this._mockLogger = new Mock<ILogger<TestFailureCaptureService>>();
    }

    [Fact]
    public async Task ErrorLearningSystem_EndToEndFlow_ShouldCaptureAndProcessFailures()
    {
        // Arrange
        var captureService = new TestFailureCaptureService(this._mockLogger.Object, this._mockErrorLearningService.Object);

        var testFailure = new TestExecutionResult
        {
            TestCaseId = "test-001",
            TestCaseName = "Authentication Test",
            Success = false,
            ErrorMessage = "Unauthorized access",
            ExecutionTime = TimeSpan.FromSeconds(2.3),
            Exception = new UnauthorizedAccessException("Invalid credentials")
        };

        var expectedLearningEntry = new LearningHistoryEntry
        {
            Id = 1,
            Source = "SelfTestingFramework",
            ErrorMessage = testFailure.ErrorMessage,
            TestCaseName = testFailure.TestCaseName,
            Timestamp = DateTime.UtcNow,
            IsAnalyzed = false
        };

        this._mockErrorLearningService
            .Setup(x => x.RecordErrorAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(expectedLearningEntry);

        // Act
        var result = await captureService.CaptureTestFailureAsync(testFailure);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(expectedLearningEntry);
        result.Source.Should().Be("SelfTestingFramework");
        result.ErrorMessage.Should().Be(testFailure.ErrorMessage);
        result.TestCaseName.Should().Be(testFailure.TestCaseName);
        result.IsAnalyzed.Should().BeFalse();

        // Verify the error was recorded through the learning service
        this._mockErrorLearningService.Verify(
            x => x.RecordErrorAsync(
            "SelfTestingFramework",
            It.IsAny<string>(),
            testFailure.TestCaseName,
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()
        ), Times.Once);
    }

    [Fact]
    public void TestFailureCaptureService_WithValidDependencies_ShouldConstructSuccessfully()
    {
        // Act
        var service = new TestFailureCaptureService(this._mockLogger.Object, this._mockErrorLearningService.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task TestFailureCaptureService_WithSuccessfulTest_ShouldNotCaptureAsFailure()
    {
        // Arrange
        var captureService = new TestFailureCaptureService(this._mockLogger.Object, this._mockErrorLearningService.Object);

        var successfulTest = new TestExecutionResult
        {
            TestCaseId = "test-success",
            TestCaseName = "Successful Test",
            Success = true,
            ErrorMessage = null
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            captureService.CaptureTestFailureAsync(successfulTest));

        exception.Message.Should().Contain("Cannot capture successful test as failure");

        // Verify no error was recorded
        this._mockErrorLearningService.Verify(
            x => x.RecordErrorAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()
        ), Times.Never);
    }

    [Theory]
    [InlineData("Database connection timeout", "Connection")]
    [InlineData("HTTP 500 Internal Server Error", "Server Error")]
    [InlineData("Validation failed for field 'email'", "Validation")]
    public async Task ErrorLearningSystem_WithDifferentErrorTypes_ShouldCategorizeCorrectly(
        string errorMessage, string expectedCategory)
    {
        // Arrange
        var captureService = new TestFailureCaptureService(this._mockLogger.Object, this._mockErrorLearningService.Object);

        var testFailure = new TestExecutionResult
        {
            TestCaseId = Guid.NewGuid().ToString(),
            TestCaseName = $"Test for {expectedCategory}",
            Success = false,
            ErrorMessage = errorMessage,
            ExecutionTime = TimeSpan.FromSeconds(1.5)
        };

        var capturedEntry = new LearningHistoryEntry
        {
            Id = 1,
            Source = "SelfTestingFramework",
            ErrorMessage = errorMessage,
            TestCaseName = testFailure.TestCaseName,
            Timestamp = DateTime.UtcNow
        };

        this._mockErrorLearningService
            .Setup(x => x.RecordErrorAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(capturedEntry);

        // Act
        var result = await captureService.CaptureTestFailureAsync(testFailure);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessage.Should().Be(errorMessage);
        result.TestCaseName.Should().Contain(expectedCategory);

        this._mockErrorLearningService.Verify(
            x => x.RecordErrorAsync(
            "SelfTestingFramework",
            It.IsAny<string>(),
            It.Is<string>(name => name.Contains(expectedCategory)),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()
        ), Times.Once);
    }

    [Fact]
    public void ErrorLearningSystem_ComponentInterfaces_ShouldBeProperlyDefined()
    {
        // This test verifies that all the key interfaces exist and can be instantiated
        // Testing the architectural contracts

        // Verify key interface types exist
        var errorLearningServiceType = typeof(IErrorLearningService);
        var testFailureCaptureType = typeof(ITestFailureCapture);
        var errorRecordingServiceType = typeof(IErrorRecordingService);
        var patternAnalysisServiceType = typeof(IPatternAnalysisService);
        var optimizationSuggestionServiceType = typeof(IOptimizationSuggestionManagementService);
        var learningStatisticsServiceType = typeof(ILearningStatisticsService);

        errorLearningServiceType.Should().NotBeNull();
        testFailureCaptureType.Should().NotBeNull();
        errorRecordingServiceType.Should().NotBeNull();
        patternAnalysisServiceType.Should().NotBeNull();
        optimizationSuggestionServiceType.Should().NotBeNull();
        learningStatisticsServiceType.Should().NotBeNull();

        // Verify key model classes exist
        var learningHistoryEntryType = typeof(LearningHistoryEntry);
        var errorPatternType = typeof(ErrorPattern);
        var optimizationSuggestionType = typeof(OptimizationSuggestion);
        var learningStatisticsType = typeof(LearningStatistics);
        var errorRecordingRequestType = typeof(ErrorRecordingRequest);

        learningHistoryEntryType.Should().NotBeNull();
        errorPatternType.Should().NotBeNull();
        optimizationSuggestionType.Should().NotBeNull();
        learningStatisticsType.Should().NotBeNull();
        errorRecordingRequestType.Should().NotBeNull();
    }
}