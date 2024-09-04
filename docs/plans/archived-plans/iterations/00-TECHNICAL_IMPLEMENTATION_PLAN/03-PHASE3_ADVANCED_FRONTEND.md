# Phase 3: Advanced Frontend Development (Weeks 9-12)

**Objective:** Complete multi-frontend architecture with MAUI applications and advanced features  
**Duration:** 4 weeks  
**Dependencies:** Phase 2 completed with stable integrations  
**Team:** 1x Senior .NET Developer + 1x Mobile Developer

## Overview

Phase 3 expands the platform to native mobile and desktop applications using .NET MAUI, while enhancing the web frontend with advanced features. This phase completes the multi-frontend architecture vision and adds sophisticated user experience features.

## 3.1 MAUI Mobile Application (Week 9-10)

### Tasks

**Week 9 - Mobile Foundation:**
- [ ] Create .NET MAUI project with proper structure
- [ ] Set up platform-specific configurations for iOS and Android
- [ ] Implement navigation system with Shell-based routing
- [ ] Create base pages and view models with MVVM pattern
- [ ] Set up dependency injection and API service integration
- [ ] Implement authentication flow for mobile

**Week 10 - Mobile Features:**
- [ ] Build main dashboard with calendar and task overview
- [ ] Implement push notification system
- [ ] Create offline data synchronization capability
- [ ] Add location-based reminder functionality
- [ ] Implement camera integration for document scanning
- [ ] Set up platform testing on iOS and Android devices

### Technical Implementation

**MAUI Project Structure:**
```
DigitalMe.Mobile/
├── Platforms/
│   ├── Android/
│   │   ├── MainActivity.cs
│   │   ├── MainApplication.cs
│   │   └── AndroidManifest.xml
│   └── iOS/
│       ├── AppDelegate.cs
│       └── Info.plist
├── Views/
│   ├── MainPage.xaml
│   ├── DashboardPage.xaml
│   ├── CalendarPage.xaml
│   ├── TasksPage.xaml
│   └── SettingsPage.xaml
├── ViewModels/
│   ├── BaseViewModel.cs
│   ├── DashboardViewModel.cs
│   ├── CalendarViewModel.cs
│   └── TasksViewModel.cs
├── Services/
│   ├── ApiService.cs
│   ├── NotificationService.cs
│   ├── LocationService.cs
│   └── CacheService.cs
└── Models/
    ├── CalendarEvent.cs
    ├── UserTask.cs
    └── ApiResponse.cs
```

**MAUI Program Configuration:**
```csharp
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("FontAwesome.ttf", "FontAwesome");
            })
            .UseMauiCommunityToolkit();

        // Register services
        builder.Services.AddSingleton<IApiService, ApiService>();
        builder.Services.AddSingleton<INotificationService, NotificationService>();
        builder.Services.AddSingleton<ILocationService, LocationService>();
        builder.Services.AddSingleton<ICacheService, CacheService>();
        
        // Register view models
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<CalendarViewModel>();
        builder.Services.AddTransient<TasksViewModel>();
        
        // Register views
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<CalendarPage>();
        builder.Services.AddTransient<TasksPage>();

        // Configure HTTP client
        builder.Services.AddHttpClient<ApiService>(client =>
        {
            client.BaseAddress = new Uri(DeviceInfo.Platform == DevicePlatform.Android 
                ? "https://10.0.2.2:7066" 
                : "https://localhost:7066");
            client.DefaultRequestHeaders.Add("User-Agent", "DigitalMe-Mobile/1.0");
        });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
```

**Main Dashboard View:**
```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:viewmodels="clr-namespace:DigitalMe.Mobile.ViewModels"
             x:Class="DigitalMe.Mobile.Views.DashboardPage"
             Title="Dashboard">
    
    <ContentPage.BindingContext>
        <viewmodels:DashboardViewModel />
    </ContentPage.BindingContext>

    <ScrollView>
        <VerticalStackLayout Padding="20" Spacing="15">
            
            <!-- Quick Stats -->
            <Grid ColumnDefinitions="*,*,*" RowDefinitions="Auto" ColumnSpacing="10">
                <Border Grid.Column="0" BackgroundColor="{AppThemeBinding Light=LightBlue, Dark=DarkBlue}"
                        Stroke="Transparent" StrokeShape="RoundRectangle 10">
                    <VerticalStackLayout Padding="15" Spacing="5">
                        <Label Text="📅" FontSize="24" HorizontalOptions="Center" />
                        <Label Text="{Binding TodayEventsCount}" FontSize="20" FontAttributes="Bold" 
                               HorizontalOptions="Center" />
                        <Label Text="Today's Events" FontSize="12" HorizontalOptions="Center" />
                    </VerticalStackLayout>
                </Border>
                
                <Border Grid.Column="1" BackgroundColor="{AppThemeBinding Light=LightGreen, Dark=DarkGreen}"
                        Stroke="Transparent" StrokeShape="RoundRectangle 10">
                    <VerticalStackLayout Padding="15" Spacing="5">
                        <Label Text="✅" FontSize="24" HorizontalOptions="Center" />
                        <Label Text="{Binding ActiveTasksCount}" FontSize="20" FontAttributes="Bold" 
                               HorizontalOptions="Center" />
                        <Label Text="Active Tasks" FontSize="12" HorizontalOptions="Center" />
                    </VerticalStackLayout>
                </Border>
                
                <Border Grid.Column="2" BackgroundColor="{AppThemeBinding Light=LightCoral, Dark=DarkRed}"
                        Stroke="Transparent" StrokeShape="RoundRectangle 10">
                    <VerticalStackLayout Padding="15" Spacing="5">
                        <Label Text="⏰" FontSize="24" HorizontalOptions="Center" />
                        <Label Text="{Binding OverdueTasksCount}" FontSize="20" FontAttributes="Bold" 
                               HorizontalOptions="Center" />
                        <Label Text="Overdue" FontSize="12" HorizontalOptions="Center" />
                    </VerticalStackLayout>
                </Border>
            </Grid>

            <!-- Next Event -->
            <Border BackgroundColor="{AppThemeBinding Light=White, Dark=Black}"
                    Stroke="{AppThemeBinding Light=LightGray, Dark=DarkGray}"
                    StrokeShape="RoundRectangle 10">
                <VerticalStackLayout Padding="15" Spacing="10">
                    <Label Text="📅 Next Event" FontSize="16" FontAttributes="Bold" />
                    <Label Text="{Binding NextEvent.Title, FallbackValue='No upcoming events'}"
                           FontSize="14" FontAttributes="Bold" />
                    <Label Text="{Binding NextEvent.StartTime, StringFormat='{0:MMM dd, yyyy HH:mm}'}"
                           FontSize="12" TextColor="Gray" />
                    <Label Text="{Binding NextEvent.Location}" FontSize="12" TextColor="Gray"
                           IsVisible="{Binding NextEvent.HasLocation}" />
                </VerticalStackLayout>
            </Border>

            <!-- Recent Tasks -->
            <Border BackgroundColor="{AppThemeBinding Light=White, Dark=Black}"
                    Stroke="{AppThemeBinding Light=LightGray, Dark=DarkGray}"
                    StrokeShape="RoundRectangle 10">
                <VerticalStackLayout Padding="15" Spacing="10">
                    <Label Text="✅ Recent Tasks" FontSize="16" FontAttributes="Bold" />
                    <CollectionView ItemsSource="{Binding RecentTasks}" MaximumHeightRequest="200">
                        <CollectionView.ItemTemplate>
                            <DataTemplate>
                                <Grid ColumnDefinitions="Auto,*,Auto" ColumnSpacing="10" Padding="0,5">
                                    <CheckBox Grid.Column="0" IsChecked="{Binding IsCompleted}"
                                             CheckedChanged="OnTaskCheckedChanged" />
                                    <Label Grid.Column="1" Text="{Binding Title}" FontSize="14"
                                           VerticalOptions="Center" />
                                    <Label Grid.Column="2" Text="{Binding DueDate, StringFormat='{0:MMM dd}'}"
                                           FontSize="12" TextColor="Gray" VerticalOptions="Center" />
                                </Grid>
                            </DataTemplate>
                        </CollectionView.ItemTemplate>
                    </CollectionView>
                </VerticalStackLayout>
            </Border>

            <!-- Quick Actions -->
            <Grid ColumnDefinitions="*,*" RowDefinitions="Auto,Auto" ColumnSpacing="10" RowSpacing="10">
                <Button Grid.Column="0" Grid.Row="0" Text="📝 New Task" 
                        Command="{Binding CreateTaskCommand}" Style="{StaticResource PrimaryButton}" />
                <Button Grid.Column="1" Grid.Row="0" Text="📅 New Event"
                        Command="{Binding CreateEventCommand}" Style="{StaticResource PrimaryButton}" />
                <Button Grid.Column="0" Grid.Row="1" Text="🔄 Sync"
                        Command="{Binding SyncCommand}" Style="{StaticResource SecondaryButton}" />
                <Button Grid.Column="1" Grid.Row="1" Text="⚙️ Settings"
                        Command="{Binding SettingsCommand}" Style="{StaticResource SecondaryButton}" />
            </Grid>

        </VerticalStackLayout>
    </ScrollView>

</ContentPage>
```

**Dashboard ViewModel:**
```csharp
public partial class DashboardViewModel : BaseViewModel
{
    private readonly IApiService _apiService;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private int todayEventsCount;

    [ObservableProperty]
    private int activeTasksCount;

    [ObservableProperty]
    private int overdueTasksCount;

    [ObservableProperty]
    private CalendarEvent? nextEvent;

    [ObservableProperty]
    private ObservableCollection<UserTask> recentTasks = new();

    public DashboardViewModel(IApiService apiService, INotificationService notificationService)
    {
        _apiService = apiService;
        _notificationService = notificationService;
    }

    [RelayCommand]
    public async Task LoadDashboardData()
    {
        try
        {
            IsBusy = true;
            
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            
            // Load today's events
            var todayEvents = await _apiService.GetCalendarEventsAsync(today, tomorrow);
            TodayEventsCount = todayEvents.Length;
            
            // Load next event
            var upcomingEvents = await _apiService.GetCalendarEventsAsync(DateTime.Now, DateTime.Now.AddDays(7));
            NextEvent = upcomingEvents.OrderBy(e => e.StartTime).FirstOrDefault();
            
            // Load tasks
            var tasks = await _apiService.GetActiveTasksAsync();
            ActiveTasksCount = tasks.Count(t => !t.IsCompleted);
            OverdueTasksCount = tasks.Count(t => !t.IsCompleted && t.DueDate < DateTime.Now);
            
            RecentTasks.Clear();
            foreach (var task in tasks.OrderByDescending(t => t.UpdatedAt).Take(5))
            {
                RecentTasks.Add(task);
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("Error loading dashboard data", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task CreateTask()
    {
        await Shell.Current.GoToAsync("//tasks/create");
    }

    [RelayCommand]
    public async Task CreateEvent()
    {
        await Shell.Current.GoToAsync("//calendar/create");
    }

    [RelayCommand]
    public async Task Sync()
    {
        await LoadDashboardData();
        await _notificationService.ShowAsync("Sync Complete", "Dashboard updated successfully");
    }

    [RelayCommand]
    public async Task Settings()
    {
        await Shell.Current.GoToAsync("//settings");
    }
}
```

**Push Notifications Service:**
```csharp
public interface INotificationService
{
    Task InitializeAsync();
    Task<bool> RequestPermissionAsync();
    Task ScheduleNotificationAsync(string title, string message, DateTime scheduledTime);
    Task ShowAsync(string title, string message);
    Task<string> GetTokenAsync();
}

public class NotificationService : INotificationService
{
    public async Task InitializeAsync()
    {
        await CrossFirebasePushNotification.Current.InitializeAsync();
        
        CrossFirebasePushNotification.Current.OnTokenRefresh += (s, p) =>
        {
            Debug.WriteLine($"FCM Token: {p.Token}");
            // Send token to your backend
            _ = Task.Run(() => SendTokenToBackend(p.Token));
        };

        CrossFirebasePushNotification.Current.OnNotificationReceived += (s, p) =>
        {
            Debug.WriteLine("Received notification");
            // Handle foreground notifications
        };
    }

    public async Task<bool> RequestPermissionAsync()
    {
        var status = await Permissions.RequestAsync<Permissions.Notification>();
        return status == PermissionStatus.Granted;
    }

    public async Task ScheduleNotificationAsync(string title, string message, DateTime scheduledTime)
    {
        var notification = new NotificationRequest
        {
            NotificationId = Random.Shared.Next(),
            Title = title,
            Description = message,
            Schedule = new NotificationRequestSchedule
            {
                NotifyTime = scheduledTime
            }
        };

        await LocalNotificationCenter.Current.Show(notification);
    }

    private async Task SendTokenToBackend(string token)
    {
        try
        {
            // Implementation to send FCM token to backend
            var httpClient = new HttpClient();
            await httpClient.PostAsync("api/notifications/register-token", 
                new StringContent(JsonSerializer.Serialize(new { Token = token })));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error sending token to backend: {ex.Message}");
        }
    }
}
```

### Deliverables
- [ ] .NET MAUI mobile application for iOS and Android
- [ ] Native UI with platform-specific optimizations
- [ ] Push notifications working on both platforms
- [ ] Offline data synchronization capability
- [ ] Location-based reminders functionality
- [ ] Camera integration for document scanning
- [ ] App Store/Play Store deployment packages ready

### Acceptance Criteria
- App runs smoothly on iOS and Android devices
- Push notifications arrive reliably
- Offline mode allows basic task and calendar viewing
- Location reminders trigger at appropriate times
- Camera functionality captures and processes documents
- UI follows platform-specific design guidelines

## 3.2 MAUI Desktop Application (Week 10-11)

### Tasks

**Week 10 - Desktop Setup:**
- [ ] Extend MAUI project for Windows and macOS platforms
- [ ] Implement desktop-specific UI layouts and navigation
- [ ] Set up system tray integration with notification area
- [ ] Create global hotkeys for quick access functionality
- [ ] Implement desktop notification system

**Week 11 - Advanced Desktop Features:**
- [ ] Add multi-window support for better productivity
- [ ] Implement file system integration for document management
- [ ] Set up automatic updates mechanism
- [ ] Create desktop widgets for quick information display
- [ ] Optimize performance for desktop usage patterns

### Technical Implementation

**Desktop-Specific Features:**
```csharp
public partial class DesktopService : IDesktopService
{
    private NotifyIcon? _trayIcon;
    private readonly IServiceProvider _serviceProvider;

    public DesktopService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task InitializeSystemTrayAsync()
    {
#if WINDOWS
        _trayIcon = new NotifyIcon();
        _trayIcon.Icon = new Icon("Resources/Images/tray_icon.ico");
        _trayIcon.Text = "DigitalMe Agent";
        _trayIcon.Visible = true;

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Show Dashboard", null, ShowDashboard);
        contextMenu.Items.Add("Quick Task", null, ShowQuickTask);
        contextMenu.Items.Add("Today's Schedule", null, ShowTodaySchedule);
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("Settings", null, ShowSettings);
        contextMenu.Items.Add("Exit", null, ExitApplication);

        _trayIcon.ContextMenuStrip = contextMenu;
        _trayIcon.DoubleClick += ShowDashboard;
#endif
    }

    public async Task RegisterGlobalHotkeysAsync()
    {
#if WINDOWS
        // Register Ctrl+Alt+D for dashboard
        var hotkeyService = _serviceProvider.GetRequiredService<IHotkeyService>();
        await hotkeyService.RegisterHotkeyAsync(
            ModifierKeys.Control | ModifierKeys.Alt, 
            Key.D, 
            ShowDashboard);

        // Register Ctrl+Alt+T for quick task
        await hotkeyService.RegisterHotkeyAsync(
            ModifierKeys.Control | ModifierKeys.Alt, 
            Key.T, 
            ShowQuickTask);
#endif
    }

    private async void ShowDashboard(object? sender, EventArgs e)
    {
        var mainWindow = Application.Current?.MainPage as AppShell;
        if (mainWindow != null)
        {
            await mainWindow.GoToAsync("//dashboard");
            BringToFront();
        }
    }

    private async void ShowQuickTask(object? sender, EventArgs e)
    {
        var quickTaskWindow = new QuickTaskWindow();
        quickTaskWindow.Show();
    }

    private void BringToFront()
    {
#if WINDOWS
        var window = Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
        if (window != null)
        {
            window.Activate();
            Microsoft.UI.Xaml.WindowManagementHelper.BringToFront(window);
        }
#endif
    }
}
```

**Quick Task Window:**
```xml
<?xml version="1.0" encoding="utf-8" ?>
<Window xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
        xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
        x:Class="DigitalMe.Desktop.Views.QuickTaskWindow"
        Title="Quick Task"
        Width="400"
        Height="300">

    <VerticalStackLayout Padding="20" Spacing="15">
        <Label Text="✨ Quick Task" FontSize="18" FontAttributes="Bold" HorizontalOptions="Center" />
        
        <Entry x:Name="TaskTitleEntry" 
               Placeholder="What do you need to do?"
               FontSize="16" />
        
        <Editor x:Name="TaskDescriptionEditor"
                Placeholder="Add details (optional)"
                HeightRequest="80" />
        
        <Grid ColumnDefinitions="*,*" ColumnSpacing="10">
            <DatePicker Grid.Column="0" x:Name="DueDatePicker" />
            <Picker Grid.Column="1" x:Name="PriorityPicker" Title="Priority">
                <Picker.ItemsSource>
                    <x:Array Type="{x:Type x:String}">
                        <x:String>Low</x:String>
                        <x:String>Medium</x:String>
                        <x:String>High</x:String>
                        <x:String>Urgent</x:String>
                    </x:Array>
                </Picker.ItemsSource>
            </Picker>
        </Grid>
        
        <Grid ColumnDefinitions="*,*" ColumnSpacing="10">
            <Button Grid.Column="0" Text="Cancel" Clicked="OnCancelClicked" />
            <Button Grid.Column="1" Text="Create Task" Clicked="OnCreateTaskClicked" 
                    Style="{StaticResource PrimaryButton}" />
        </Grid>
    </VerticalStackLayout>

</Window>
```

**Desktop Window Manager:**
```csharp
public class DesktopWindowManager : IDesktopWindowManager
{
    private readonly List<Window> _activeWindows = new();
    private readonly IServiceProvider _serviceProvider;

    public DesktopWindowManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<Window> CreateWindowAsync<T>(string title, object? dataContext = null) 
        where T : ContentPage, new()
    {
        var page = new T();
        if (dataContext != null)
            page.BindingContext = dataContext;

        var window = new Window(page)
        {
            Title = title,
            Width = 800,
            Height = 600
        };

        _activeWindows.Add(window);
        
        window.Destroying += (s, e) => _activeWindows.Remove(window);

        Application.Current?.OpenWindow(window);
        
        return window;
    }

    public async Task ShowCalendarWindowAsync()
    {
        await CreateWindowAsync<CalendarPage>("Calendar - DigitalMe");
    }

    public async Task ShowTasksWindowAsync()
    {
        await CreateWindowAsync<TasksPage>("Tasks - DigitalMe");
    }

    public void CloseAllWindows()
    {
        foreach (var window in _activeWindows.ToList())
        {
            window.Close();
        }
        _activeWindows.Clear();
    }
}
```

### Deliverables
- [ ] MAUI desktop application for Windows and macOS
- [ ] System tray integration with context menu
- [ ] Global hotkeys for quick access
- [ ] Multi-window support for productivity
- [ ] Desktop notifications system
- [ ] File system integration capabilities
- [ ] Automatic update mechanism

### Acceptance Criteria
- Desktop app provides native Windows/macOS experience
- System tray icon works with all expected features
- Global hotkeys respond correctly
- Multiple windows can be opened and managed
- Desktop notifications appear in system notification area
- Automatic updates work seamlessly

## 3.3 Advanced Web Features (Week 11-12)

### Tasks

**Week 11 - Advanced Calendar Management:**
- [ ] Implement drag-and-drop calendar event editing
- [ ] Add calendar view switching (month, week, day, agenda)
- [ ] Create recurring event management
- [ ] Implement calendar sharing and collaboration features
- [ ] Add calendar import/export functionality

**Week 12 - Analytics and Advanced Features:**
- [ ] Build conversation history browser with search
- [ ] Create analytics and reporting dashboard
- [ ] Implement bulk operations for data management
- [ ] Add export/import functionality for user data
- [ ] Set up progressive web app (PWA) features

### Technical Implementation

**Advanced Calendar Component:**
```razor
@using DigitalMe.Web.Components.Calendar
@inject IJSRuntime JSRuntime

<div class="advanced-calendar-container">
    <div class="calendar-toolbar">
        <div class="view-switcher">
            <button class="btn @(CurrentView == CalendarView.Month ? "btn-primary" : "btn-outline-primary")"
                    @onclick="() => SwitchView(CalendarView.Month)">Month</button>
            <button class="btn @(CurrentView == CalendarView.Week ? "btn-primary" : "btn-outline-primary")"
                    @onclick="() => SwitchView(CalendarView.Week)">Week</button>
            <button class="btn @(CurrentView == CalendarView.Day ? "btn-primary" : "btn-outline-primary")"
                    @onclick="() => SwitchView(CalendarView.Day)">Day</button>
            <button class="btn @(CurrentView == CalendarView.Agenda ? "btn-primary" : "btn-outline-primary")"
                    @onclick="() => SwitchView(CalendarView.Agenda)">Agenda</button>
        </div>
        
        <div class="navigation-controls">
            <button class="btn btn-outline-secondary" @onclick="NavigatePrevious">←</button>
            <h4 class="current-period">@GetCurrentPeriodText()</h4>
            <button class="btn btn-outline-secondary" @onclick="NavigateNext">→</button>
            <button class="btn btn-outline-primary" @onclick="NavigateToday">Today</button>
        </div>
        
        <div class="calendar-actions">
            <button class="btn btn-success" @onclick="CreateEvent">+ New Event</button>
            <div class="dropdown">
                <button class="btn btn-outline-secondary dropdown-toggle" 
                        data-bs-toggle="dropdown">Options</button>
                <ul class="dropdown-menu">
                    <li><a class="dropdown-item" @onclick="ImportCalendar">Import Calendar</a></li>
                    <li><a class="dropdown-item" @onclick="ExportCalendar">Export Calendar</a></li>
                    <li><hr class="dropdown-divider"></li>
                    <li><a class="dropdown-item" @onclick="CalendarSettings">Settings</a></li>
                </ul>
            </div>
        </div>
    </div>

    <div class="calendar-view-container" @ref="calendarContainer">
        @switch (CurrentView)
        {
            case CalendarView.Month:
                <MonthView Events="Events" CurrentDate="CurrentDate" 
                          OnEventClick="HandleEventClick"
                          OnDateClick="HandleDateClick"
                          OnEventDrop="HandleEventDrop" />
                break;
            case CalendarView.Week:
                <WeekView Events="Events" CurrentDate="CurrentDate" 
                         OnEventClick="HandleEventClick"
                         OnTimeSlotClick="HandleTimeSlotClick"
                         OnEventDrop="HandleEventDrop" />
                break;
            case CalendarView.Day:
                <DayView Events="Events" CurrentDate="CurrentDate" 
                        OnEventClick="HandleEventClick"
                        OnTimeSlotClick="HandleTimeSlotClick" />
                break;
            case CalendarView.Agenda:
                <AgendaView Events="Events" CurrentDate="CurrentDate" 
                           DateRange="GetAgendaDateRange()"
                           OnEventClick="HandleEventClick" />
                break;
        }
    </div>
</div>

@code {
    [Parameter] public CalendarEvent[] Events { get; set; } = Array.Empty<CalendarEvent>();
    [Parameter] public EventCallback<CalendarEvent> OnEventSelected { get; set; }
    [Parameter] public EventCallback<DateTime> OnDateSelected { get; set; }

    private CalendarView CurrentView = CalendarView.Month;
    private DateTime CurrentDate = DateTime.Today;
    private ElementReference calendarContainer;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JSRuntime.InvokeVoidAsync("initializeCalendarDragDrop", calendarContainer);
        }
    }

    private async Task HandleEventDrop(CalendarEvent calendarEvent, DateTime newStartTime)
    {
        try
        {
            var duration = calendarEvent.EndTime - calendarEvent.StartTime;
            var updateRequest = new UpdateCalendarEventRequest
            {
                StartTime = newStartTime,
                EndTime = newStartTime + duration
            };

            await ApiService.UpdateCalendarEventAsync(calendarEvent.Id, updateRequest);
            await OnEventSelected.InvokeAsync(calendarEvent);
        }
        catch (Exception ex)
        {
            await JSRuntime.InvokeVoidAsync("showToast", "Error updating event", "error");
        }
    }

    private string GetCurrentPeriodText()
    {
        return CurrentView switch
        {
            CalendarView.Month => CurrentDate.ToString("MMMM yyyy"),
            CalendarView.Week => $"{GetWeekStart():MMM dd} - {GetWeekEnd():MMM dd, yyyy}",
            CalendarView.Day => CurrentDate.ToString("dddd, MMMM dd, yyyy"),
            CalendarView.Agenda => $"Agenda - {CurrentDate:MMM yyyy}",
            _ => ""
        };
    }
}
```

**Analytics Dashboard:**
```razor
@page "/analytics"
@using Chart.js.Blazor
@inject IAnalyticsService AnalyticsService

<PageTitle>Analytics - DigitalMe</PageTitle>

<div class="analytics-container">
    <div class="d-flex justify-content-between align-items-center mb-4">
        <h2>📊 Analytics Dashboard</h2>
        <div class="date-range-picker">
            <select @bind="selectedPeriod" @onchange="LoadAnalytics" class="form-select">
                <option value="week">This Week</option>
                <option value="month">This Month</option>
                <option value="quarter">This Quarter</option>
                <option value="year">This Year</option>
            </select>
        </div>
    </div>

    <div class="row">
        <!-- Key Metrics Cards -->
        <div class="col-md-3">
            <div class="metric-card">
                <div class="metric-icon">📅</div>
                <div class="metric-value">@analyticsData.TotalEvents</div>
                <div class="metric-label">Calendar Events</div>
                <div class="metric-change @(analyticsData.EventsChange >= 0 ? "positive" : "negative")">
                    @analyticsData.EventsChange.ToString("+0;-#")%
                </div>
            </div>
        </div>
        
        <div class="col-md-3">
            <div class="metric-card">
                <div class="metric-icon">✅</div>
                <div class="metric-value">@analyticsData.CompletedTasks</div>
                <div class="metric-label">Tasks Completed</div>
                <div class="metric-change @(analyticsData.TasksChange >= 0 ? "positive" : "negative")">
                    @analyticsData.TasksChange.ToString("+0;-#")%
                </div>
            </div>
        </div>
        
        <div class="col-md-3">
            <div class="metric-card">
                <div class="metric-icon">💬</div>
                <div class="metric-value">@analyticsData.ConversationCount</div>
                <div class="metric-label">Conversations</div>
                <div class="metric-change @(analyticsData.ConversationsChange >= 0 ? "positive" : "negative")">
                    @analyticsData.ConversationsChange.ToString("+0;-#")%
                </div>
            </div>
        </div>
        
        <div class="col-md-3">
            <div class="metric-card">
                <div class="metric-icon">⏱️</div>
                <div class="metric-value">@analyticsData.ProductivityScore</div>
                <div class="metric-label">Productivity Score</div>
                <div class="metric-change positive">
                    +@analyticsData.ProductivityImprovement%
                </div>
            </div>
        </div>
    </div>

    <div class="row mt-4">
        <!-- Activity Timeline Chart -->
        <div class="col-md-8">
            <div class="card">
                <div class="card-header">
                    <h5>📈 Activity Timeline</h5>
                </div>
                <div class="card-body">
                    <Chart Config="activityChart" @ref="activityChartRef" />
                </div>
            </div>
        </div>
        
        <!-- Task Completion Rate -->
        <div class="col-md-4">
            <div class="card">
                <div class="card-header">
                    <h5>🎯 Task Completion Rate</h5>
                </div>
                <div class="card-body">
                    <Chart Config="completionChart" @ref="completionChartRef" />
                </div>
            </div>
        </div>
    </div>

    <div class="row mt-4">
        <!-- Top Categories -->
        <div class="col-md-6">
            <div class="card">
                <div class="card-header">
                    <h5>🏷️ Top Categories</h5>
                </div>
                <div class="card-body">
                    @foreach (var category in analyticsData.TopCategories.Take(5))
                    {
                        <div class="category-item">
                            <div class="category-name">@category.Name</div>
                            <div class="category-bar">
                                <div class="category-progress" style="width: @(category.Percentage)%"></div>
                            </div>
                            <div class="category-count">@category.Count</div>
                        </div>
                    }
                </div>
            </div>
        </div>
        
        <!-- Recent Activity -->
        <div class="col-md-6">
            <div class="card">
                <div class="card-header">
                    <h5>🕒 Recent Activity</h5>
                </div>
                <div class="card-body">
                    <div class="activity-timeline">
                        @foreach (var activity in analyticsData.RecentActivities.Take(10))
                        {
                            <div class="activity-item">
                                <div class="activity-time">@activity.Timestamp.ToString("HH:mm")</div>
                                <div class="activity-icon">@GetActivityIcon(activity.Type)</div>
                                <div class="activity-description">@activity.Description</div>
                            </div>
                        }
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

@code {
    private string selectedPeriod = "month";
    private AnalyticsData analyticsData = new();
    private LineConfig activityChart = new();
    private PieConfig completionChart = new();
    private Chart? activityChartRef;
    private Chart? completionChartRef;

    protected override async Task OnInitializedAsync()
    {
        await LoadAnalytics();
        SetupCharts();
    }

    private async Task LoadAnalytics()
    {
        analyticsData = await AnalyticsService.GetAnalyticsAsync(selectedPeriod);
        await UpdateCharts();
    }

    private void SetupCharts()
    {
        // Activity timeline chart
        activityChart.Data.Labels = analyticsData.ActivityLabels;
        activityChart.Data.Datasets.Add(new LineDataset<int>(analyticsData.ActivityData)
        {
            Label = "Daily Activity",
            BorderColor = ColorUtil.FromDrawingColor(System.Drawing.Color.Blue),
            Fill = false,
            BorderWidth = 2
        });

        // Task completion chart
        completionChart.Data.Labels = new[] { "Completed", "Pending", "Overdue" };
        completionChart.Data.Datasets.Add(new PieDataset<int>(new[] 
        { 
            analyticsData.CompletedTasks, 
            analyticsData.PendingTasks, 
            analyticsData.OverdueTasks 
        })
        {
            BackgroundColor = new[]
            {
                ColorUtil.FromDrawingColor(System.Drawing.Color.Green),
                ColorUtil.FromDrawingColor(System.Drawing.Color.Orange),
                ColorUtil.FromDrawingColor(System.Drawing.Color.Red)
            }
        });
    }
}
```

### Deliverables
- [ ] Advanced calendar management with drag-and-drop
- [ ] Multiple calendar view options (month, week, day, agenda)
- [ ] Comprehensive analytics and reporting dashboard
- [ ] Conversation history browser with search capabilities
- [ ] Bulk data operations functionality
- [ ] Data export/import capabilities
- [ ] Progressive Web App (PWA) features

### Acceptance Criteria
- Calendar drag-and-drop works smoothly across all views
- Analytics provide meaningful insights into user activity
- Conversation history search returns relevant results quickly
- Bulk operations handle large datasets without errors
- PWA can be installed and works offline
- Data export/import maintains data integrity

## 3.4 Real-time Communication Enhancement (Week 12)

### Tasks

**Real-time Infrastructure:**
- [ ] Enhance SignalR implementation for better scalability
- [ ] Implement cross-platform notification system
- [ ] Set up event-driven architecture for all data changes
- [ ] Configure message queues for async operations
- [ ] Implement connection resilience and reconnection logic

### Technical Implementation

**Enhanced SignalR Hub:**
```csharp
[Authorize]
public class EnhancedAgentHub : Hub
{
    private readonly IUserConnectionManager _connectionManager;
    private readonly INotificationService _notificationService;
    private readonly ILogger<EnhancedAgentHub> _logger;

    public EnhancedAgentHub(
        IUserConnectionManager connectionManager,
        INotificationService notificationService,
        ILogger<EnhancedAgentHub> logger)
    {
        _connectionManager = connectionManager;
        _notificationService = notificationService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await _connectionManager.AddConnectionAsync(userId, Context.ConnectionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
            
            _logger.LogInformation("User {UserId} connected with connection {ConnectionId}", 
                userId, Context.ConnectionId);
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await _connectionManager.RemoveConnectionAsync(userId, Context.ConnectionId);
            
            _logger.LogInformation("User {UserId} disconnected from connection {ConnectionId}", 
                userId, Context.ConnectionId);
        }
        
        await base.OnDisconnectedAsync(exception);
    }

    [HubMethodName("SendMessage")]
    public async Task SendMessageAsync(string message)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId)) return;

        try
        {
            // Process message through agent service
            var agentService = Context.GetHttpContext()?.RequestServices
                .GetRequiredService<IPersonalAgentService>();
                
            var response = await agentService.ProcessRequestAsync(message, new AgentContext
            {
                UserId = userId,
                Platform = "web",
                ConnectionId = Context.ConnectionId
            });

            // Send response back to user
            await Clients.Caller.SendAsync("ReceiveMessage", response);
            
            // Notify other connected devices
            await Clients.Group($"User_{userId}")
                .SendAsync("ConversationUpdated", new { message, response });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message from user {UserId}", userId);
            await Clients.Caller.SendAsync("Error", "Failed to process your message");
        }
    }

    [HubMethodName("JoinRoom")]
    public async Task JoinRoomAsync(string roomName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
        await Clients.Group(roomName).SendAsync("UserJoined", Context.UserIdentifier);
    }

    [HubMethodName("SubscribeToUpdates")]
    public async Task SubscribeToUpdatesAsync(string[] updateTypes)
    {
        var userId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(userId)) return;

        foreach (var updateType in updateTypes)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Updates_{updateType}_{userId}");
        }
    }
}
```

**Connection Resilience Service:**
```typescript
// SignalR client with automatic reconnection
class ResilientSignalRService {
    private connection: HubConnection;
    private isStarted: boolean = false;
    private reconnectAttempts: number = 0;
    private maxReconnectAttempts: number = 10;
    private reconnectDelay: number = 1000;

    constructor(private hubUrl: string) {
        this.connection = new HubConnectionBuilder()
            .withUrl(hubUrl)
            .withAutomaticReconnect({
                nextRetryDelayInMilliseconds: (retryContext) => {
                    if (retryContext.previousRetryCount < 5) {
                        return Math.random() * 10000; // Random delay up to 10 seconds
                    } else {
                        return null; // Stop automatic reconnection after 5 attempts
                    }
                }
            })
            .configureLogging(LogLevel.Information)
            .build();

        this.setupEventHandlers();
    }

    private setupEventHandlers(): void {
        this.connection.onreconnecting((error) => {
            console.log('SignalR reconnecting...', error);
            this.showConnectionStatus('Reconnecting...');
        });

        this.connection.onreconnected((connectionId) => {
            console.log('SignalR reconnected', connectionId);
            this.showConnectionStatus('Connected');
            this.reconnectAttempts = 0;
        });

        this.connection.onclose(async (error) => {
            console.log('SignalR connection closed', error);
            this.isStarted = false;
            this.showConnectionStatus('Disconnected');
            
            // Attempt manual reconnection if automatic failed
            if (this.reconnectAttempts < this.maxReconnectAttempts) {
                await this.attemptReconnection();
            }
        });

        // Application-specific event handlers
        this.connection.on('TaskUpdated', (task) => {
            this.handleTaskUpdated(task);
        });

        this.connection.on('CalendarEventUpdated', (event) => {
            this.handleCalendarEventUpdated(event);
        });

        this.connection.on('NotificationReceived', (notification) => {
            this.handleNotificationReceived(notification);
        });
    }

    public async startAsync(): Promise<void> {
        if (this.isStarted) return;

        try {
            await this.connection.start();
            this.isStarted = true;
            this.reconnectAttempts = 0;
            this.showConnectionStatus('Connected');
            console.log('SignalR connected successfully');
        } catch (error) {
            console.error('SignalR connection failed', error);
            await this.attemptReconnection();
        }
    }

    private async attemptReconnection(): Promise<void> {
        this.reconnectAttempts++;
        const delay = this.reconnectDelay * Math.pow(2, this.reconnectAttempts - 1);
        
        console.log(`Attempting reconnection ${this.reconnectAttempts}/${this.maxReconnectAttempts} in ${delay}ms`);
        
        setTimeout(async () => {
            try {
                await this.connection.start();
                this.isStarted = true;
                this.reconnectAttempts = 0;
                this.showConnectionStatus('Connected');
            } catch (error) {
                if (this.reconnectAttempts < this.maxReconnectAttempts) {
                    await this.attemptReconnection();
                } else {
                    this.showConnectionStatus('Connection failed');
                }
            }
        }, delay);
    }

    private showConnectionStatus(status: string): void {
        // Update UI to show connection status
        const statusElement = document.getElementById('connection-status');
        if (statusElement) {
            statusElement.textContent = status;
            statusElement.className = `connection-status ${status.toLowerCase().replace(' ', '-')}`;
        }
    }

    private handleTaskUpdated(task: any): void {
        // Dispatch custom event for task updates
        window.dispatchEvent(new CustomEvent('taskUpdated', { detail: task }));
    }

    private handleCalendarEventUpdated(event: any): void {
        // Dispatch custom event for calendar updates
        window.dispatchEvent(new CustomEvent('calendarEventUpdated', { detail: event }));
    }

    private handleNotificationReceived(notification: any): void {
        // Show browser notification
        if ('Notification' in window && Notification.permission === 'granted') {
            new Notification(notification.title, {
                body: notification.message,
                icon: '/images/notification-icon.png'
            });
        }
    }
}
```

### Deliverables
- [ ] Enhanced SignalR implementation with better scalability
- [ ] Robust connection management with automatic reconnection
- [ ] Cross-platform notification system working
- [ ] Event-driven architecture for real-time updates
- [ ] Message queue integration for async operations

### Acceptance Criteria
- SignalR connections maintain stability under load
- Automatic reconnection works reliably
- Cross-platform notifications arrive consistently
- Real-time updates appear within 1 second across all clients
- System handles disconnections gracefully

## Phase 3 Success Criteria

### Multi-Platform Validation
- [ ] MAUI mobile app works seamlessly on iOS and Android
- [ ] MAUI desktop app provides native experience on Windows/macOS
- [ ] Web application includes all advanced features
- [ ] Real-time synchronization works across all platforms
- [ ] User can switch between platforms without losing context

### Feature Completeness
- [ ] All planned features implemented and tested
- [ ] Performance meets established benchmarks
- [ ] Security measures are in place and tested
- [ ] User experience is consistent across platforms
- [ ] Analytics provide valuable insights

### Technical Excellence
- [ ] Code quality meets standards (90%+ test coverage)
- [ ] No critical bugs in any platform
- [ ] Performance benchmarks achieved
- [ ] Security vulnerabilities addressed
- [ ] Documentation is complete and accurate

## Risk Assessment

### Technical Challenges
**MAUI Platform Differences:** Different behavior on iOS vs Android  
**Performance Optimization:** Ensuring smooth operation across all platforms  
**Real-time Synchronization:** Maintaining data consistency  

### Timeline Risks
**Mobile App Store Approval:** Potential delays in app store review process  
**Platform-Specific Issues:** Unexpected platform-specific bugs  
**Integration Complexity:** Challenges with real-time synchronization  

## Next Phase Preparation

### Phase 4 Prerequisites
- [ ] All Phase 3 deliverables completed and tested
- [ ] User acceptance testing completed across all platforms
- [ ] Performance benchmarks established and met
- [ ] Azure production environment prepared
- [ ] CI/CD pipeline configured for all platforms