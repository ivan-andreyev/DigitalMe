using DigitalMe.Data.Entities;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace DigitalMe.Tests.Unit.Data.Entities;

/// <summary>
/// TDD tests for ApiUsageRecord entity.
/// These tests define the usage tracking requirements before implementation.
/// </summary>
public class ApiUsageRecordTests
{
    [Fact]
    public void ApiUsageRecord_Should_Track_Request_Details()
    {
        // Arrange & Act
        var usage = new ApiUsageRecord
        {
            UserId = "user123",
            Provider = "Anthropic",
            Model = "claude-3-opus",
            RequestType = "chat.completion",
            TokensUsed = 1500,
            CostEstimate = 0.0225m,
            ResponseTimeMs = 1234,
            Success = true
        };

        // Assert
        usage.UserId.Should().Be("user123");
        usage.Provider.Should().Be("Anthropic");
        usage.Model.Should().Be("claude-3-opus");
        usage.RequestType.Should().Be("chat.completion");
        usage.TokensUsed.Should().Be(1500);
        usage.CostEstimate.Should().Be(0.0225m);
        usage.ResponseTimeMs.Should().Be(1234);
        usage.Success.Should().BeTrue();
    }

    [Theory]
    [InlineData(null, "Anthropic")]
    [InlineData("user123", null)]
    public void ApiUsageRecord_Should_Require_Mandatory_Fields(string userId, string provider)
    {
        // Arrange
        var usage = new ApiUsageRecord
        {
            UserId = userId!,
            Provider = provider!
        };

        // Act
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(usage);
        var isValid = Validator.TryValidateObject(usage, context, validationResults, true);

        // Assert
        isValid.Should().BeFalse("entity should fail validation when required fields are null");
        validationResults.Should().NotBeEmpty("validation errors should be reported");
    }

    [Fact]
    public void ApiUsageRecord_Should_Calculate_Cost_With_Decimal_Precision()
    {
        // Arrange & Act - Test decimal precision for financial calculations
        var usage = new ApiUsageRecord
        {
            UserId = "user123",
            Provider = "Anthropic",
            TokensUsed = 1000,
            CostEstimate = 0.015m // $0.015 per 1K tokens
        };

        // Assert
        usage.CostEstimate.Should().BeOfType(typeof(decimal), "cost must use decimal for financial precision");
        usage.CostEstimate.Should().Be(0.015m);
    }

    [Fact]
    public void ApiUsageRecord_Should_Inherit_From_BaseEntity()
    {
        // Arrange & Act
        var usage = new ApiUsageRecord();

        // Assert
        usage.Should().BeAssignableTo<BaseEntity>("ApiUsageRecord must inherit from BaseEntity");
        usage.Id.Should().NotBeEmpty("BaseEntity provides Id");
        usage.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5), "BaseEntity provides CreatedAt");
        usage.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5), "BaseEntity provides UpdatedAt");
    }

    [Fact]
    public void ApiUsageRecord_Should_Track_Timestamp()
    {
        // Arrange
        var requestTime = DateTime.UtcNow.AddMinutes(-5);

        // Act
        var usage = new ApiUsageRecord
        {
            UserId = "user123",
            Provider = "Anthropic",
            RequestTimestamp = requestTime
        };

        // Assert
        usage.RequestTimestamp.Should().BeCloseTo(requestTime, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ApiUsageRecord_Should_Default_Timestamp_To_UtcNow()
    {
        // Arrange & Act
        var usage = new ApiUsageRecord
        {
            UserId = "user123",
            Provider = "Anthropic"
        };

        // Assert
        usage.RequestTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5),
            "RequestTimestamp should default to current UTC time");
    }

    [Fact]
    public void ApiUsageRecord_Should_Track_Success_Status()
    {
        // Arrange & Act - Successful request
        var successUsage = new ApiUsageRecord
        {
            UserId = "user123",
            Provider = "Anthropic",
            Success = true,
            ErrorType = null
        };

        // Arrange & Act - Failed request
        var failedUsage = new ApiUsageRecord
        {
            UserId = "user123",
            Provider = "Anthropic",
            Success = false,
            ErrorType = "RateLimitExceeded"
        };

        // Assert
        successUsage.Success.Should().BeTrue();
        successUsage.ErrorType.Should().BeNull();

        failedUsage.Success.Should().BeFalse();
        failedUsage.ErrorType.Should().Be("RateLimitExceeded");
    }

    [Fact]
    public void ApiUsageRecord_Should_Track_Response_Time()
    {
        // Arrange & Act
        var usage = new ApiUsageRecord
        {
            UserId = "user123",
            Provider = "Anthropic",
            ResponseTimeMs = 2345
        };

        // Assert
        usage.ResponseTimeMs.Should().Be(2345, "response time should be tracked in milliseconds");
    }

    [Fact]
    public void ApiUsageRecord_Should_Support_Optional_ConfigurationId()
    {
        // Arrange & Act - Usage with linked configuration
        var linkedUsage = new ApiUsageRecord
        {
            UserId = "user123",
            Provider = "Anthropic",
            ConfigurationId = Guid.NewGuid()
        };

        // Arrange & Act - Usage without linked configuration (user's own key)
        var unlimitedUsage = new ApiUsageRecord
        {
            UserId = "user123",
            Provider = "Anthropic",
            ConfigurationId = null
        };

        // Assert
        linkedUsage.ConfigurationId.Should().NotBeNull("ConfigurationId can link to ApiConfiguration");
        unlimitedUsage.ConfigurationId.Should().BeNull("ConfigurationId is optional for user's own keys");
    }

    [Fact]
    public void ApiUsageRecord_Should_Store_Request_Type()
    {
        // Arrange & Act - Different request types
        var chatUsage = new ApiUsageRecord
        {
            UserId = "user123",
            Provider = "Anthropic",
            RequestType = "chat.completion"
        };

        var embeddingUsage = new ApiUsageRecord
        {
            UserId = "user123",
            Provider = "OpenAI",
            RequestType = "embeddings"
        };

        // Assert
        chatUsage.RequestType.Should().Be("chat.completion");
        embeddingUsage.RequestType.Should().Be("embeddings");
    }

    [Fact]
    public void ApiUsageRecord_Should_Track_Token_Usage()
    {
        // Arrange & Act
        var usage = new ApiUsageRecord
        {
            UserId = "user123",
            Provider = "Anthropic",
            TokensUsed = 5000,
            InputTokens = 3000,
            OutputTokens = 2000
        };

        // Assert
        usage.TokensUsed.Should().Be(5000, "total tokens used should be tracked");
        usage.InputTokens.Should().Be(3000, "input tokens should be tracked separately");
        usage.OutputTokens.Should().Be(2000, "output tokens should be tracked separately");
    }

    [Fact]
    public void ApiUsageRecord_Should_Allow_Nullable_OptionalFields()
    {
        // Arrange & Act
        var usage = new ApiUsageRecord
        {
            UserId = "user123",
            Provider = "Anthropic",
            ConfigurationId = null,
            Model = null,
            RequestType = null,
            ErrorType = null,
            ErrorMessage = null
        };

        // Assert
        usage.ConfigurationId.Should().BeNull("ConfigurationId is optional");
        usage.Model.Should().BeNull("Model is optional");
        usage.RequestType.Should().BeNull("RequestType is optional");
        usage.ErrorType.Should().BeNull("ErrorType is optional");
        usage.ErrorMessage.Should().BeNull("ErrorMessage is optional");
    }

    [Fact]
    public void ApiUsageRecord_Should_Store_Error_Details()
    {
        // Arrange & Act
        var usage = new ApiUsageRecord
        {
            UserId = "user123",
            Provider = "Anthropic",
            Success = false,
            ErrorType = "InvalidApiKey",
            ErrorMessage = "The provided API key is invalid or has been revoked"
        };

        // Assert
        usage.Success.Should().BeFalse();
        usage.ErrorType.Should().Be("InvalidApiKey");
        usage.ErrorMessage.Should().Be("The provided API key is invalid or has been revoked");
    }

    [Fact]
    public void ApiUsageRecord_Should_Support_Multiple_Providers()
    {
        // Arrange & Act
        var anthropicUsage = new ApiUsageRecord
        {
            UserId = "user123",
            Provider = "Anthropic",
            Model = "claude-3-opus"
        };

        var openaiUsage = new ApiUsageRecord
        {
            UserId = "user123",
            Provider = "OpenAI",
            Model = "gpt-4-turbo"
        };

        var googleUsage = new ApiUsageRecord
        {
            UserId = "user123",
            Provider = "Google",
            Model = "gemini-pro"
        };

        // Assert
        anthropicUsage.Provider.Should().Be("Anthropic");
        openaiUsage.Provider.Should().Be("OpenAI");
        googleUsage.Provider.Should().Be("Google");
    }

    [Fact]
    public void ApiUsageRecord_Should_Default_Success_To_False()
    {
        // Arrange & Act
        var usage = new ApiUsageRecord
        {
            UserId = "user123",
            Provider = "Anthropic"
        };

        // Assert
        usage.Success.Should().BeFalse("Success should default to false until confirmed");
    }

    [Fact]
    public void ApiUsageRecord_Should_Default_Numeric_Fields_To_Zero()
    {
        // Arrange & Act
        var usage = new ApiUsageRecord
        {
            UserId = "user123",
            Provider = "Anthropic"
        };

        // Assert
        usage.TokensUsed.Should().Be(0, "TokensUsed should default to 0");
        usage.InputTokens.Should().Be(0, "InputTokens should default to 0");
        usage.OutputTokens.Should().Be(0, "OutputTokens should default to 0");
        usage.ResponseTimeMs.Should().Be(0, "ResponseTimeMs should default to 0");
        usage.CostEstimate.Should().Be(0m, "CostEstimate should default to 0");
    }
}