namespace DigitalMe.MAUI.Services;

public interface IPlatformService
{
    string GetPlatformName();
    string GetDeviceModel();
    string GetOperatingSystem();
    bool IsPhysicalDevice();
    Task<string> GetDeviceIdAsync();
    Task<bool> HasInternetConnectionAsync();
    Task<string> GetAppVersionAsync();
    Task OpenUrlAsync(string url);
    Task ShareTextAsync(string text, string title = "");
    Task<bool> RequestPermissionAsync(string permission);
}

public interface INotificationService
{
    Task InitializeAsync();
    Task<bool> RequestPermissionAsync();
    Task ShowNotificationAsync(string title, string message, string? data = null);
    Task ScheduleNotificationAsync(string title, string message, DateTime scheduledTime, string? data = null);
    Task CancelAllNotificationsAsync();
    event EventHandler<NotificationEventArgs>? NotificationReceived;
}

public class NotificationEventArgs : EventArgs
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Data { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.Now;
}