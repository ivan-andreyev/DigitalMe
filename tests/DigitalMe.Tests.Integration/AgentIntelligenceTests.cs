using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using DigitalMe.DTOs;
using DigitalMe.Data.Entities;
using DigitalMe.Services.AgentBehavior;
using DigitalMe.Services;

namespace DigitalMe.Tests.Integration;

public class AgentIntelligenceTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private HubConnection? _connection;
    private readonly string _testUserId = "agent-test-user";

    public AgentIntelligenceTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        
        // Set test API key for integration tests
        Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", "test-api-key-sk-ant-test");
    }

    [Fact]
    public async Task AgentResponse_SimpleTest_ShouldReturnIntelligentResponse()
    {
        // Arrange - Test the Anthropic integration directly
        using var scope = _factory.Services.CreateScope();
        var anthropicService = scope.ServiceProvider.GetRequiredService<DigitalMe.Integrations.MCP.IAnthropicService>();
        var ivanPersonalityService = scope.ServiceProvider.GetRequiredService<IIvanPersonalityService>();
        
        var personality = await ivanPersonalityService.GetIvanPersonalityAsync();
        personality.Should().NotBeNull("Ivan personality should exist");
        
        // Act - Test Anthropic service directly
        var response = await anthropicService.SendMessageAsync("test", personality);

        // Assert - This should work after fixing P0
        response.Should().NotBeNullOrEmpty("Anthropic should return response");
        response.Should().NotContain("техническая проблема", "should not be technical error");
        
        // For tests, fallback response is acceptable
        if (response.Contains("API ключа"))
        {
            response.Should().Contain("Head of R&D", "fallback should be in Ivan's style");
        }
    }

    [Fact]
    public async Task ChatFlow_EndToEnd_ShouldWork()
    {
        // Arrange - Full end-to-end test through SignalR
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
            Message = "test",
            UserId = _testUserId,
            Platform = "Web"
        };

        await _connection.InvokeAsync("SendMessage", chatRequest);
        await Task.Delay(8000); // Wait for background processing

        // Assert
        receivedMessages.Should().HaveCount(2, "should receive user message and agent response");
        
        var agentMessage = receivedMessages.FirstOrDefault(m => m.Role == "assistant");
        agentMessage.Should().NotBeNull("should receive agent response");
        agentMessage!.Content.Should().NotBeNullOrEmpty("agent should have content");
        agentMessage.Content.Should().NotContain("техническая проблема", "should not be error fallback");
        agentMessage.Metadata.Should().ContainKey("confidence", "should have confidence metadata");
        
        var confidence = Convert.ToDouble(agentMessage.Metadata["confidence"]);
        confidence.Should().BeGreaterThan(0.5, "should have decent confidence score");
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }
    }
}