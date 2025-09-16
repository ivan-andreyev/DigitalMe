using DigitalMe.Data.Entities;
using DigitalMe.Integrations.MCP;
using DigitalMe.Models;
using DigitalMe.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DigitalMe.Tests.Integration;

public class McpIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>, IAsyncDisposable
{
    private readonly CustomWebApplicationFactory<Program> _factory;
#pragma warning disable CS0414 // Field assigned but never used - intended for future MCP server configuration
    private readonly string _mcpServerUrl = "http://localhost:3000/mcp";

    public McpIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MCPClient_ShouldInitializeSuccessfully()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var mcpClient = scope.ServiceProvider.GetRequiredService<IMcpClient>();

        // Act
        var result = await mcpClient.InitializeAsync();

        // Assert
        result.Should().BeTrue("MCP client should initialize successfully with running server");
        mcpClient.IsConnected.Should().BeTrue("MCP client should be connected after initialization");
    }

    [Fact]
    public async Task MCPServiceProper_ShouldHandleMessageAsync()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var mcpService = scope.ServiceProvider.GetRequiredService<IMcpService>();
        var ivanPersonalityService = scope.ServiceProvider.GetRequiredService<IPersonalityService>();
        
        var personality = await ivanPersonalityService.GetPersonalityAsync();
        var context = new PersonalityContext
        {
            Profile = personality.Value,
            RecentMessages = new List<Message>(),
            CurrentState = new Dictionary<string, object>
            {
                ["userId"] = "test-user",
                ["platform"] = "Integration-Test",
                ["isRealTime"] = false
            }
        };

        // Act
        var response = await mcpService.SendMessageAsync("test", context);

        // Assert
        response.IsSuccess.Should().BeTrue("MCP service call should succeed");
        response.Value.Should().NotBeNullOrEmpty("MCP service should return response");

        // Should be Russian response from Ivan
        response.Value.Should().Contain("система работает", "should get test response from Ivan");
        response.Value.Should().Contain("MCP протокол", "should mention MCP protocol");
    }

    [Fact]
    public async Task MCPClient_ShouldListToolsSuccessfully()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var mcpClient = scope.ServiceProvider.GetRequiredService<IMcpClient>();
        await mcpClient.InitializeAsync();

        // Act
        var tools = await mcpClient.ListToolsAsync();

        // Assert
        tools.Should().NotBeEmpty("MCP server should provide tools");
        tools.Should().Contain(t => t.Name == "get_personality_info", "should have personality info tool");
        tools.Should().Contain(t => t.Name == "structured_thinking", "should have structured thinking tool");
    }

    [Fact]
    public async Task MCPClient_ShouldCallToolSuccessfully()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var mcpClient = scope.ServiceProvider.GetRequiredService<IMcpClient>();
        await mcpClient.InitializeAsync();

        // Act - Call structured_thinking tool
        var result = await mcpClient.CallToolAsync("structured_thinking", 
            new Dictionary<string, object> { ["problem"] = "Optimize database queries" });

        // Assert
        result.Should().NotBeNull("tool call should return result");
        result.Error.Should().BeNull("tool call should not have errors");
        result.Result.Should().NotBeNull("tool call should have result content");
        result.Result.Content.Should().Contain("ФАКТОРЫ", "should contain Ivan's structured analysis");
    }

    [Fact]
    public async Task EndToEnd_MCPIntegration_ShouldWorkThroughMessageProcessor()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var messageProcessor = scope.ServiceProvider.GetRequiredService<IMessageProcessor>();
        
        var chatRequest = new DigitalMe.DTOs.ChatRequestDto
        {
            Message = "Как принимать решения?",
            UserId = "integration-test-user",
            Platform = "MCP-Integration-Test"
        };

        // Act - Process through MessageProcessor which should use MCP
        var userResult = await messageProcessor.ProcessUserMessageAsync(chatRequest);
        userResult.IsSuccess.Should().BeTrue("should successfully process user message");

        var agentResult = await messageProcessor.ProcessAgentResponseAsync(chatRequest, userResult.Value.Conversation.Id);
        agentResult.IsSuccess.Should().BeTrue("should successfully process agent response");

        // Assert
        userResult.Value.Should().NotBeNull("should process user message");
        agentResult.Value.Should().NotBeNull("should process agent response");

        agentResult.Value.AgentResponse.Content.Should().NotBeNullOrEmpty("should have agent response content");
        agentResult.Value.AgentResponse.ConfidenceScore.Should().BeGreaterThan(0, "should have confidence score");

        // Should contain Ivan's personal approach
        agentResult.Value.AgentResponse.Content.Should().Contain("figuring this out", "should mention Ivan's personal honesty");
    }

    public ValueTask DisposeAsync()
    {
        // Clean up any resources if needed
        return ValueTask.CompletedTask;
    }
}

