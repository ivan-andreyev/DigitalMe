using Xunit;
using FluentAssertions;
using DigitalMe.Services.Usage;
using DigitalMe.Services.Notifications;
using DigitalMe.Repositories;
using DigitalMe.Data.Entities;
using DigitalMe.Models.Usage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DigitalMe.Tests.Unit.Services.Usage;

/// <summary>
/// TDD Test Suite for QuotaManager
/// RED PHASE: Tests should initially fail until implementation is complete
/// Tests cover quota enforcement, reset logic, and notification thresholds
/// </summary>
public class QuotaManagerTests
{
    private readonly Mock<IApiUsageRepository> _mockRepo;
    private readonly Mock<INotificationService> _mockNotifier;
    private readonly QuotaManager _manager;

    public QuotaManagerTests()
    {
        _mockRepo = new Mock<IApiUsageRepository>();
        _mockNotifier = new Mock<INotificationService>();
        _manager = new QuotaManager(
            _mockRepo.Object,
            _mockNotifier.Object,
            NullLogger<QuotaManager>.Instance);
    }

    #region CanUseTokensAsync Tests

    [Fact]
    public async Task CanUseTokensAsync_Should_Allow_Usage_Within_Limit()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "Anthropic";

        var quota = new UserQuota
        {
            UserId = userId,
            Provider = provider,
            DailyTokenLimit = 10000,
            SubscriptionTier = "Free"
        };

        var dailyUsage = new DailyUsage
        {
            UserId = userId,
            Provider = provider,
            Date = DateTime.Today,
            TokensUsed = 5000 // 50% used
        };

        _mockRepo
            .Setup(r => r.GetUserQuotaAsync(userId, provider))
            .ReturnsAsync(quota);

        _mockRepo
            .Setup(r => r.GetDailyUsageAsync(userId, provider, DateTime.Today))
            .ReturnsAsync(dailyUsage);

        // Act
        var canUse = await _manager.CanUseTokensAsync(userId, provider, 3000);

        // Assert
        canUse.Should().BeTrue(); // 5000 + 3000 = 8000 <= 10000
    }

    [Fact]
    public async Task CanUseTokensAsync_Should_Reject_Usage_Exceeding_Limit()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "Anthropic";

        var quota = new UserQuota
        {
            UserId = userId,
            Provider = provider,
            DailyTokenLimit = 10000
        };

        var dailyUsage = new DailyUsage
        {
            UserId = userId,
            Provider = provider,
            Date = DateTime.Today,
            TokensUsed = 9500 // 95% used
        };

        _mockRepo
            .Setup(r => r.GetUserQuotaAsync(userId, provider))
            .ReturnsAsync(quota);

        _mockRepo
            .Setup(r => r.GetDailyUsageAsync(userId, provider, DateTime.Today))
            .ReturnsAsync(dailyUsage);

        // Act
        var canUse = await _manager.CanUseTokensAsync(userId, provider, 600);

        // Assert
        canUse.Should().BeFalse(); // 9500 + 600 = 10100 > 10000
    }

    [Fact]
    public async Task CanUseTokensAsync_Should_Use_Default_Quota_When_Not_Configured()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "Anthropic";

        _mockRepo
            .Setup(r => r.GetUserQuotaAsync(userId, provider))
            .ReturnsAsync((UserQuota?)null); // No quota configured

        var dailyUsage = new DailyUsage
        {
            UserId = userId,
            Provider = provider,
            Date = DateTime.Today,
            TokensUsed = 5000
        };

        _mockRepo
            .Setup(r => r.GetDailyUsageAsync(userId, provider, DateTime.Today))
            .ReturnsAsync(dailyUsage);

        // Act
        var canUse = await _manager.CanUseTokensAsync(userId, provider, 4000);

        // Assert
        canUse.Should().BeTrue(); // Default 10K limit: 5000 + 4000 = 9000 <= 10000
    }

    [Fact]
    public async Task CanUseTokensAsync_Should_Allow_When_No_Usage_Yet()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "Anthropic";

        var quota = new UserQuota
        {
            UserId = userId,
            Provider = provider,
            DailyTokenLimit = 10000
        };

        _mockRepo
            .Setup(r => r.GetUserQuotaAsync(userId, provider))
            .ReturnsAsync(quota);

        _mockRepo
            .Setup(r => r.GetDailyUsageAsync(userId, provider, DateTime.Today))
            .ReturnsAsync((DailyUsage?)null); // No usage today

        // Act
        var canUse = await _manager.CanUseTokensAsync(userId, provider, 5000);

        // Assert
        canUse.Should().BeTrue(); // 0 + 5000 = 5000 <= 10000
    }

    [Theory]
    [InlineData(null, "Anthropic", 1000)]
    [InlineData("", "Anthropic", 1000)]
    [InlineData("user123", null, 1000)]
    [InlineData("user123", "", 1000)]
    [InlineData("user123", "Anthropic", -1)]
    public async Task CanUseTokensAsync_Should_Reject_Invalid_Inputs(
        string userId,
        string provider,
        int tokens)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _manager.CanUseTokensAsync(userId, provider, tokens));
    }

    #endregion

    #region GetQuotaStatusAsync Tests

    [Fact]
    public async Task GetQuotaStatusAsync_Should_Return_Correct_Status()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "Anthropic";

        var quota = new UserQuota
        {
            UserId = userId,
            Provider = provider,
            DailyTokenLimit = 10000
        };

        var dailyUsage = new DailyUsage
        {
            UserId = userId,
            Provider = provider,
            Date = DateTime.Today,
            TokensUsed = 7500 // 75% used
        };

        _mockRepo
            .Setup(r => r.GetUserQuotaAsync(userId, provider))
            .ReturnsAsync(quota);

        _mockRepo
            .Setup(r => r.GetDailyUsageAsync(userId, provider, DateTime.Today))
            .ReturnsAsync(dailyUsage);

        // Act
        var status = await _manager.GetQuotaStatusAsync(userId, provider);

        // Assert
        status.DailyLimit.Should().Be(10000);
        status.Used.Should().Be(7500);
        status.Remaining.Should().Be(2500);
        status.PercentUsed.Should().Be(75);
        status.ResetsAt.Should().Be(DateTime.Today.AddDays(1));
    }

    [Fact]
    public async Task GetQuotaStatusAsync_Should_Handle_Exceeded_Quota()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "Anthropic";

        var quota = new UserQuota
        {
            UserId = userId,
            Provider = provider,
            DailyTokenLimit = 10000
        };

        var dailyUsage = new DailyUsage
        {
            UserId = userId,
            Provider = provider,
            Date = DateTime.Today,
            TokensUsed = 12000 // 120% used (exceeded)
        };

        _mockRepo
            .Setup(r => r.GetUserQuotaAsync(userId, provider))
            .ReturnsAsync(quota);

        _mockRepo
            .Setup(r => r.GetDailyUsageAsync(userId, provider, DateTime.Today))
            .ReturnsAsync(dailyUsage);

        // Act
        var status = await _manager.GetQuotaStatusAsync(userId, provider);

        // Assert
        status.Used.Should().Be(12000);
        status.Remaining.Should().Be(0); // Math.Max(0, ...) ensures non-negative
        status.PercentUsed.Should().Be(120);
    }

    [Fact]
    public async Task GetQuotaStatusAsync_Should_Handle_No_Usage()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "Anthropic";

        var quota = new UserQuota
        {
            UserId = userId,
            Provider = provider,
            DailyTokenLimit = 10000
        };

        _mockRepo
            .Setup(r => r.GetUserQuotaAsync(userId, provider))
            .ReturnsAsync(quota);

        _mockRepo
            .Setup(r => r.GetDailyUsageAsync(userId, provider, DateTime.Today))
            .ReturnsAsync((DailyUsage?)null);

        // Act
        var status = await _manager.GetQuotaStatusAsync(userId, provider);

        // Assert
        status.DailyLimit.Should().Be(10000);
        status.Used.Should().Be(0);
        status.Remaining.Should().Be(10000);
        status.PercentUsed.Should().Be(0);
    }

    #endregion

    #region GetOrCreateDailyUsageAsync Tests

    [Fact]
    public async Task GetOrCreateDailyUsageAsync_Should_Return_Existing_Usage_For_Today()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "Anthropic";

        var existingUsage = new DailyUsage
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = provider,
            Date = DateTime.Today,
            TokensUsed = 5000
        };

        _mockRepo
            .Setup(r => r.GetDailyUsageAsync(userId, provider, DateTime.Today))
            .ReturnsAsync(existingUsage);

        // Act
        var usage = await _manager.GetOrCreateDailyUsageAsync(userId, provider);

        // Assert
        usage.Should().BeSameAs(existingUsage);
        usage.Date.Should().Be(DateTime.Today);
        _mockRepo.Verify(r => r.CreateDailyUsageAsync(It.IsAny<DailyUsage>()), Times.Never);
    }

    [Fact]
    public async Task GetOrCreateDailyUsageAsync_Should_Create_New_Usage_If_Not_Exists()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "Anthropic";

        _mockRepo
            .Setup(r => r.GetDailyUsageAsync(userId, provider, DateTime.Today))
            .ReturnsAsync((DailyUsage?)null);

        _mockRepo
            .Setup(r => r.CreateDailyUsageAsync(It.IsAny<DailyUsage>()))
            .ReturnsAsync((DailyUsage du) => du);

        // Act
        var usage = await _manager.GetOrCreateDailyUsageAsync(userId, provider);

        // Assert
        usage.UserId.Should().Be(userId);
        usage.Provider.Should().Be(provider);
        usage.Date.Should().Be(DateTime.Today);
        usage.TokensUsed.Should().Be(0);
        _mockRepo.Verify(r => r.CreateDailyUsageAsync(It.IsAny<DailyUsage>()), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateDailyUsageAsync_Should_Reset_Usage_For_New_Day()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "Anthropic";

        var yesterdayUsage = new DailyUsage
        {
            UserId = userId,
            Provider = provider,
            Date = DateTime.Today.AddDays(-1), // Yesterday
            TokensUsed = 10000
        };

        _mockRepo
            .Setup(r => r.GetDailyUsageAsync(userId, provider, DateTime.Today))
            .ReturnsAsync((DailyUsage?)null); // No usage for today

        _mockRepo
            .Setup(r => r.CreateDailyUsageAsync(It.IsAny<DailyUsage>()))
            .ReturnsAsync((DailyUsage du) => du);

        // Act
        var usage = await _manager.GetOrCreateDailyUsageAsync(userId, provider);

        // Assert
        usage.Date.Should().Be(DateTime.Today); // New day
        usage.TokensUsed.Should().Be(0); // Reset
        _mockRepo.Verify(r => r.CreateDailyUsageAsync(
            It.Is<DailyUsage>(du => du.Date == DateTime.Today && du.TokensUsed == 0)),
            Times.Once);
    }

    #endregion

    #region UpdateUsageAsync Tests

    [Fact]
    public async Task UpdateUsageAsync_Should_Update_Daily_Usage()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "Anthropic";

        var quota = new UserQuota
        {
            UserId = userId,
            Provider = provider,
            DailyTokenLimit = 10000
        };

        var dailyUsage = new DailyUsage
        {
            UserId = userId,
            Provider = provider,
            Date = DateTime.Today,
            TokensUsed = 5000
        };

        _mockRepo
            .Setup(r => r.GetUserQuotaAsync(userId, provider))
            .ReturnsAsync(quota);

        _mockRepo
            .Setup(r => r.GetDailyUsageAsync(userId, provider, DateTime.Today))
            .ReturnsAsync(dailyUsage);

        _mockRepo
            .Setup(r => r.UpdateDailyUsageAsync(It.IsAny<DailyUsage>()))
            .ReturnsAsync((DailyUsage du) => du);

        // Act
        await _manager.UpdateUsageAsync(userId, provider, 2000);

        // Assert
        _mockRepo.Verify(r => r.UpdateDailyUsageAsync(
            It.Is<DailyUsage>(du => du.TokensUsed == 7000)), // 5000 + 2000
            Times.Once);
    }

    [Fact]
    public async Task UpdateUsageAsync_Should_Send_Warning_At_80_Percent()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "Anthropic";

        var quota = new UserQuota
        {
            UserId = userId,
            Provider = provider,
            DailyTokenLimit = 10000,
            NotificationsEnabled = true
        };

        var dailyUsage = new DailyUsage
        {
            UserId = userId,
            Provider = provider,
            Date = DateTime.Today,
            TokensUsed = 7000
        };

        _mockRepo
            .Setup(r => r.GetUserQuotaAsync(userId, provider))
            .ReturnsAsync(quota);

        _mockRepo
            .Setup(r => r.GetDailyUsageAsync(userId, provider, DateTime.Today))
            .ReturnsAsync(dailyUsage);

        _mockRepo
            .Setup(r => r.UpdateDailyUsageAsync(It.IsAny<DailyUsage>()))
            .ReturnsAsync((DailyUsage du) => du);

        // Act
        await _manager.UpdateUsageAsync(userId, provider, 1500); // 85% total

        // Assert
        _mockNotifier.Verify(n => n.SendQuotaWarningAsync(
            userId,
            It.Is<decimal>(p => p >= 80 && p < 90)),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUsageAsync_Should_Send_Warning_At_90_Percent()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "Anthropic";

        var quota = new UserQuota
        {
            UserId = userId,
            Provider = provider,
            DailyTokenLimit = 10000,
            NotificationsEnabled = true
        };

        var dailyUsage = new DailyUsage
        {
            UserId = userId,
            Provider = provider,
            Date = DateTime.Today,
            TokensUsed = 8500
        };

        _mockRepo
            .Setup(r => r.GetUserQuotaAsync(userId, provider))
            .ReturnsAsync(quota);

        _mockRepo
            .Setup(r => r.GetDailyUsageAsync(userId, provider, DateTime.Today))
            .ReturnsAsync(dailyUsage);

        _mockRepo
            .Setup(r => r.UpdateDailyUsageAsync(It.IsAny<DailyUsage>()))
            .ReturnsAsync((DailyUsage du) => du);

        // Act
        await _manager.UpdateUsageAsync(userId, provider, 700); // 92% total

        // Assert
        _mockNotifier.Verify(n => n.SendQuotaWarningAsync(
            userId,
            It.Is<decimal>(p => p >= 90 && p < 100)),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUsageAsync_Should_Send_Exceeded_At_100_Percent()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "Anthropic";

        var quota = new UserQuota
        {
            UserId = userId,
            Provider = provider,
            DailyTokenLimit = 10000,
            NotificationsEnabled = true
        };

        var dailyUsage = new DailyUsage
        {
            UserId = userId,
            Provider = provider,
            Date = DateTime.Today,
            TokensUsed = 9500
        };

        _mockRepo
            .Setup(r => r.GetUserQuotaAsync(userId, provider))
            .ReturnsAsync(quota);

        _mockRepo
            .Setup(r => r.GetDailyUsageAsync(userId, provider, DateTime.Today))
            .ReturnsAsync(dailyUsage);

        _mockRepo
            .Setup(r => r.UpdateDailyUsageAsync(It.IsAny<DailyUsage>()))
            .ReturnsAsync((DailyUsage du) => du);

        // Act
        await _manager.UpdateUsageAsync(userId, provider, 1000); // 105% total

        // Assert
        _mockNotifier.Verify(n => n.SendQuotaExceededAsync(userId), Times.Once);
        _mockNotifier.Verify(n => n.SendQuotaWarningAsync(It.IsAny<string>(), It.IsAny<decimal>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUsageAsync_Should_Not_Send_Notifications_When_Disabled()
    {
        // Arrange
        const string userId = "user123";
        const string provider = "Anthropic";

        var quota = new UserQuota
        {
            UserId = userId,
            Provider = provider,
            DailyTokenLimit = 10000,
            NotificationsEnabled = false // Disabled
        };

        var dailyUsage = new DailyUsage
        {
            UserId = userId,
            Provider = provider,
            Date = DateTime.Today,
            TokensUsed = 8000
        };

        _mockRepo
            .Setup(r => r.GetUserQuotaAsync(userId, provider))
            .ReturnsAsync(quota);

        _mockRepo
            .Setup(r => r.GetDailyUsageAsync(userId, provider, DateTime.Today))
            .ReturnsAsync(dailyUsage);

        _mockRepo
            .Setup(r => r.UpdateDailyUsageAsync(It.IsAny<DailyUsage>()))
            .ReturnsAsync((DailyUsage du) => du);

        // Act
        await _manager.UpdateUsageAsync(userId, provider, 1500); // 95% total

        // Assert
        _mockNotifier.Verify(n => n.SendQuotaWarningAsync(It.IsAny<string>(), It.IsAny<decimal>()), Times.Never);
        _mockNotifier.Verify(n => n.SendQuotaExceededAsync(It.IsAny<string>()), Times.Never);
    }

    #endregion
}