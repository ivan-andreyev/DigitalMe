using System.Runtime.InteropServices;

namespace DigitalMe.MAUI.Services;

public class PlatformService : IPlatformService
{
    public string GetPlatformName()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" :
               RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS" :
               RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux" : "Unknown";
    }

    public string GetDeviceModel()
    {
        return Environment.MachineName;
    }

    public string GetOperatingSystem()
    {
        return RuntimeInformation.OSDescription;
    }

    public bool IsPhysicalDevice()
    {
        return true; // Always true for demo
    }

    public async Task<string> GetDeviceIdAsync()
    {
        try
        {
            // Generate a persistent device ID based on machine name
            var machineId = Environment.MachineName + Environment.UserName;
            return await Task.FromResult(machineId.GetHashCode().ToString("X"));
        }
        catch (Exception)
        {
            return Guid.NewGuid().ToString();
        }
    }

    public async Task<bool> HasInternetConnectionAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            var response = await client.GetAsync("https://www.google.com");
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<string> GetAppVersionAsync()
    {
        return await Task.FromResult("1.0.0-demo");
    }

    public async Task OpenUrlAsync(string url)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            System.Diagnostics.Process.Start(psi);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error opening URL: {ex.Message}");
        }
    }

    public async Task ShareTextAsync(string text, string title = "")
    {
        try
        {
            // For demo purposes - just log to debug
            await Task.CompletedTask;
            System.Diagnostics.Debug.WriteLine($"Share requested: {title} - {text}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error sharing text: {ex.Message}");
        }
    }

    public async Task<bool> RequestPermissionAsync(string permission)
    {
        try
        {
            // Mock permission system - always grant for demo
            await Task.Delay(100); // Simulate permission dialog
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error requesting permission {permission}: {ex.Message}");
            return false;
        }
    }
}