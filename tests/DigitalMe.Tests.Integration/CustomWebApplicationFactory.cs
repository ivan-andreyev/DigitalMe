using DigitalMe.Data;
using DigitalMe.Data.Entities;
using DigitalMe.Integrations.MCP.Models;
using DigitalMe.Models;
using DigitalMe.Services.AgentBehavior;
using DigitalMe.Services.Tools;
using DigitalMe.Services.Tools.Strategies;
using DigitalMe.Tests.Unit.Fixtures;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace DigitalMe.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory that properly initializes Tool Registry for integration tests.
/// Registers only working tool strategies to avoid dependency issues in tests.
/// </summary>
public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    // Shared database name for all scopes within this factory instance
    private readonly string _databaseName = $"TestDb_{Guid.NewGuid()}";
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {        
        builder.ConfigureServices(services =>
        {
            Console.WriteLine("DEBUG FACTORY: ConfigureServices called - this runs BEFORE Program.cs");
            // Remove problematic tool strategies that have external dependencies
            var serviceDescriptors = services.Where(s => s.ServiceType == typeof(IToolStrategy)).ToList();
            foreach (var descriptor in serviceDescriptors)
            {
                services.Remove(descriptor);
            }
            
            // Add only the MemoryToolStrategy which has minimal dependencies (only logging)
            services.AddScoped<IToolStrategy, MemoryToolStrategy>();
            
            // Configure test database - Remove existing DbContext registrations
            var dbContextDescriptors = services.Where(d => d.ServiceType == typeof(DbContextOptions<DigitalMeDbContext>)).ToList();
            foreach (var descriptor in dbContextDescriptors)
                services.Remove(descriptor);

            var dbContextServiceDescriptors = services.Where(d => d.ServiceType == typeof(DigitalMeDbContext)).ToList();
            foreach (var descriptor in dbContextServiceDescriptors)
                services.Remove(descriptor);

            // Add in-memory database for testing - use shared database name
            services.AddDbContext<DigitalMeDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            }, ServiceLifetime.Scoped);

            // Mock IMcpService for proper integration testing
            var mcpServiceDescriptors = services.Where(d => d.ServiceType == typeof(DigitalMe.Services.IMcpService)).ToList();
            foreach (var descriptor in mcpServiceDescriptors)
                services.Remove(descriptor);

            services.AddScoped<DigitalMe.Services.IMcpService>(provider =>
            {
                var mockService = new Mock<DigitalMe.Services.IMcpService>();
                
                // Mock InitializeAsync
                mockService.Setup(x => x.InitializeAsync())
                          .ReturnsAsync(true);
                
                // Mock SendMessageAsync with Ivan-style responses
                mockService.Setup(x => x.SendMessageAsync(It.IsAny<string>(), It.IsAny<PersonalityContext>()))
                          .ReturnsAsync("Mock Ivan: система работает через MCP протокол, структурированный подход!");
                
                // Mock CallToolAsync for tool testing
                mockService.Setup(x => x.CallToolAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                          .ReturnsAsync(new McpResponse 
                          {
                              JsonRpc = "2.0",
                              Id = Guid.NewGuid().ToString(),
                              Result = new McpResult
                              {
                                  Content = "ФАКТОРЫ И СТРУКТУРИРОВАННЫЙ АНАЛИЗ от Mock Ivan",
                                  Metadata = new Dictionary<string, object> { ["source"] = "mock" }
                              },
                              Error = null
                          });
                
                // Mock IsConnectedAsync
                mockService.Setup(x => x.IsConnectedAsync())
                          .ReturnsAsync(true);
                
                // Mock DisconnectAsync
                mockService.Setup(x => x.DisconnectAsync())
                          .Returns(Task.CompletedTask);
                
                return mockService.Object;
            });

            // Mock IClaudeApiService for proper API response testing
            var claudeApiDescriptors = services.Where(d => d.ServiceType == typeof(DigitalMe.Integrations.MCP.IClaudeApiService)).ToList();
            foreach (var descriptor in claudeApiDescriptors)
                services.Remove(descriptor);

            services.AddScoped<DigitalMe.Integrations.MCP.IClaudeApiService>(provider =>
            {
                var mockService = new Mock<DigitalMe.Integrations.MCP.IClaudeApiService>();
                
                // Mock GenerateResponseAsync
                mockService.Setup(x => x.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync("Mock Ivan response: структурированный подход к задаче получен!");
                
                // Mock GeneratePersonalityResponseAsync
                mockService.Setup(x => x.GeneratePersonalityResponseAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync("Mock Ivan personality: получил сообщение, анализирую структурно!");
                
                // Mock ValidateApiConnectionAsync
                mockService.Setup(x => x.ValidateApiConnectionAsync())
                          .ReturnsAsync(true);
                
                // Mock GetHealthStatusAsync
                mockService.Setup(x => x.GetHealthStatusAsync())
                          .ReturnsAsync(new DigitalMe.Integrations.MCP.ClaudeApiHealth
                          {
                              IsHealthy = true,
                              Status = "Connected",
                              ResponseTimeMs = 150,
                              LastChecked = DateTime.UtcNow,
                              Model = "claude-3-sonnet-20240229",
                              MaxTokens = 4096
                          });
                
                return mockService.Object;
            });

            // Mock IMCPClient for MCP integration testing
            var mcpClientDescriptors = services.Where(d => d.ServiceType == typeof(DigitalMe.Integrations.MCP.IMcpClient)).ToList();
            foreach (var descriptor in mcpClientDescriptors)
                services.Remove(descriptor);

            services.AddScoped<DigitalMe.Integrations.MCP.IMcpClient>(provider =>
            {
                var mockClient = new Mock<DigitalMe.Integrations.MCP.IMcpClient>();
                
                // Mock IsConnected property
                mockClient.Setup(x => x.IsConnected).Returns(true);
                
                // Mock InitializeAsync
                mockClient.Setup(x => x.InitializeAsync())
                         .ReturnsAsync(true);
                
                // Mock ListToolsAsync
                mockClient.Setup(x => x.ListToolsAsync())
                         .ReturnsAsync(new List<McpTool>
                         {
                             new McpTool { Name = "get_personality_info", Description = "Get Ivan personality information" },
                             new McpTool { Name = "structured_thinking", Description = "Apply structured thinking approach" }
                         });
                
                // Mock CallToolAsync
                mockClient.Setup(x => x.CallToolAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                         .ReturnsAsync(new McpResponse
                         {
                             JsonRpc = "2.0",
                             Id = Guid.NewGuid().ToString(),
                             Result = new McpResult
                             {
                                 Content = "ФАКТОРЫ И СТРУКТУРИРОВАННЫЙ АНАЛИЗ от Mock Ivan"
                             },
                             Error = null
                         });
                
                // Mock DisconnectAsync
                mockClient.Setup(x => x.DisconnectAsync())
                         .Returns(Task.CompletedTask);
                
                return mockClient.Object;
            });


            // Mock IIvanPersonalityService for personality testing - DATABASE-AWARE for error tests
            var ivanServiceDescriptors = services.Where(d => d.ServiceType == typeof(DigitalMe.Services.IIvanPersonalityService)).ToList();
            foreach (var descriptor in ivanServiceDescriptors)
                services.Remove(descriptor);

            services.AddScoped<DigitalMe.Services.IIvanPersonalityService>(provider =>
            {
                var mockService = new Mock<DigitalMe.Services.IIvanPersonalityService>();
                
                // Make mock database-aware: return null if no Ivan personality exists in DB
                mockService.Setup(x => x.GetIvanPersonalityAsync())
                          .Returns(async () =>
                          {
                              var context = provider.GetRequiredService<DigitalMeDbContext>();
                              var existingProfile = await context.PersonalityProfiles
                                  .FirstOrDefaultAsync(p => p.Name == "Ivan");
                              
                              Console.WriteLine($"DEBUG MOCK: Checking Ivan personality in database...");
                              Console.WriteLine($"DEBUG MOCK: Found existing profile: {existingProfile != null}");
                              
                              if (existingProfile != null)
                              {
                                  Console.WriteLine($"DEBUG MOCK: Returning mock Ivan profile");
                                  var ivanProfile = PersonalityTestFixtures.CreateCompleteIvanProfile();
                                  ivanProfile.Name = "Ivan";
                                  return ivanProfile;
                              }
                              
                              Console.WriteLine($"DEBUG MOCK: Returning null - no Ivan personality in database");
                              return null; // Return null when no Ivan personality in database
                          });
                
                return mockService.Object;
            });

            // Mock IAgentBehaviorEngine for agent response testing
            var agentBehaviorEngineDescriptors = services.Where(d => d.ServiceType == typeof(DigitalMe.Services.AgentBehavior.IAgentBehaviorEngine)).ToList();
            foreach (var descriptor in agentBehaviorEngineDescriptors)
                services.Remove(descriptor);

            services.AddScoped<DigitalMe.Services.AgentBehavior.IAgentBehaviorEngine>(provider =>
            {
                var mockEngine = new Mock<DigitalMe.Services.AgentBehavior.IAgentBehaviorEngine>();
                
                mockEngine.Setup(x => x.ProcessMessageAsync(It.IsAny<string>(), It.IsAny<PersonalityContext>()))
                          .ReturnsAsync((string message, PersonalityContext context) => new AgentResponse
                          {
                              Content = "Mock Ivan response: получил сообщение, анализирую структурно! " + message,
                              Mood = new MoodAnalysis 
                              { 
                                  PrimaryMood = "analytical", 
                                  Intensity = 0.8,
                                  MoodScores = new Dictionary<string, double> { ["focused"] = 0.9, ["pragmatic"] = 0.8 }
                              },
                              ConfidenceScore = 0.85,
                              TriggeredTools = new List<string>(),
                              Metadata = new Dictionary<string, object>
                              {
                                  ["originalMessage"] = message,
                                  ["testMock"] = true,
                                  ["processingTime"] = 150
                              }
                          });
                
                return mockEngine.Object;
            });
            
            // Configure SignalR for testing - disable problematic features
            services.Configure<Microsoft.AspNetCore.SignalR.HubOptions>(options =>
            {
                options.EnableDetailedErrors = true;
                options.HandshakeTimeout = TimeSpan.FromSeconds(30);
                options.KeepAliveInterval = TimeSpan.FromSeconds(30);
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
            });
        });
        
        // Configure test-specific middleware behavior
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add test-specific configuration
            var testSettings = new Dictionary<string, string?>
            {
                ["DisableHttpsRedirection"] = "true",
                ["DisableRateLimiting"] = "true",
                ["DisableAuthentication"] = "true"
            };
            config.AddInMemoryCollection(testSettings);
        });
        
        builder.UseEnvironment("Testing");
        
        // Override middleware configuration for testing
        builder.Configure(app =>
        {
            // Skip HTTPS redirection for tests
            // Skip rate limiting for tests  
            // Skip authentication for SignalR tests
            
            app.UseRouting();
            
            // Use endpoints for proper mapping
            app.UseEndpoints(endpoints =>
            {
                // Map SignalR hub directly without middleware complications
                endpoints.MapHub<DigitalMe.Hubs.ChatHub>("/chathub");
                
                // Map basic controllers
                endpoints.MapControllers();
            });
        });
    }
    
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        
        // Initialize Tool Registry after the host is created (same as Program.cs)
        try
        {
            using var toolScope = host.Services.CreateScope();
            var toolRegistry = toolScope.ServiceProvider.GetRequiredService<IToolRegistry>();
            var toolStrategies = toolScope.ServiceProvider.GetServices<IToolStrategy>();
            
            foreach (var strategy in toolStrategies)
            {
                toolRegistry.RegisterTool(strategy);
            }
            
            var appLogger = host.Services.GetService<ILogger<CustomWebApplicationFactory<TStartup>>>();
            appLogger?.LogInformation("🔧 TEST TOOL REGISTRY INITIALIZED with {Count} strategies", toolStrategies.Count());
        }
        catch (Exception ex)
        {
            var appLogger = host.Services.GetService<ILogger<CustomWebApplicationFactory<TStartup>>>();
            appLogger?.LogError(ex, "❌ Failed to initialize Test Tool Registry");
        }
        
        // Seed Ivan personality for integration tests using the same seeder as main application
        try
        {
            using var dbScope = host.Services.CreateScope();
            var context = dbScope.ServiceProvider.GetRequiredService<DigitalMeDbContext>();
            
            // Ensure database is created first
            context.Database.EnsureCreated();
            
            // Use the same seeder as main application for consistency
            var shouldSeed = Environment.GetEnvironmentVariable("DIGITALME_SEED_IVAN_PERSONALITY") != "false";
            if (shouldSeed)
            {
                DigitalMe.Data.Seeders.IvanDataSeeder.SeedBasicIvanProfile(context);
                
                var appLogger = host.Services.GetService<ILogger<CustomWebApplicationFactory<TStartup>>>();
                appLogger?.LogInformation("🧑‍💻 IVAN PERSONALITY SEEDED for integration tests using IvanDataSeeder");
            }
            else
            {
                var appLogger = host.Services.GetService<ILogger<CustomWebApplicationFactory<TStartup>>>();
                appLogger?.LogInformation("🧪 IVAN PERSONALITY SEEDING SKIPPED - test expects missing personality");
            }
        }
        catch (Exception ex)
        {
            var appLogger = host.Services.GetService<ILogger<CustomWebApplicationFactory<TStartup>>>();
            appLogger?.LogError(ex, "❌ Failed to seed Ivan personality");
        }
        
        return host;
    }
}