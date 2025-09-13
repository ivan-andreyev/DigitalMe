using System;
using System.Collections.Generic;

namespace DigitalMe.Services.Learning.ErrorLearning.SuggestionEngine.Models;

/// <summary>
/// Represents current system context for generating contextual optimization suggestions
/// Provides environmental and temporal information for intelligent recommendation generation
/// </summary>
public class SystemContext
{
    /// <summary>
    /// Current timestamp for context
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Current system environment (Development, Staging, Production)
    /// </summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// Current system load metrics
    /// </summary>
    public SystemLoad SystemLoad { get; set; } = new();

    /// <summary>
    /// Recent error rate trends
    /// </summary>
    public ErrorRateTrends ErrorTrends { get; set; } = new();

    /// <summary>
    /// Active user sessions and usage patterns
    /// </summary>
    public UsagePatterns UsagePatterns { get; set; } = new();

    /// <summary>
    /// Recent deployments and changes
    /// </summary>
    public List<RecentChange> RecentChanges { get; set; } = new();

    /// <summary>
    /// Current business context (peak hours, maintenance windows, etc.)
    /// </summary>
    public BusinessContext BusinessContext { get; set; } = new();

    /// <summary>
    /// Available system resources
    /// </summary>
    public ResourceAvailability Resources { get; set; } = new();

    /// <summary>
    /// External system dependencies status
    /// </summary>
    public Dictionary<string, DependencyStatus> ExternalDependencies { get; set; } = new();
}

/// <summary>
/// System load metrics
/// </summary>
public class SystemLoad
{
    /// <summary>
    /// CPU utilization percentage (0-100)
    /// </summary>
    public double CpuUtilization { get; set; }

    /// <summary>
    /// Memory utilization percentage (0-100)
    /// </summary>
    public double MemoryUtilization { get; set; }

    /// <summary>
    /// Disk I/O utilization percentage (0-100)
    /// </summary>
    public double DiskUtilization { get; set; }

    /// <summary>
    /// Network utilization percentage (0-100)
    /// </summary>
    public double NetworkUtilization { get; set; }

    /// <summary>
    /// Current request rate (requests per second)
    /// </summary>
    public double RequestRate { get; set; }

    /// <summary>
    /// Average response time in milliseconds
    /// </summary>
    public double AverageResponseTime { get; set; }
}

/// <summary>
/// Error rate trends over time
/// </summary>
public class ErrorRateTrends
{
    /// <summary>
    /// Current error rate (errors per minute)
    /// </summary>
    public double CurrentErrorRate { get; set; }

    /// <summary>
    /// Error rate 1 hour ago
    /// </summary>
    public double ErrorRateOneHourAgo { get; set; }

    /// <summary>
    /// Error rate 24 hours ago
    /// </summary>
    public double ErrorRateOneDayAgo { get; set; }

    /// <summary>
    /// Error rate 7 days ago (same time of week)
    /// </summary>
    public double ErrorRateOneWeekAgo { get; set; }

    /// <summary>
    /// Trend direction (Increasing, Decreasing, Stable)
    /// </summary>
    public TrendDirection TrendDirection { get; set; }

    /// <summary>
    /// Most common error categories in recent period
    /// </summary>
    public Dictionary<string, int> RecentErrorCategories { get; set; } = new();
}

/// <summary>
/// Usage patterns and user behavior
/// </summary>
public class UsagePatterns
{
    /// <summary>
    /// Number of active user sessions
    /// </summary>
    public int ActiveSessions { get; set; }

    /// <summary>
    /// Peak usage hours based on historical data
    /// </summary>
    public List<int> PeakHours { get; set; } = new();

    /// <summary>
    /// Most frequently accessed API endpoints
    /// </summary>
    public Dictionary<string, int> PopularEndpoints { get; set; } = new();

    /// <summary>
    /// User behavior patterns
    /// </summary>
    public Dictionary<string, double> BehaviorMetrics { get; set; } = new();
}

/// <summary>
/// Recent system changes
/// </summary>
public class RecentChange
{
    /// <summary>
    /// Type of change (Deployment, Configuration, Infrastructure)
    /// </summary>
    public string ChangeType { get; set; } = string.Empty;

    /// <summary>
    /// Description of the change
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// When the change was made
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Impact level of the change
    /// </summary>
    public ChangeImpact Impact { get; set; }

    /// <summary>
    /// Components affected by the change
    /// </summary>
    public List<string> AffectedComponents { get; set; } = new();
}

/// <summary>
/// Business context information
/// </summary>
public class BusinessContext
{
    /// <summary>
    /// Whether current time is during peak business hours
    /// </summary>
    public bool IsPeakHours { get; set; }

    /// <summary>
    /// Whether system is in maintenance window
    /// </summary>
    public bool IsMaintenanceWindow { get; set; }

    /// <summary>
    /// Current business priority level
    /// </summary>
    public BusinessPriority Priority { get; set; }

    /// <summary>
    /// Upcoming scheduled events that might affect system
    /// </summary>
    public List<ScheduledEvent> UpcomingEvents { get; set; } = new();
}

/// <summary>
/// Resource availability information
/// </summary>
public class ResourceAvailability
{
    /// <summary>
    /// Available development team capacity (hours)
    /// </summary>
    public double DevelopmentCapacity { get; set; }

    /// <summary>
    /// Available testing resources
    /// </summary>
    public double TestingCapacity { get; set; }

    /// <summary>
    /// Infrastructure budget available
    /// </summary>
    public double InfrastructureBudget { get; set; }

    /// <summary>
    /// Whether emergency maintenance is possible
    /// </summary>
    public bool EmergencyMaintenancePossible { get; set; }
}

/// <summary>
/// Scheduled business event
/// </summary>
public class ScheduledEvent
{
    /// <summary>
    /// Event name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Event start time
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Event duration
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Expected impact on system
    /// </summary>
    public string ExpectedImpact { get; set; } = string.Empty;
}

/// <summary>
/// Trend direction enumeration
/// </summary>
public enum TrendDirection
{
    Decreasing,
    Stable,
    Increasing,
    Volatile
}

/// <summary>
/// Change impact level
/// </summary>
public enum ChangeImpact
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Business priority level
/// </summary>
public enum BusinessPriority
{
    Low,
    Normal,
    High,
    Critical
}

/// <summary>
/// External dependency status
/// </summary>
public enum DependencyStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}