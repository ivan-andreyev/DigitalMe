using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;

namespace DigitalMe.Web.Services;

/// <summary>
/// Auto-scaling health check service that monitors CPU and memory usage
/// for Google Cloud Run auto-scaling decisions based on resource thresholds.
/// </summary>
public class AutoScalingHealthCheck : IHealthCheck
{
    /// <summary>
    /// Performs health check by monitoring CPU and memory usage
    /// </summary>
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cpuUsage = GetCpuUsage();
            var memoryUsage = GetMemoryUsage();
            
            var data = new Dictionary<string, object>
            {
                ["cpu_usage_percent"] = Math.Round(cpuUsage, 2),
                ["memory_usage_mb"] = Math.Round(memoryUsage, 2),
                ["processor_count"] = Environment.ProcessorCount,
                ["timestamp"] = DateTime.UtcNow.ToString("O")
            };
            
            // High resource usage threshold (90%) indicates scaling needed
            if (cpuUsage > 90 || memoryUsage > 1000) // 1000MB = ~1GB threshold
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"High resource usage - CPU: {cpuUsage:F1}%, Memory: {memoryUsage:F1}MB",
                    data: data));
            }
            
            // Medium resource usage threshold (70%) indicates potential scaling
            if (cpuUsage > 70 || memoryUsage > 700) // 700MB threshold
            {
                return Task.FromResult(HealthCheckResult.Healthy(
                    $"Elevated resource usage - CPU: {cpuUsage:F1}%, Memory: {memoryUsage:F1}MB",
                    data: data));
            }
            
            return Task.FromResult(HealthCheckResult.Healthy(
                $"Normal resource usage - CPU: {cpuUsage:F1}%, Memory: {memoryUsage:F1}MB",
                data: data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Failed to check auto-scaling metrics", 
                ex,
                data: new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["timestamp"] = DateTime.UtcNow.ToString("O")
                }));
        }
    }
    
    /// <summary>
    /// Gets current CPU usage percentage using Process monitoring
    /// with improved sampling accuracy for production auto-scaling
    /// </summary>
    private double GetCpuUsage()
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            
            // First sample
            var startTime = DateTime.UtcNow;
            var startCpuUsage = process.TotalProcessorTime;
            
            // Wait for sampling period (500ms for balance of accuracy vs latency)
            Thread.Sleep(500);
            
            // Second sample
            var endTime = DateTime.UtcNow;
            var endCpuUsage = process.TotalProcessorTime;
            
            // Calculate CPU usage percentage
            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            
            // Account for multi-core systems
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
            
            // Ensure reasonable bounds (0-100%)
            var cpuPercentage = Math.Max(0, Math.Min(100, cpuUsageTotal * 100));
            
            return cpuPercentage;
        }
        catch (Exception)
        {
            // Return conservative estimate on error
            return 0.0;
        }
    }
    
    /// <summary>
    /// Gets current memory usage in MB using GC.GetTotalMemory
    /// for managed memory allocation tracking
    /// </summary>
    private double GetMemoryUsage()
    {
        try
        {
            // Get current managed memory usage
            var managedMemoryBytes = GC.GetTotalMemory(false);
            var managedMemoryMB = managedMemoryBytes / (1024.0 * 1024.0);
            
            // Also get process working set for total memory picture
            using var process = Process.GetCurrentProcess();
            var workingSetMB = process.WorkingSet64 / (1024.0 * 1024.0);
            
            // Return the higher of the two for conservative scaling decisions
            return Math.Max(managedMemoryMB, workingSetMB);
        }
        catch (Exception)
        {
            // Return conservative estimate on error
            return 0.0;
        }
    }
}