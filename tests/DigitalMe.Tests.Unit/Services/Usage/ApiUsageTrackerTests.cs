using Xunit;
using FluentAssertions;
using DigitalMe.Services.Usage;
using DigitalMe.Repositories;
using DigitalMe.Data.Entities;
using DigitalMe.Models.Usage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DigitalMe.Tests.Unit.Services.Usage;

/// <summary>
/// TDD Test Suite for ApiUsageTracker
/// RED PHASE: Tests should initially fail until implementation is complete
/// Tests cover usage recording, cost calculation, and statistics aggregation
/// </summary>
public class ApiUsageTrackerTests
{
    private readonly Mock<IApiUsageRepository> _mockRepo;
    private readonly ApiUsageTracker _tracker;

    public ApiUsageTrackerTests()
    {
        _mockRepo = new Mock<IApiUsageRepository>();
        _tracker = new ApiUsageTracker(
            _mockRepo.Object,
            NullLogger<ApiUsageTracker>.Instance);
    }

    #region RecordUsageAsync Tests

    [Fact]
    public async Task RecordUsageAsync_Should_Save_Usage_Record_With_Correct_Details()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "Anthropic";

        var details = new UsageDetails
        {
            RequestType = "chat.completion",
            TokensUsed = 1500,
            Provider = provider,
            ResponseTime = 1234,
            Success = true
        };

        _mockRepo
            .Setup(r => r.SaveUsageRecordAsync(It.IsAny<ApiUsageRecord>()))
            .ReturnsAsync((ApiUsageRecord record) => record);

        // Act
        await _tracker.RecordUsageAsync(userId, provider, details);

        // Assert
        _mockRepo.Verify(r => r.SaveUsageRecordAsync(
            It.Is<ApiUsageRecord>(u =>
                u.UserId == userId &&
                u.Provider == provider &&
                u.RequestType == details.RequestType &&
                u.TokensUsed == details.TokensUsed &&
                u.ResponseTimeMs == details.ResponseTime &&
                u.Success == details.Success)),
            Times.Once);
    }

    [Fact]
    public async Task RecordUsageAsync_Should_Calculate_And_Store_Cost_Estimate()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "Anthropic";
        const int tokens = 1000;
        const decimal expectedCost = 0.015m; // $0.015 per 1K tokens

        var details = new UsageDetails
        {
            RequestType = "chat.completion",
            TokensUsed = tokens,
            Provider = provider,
            ResponseTime = 1000,
            Success = true
        };

        _mockRepo
            .Setup(r => r.SaveUsageRecordAsync(It.IsAny<ApiUsageRecord>()))
            .ReturnsAsync((ApiUsageRecord record) => record);

        // Act
        await _tracker.RecordUsageAsync(userId, provider, details);

        // Assert
        _mockRepo.Verify(r => r.SaveUsageRecordAsync(
            It.Is<ApiUsageRecord>(u =>
                u.CostEstimate == expectedCost)),
            Times.Once);
    }

    [Fact]
    public async Task RecordUsageAsync_Should_Store_Error_Details_On_Failure()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "Anthropic";
        const string errorType = "RateLimitExceeded";

        var details = new UsageDetails
        {
            RequestType = "chat.completion",
            TokensUsed = 0,
            Provider = provider,
            ResponseTime = 500,
            Success = false,
            ErrorType = errorType
        };

        _mockRepo
            .Setup(r => r.SaveUsageRecordAsync(It.IsAny<ApiUsageRecord>()))
            .ReturnsAsync((ApiUsageRecord record) => record);

        // Act
        await _tracker.RecordUsageAsync(userId, provider, details);

        // Assert
        _mockRepo.Verify(r => r.SaveUsageRecordAsync(
            It.Is<ApiUsageRecord>(u =>
                u.Success == false &&
                u.ErrorType == errorType)),
            Times.Once);
    }

    [Fact]
    public async Task RecordUsageAsync_Should_Update_Daily_Usage()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "Anthropic";
        const int tokens = 1500;

        var details = new UsageDetails
        {
            RequestType = "chat.completion",
            TokensUsed = tokens,
            Provider = provider,
            ResponseTime = 1000,
            Success = true
        };

        var existingDailyUsage = new DailyUsage
        {
            UserId = userId,
            Provider = provider,
            Date = DateTime.Today,
            TokensUsed = 1000,
            RequestCount = 5
        };

        _mockRepo
            .Setup(r => r.GetDailyUsageAsync(userId, provider, DateTime.Today))
            .ReturnsAsync(existingDailyUsage);

        _mockRepo
            .Setup(r => r.UpdateDailyUsageAsync(It.IsAny<DailyUsage>()))
            .ReturnsAsync((DailyUsage du) => du);

        _mockRepo
            .Setup(r => r.SaveUsageRecordAsync(It.IsAny<ApiUsageRecord>()))
            .ReturnsAsync((ApiUsageRecord record) => record);

        // Act
        await _tracker.RecordUsageAsync(userId, provider, details);

        // Assert
        _mockRepo.Verify(r => r.UpdateDailyUsageAsync(
            It.Is<DailyUsage>(du =>
                du.TokensUsed == 2500)), // 1000 + 1500
            Times.Once);
    }

    [Fact]
    public async Task RecordUsageAsync_Should_Create_Daily_Usage_If_Not_Exists()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "Anthropic";
        const int tokens = 1500;

        var details = new UsageDetails
        {
            RequestType = "chat.completion",
            TokensUsed = tokens,
            Provider = provider,
            ResponseTime = 1000,
            Success = true
        };

        _mockRepo
            .Setup(r => r.GetDailyUsageAsync(userId, provider, DateTime.Today))
            .ReturnsAsync((DailyUsage?)null);

        _mockRepo
            .Setup(r => r.CreateDailyUsageAsync(It.IsAny<DailyUsage>()))
            .ReturnsAsync((DailyUsage du) => du);

        _mockRepo
            .Setup(r => r.SaveUsageRecordAsync(It.IsAny<ApiUsageRecord>()))
            .ReturnsAsync((ApiUsageRecord record) => record);

        // Act
        await _tracker.RecordUsageAsync(userId, provider, details);

        // Assert
        _mockRepo.Verify(r => r.CreateDailyUsageAsync(
            It.Is<DailyUsage>(du =>
                du.UserId == userId &&
                du.Provider == provider &&
                du.Date == DateTime.Today &&
                du.TokensUsed == tokens)),
            Times.Once);
    }

    [Theory]
    [InlineData(null, "Anthropic")]
    [InlineData("", "Anthropic")]
    [InlineData("user123", null)]
    [InlineData("user123", "")]
    public async Task RecordUsageAsync_Should_Reject_Invalid_Inputs(string userId, string provider)
    {
        // Arrange
        var details = new UsageDetails
        {
            RequestType = "chat.completion",
            TokensUsed = 1000,
            Provider = provider,
            ResponseTime = 1000,
            Success = true
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _tracker.RecordUsageAsync(userId, provider, details));
    }

    #endregion

    #region CalculateCost Tests

    [Theory]
    [InlineData("Anthropic", 1000, 0.015)]      // $0.015 per 1K tokens
    [InlineData("Anthropic", 2000, 0.030)]      // $0.030 per 2K tokens
    [InlineData("OpenAI", 1000, 0.020)]         // $0.020 per 1K tokens (higher rate)
    [InlineData("Slack", 1000, 0.0)]            // Free API
    [InlineData("GitHub", 1000, 0.0)]           // Free API
    public void CalculateCost_Should_Return_Correct_Cost_For_Provider(
        string provider,
        int tokens,
        decimal expectedCost)
    {
        // Act
        var cost = _tracker.CalculateCost(provider, tokens);

        // Assert
        cost.Should().Be(expectedCost);
    }

    [Fact]
    public void CalculateCost_Should_Return_Zero_For_Unknown_Provider()
    {
        // Arrange
        const string unknownProvider = "UnknownProvider";

        // Act
        var cost = _tracker.CalculateCost(unknownProvider, 1000);

        // Assert
        cost.Should().Be(0);
    }

    [Theory]
    [InlineData("Anthropic", 0, 0.0)]           // Zero tokens
    [InlineData("OpenAI", 500, 0.010)]          // Half of 1K
    [InlineData("Anthropic", 1, 0.000015)]      // Single token
    public void CalculateCost_Should_Handle_Various_Token_Counts(
        string provider,
        int tokens,
        decimal expectedCost)
    {
        // Act
        var cost = _tracker.CalculateCost(provider, tokens);

        // Assert
        cost.Should().Be(expectedCost);
    }

    #endregion

    #region GetUsageStatsAsync Tests

    [Fact]
    public async Task GetUsageStatsAsync_Should_Aggregate_Total_Tokens_Correctly()
    {
        // Arrange
        const string userId = "user123";
        var startDate = DateTime.Today.AddDays(-7);
        var endDate = DateTime.Today;

        var records = new List<ApiUsageRecord>
        {
            new() { TokensUsed = 1000, CostEstimate = 0.015m, Success = true, ResponseTimeMs = 1000 },
            new() { TokensUsed = 2000, CostEstimate = 0.030m, Success = true, ResponseTimeMs = 1200 },
            new() { TokensUsed = 1500, CostEstimate = 0.0225m, Success = true, ResponseTimeMs = 1100 }
        };

        _mockRepo
            .Setup(r => r.GetUsageRecordsAsync(userId, startDate, endDate))
            .ReturnsAsync(records);

        // Act
        var stats = await _tracker.GetUsageStatsAsync(userId, startDate, endDate);

        // Assert
        stats.TotalTokens.Should().Be(4500);
        stats.RequestCount.Should().Be(3);
    }

    [Fact]
    public async Task GetUsageStatsAsync_Should_Aggregate_Total_Cost_Correctly()
    {
        // Arrange
        const string userId = "user123";
        var startDate = DateTime.Today.AddDays(-7);
        var endDate = DateTime.Today;

        var records = new List<ApiUsageRecord>
        {
            new() { TokensUsed = 1000, CostEstimate = 0.015m, Success = true },
            new() { TokensUsed = 2000, CostEstimate = 0.030m, Success = true },
            new() { TokensUsed = 1500, CostEstimate = 0.0225m, Success = true }
        };

        _mockRepo
            .Setup(r => r.GetUsageRecordsAsync(userId, startDate, endDate))
            .ReturnsAsync(records);

        // Act
        var stats = await _tracker.GetUsageStatsAsync(userId, startDate, endDate);

        // Assert
        stats.TotalCost.Should().Be(0.0675m);
    }

    [Fact]
    public async Task GetUsageStatsAsync_Should_Calculate_Success_Rate()
    {
        // Arrange
        const string userId = "user123";
        var startDate = DateTime.Today.AddDays(-7);
        var endDate = DateTime.Today;

        var records = new List<ApiUsageRecord>
        {
            new() { Success = true, TokensUsed = 1000 },
            new() { Success = true, TokensUsed = 1000 },
            new() { Success = false, TokensUsed = 0 },
            new() { Success = true, TokensUsed = 1000 }
        };

        _mockRepo
            .Setup(r => r.GetUsageRecordsAsync(userId, startDate, endDate))
            .ReturnsAsync(records);

        // Act
        var stats = await _tracker.GetUsageStatsAsync(userId, startDate, endDate);

        // Assert
        stats.SuccessRate.Should().Be(75); // 3 out of 4 successful
    }

    [Fact]
    public async Task GetUsageStatsAsync_Should_Calculate_Average_Response_Time()
    {
        // Arrange
        const string userId = "user123";
        var startDate = DateTime.Today.AddDays(-7);
        var endDate = DateTime.Today;

        var records = new List<ApiUsageRecord>
        {
            new() { ResponseTimeMs = 1000, Success = true, TokensUsed = 1000 },
            new() { ResponseTimeMs = 1200, Success = true, TokensUsed = 1000 },
            new() { ResponseTimeMs = 1100, Success = true, TokensUsed = 1000 }
        };

        _mockRepo
            .Setup(r => r.GetUsageRecordsAsync(userId, startDate, endDate))
            .ReturnsAsync(records);

        // Act
        var stats = await _tracker.GetUsageStatsAsync(userId, startDate, endDate);

        // Assert
        stats.AverageResponseTime.Should().Be(1100); // (1000 + 1200 + 1100) / 3
    }

    [Fact]
    public async Task GetUsageStatsAsync_Should_Group_By_Provider()
    {
        // Arrange
        const string userId = "user123";
        var startDate = DateTime.Today.AddDays(-7);
        var endDate = DateTime.Today;

        var records = new List<ApiUsageRecord>
        {
            new() { Provider = "Anthropic", TokensUsed = 1000, CostEstimate = 0.015m, Success = true },
            new() { Provider = "Anthropic", TokensUsed = 1500, CostEstimate = 0.0225m, Success = true },
            new() { Provider = "OpenAI", TokensUsed = 2000, CostEstimate = 0.040m, Success = true }
        };

        _mockRepo
            .Setup(r => r.GetUsageRecordsAsync(userId, startDate, endDate))
            .ReturnsAsync(records);

        // Act
        var stats = await _tracker.GetUsageStatsAsync(userId, startDate, endDate);

        // Assert
        stats.ByProvider.Should().HaveCount(2);
        stats.ByProvider["Anthropic"].Tokens.Should().Be(2500);
        stats.ByProvider["Anthropic"].Cost.Should().Be(0.0375m);
        stats.ByProvider["Anthropic"].Requests.Should().Be(2);
        stats.ByProvider["OpenAI"].Tokens.Should().Be(2000);
        stats.ByProvider["OpenAI"].Cost.Should().Be(0.040m);
        stats.ByProvider["OpenAI"].Requests.Should().Be(1);
    }

    [Fact]
    public async Task GetUsageStatsAsync_Should_Handle_Empty_Records()
    {
        // Arrange
        const string userId = "user123";
        var startDate = DateTime.Today.AddDays(-7);
        var endDate = DateTime.Today;

        _mockRepo
            .Setup(r => r.GetUsageRecordsAsync(userId, startDate, endDate))
            .ReturnsAsync(new List<ApiUsageRecord>());

        // Act
        var stats = await _tracker.GetUsageStatsAsync(userId, startDate, endDate);

        // Assert
        stats.TotalTokens.Should().Be(0);
        stats.TotalCost.Should().Be(0);
        stats.RequestCount.Should().Be(0);
        stats.SuccessRate.Should().Be(0);
        stats.AverageResponseTime.Should().Be(0);
        stats.ByProvider.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GetUsageStatsAsync_Should_Reject_Invalid_UserId(string userId)
    {
        // Arrange
        var startDate = DateTime.Today.AddDays(-7);
        var endDate = DateTime.Today;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _tracker.GetUsageStatsAsync(userId, startDate, endDate));
    }

    #endregion
}