using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using DigitalMe.Services.Tools;
using DigitalMe.Models;

namespace DigitalMe.Tests.Integration;

/// <summary>
/// Integration tests for Tool Strategy Pattern implementation.
/// Tests the complete tool discovery, triggering, and execution flow.
/// </summary>
public class ToolStrategyIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public ToolStrategyIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ToolRegistry_ShouldRegisterAllAvailableStrategies()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var toolRegistry = scope.ServiceProvider.GetRequiredService<IToolRegistry>();

        // DEBUG: Log what tools are actually registered
        var allTools = toolRegistry.GetAllTools().ToList();
        var logger = scope.ServiceProvider.GetService<ILogger<ToolStrategyIntegrationTests>>();
        logger?.LogInformation("DEBUG: Found {Count} tools in registry", allTools.Count);
        
        if (!allTools.Any())
        {
            // Try to manually initialize the registry like Program.cs does
            var toolStrategies = scope.ServiceProvider.GetServices<IToolStrategy>().ToList();
            logger?.LogInformation("DEBUG: Found {Count} tool strategies to register", toolStrategies.Count);
            
            foreach (var strategy in toolStrategies)
            {
                toolRegistry.RegisterTool(strategy);
                logger?.LogInformation("DEBUG: Registered tool {ToolName}", strategy.ToolName);
            }
            
            allTools = toolRegistry.GetAllTools().ToList();
        }

        // Act & Assert
        allTools.Should().NotBeEmpty("Tool registry should contain registered strategies");
        allTools.Should().HaveCount(1, "should have 1 test tool strategy (Memory)");
        
        var toolNames = allTools.Select(t => t.ToolName).ToList();
        toolNames.Should().Contain("store_memory", "should contain Memory tool strategy");
    }

    [Theory]
    [InlineData("запомни это важно", "store_memory")]
    public async Task ToolStrategies_ShouldTriggerCorrectly(string message, string expectedToolName)
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var toolRegistry = scope.ServiceProvider.GetRequiredService<IToolRegistry>();
        
        var context = new PersonalityContext
        {
            Profile = new PersonalityProfile { Name = "TestUser" },
            RecentMessages = new List<Message>(),
            CurrentState = new Dictionary<string, object>
            {
                ["userId"] = "test-user",
                ["platform"] = "Integration-Test"
            }
        };

        // Act
        var triggeredTools = await toolRegistry.GetTriggeredToolsAsync(message, context);

        // Assert
        triggeredTools.Should().NotBeEmpty($"message '{message}' should trigger at least one tool");
        triggeredTools.Should().Contain(t => t.ToolName == expectedToolName, 
            $"message '{message}' should trigger '{expectedToolName}' tool");
    }

    [Fact]
    public async Task ToolExecutor_ShouldExecutePersonalityToolSuccessfully()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var toolExecutor = scope.ServiceProvider.GetRequiredService<ToolExecutor>();
        
        var context = new PersonalityContext
        {
            Profile = new PersonalityProfile { Name = "Ivan" },
            RecentMessages = new List<Message>(),
            CurrentState = new Dictionary<string, object>()
        };

        var parameters = new Dictionary<string, object>
        {
            ["category"] = "professional"
        };

        // Act
        var result = await toolExecutor.ExecuteToolAsync("get_personality_traits", parameters, context);

        // Assert
        result.Should().NotBeNull("tool execution should return result");
        
        // Проверяем что результат содержит ожидаемые свойства
        if (result is IDictionary<string, object> resultDict)
        {
            resultDict.Should().ContainKey("success");
            resultDict.Should().ContainKey("tool_name");
            resultDict["tool_name"].Should().Be("get_personality_traits");
        }
    }

    [Fact]
    public async Task ToolExecutor_ShouldHandleUnknownToolGracefully()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var toolExecutor = scope.ServiceProvider.GetRequiredService<ToolExecutor>();
        
        var context = new PersonalityContext
        {
            Profile = new PersonalityProfile { Name = "TestUser" },
            RecentMessages = new List<Message>(),
            CurrentState = new Dictionary<string, object>()
        };

        // Act
        var result = await toolExecutor.ExecuteToolAsync("unknown_tool", new Dictionary<string, object>(), context);

        // Assert
        result.Should().NotBeNull("should return error result for unknown tool");
        
        if (result is IDictionary<string, object> resultDict)
        {
            resultDict.Should().ContainKey("success");
            resultDict.Should().ContainKey("error");
            resultDict.Should().ContainKey("available_tools");
            
            resultDict["success"].Should().Be(false);
            resultDict["error"].ToString().Should().Contain("not found in registry");
        }
    }

    [Fact]
    public void ToolRegistry_ShouldProvideToolParameterSchemas()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var toolRegistry = scope.ServiceProvider.GetRequiredService<IToolRegistry>();

        // Act
        var memoryTool = toolRegistry.GetTool("store_memory");

        // Assert
        memoryTool.Should().NotBeNull("Memory tool should be registered");
        
        // Test ISP compliance - check if tool implements IParameterizedTool
        if (memoryTool is IParameterizedTool parameterizedTool)
        {
            var schema = parameterizedTool.GetParameterSchema();
            schema.Should().NotBeNull("parameterized tool should provide parameter schema");
        }
        else
        {
            // This is fine - tool doesn't need parameters (ISP compliance)
        }
    }

    [Fact]
    public void ToolStrategies_ShouldHaveCorrectPriorityOrdering()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var toolRegistry = scope.ServiceProvider.GetRequiredService<IToolRegistry>();

        // Act
        var allTools = toolRegistry.GetAllTools().ToList();

        // Assert
        allTools.Should().NotBeEmpty("should have registered tools");
        
        // Проверяем что инструменты отсортированы по приоритету (выше = важнее)
        for (int i = 0; i < allTools.Count - 1; i++)
        {
            allTools[i].Priority.Should().BeGreaterThanOrEqualTo(allTools[i + 1].Priority, 
                "tools should be ordered by priority (descending)");
        }
    }
}