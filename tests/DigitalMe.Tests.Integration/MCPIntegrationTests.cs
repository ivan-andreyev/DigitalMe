using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using DigitalMe.Integrations.MCP;
using DigitalMe.Services;
using DigitalMe.Data.Entities;
using DigitalMe.Models;

namespace DigitalMe.Tests.Integration;

public class MCPIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>, IAsyncDisposable
{
    private readonly CustomWebApplicationFactory<Program> _factory;
#pragma warning disable CS0414 // Field assigned but never used - intended for future MCP server configuration
    private readonly string _mcpServerUrl = "http://localhost:3000/mcp";

    public MCPIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MCPClient_ShouldInitializeSuccessfully()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var mcpClient = scope.ServiceProvider.GetRequiredService<IMCPClient>();

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
        var ivanPersonalityService = scope.ServiceProvider.GetRequiredService<IIvanPersonalityService>();
        
        var personality = await ivanPersonalityService.GetIvanPersonalityAsync();
        var context = new PersonalityContext
        {
            Profile = personality,
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
        response.Should().NotBeNullOrEmpty("MCP service should return response");
        
        // Should be Russian response from Ivan
        response.Should().Contain("система работает", "should get test response from Ivan");
        response.Should().Contain("MCP протокол", "should mention MCP protocol");
    }

    [Fact]
    public async Task MCPClient_ShouldListToolsSuccessfully()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var mcpClient = scope.ServiceProvider.GetRequiredService<IMCPClient>();
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
        var mcpClient = scope.ServiceProvider.GetRequiredService<IMCPClient>();
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
        var agentResult = await messageProcessor.ProcessAgentResponseAsync(chatRequest, userResult.Conversation.Id);

        // Assert
        userResult.Should().NotBeNull("should process user message");
        agentResult.Should().NotBeNull("should process agent response");
        
        agentResult.AgentResponse.Content.Should().NotBeNullOrEmpty("should have agent response content");
        agentResult.AgentResponse.ConfidenceScore.Should().BeGreaterThan(0, "should have confidence score");
        
        // Should contain Ivan's structured approach
        agentResult.AgentResponse.Content.Should().Contain("структурно", "should mention structured approach");
    }

    public ValueTask DisposeAsync()
    {
        // Clean up any resources if needed
        return ValueTask.CompletedTask;
    }
}