using System.Reflection;
using CommunityToolkit.Maui;
using DigitalMe.MAUI.Data;
using DigitalMe.MAUI.Models;
using DigitalMe.MAUI.Services;
using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Extensions.Configuration;

namespace DigitalMe.MAUI;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("inter.ttf", "Inter");
				fonts.AddFont("robotomono.ttf", "RobotoMono");
			});

		// Add configuration from appsettings.json
		var assembly = Assembly.GetExecutingAssembly();
		using var stream = assembly.GetManifestResourceStream("DigitalMe.MAUI.appsettings.json");
		if (stream != null)
		{
			var config = new ConfigurationBuilder()
				.AddJsonStream(stream)
				.Build();
			builder.Configuration.AddConfiguration(config);
		}

		// Configure DigitalMe settings
		builder.Services.Configure<MauiConfiguration>(
			builder.Configuration.GetSection("DigitalMe"));

		builder.Services.AddMauiBlazorWebView();
#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
#endif

		// Add HTTP Client for API communication
		builder.Services.AddHttpClient();

		// Platform-specific services
		builder.Services.AddSingleton<IPlatformService, PlatformService>();
		builder.Services.AddSingleton<INotificationService, NotificationService>();

		// SignalR Chat Service
		builder.Services.AddSingleton<ISignalRChatService, SignalRChatService>();

		// Authentication Service
		builder.Services.AddAuthService();

		// Legacy demo service
		builder.Services.AddSingleton<WeatherForecastService>();

		return builder.Build();
	}
}
