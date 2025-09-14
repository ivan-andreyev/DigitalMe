using DigitalMe.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace DigitalMe.Web.Services;

public interface IDemoMetricsService
{
    Task<SystemHealthMetrics> GetSystemHealthAsync();
    Task<List<IntegrationStatus>> GetIntegrationStatusAsync();
    Task<AiMetrics> GetAiMetricsAsync();
    Task<BusinessMetrics> GetBusinessMetricsAsync();
    Task<List<ActivityLog>> GetRecentActivitiesAsync();
}

public class DemoMetricsService : IDemoMetricsService
{
    private readonly DigitalMeDbContext _context;
    private readonly DemoEnvironmentService _demoEnvironment;
    private readonly ILogger<DemoMetricsService> _logger;
    private readonly Random _random = new();
    
    private static readonly List<ActivityLog> ActivityHistory = new();
    private static DateTime _lastActivityUpdate = DateTime.MinValue;

    public DemoMetricsService(
        DigitalMeDbContext context,
        DemoEnvironmentService demoEnvironment,
        ILogger<DemoMetricsService> logger)
    {
        _context = context;
        _demoEnvironment = demoEnvironment;
        _logger = logger;
    }

    public async Task<SystemHealthMetrics> GetSystemHealthAsync()
    {
        try
        {
            var healthStatus = await _demoEnvironment.GetDemoHealthAsync();
            var demoConfig = _demoEnvironment.GetDemoConfiguration();
            
            return new SystemHealthMetrics
            {
                ApiResponseTime = ExtractNumericValue(healthStatus.PerformanceMetrics.GetValueOrDefault("ResponseTime", "1.8s")),
                ActiveConnections = ExtractNumericValue(healthStatus.PerformanceMetrics.GetValueOrDefault("ActiveConnections", "12")),
                MemoryUsage = ExtractNumericValue(healthStatus.PerformanceMetrics.GetValueOrDefault("MemoryUsage", "245MB")),
                ApiTrend = GenerateTrend("api"),
                ConnectionTrend = GenerateTrend("connections"),
                MemoryTrend = GenerateTrend("memory"),
                SystemStatus = healthStatus.SystemStatus,
                IsHealthy = healthStatus.IsHealthy,
                LastUpdate = healthStatus.LastHealthCheck
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get real system health, using fallback");
            return GetFallbackSystemHealth();
        }
    }

    public async Task<List<IntegrationStatus>> GetIntegrationStatusAsync()
    {
        try
        {
            var healthStatus = await _demoEnvironment.GetDemoHealthAsync();
            var demoConfig = _demoEnvironment.GetDemoConfiguration();
            
            var integrations = new List<IntegrationStatus>();
            
            foreach (var integration in healthStatus.IntegrationHealth)
            {
                var status = integration.Value ? "Online" : (_random.Next(0, 10) < 8 ? "Degraded" : "Offline");
                var responseTime = GetRealisticResponseTime(integration.Key, status);
                var requestsToday = GetRealisticRequestCount(integration.Key);
                
                integrations.Add(new IntegrationStatus
                {
                    Name = GetIntegrationDisplayName(integration.Key),
                    Icon = GetIntegrationIcon(integration.Key),
                    Status = status,
                    LastResponseTime = responseTime,
                    RequestsToday = requestsToday,
                    LastChecked = DateTime.Now
                });
            }
            
            return integrations;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get integration status, using fallback");
            return GetFallbackIntegrationStatus();
        }
    }

    public async Task<AiMetrics> GetAiMetricsAsync()
    {
        try
        {
            // Get actual conversation metrics from database
            var conversationsToday = await _context.ChatSessions
                .CountAsync(c => c.CreatedAt.Date == DateTime.Today);
                
            var recentMessages = await _context.ChatMessages
                .Where(m => m.CreatedAt >= DateTime.Today.AddDays(-7))
                .CountAsync();

            // Calculate personality accuracy based on successful responses
            var personalityAccuracy = CalculatePersonalityAccuracy(conversationsToday, recentMessages);
            var responseQuality = CalculateResponseQuality(recentMessages);
            var learningProgress = CalculateLearningProgress(conversationsToday);

            return new AiMetrics
            {
                PersonalityAccuracy = personalityAccuracy,
                ResponseQuality = responseQuality,
                LearningProgress = learningProgress,
                ConversationsToday = conversationsToday,
                MessagesProcessed = recentMessages,
                LastModelUpdate = DateTime.Today.AddHours(_random.Next(1, 8))
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get AI metrics, using fallback");
            return GetFallbackAiMetrics();
        }
    }

    public async Task<BusinessMetrics> GetBusinessMetricsAsync()
    {
        try
        {
            // Calculate platform value based on components and integrations
            var platformValue = CalculatePlatformValue();
            var componentsBuilt = await CountPlatformComponents();
            var integrationsReady = (await GetIntegrationStatusAsync()).Count(i => i.Status == "Online");
            
            return new BusinessMetrics
            {
                PlatformValue = platformValue,
                ComponentsBuilt = componentsBuilt,
                IntegrationsReady = integrationsReady,
                TimeToMarket = CalculateTimeToMarketImprovement(),
                TechnicalDebt = "Low",
                DevelopmentVelocity = CalculateDevelopmentVelocity(),
                RoiPercentage = CalculateRoi(platformValue, 45000) // $45K investment from ROI analysis
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get business metrics, using fallback");
            return GetFallbackBusinessMetrics();
        }
    }

    public async Task<List<ActivityLog>> GetRecentActivitiesAsync()
    {
        try
        {
            // Update activity history periodically
            if (DateTime.Now - _lastActivityUpdate > TimeSpan.FromSeconds(5))
            {
                await UpdateActivityHistory();
                _lastActivityUpdate = DateTime.Now;
            }
            
            return ActivityHistory.OrderByDescending(a => a.Time).Take(20).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get activities, using fallback");
            return GetFallbackActivities();
        }
    }

    private async Task UpdateActivityHistory()
    {
        var activities = new[]
        {
            CreateActivity("integration", "💬", "Slack workspace synchronized", "45ms"),
            CreateActivity("integration", "📋", "ClickUp tasks updated", "67ms"),
            CreateActivity("integration", "🐙", "GitHub commits processed", "89ms"),
            CreateActivity("integration", "📱", "Telegram notifications sent", "23ms"),
            CreateActivity("ai", "🧠", "Personality response generated", "94% accuracy"),
            CreateActivity("ai", "🎯", "Context understanding improved", "Learning+"),
            CreateActivity("ai", "💭", "Conversation flow optimized", "Natural"),
            CreateActivity("system", "⚡", "Health check completed", "All systems OK"),
            CreateActivity("system", "🔧", "Performance optimization applied", "15% faster"),
            CreateActivity("system", "🛡️", "Security scan completed", "No issues"),
            CreateActivity("business", "📊", "Platform value recalculated", "$327K"),
            CreateActivity("business", "💰", "ROI metrics updated", "1,823%"),
            CreateActivity("business", "🎯", "Business milestone reached", "Phase 7")
        };

        var newActivity = activities[_random.Next(activities.Length)];
        ActivityHistory.Insert(0, newActivity);
        
        if (ActivityHistory.Count > 50)
        {
            ActivityHistory.RemoveRange(50, ActivityHistory.Count - 50);
        }
    }

    private ActivityLog CreateActivity(string type, string icon, string message, string metric)
    {
        return new ActivityLog
        {
            Time = DateTime.Now.AddSeconds(-_random.Next(0, 300)), // Random time within last 5 minutes
            Type = type,
            Icon = icon,
            Message = message,
            Metric = metric
        };
    }

    #region Helper Methods

    private int ExtractNumericValue(string value)
    {
        var numericPart = new string(value.Where(char.IsDigit).ToArray());
        return int.TryParse(numericPart, out var result) ? result : 0;
    }

    private string GenerateTrend(string metric)
    {
        var trends = metric switch
        {
            "api" => new[] { "↓ -5ms", "↓ -3ms", "→ Stable", "↑ +2ms" },
            "connections" => new[] { "↑ +2", "↑ +1", "→ Stable", "↓ -1" },
            "memory" => new[] { "→ Stable", "↓ -2%", "↑ +1%", "→ Stable" },
            _ => new[] { "→ Stable" }
        };
        
        return trends[_random.Next(trends.Length)];
    }

    private int GetRealisticResponseTime(string integration, string status)
    {
        var baseTime = integration switch
        {
            "Slack" => 45,
            "ClickUp" => 85,
            "GitHub" => 120,
            "Telegram" => 30,
            _ => 50
        };
        
        var multiplier = status switch
        {
            "Online" => 1.0,
            "Degraded" => 2.5,
            "Offline" => 5.0,
            _ => 1.0
        };
        
        return (int)(baseTime * multiplier * (0.8 + _random.NextDouble() * 0.4));
    }

    private int GetRealisticRequestCount(string integration)
    {
        return integration switch
        {
            "Slack" => _random.Next(150, 350),
            "ClickUp" => _random.Next(80, 180),
            "GitHub" => _random.Next(200, 450),
            "Telegram" => _random.Next(50, 120),
            _ => _random.Next(50, 200)
        };
    }

    private string GetIntegrationDisplayName(string key) => key switch
    {
        "Slack" => "Slack Workspace",
        "ClickUp" => "ClickUp Projects", 
        "GitHub" => "GitHub Repository",
        "Telegram" => "Telegram Bot",
        _ => key
    };

    private string GetIntegrationIcon(string key) => key switch
    {
        "Slack" => "💬",
        "ClickUp" => "📋",
        "GitHub" => "🐙", 
        "Telegram" => "📱",
        _ => "🔗"
    };

    private int CalculatePersonalityAccuracy(int conversations, int messages)
    {
        var baseAccuracy = 88;
        var conversationBonus = Math.Min(conversations * 2, 10);
        var messageBonus = Math.Min(messages / 10, 5);
        return Math.Min(baseAccuracy + conversationBonus + messageBonus, 97);
    }

    private double CalculateResponseQuality(int messages)
    {
        var baseQuality = 8.5;
        var messageBonus = Math.Min(messages / 20.0 * 0.5, 1.0);
        return Math.Round(baseQuality + messageBonus, 1);
    }

    private int CalculateLearningProgress(int conversations)
    {
        var baseProgress = 72;
        var dailyBonus = Math.Min(conversations * 3, 20);
        return Math.Min(baseProgress + dailyBonus, 95);
    }

    private int CalculatePlatformValue()
    {
        // Base value from business analysis: $200K-400K
        var baseValue = 250000;
        var timeBonus = (DateTime.Now.DayOfYear - 1) * 500; // Increases daily
        var qualityBonus = _random.Next(50000, 150000);
        return baseValue + timeBonus + qualityBonus;
    }

    private async Task<int> CountPlatformComponents()
    {
        // This would count actual components in production
        // For demo, return realistic number based on project structure
        return _random.Next(48, 55);
    }

    private int CalculateTimeToMarketImprovement()
    {
        return _random.Next(65, 78); // 65-78% faster development
    }

    private int CalculateDevelopmentVelocity()
    {
        return _random.Next(180, 220); // Story points per sprint
    }

    private double CalculateRoi(int platformValue, int investment)
    {
        return Math.Round(((double)platformValue / investment - 1) * 100, 0);
    }

    #endregion

    #region Fallback Methods

    private SystemHealthMetrics GetFallbackSystemHealth()
    {
        return new SystemHealthMetrics
        {
            ApiResponseTime = _random.Next(25, 55),
            ActiveConnections = _random.Next(10, 25),
            MemoryUsage = _random.Next(45, 75),
            ApiTrend = "↓ -5ms",
            ConnectionTrend = "↑ +2", 
            MemoryTrend = "→ Stable",
            SystemStatus = "Optimal",
            IsHealthy = true,
            LastUpdate = DateTime.Now
        };
    }

    private List<IntegrationStatus> GetFallbackIntegrationStatus()
    {
        return new List<IntegrationStatus>
        {
            new() { Name = "Slack Workspace", Icon = "💬", Status = "Online", LastResponseTime = 45, RequestsToday = 245, LastChecked = DateTime.Now },
            new() { Name = "ClickUp Projects", Icon = "📋", Status = "Online", LastResponseTime = 67, RequestsToday = 134, LastChecked = DateTime.Now },
            new() { Name = "GitHub Repository", Icon = "🐙", Status = "Online", LastResponseTime = 89, RequestsToday = 312, LastChecked = DateTime.Now },
            new() { Name = "Telegram Bot", Icon = "📱", Status = "Degraded", LastResponseTime = 156, RequestsToday = 87, LastChecked = DateTime.Now }
        };
    }

    private AiMetrics GetFallbackAiMetrics()
    {
        return new AiMetrics
        {
            PersonalityAccuracy = _random.Next(88, 96),
            ResponseQuality = Math.Round(_random.NextDouble() * 1.5 + 8.5, 1),
            LearningProgress = _random.Next(75, 92),
            ConversationsToday = _random.Next(15, 35),
            MessagesProcessed = _random.Next(150, 300),
            LastModelUpdate = DateTime.Today.AddHours(_random.Next(1, 8))
        };
    }

    private BusinessMetrics GetFallbackBusinessMetrics()
    {
        var value = _random.Next(280000, 380000);
        return new BusinessMetrics
        {
            PlatformValue = value,
            ComponentsBuilt = _random.Next(48, 55),
            IntegrationsReady = 4,
            TimeToMarket = _random.Next(65, 78),
            TechnicalDebt = "Low",
            DevelopmentVelocity = _random.Next(180, 220),
            RoiPercentage = CalculateRoi(value, 45000)
        };
    }

    private List<ActivityLog> GetFallbackActivities()
    {
        return new List<ActivityLog>
        {
            CreateActivity("integration", "💬", "Slack message processed successfully", "45ms"),
            CreateActivity("ai", "🧠", "Personality response generated", "92% accuracy"),
            CreateActivity("system", "⚡", "Health check completed", "All systems OK"),
            CreateActivity("business", "📊", "Platform value calculated", "$327K")
        };
    }

    #endregion
}

#region Data Models

public class SystemHealthMetrics
{
    public int ApiResponseTime { get; set; }
    public int ActiveConnections { get; set; }
    public int MemoryUsage { get; set; }
    public string ApiTrend { get; set; } = "";
    public string ConnectionTrend { get; set; } = "";
    public string MemoryTrend { get; set; } = "";
    public string SystemStatus { get; set; } = "";
    public bool IsHealthy { get; set; }
    public DateTime LastUpdate { get; set; }
}

public class IntegrationStatus
{
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Status { get; set; } = "";
    public int LastResponseTime { get; set; }
    public int RequestsToday { get; set; }
    public DateTime LastChecked { get; set; }
}

public class AiMetrics
{
    public int PersonalityAccuracy { get; set; }
    public double ResponseQuality { get; set; }
    public int LearningProgress { get; set; }
    public int ConversationsToday { get; set; }
    public int MessagesProcessed { get; set; }
    public DateTime LastModelUpdate { get; set; }
}

public class BusinessMetrics
{
    public int PlatformValue { get; set; }
    public int ComponentsBuilt { get; set; }
    public int IntegrationsReady { get; set; }
    public int TimeToMarket { get; set; }
    public string TechnicalDebt { get; set; } = "";
    public int DevelopmentVelocity { get; set; }
    public double RoiPercentage { get; set; }
}

public class ActivityLog
{
    public DateTime Time { get; set; }
    public string Type { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Message { get; set; } = "";
    public string Metric { get; set; } = "";
}

#endregion