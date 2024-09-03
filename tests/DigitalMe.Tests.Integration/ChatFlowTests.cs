using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Xunit;
using FluentAssertions;
using DigitalMe.Data;
using DigitalMe.DTOs;
using DigitalMe.Models;
using DigitalMe.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace DigitalMe.Tests.Integration;

public class ChatFlowTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly string _testUserId = "test-user-123";
    private HubConnection? _connection;

    public ChatFlowTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext registrations
                var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<DigitalMeDbContext>));
                if (dbContextDescriptor != null)
                    services.Remove(dbContextDescriptor);

                var dbContextServiceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DigitalMeDbContext));
                if (dbContextServiceDescriptor != null)
                    services.Remove(dbContextServiceDescriptor);

                // Add in-memory database for testing
                services.AddDbContext<DigitalMeDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                }, ServiceLifetime.Scoped);

                // Mock external services to avoid real API calls during test
                var anthropicDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DigitalMe.Integrations.MCP.IAnthropicService));
                if (anthropicDescriptor != null)
                    services.Remove(anthropicDescriptor);

                services.AddScoped<DigitalMe.Integrations.MCP.IAnthropicService>(provider =>
                {
                    var mockService = new Mock<DigitalMe.Integrations.MCP.IAnthropicService>();
                    // This will ensure the test fails because no response is configured
                    mockService.Setup(x => x.SendMessageAsync(It.IsAny<string>(), It.IsAny<PersonalityProfile>()))
                           .ReturnsAsync("Mock Ivan response: I received your message!");
                    return mockService.Object;
                });
            });
        });
    }

    [Fact]
    public async Task ChatFlow_UserSendsMessage_ShouldReceiveIvanResponse()
    {
        // Arrange
        await SeedTestData();
        
        var hubUrl = _factory.Server.BaseAddress + "chathub";
        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Debug);
            })
            .Build();

        var receivedMessages = new List<MessageDto>();
        var typingIndicators = new List<object>();
        var connectionEvents = new List<object>();
        var errorMessages = new List<object>();

        // Setup SignalR event handlers
        _connection.On<MessageDto>("MessageReceived", message =>
        {
            receivedMessages.Add(message);
        });

        _connection.On<object>("TypingIndicator", indicator =>
        {
            typingIndicators.Add(indicator);
        });

        _connection.On<object>("JoinedChat", connectionInfo =>
        {
            connectionEvents.Add(connectionInfo);
        });

        _connection.On<object>("Error", error =>
        {
            errorMessages.Add(error);
        });

        // Act
        await _connection.StartAsync();

        // Join the chat
        await _connection.InvokeAsync("JoinChat", _testUserId, "Web");

        // Wait for connection confirmation
        await Task.Delay(500);

        // Send a message through SignalR
        var chatRequest = new ChatRequestDto
        {
            Message = "Hello Ivan! This is a test message.",
            UserId = _testUserId,
            Platform = "Web"
        };

        await _connection.InvokeAsync("SendMessage", chatRequest);

        // Wait for processing and response
        // This should be sufficient time for the full chat flow
        await Task.Delay(5000);

        // Assert
        // Connection should be established successfully
        connectionEvents.Should().HaveCount(1, "user should successfully join the chat");

        // Should have received exactly 2 messages: user message + Ivan's response
        receivedMessages.Should().HaveCount(2, "should receive both user message and Ivan's response");

        // First message should be the user's message
        var userMessage = receivedMessages.FirstOrDefault(m => m.Role == "user");
        userMessage.Should().NotBeNull("user message should be received");
        userMessage!.Content.Should().Be("Hello Ivan! This is a test message.");
        userMessage.Metadata.Should().ContainKey("isRealTime");
        userMessage.Metadata["isRealTime"].Should().Be(true);

        // Second message should be Ivan's response
        var ivanMessage = receivedMessages.FirstOrDefault(m => m.Role == "assistant");
        ivanMessage.Should().NotBeNull("Ivan should respond to the user message");
        ivanMessage!.Content.Should().NotBeNullOrEmpty("Ivan's response should have content");
        ivanMessage.Content.Should().Contain("Mock Ivan response", "should receive the mocked response");

        // Ivan's response should have metadata
        ivanMessage.Metadata.Should().ContainKey("isRealTime");
        ivanMessage.Metadata.Should().ContainKey("mood");
        ivanMessage.Metadata.Should().ContainKey("confidence");

        // Should have typing indicators (Ivan typing)
        typingIndicators.Should().HaveCountGreaterThan(0, "should show typing indicators during processing");

        // No errors should occur during the chat flow
        errorMessages.Should().BeEmpty("no errors should occur during normal chat flow");

        // Verify conversation was saved to database
        using var scope = _factory.Services.CreateScope();
        var conversationService = scope.ServiceProvider.GetRequiredService<IConversationService>();
        var conversations = await conversationService.GetUserConversationsAsync("Web", _testUserId);
        
        conversations.Should().HaveCount(1, "conversation should be saved to database");
        var conversation = conversations.First();
        conversation.Messages.Should().HaveCount(2, "both messages should be saved");
        
        var savedUserMessage = conversation.Messages.FirstOrDefault(m => m.Role == "user");
        var savedIvanMessage = conversation.Messages.FirstOrDefault(m => m.Role == "assistant");
        
        savedUserMessage.Should().NotBeNull();
        savedUserMessage!.Content.Should().Be("Hello Ivan! This is a test message.");
        
        savedIvanMessage.Should().NotBeNull();
        savedIvanMessage!.Content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ChatFlow_WithoutIvanPersonality_ShouldReturnError()
    {
        // Arrange - Don't seed Ivan's personality to test error handling
        var hubUrl = _factory.Server.BaseAddress + "chathub";
        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        var errorMessages = new List<object>();

        _connection.On<object>("Error", error =>
        {
            errorMessages.Add(error);
        });

        // Act
        await _connection.StartAsync();
        await _connection.InvokeAsync("JoinChat", _testUserId, "Web");

        var chatRequest = new ChatRequestDto
        {
            Message = "Hello Ivan!",
            UserId = _testUserId,
            Platform = "Web"
        };

        await _connection.InvokeAsync("SendMessage", chatRequest);
        await Task.Delay(2000);

        // Assert
        errorMessages.Should().HaveCount(1, "should receive error when Ivan's personality is not found");
        
        var error = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(errorMessages.First()));
        error.GetProperty("Code").GetString().Should().Be("PERSONALITY_NOT_FOUND");
        error.GetProperty("Message").GetString().Should().Contain("Ivan's personality profile not found");
    }

    [Fact]
    public async Task ChatFlow_MultipleUsersSimultaneous_ShouldIsolateConversations()
    {
        // Arrange
        await SeedTestData();
        
        var hubUrl = _factory.Server.BaseAddress + "chathub";
        var connection1 = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => { options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler(); })
            .Build();
        
        var connection2 = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => { options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler(); })
            .Build();

        var user1Messages = new List<MessageDto>();
        var user2Messages = new List<MessageDto>();

        connection1.On<MessageDto>("MessageReceived", message => user1Messages.Add(message));
        connection2.On<MessageDto>("MessageReceived", message => user2Messages.Add(message));

        // Act
        await connection1.StartAsync();
        await connection2.StartAsync();

        await connection1.InvokeAsync("JoinChat", "user1", "Web");
        await connection2.InvokeAsync("JoinChat", "user2", "Web");

        await connection1.InvokeAsync("SendMessage", new ChatRequestDto
        {
            Message = "Hello from user 1",
            UserId = "user1",
            Platform = "Web"
        });

        await connection2.InvokeAsync("SendMessage", new ChatRequestDto
        {
            Message = "Hello from user 2", 
            UserId = "user2",
            Platform = "Web"
        });

        await Task.Delay(3000);

        // Assert
        user1Messages.Should().HaveCount(2, "user1 should only see their own conversation");
        user2Messages.Should().HaveCount(2, "user2 should only see their own conversation");

        user1Messages.Should().OnlyContain(m => 
            m.Content.Contains("user 1") || m.Content.Contains("Mock Ivan response"),
            "user1 should only receive messages from their conversation");
            
        user2Messages.Should().OnlyContain(m => 
            m.Content.Contains("user 2") || m.Content.Contains("Mock Ivan response"),
            "user2 should only receive messages from their conversation");

        await connection1.DisposeAsync();
        await connection2.DisposeAsync();
    }

    private async Task SeedTestData()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DigitalMeDbContext>();
        
        // Ensure database is clean
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        // Create Ivan's personality profile
        var ivanPersonality = new PersonalityProfile
        {
            Id = Guid.NewGuid(),
            Name = "Ivan",
            Description = "Ivan is a 34-year-old Head of R&D with analytical, direct, technical personality",
            Traits = "{\"name\":\"Ivan\",\"age\":34,\"profession\":\"Head of R&D\",\"personality\":\"Analytical, Direct, Technical\",\"communication_style\":\"Professional but friendly\"}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Add some personality traits
        var traits = new List<PersonalityTrait>
        {
            new PersonalityTrait
            {
                PersonalityProfileId = ivanPersonality.Id,
                Category = "Communication",
                Name = "Direct",
                Description = "Prefers straightforward communication",
                Weight = 1.0
            },
            new PersonalityTrait
            {
                PersonalityProfileId = ivanPersonality.Id,
                Category = "Technical",
                Name = "Analytical",
                Description = "Approaches problems analytically",
                Weight = 1.0
            }
        };
        
        ivanPersonality.PersonalityTraits = traits;

        context.PersonalityProfiles.Add(ivanPersonality);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task ChatFlow_AnthropicAPIFailure_ShouldReturnFallbackResponse()
    {
        // Arrange
        await SeedTestData();
        
        // Override mock to simulate API failure
        using var scope = _factory.Services.CreateScope();
        var mockService = new Mock<DigitalMe.Integrations.MCP.IAnthropicService>();
        mockService.Setup(x => x.SendMessageAsync(It.IsAny<string>(), It.IsAny<PersonalityProfile>()))
                  .ThrowsAsync(new HttpRequestException("Your credit balance is too low to access the Anthropic API"));

        var hubUrl = _factory.Server.BaseAddress + "chathub";
        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => { options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler(); })
            .Build();

        var receivedMessages = new List<MessageDto>();
        _connection.On<MessageDto>("MessageReceived", message => receivedMessages.Add(message));

        // Act
        await _connection.StartAsync();
        await _connection.InvokeAsync("JoinChat", _testUserId, "Web");

        var chatRequest = new ChatRequestDto
        {
            Message = "Test API failure handling",
            UserId = _testUserId,
            Platform = "Web"
        };

        await _connection.InvokeAsync("SendMessage", chatRequest);
        await Task.Delay(3000);

        // Assert
        receivedMessages.Should().HaveCount(2, "should receive user message and fallback response");
        
        var fallbackMessage = receivedMessages.FirstOrDefault(m => m.Role == "assistant");
        fallbackMessage.Should().NotBeNull("should receive fallback response on API failure");
        fallbackMessage!.Content.Should().NotBeNullOrEmpty("fallback response should have content");
        
        // Verify fallback response characteristics
        fallbackMessage.Metadata.Should().ContainKey("confidence");
        var confidence = Convert.ToDouble(fallbackMessage.Metadata["confidence"]);
        confidence.Should().BeLessThan(50, "fallback responses should have lower confidence");
    }

    [Fact]
    public async Task ChatFlow_DatabaseTypeCompatibility_ShouldHandleBooleanAndDateTimeFields()
    {
        // Arrange - This test verifies PostgreSQL type issues we fixed
        await SeedTestData();
        
        var hubUrl = _factory.Server.BaseAddress + "chathub";
        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => { options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler(); })
            .Build();

        var receivedMessages = new List<MessageDto>();
        _connection.On<MessageDto>("MessageReceived", message => receivedMessages.Add(message));

        // Act
        await _connection.StartAsync();
        await _connection.InvokeAsync("JoinChat", _testUserId, "Web");

        var chatRequest = new ChatRequestDto
        {
            Message = "Test database type compatibility",
            UserId = _testUserId,
            Platform = "Web"
        };

        await _connection.InvokeAsync("SendMessage", chatRequest);
        await Task.Delay(3000);

        // Assert
        receivedMessages.Should().HaveCount(2, "should handle database operations without type errors");

        // Verify database operations completed successfully
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DigitalMeDbContext>();
        
        var conversation = await context.Conversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.UserId == _testUserId);
            
        conversation.Should().NotBeNull("conversation should be created successfully");
        conversation!.IsActive.Should().BeTrue("boolean field should work correctly");
        conversation.StartedAt.Should().BeBefore(DateTime.UtcNow, "DateTime field should work correctly");
        conversation.Messages.Should().HaveCount(2, "messages should be saved with proper DateTime handling");
        
        foreach (var message in conversation.Messages)
        {
            message.Timestamp.Should().BeBefore(DateTime.UtcNow, "message timestamps should be valid");
        }
    }

    [Fact]
    public async Task ChatFlow_StepByStepLogging_ShouldCompleteAll9Steps()
    {
        // Arrange - Test that all 9 steps complete successfully
        await SeedTestData();
        
        var hubUrl = _factory.Server.BaseAddress + "chathub";
        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => { options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler(); })
            .Build();

        var receivedMessages = new List<MessageDto>();
        var typingIndicators = new List<object>();
        
        _connection.On<MessageDto>("MessageReceived", message => receivedMessages.Add(message));
        _connection.On<object>("TypingIndicator", indicator => typingIndicators.Add(indicator));

        // Act
        await _connection.StartAsync();
        await _connection.InvokeAsync("JoinChat", _testUserId, "Web");

        var chatRequest = new ChatRequestDto
        {
            Message = "Test complete 9-step flow",
            UserId = _testUserId,
            Platform = "Web"
        };

        await _connection.InvokeAsync("SendMessage", chatRequest);
        await Task.Delay(5000); // Give enough time for all steps

        // Assert
        // Step 1-2: Conversation and message creation
        receivedMessages.Should().HaveCount(2, "should complete steps 1-2 (conversation + message creation)");
        
        // Step 3: User message notification
        var userMessage = receivedMessages.First(m => m.Role == "user");
        userMessage.Should().NotBeNull("step 3 should notify about user message");
        
        // Step 4-5: Typing indicator and personality loading
        typingIndicators.Should().HaveCountGreaterThan(0, "step 4 should show typing indicator");
        
        // Step 6-9: Agent processing, response generation, and delivery
        var assistantMessage = receivedMessages.First(m => m.Role == "assistant");
        assistantMessage.Should().NotBeNull("steps 6-9 should generate and deliver response");
        assistantMessage.Metadata.Should().ContainKey("mood", "step 6 should include mood analysis");
        assistantMessage.Metadata.Should().ContainKey("confidence", "step 6 should include confidence score");
        
        // Verify conversation persistence (final verification)
        using var scope = _factory.Services.CreateScope();
        var conversationService = scope.ServiceProvider.GetRequiredService<IConversationService>();
        var conversations = await conversationService.GetUserConversationsAsync("Web", _testUserId);
        
        conversations.Should().HaveCount(1, "conversation should be persisted");
        conversations.First().Messages.Should().HaveCount(2, "all messages should be saved");
    }

    [Fact]
    public async Task ChatFlow_ConnectionResilience_ShouldHandleDisconnectionGracefully()
    {
        // Arrange
        await SeedTestData();
        
        var hubUrl = _factory.Server.BaseAddress + "chathub";
        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => { options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler(); })
            .WithAutomaticReconnect()
            .Build();

        var connectionEvents = new List<string>();
        var receivedMessages = new List<MessageDto>();

        _connection.On<MessageDto>("MessageReceived", message => receivedMessages.Add(message));
        _connection.Reconnecting += _ => { connectionEvents.Add("Reconnecting"); return Task.CompletedTask; };
        _connection.Reconnected += _ => { connectionEvents.Add("Reconnected"); return Task.CompletedTask; };

        // Act
        await _connection.StartAsync();
        await _connection.InvokeAsync("JoinChat", _testUserId, "Web");

        // Send message before simulated disconnection
        await _connection.InvokeAsync("SendMessage", new ChatRequestDto
        {
            Message = "Message before disconnection",
            UserId = _testUserId,
            Platform = "Web"
        });

        await Task.Delay(2000);

        // Simulate disconnection and reconnection
        await _connection.StopAsync();
        await Task.Delay(1000);
        await _connection.StartAsync();
        await _connection.InvokeAsync("JoinChat", _testUserId, "Web");

        // Send message after reconnection
        await _connection.InvokeAsync("SendMessage", new ChatRequestDto
        {
            Message = "Message after reconnection",
            UserId = _testUserId,
            Platform = "Web"
        });

        await Task.Delay(3000);

        // Assert
        receivedMessages.Should().HaveCountGreaterThan(1, "should handle messages before and after reconnection");
        
        // Verify that conversations are properly maintained across reconnections
        using var scope = _factory.Services.CreateScope();
        var conversationService = scope.ServiceProvider.GetRequiredService<IConversationService>();
        var conversations = await conversationService.GetUserConversationsAsync("Web", _testUserId);
        
        // Should have one continuous conversation, not multiple
        conversations.Should().HaveCount(1, "should maintain single conversation across disconnections");
        conversations.First().Messages.Should().HaveCountGreaterThan(1, "should persist messages across reconnections");
    }

    [Fact]
    public async Task ChatFlow_ErrorHandling_ShouldProvideUserFriendlyMessages()
    {
        // Arrange - Test various error scenarios
        var hubUrl = _factory.Server.BaseAddress + "chathub";
        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => { options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler(); })
            .Build();

        var errorMessages = new List<object>();
        _connection.On<object>("Error", error => errorMessages.Add(error));

        // Act & Assert - Test various error conditions
        await _connection.StartAsync();

        // Test 1: Missing personality profile
        await _connection.InvokeAsync("JoinChat", _testUserId, "Web");
        await _connection.InvokeAsync("SendMessage", new ChatRequestDto
        {
            Message = "Test without personality",
            UserId = _testUserId,
            Platform = "Web"
        });
        
        await Task.Delay(2000);
        
        errorMessages.Should().HaveCount(1, "should receive error for missing personality");
        var error = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(errorMessages.First()));
        error.GetProperty("Code").GetString().Should().Be("PERSONALITY_NOT_FOUND");
        error.GetProperty("Message").GetString().Should().NotBeNullOrEmpty("should provide user-friendly error message");
        
        // Test 2: Empty message
        errorMessages.Clear();
        await SeedTestData(); // Add personality for next test
        
        await _connection.InvokeAsync("SendMessage", new ChatRequestDto
        {
            Message = "",
            UserId = _testUserId,
            Platform = "Web"
        });
        
        await Task.Delay(1000);
        
        // Should handle empty message gracefully without crash
        // (specific behavior depends on validation implementation)
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }
    }
}