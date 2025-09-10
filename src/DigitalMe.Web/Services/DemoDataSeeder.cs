using DigitalMe.Web.Models;
using DigitalMe.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace DigitalMe.Web.Services;

public class DemoDataSeeder
{
    private readonly DigitalMeDbContext _context;
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeeder(DigitalMeDbContext context, ILogger<DemoDataSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedDemoDataAsync()
    {
        try
        {
            _logger.LogInformation("Starting demo data seeding...");

            // Ensure database is created
            await _context.Database.EnsureCreatedAsync();

            // Clear existing demo data for clean demo
            await ClearExistingDemoDataAsync();

            // Seed demo user profile for Ivan
            var demoUser = await SeedDemoUserProfileAsync();

            // Seed main user account (mr.red.404@gmail.com)
            await SeedMainUserAccountAsync();

            // Seed demo chat session with realistic conversation
            await SeedDemoConversationAsync(demoUser);

            // Seed system configuration for demo optimizations
            await SeedDemoSystemConfigurationAsync();

            await _context.SaveChangesAsync();
            _logger.LogInformation("Demo data seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during demo data seeding");
            throw;
        }
    }

    private async Task ClearExistingDemoDataAsync()
    {
        _logger.LogInformation("Clearing existing demo data for clean environment");
        
        // Remove existing chat messages and sessions for clean demo
        var existingMessages = await _context.ChatMessages.ToListAsync();
        var existingSessions = await _context.ChatSessions.ToListAsync();
        
        _context.ChatMessages.RemoveRange(existingMessages);
        _context.ChatSessions.RemoveRange(existingSessions);
        
        await _context.SaveChangesAsync();
        _logger.LogInformation("Existing demo data cleared");
    }

    private async Task<UserProfile> SeedDemoUserProfileAsync()
    {
        _logger.LogInformation("Seeding demo user profile for Ivan");

        // Check if demo user already exists
        var existingUser = await _context.UserProfiles
            .FirstOrDefaultAsync(u => u.Email == "ivan.demo@ellyanalytics.com");

        if (existingUser != null)
        {
            _logger.LogInformation("Demo user profile already exists");
            return existingUser;
        }

        var demoUser = new UserProfile
        {
            Id = Guid.NewGuid(),
            Email = "ivan.demo@ellyanalytics.com",
            UserName = "Ivan (Demo)",
            FirstName = "Ivan",
            LastName = "Petrov",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30), // Created 30 days ago for realism
            LastLoginAt = DateTime.UtcNow.AddMinutes(-15), // Recent login for demo
            ProfileData = """
            {
                "role": "Head of R&D",
                "company": "EllyAnalytics",
                "experience": "10+ years in enterprise software development",
                "specialization": "C# .NET, Enterprise Architecture, AI Integration",
                "leadership_style": "Technical mentoring and architectural guidance",
                "key_traits": {
                    "technical_expertise": "Advanced .NET, Microservices, DDD",
                    "problem_solving": "Systematic TDD approach",
                    "communication": "Direct, solution-oriented",
                    "innovation": "R&D focus, cutting-edge technologies"
                },
                "recent_achievements": [
                    "DigitalMe Platform - $400K enterprise AI integration system",
                    "Advanced personality modeling with Claude API",
                    "Multi-platform enterprise connectors (Slack, ClickUp, GitHub, Telegram)"
                ]
            }
            """
        };

        _context.UserProfiles.Add(demoUser);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Demo user profile created successfully");
        return demoUser;
    }

    private async Task SeedDemoConversationAsync(UserProfile demoUser)
    {
        _logger.LogInformation("Seeding demo conversation examples");

        // Create a demo chat session
        var demoSession = new ChatSession
        {
            Id = Guid.NewGuid(),
            UserId = demoUser.Id,
            Title = "Enterprise Architecture Discussion",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddMinutes(-20),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        _context.ChatSessions.Add(demoSession);
        await _context.SaveChangesAsync();

        // Add realistic demo conversation messages
        var demoMessages = new[]
        {
            new ChatMessageEntity 
            { 
                Id = Guid.NewGuid(),
                SessionId = demoSession.Id,
                Content = "What's your experience with enterprise architecture?",
                MessageType = "user",
                CreatedAt = DateTime.UtcNow.AddMinutes(-15),
                Metadata = """{"responseTime": null, "demoMessage": true}"""
            },
            new ChatMessageEntity 
            { 
                Id = Guid.NewGuid(),
                SessionId = demoSession.Id,
                Content = "I have extensive experience designing enterprise-grade systems at EllyAnalytics as Head of R&D. My focus is on scalable .NET architectures using Domain-Driven Design principles, microservices patterns, and clean architecture. I prioritize maintainability, testability, and performance in everything I build. Recently completed the DigitalMe platform - a $400K enterprise AI integration system showcasing these capabilities.",
                MessageType = "assistant",
                CreatedAt = DateTime.UtcNow.AddMinutes(-14),
                Metadata = """{"responseTime": 1800, "demoMessage": true, "tokens": 95}"""
            },
            new ChatMessageEntity 
            { 
                Id = Guid.NewGuid(),
                SessionId = demoSession.Id,
                Content = "How do you approach technical leadership?",
                MessageType = "user",
                CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                Metadata = """{"responseTime": null, "demoMessage": true}"""
            },
            new ChatMessageEntity 
            { 
                Id = Guid.NewGuid(),
                SessionId = demoSession.Id,
                Content = "My leadership philosophy combines hands-on technical mentoring with strategic architectural guidance. I believe in leading by example - writing clean, well-tested code while guiding teams through complex technical decisions. I focus on knowledge sharing, creating maintainable systems, and ensuring the entire team can confidently work with our architecture. The DigitalMe platform exemplifies this approach with its modular design and comprehensive testing strategy.",
                MessageType = "assistant",
                CreatedAt = DateTime.UtcNow.AddMinutes(-9),
                Metadata = """{"responseTime": 2100, "demoMessage": true, "tokens": 87}"""
            },
            new ChatMessageEntity 
            { 
                Id = Guid.NewGuid(),
                SessionId = demoSession.Id,
                Content = "Tell me about the DigitalMe platform's technical achievements.",
                MessageType = "user",
                CreatedAt = DateTime.UtcNow.AddMinutes(-6),
                Metadata = """{"responseTime": null, "demoMessage": true}"""
            },
            new ChatMessageEntity 
            { 
                Id = Guid.NewGuid(),
                SessionId = demoSession.Id,
                Content = "The DigitalMe platform represents a significant technical achievement - an enterprise-grade AI personality system with multi-platform integrations. Built with .NET 8, Blazor Server, and Entity Framework, it features real-time Claude API integration, comprehensive personality modeling, and production-ready deployment patterns. The platform includes Slack, ClickUp, GitHub, and Telegram connectors, demonstrating advanced integration architecture. Total platform value is estimated at $200K-400K, showcasing our R&D capabilities and creating reusable enterprise components.",
                MessageType = "assistant",
                CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                Metadata = """{"responseTime": 1950, "demoMessage": true, "tokens": 112}"""
            }
        };

        foreach (var message in demoMessages)
        {
            _context.ChatMessages.Add(message);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation($"Demo conversation with {demoMessages.Length} messages created");
    }

    private async Task SeedDemoSystemConfigurationAsync()
    {
        _logger.LogInformation("Seeding demo system configuration");

        var demoConfigurations = new[]
        {
            new SystemConfiguration 
            { 
                Key = "demo.mode.enabled", 
                Value = "true", 
                ValueType = "boolean",
                Description = "Enable demo mode optimizations"
            },
            new SystemConfiguration 
            { 
                Key = "demo.response.max_time", 
                Value = "2000", 
                ValueType = "integer",
                Description = "Maximum response time for smooth demo (ms)"
            },
            new SystemConfiguration 
            { 
                Key = "demo.integrations.mock", 
                Value = "true", 
                ValueType = "boolean",
                Description = "Use mock integration responses for reliable demo"
            },
            new SystemConfiguration 
            { 
                Key = "demo.metrics.show_impressive", 
                Value = "true", 
                ValueType = "boolean",
                Description = "Show impressive metrics for stakeholder presentation"
            },
            new SystemConfiguration 
            { 
                Key = "demo.personality.traits_count", 
                Value = "150", 
                ValueType = "integer",
                Description = "Number of personality traits to display"
            },
            new SystemConfiguration 
            { 
                Key = "demo.platform.value", 
                Value = "400000", 
                ValueType = "integer",
                Description = "Platform value for business demonstration ($)"
            }
        };

        foreach (var config in demoConfigurations)
        {
            var existing = await _context.SystemConfigurations
                .FirstOrDefaultAsync(c => c.Key == config.Key);
            
            if (existing == null)
            {
                _context.SystemConfigurations.Add(config);
            }
            else
            {
                existing.Value = config.Value;
                existing.Description = config.Description;
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Demo system configuration seeded successfully");
    }

    public async Task<DemoDataSummary> GetDemoDataSummaryAsync()
    {
        var summary = new DemoDataSummary
        {
            UserProfileCount = await _context.UserProfiles.CountAsync(),
            ChatSessionCount = await _context.ChatSessions.CountAsync(),
            ChatMessageCount = await _context.ChatMessages.CountAsync(),
            ConfigurationCount = await _context.SystemConfigurations.CountAsync(),
            LastSeededAt = DateTime.UtcNow,
            IsDemoModeEnabled = await _context.SystemConfigurations
                .AnyAsync(c => c.Key == "demo.mode.enabled" && c.Value == "true")
        };

        return summary;
    }

    private async Task SeedMainUserAccountAsync()
    {
        _logger.LogInformation("Seeding main user account (mr.red.404@gmail.com)");

        // Check if user already exists
        var existingUser = await _context.UserProfiles
            .FirstOrDefaultAsync(u => u.Email == "mr.red.404@gmail.com");

        if (existingUser != null)
        {
            _logger.LogInformation("Main user account already exists");
            return;
        }

        var mainUser = new UserProfile
        {
            Id = Guid.NewGuid(),
            Email = "mr.red.404@gmail.com",
            UserName = "Ivan Petrov",
            FirstName = "Ivan",
            LastName = "Petrov", 
            DisplayName = "Ivan",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = null,
            ProfileData = """
            {
                "role": "Head of R&D",
                "company": "EllyAnalytics",
                "experience": "10+ years in enterprise software development",
                "specialization": "C# .NET, Enterprise Architecture, AI Integration",
                "leadership_style": "Technical mentoring and architectural guidance",
                "key_traits": {
                    "technical_expertise": "Advanced .NET, Microservices, DDD",
                    "problem_solving": "Systematic TDD approach",
                    "communication": "Direct, solution-oriented",
                    "innovation": "R&D focus, cutting-edge technologies"
                }
            }
            """
        };

        _context.UserProfiles.Add(mainUser);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("✅ Seeded main user account: {Email}", mainUser.Email);
    }
}

public class DemoDataSummary
{
    public int UserProfileCount { get; set; }
    public int ChatSessionCount { get; set; }
    public int ChatMessageCount { get; set; }
    public int ConfigurationCount { get; set; }
    public DateTime LastSeededAt { get; set; }
    public bool IsDemoModeEnabled { get; set; }
}