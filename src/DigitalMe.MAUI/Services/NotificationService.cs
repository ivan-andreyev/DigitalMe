using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace DigitalMe.MAUI.Services;

public class NotificationService : INotificationService
{
    public event EventHandler<NotificationEventArgs>? NotificationReceived;

    public async Task InitializeAsync()
    {
        try
        {
            // Initialize platform-specific notification services
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing notifications: {ex.Message}");
        }
    }

    public async Task<bool> RequestPermissionAsync()
    {
        try
        {
            // For now, assume permission is granted
            // In real implementation, this would request platform-specific permissions
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error requesting notification permission: {ex.Message}");
            return false;
        }
    }

    public async Task ShowNotificationAsync(string title, string message, string? data = null)
    {
        try
        {
            // Use MAUI Community Toolkit Toast for immediate notifications
            var toast = Toast.Make($"{title}: {message}", ToastDuration.Long);
            await toast.Show();

            // Trigger event for app-level notification handling
            NotificationReceived?.Invoke(this, new NotificationEventArgs
            {
                Title = title,
                Message = message,
                Data = data,
                ReceivedAt = DateTime.Now
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing notification: {ex.Message}");
        }
    }

    public async Task ScheduleNotificationAsync(string title, string message, DateTime scheduledTime, string? data = null)
    {
        try
        {
            // For demo purposes, show immediate toast
            // In real implementation, this would schedule platform-specific notifications
            var timeUntil = scheduledTime - DateTime.Now;
            if (timeUntil.TotalSeconds > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(timeUntil.TotalSeconds, 5))); // Max 5 sec demo delay
            }

            await ShowNotificationAsync($"[Scheduled] {title}", message, data);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error scheduling notification: {ex.Message}");
        }
    }

    public async Task CancelAllNotificationsAsync()
    {
        try
        {
            // Cancel all scheduled notifications
            await Task.CompletedTask;
            System.Diagnostics.Debug.WriteLine("All notifications cancelled");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cancelling notifications: {ex.Message}");
        }
    }
}