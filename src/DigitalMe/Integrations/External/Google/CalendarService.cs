using DigitalMe.Data.Entities;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;

namespace DigitalMe.Integrations.External.Google;

/// <summary>
/// Сервис для интеграции с Google Calendar API.
/// Обеспечивает создание, получение, обновление и удаление событий календаря.
/// </summary>
public class CalendarService : ICalendarService
{
    private readonly ILogger<CalendarService> _logger;
    private global::Google.Apis.Calendar.v3.CalendarService? _googleCalendarService;
    private UserCredential? _credential;
    private bool _isConnected;
    private string _calendarId = "primary";

    public CalendarService(ILogger<CalendarService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Инициализирует подключение к Google Calendar API с использованием предоставленных учетных данных.
    /// </summary>
    /// <param name="credentials">Учетные данные в формате JSON для авторизации</param>
    /// <returns>true если инициализация прошла успешно, false в противном случае</returns>
    public async Task<bool> InitializeAsync(string credentials)
    {
        try
        {
            _logger.LogInformation("Initializing Google Calendar connection...");

            // Parse OAuth2 credentials (expecting JSON format)
            var credentialStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(credentials));

            // Initialize OAuth2 flow for Calendar API access
            var clientSecrets = await GoogleClientSecrets.FromStreamAsync(credentialStream).ConfigureAwait(false);
            _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets.Secrets,
                new[] { global::Google.Apis.Calendar.v3.CalendarService.Scope.Calendar },
                "user",
                CancellationToken.None).ConfigureAwait(false);

            // Create Calendar service instance
            _googleCalendarService = new global::Google.Apis.Calendar.v3.CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = _credential,
                ApplicationName = "DigitalMe Calendar Integration"
            });

            _isConnected = _credential != null && _googleCalendarService != null;

            _logger.LogInformation("Google Calendar connection established: {Status}", _isConnected);
            return _isConnected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Google Calendar connection");
            return false;
        }
    }

    /// <summary>
    /// Создает новое событие в Google Calendar.
    /// </summary>
    /// <param name="title">Заголовок события</param>
    /// <param name="startTime">Время начала события</param>
    /// <param name="endTime">Время окончания события</param>
    /// <param name="description">Опциональное описание события</param>
    /// <returns>Созданное событие календаря</returns>
    public async Task<CalendarEvent> CreateEventAsync(string title, DateTime startTime, DateTime endTime, string? description = null)
    {
        if (!_isConnected || _googleCalendarService == null)
        {
            throw new InvalidOperationException("Calendar service not initialized. Call InitializeAsync first.");
        }

        _logger.LogInformation("Creating Google Calendar event: {Title} from {Start} to {End}", title, startTime, endTime);

        var googleEvent = new Event
        {
            Summary = title,
            Description = description,
            Start = new EventDateTime
            {
                DateTimeDateTimeOffset = startTime,
                TimeZone = TimeZoneInfo.Local.Id
            },
            End = new EventDateTime
            {
                DateTimeDateTimeOffset = endTime,
                TimeZone = TimeZoneInfo.Local.Id
            }
        };

        var request = _googleCalendarService.Events.Insert(googleEvent, _calendarId);
        var createdEvent = await request.ExecuteAsync().ConfigureAwait(false);

        return new CalendarEvent
        {
            EventId = createdEvent.Id ?? string.Empty,
            CalendarId = _calendarId,
            Title = createdEvent.Summary ?? title,
            Description = createdEvent.Description ?? description ?? "",
            StartTime = createdEvent.Start?.DateTimeDateTimeOffset?.DateTime ?? startTime,
            EndTime = createdEvent.End?.DateTimeDateTimeOffset?.DateTime ?? endTime,
            Location = createdEvent.Location ?? "",
            Attendees = createdEvent.Attendees?.Select(a => a.Email).ToList() ?? new List<string>()
        };
    }

    public async Task<IEnumerable<CalendarEvent>> GetUpcomingEventsAsync(int limit = 10)
    {
        if (!_isConnected || _googleCalendarService == null)
        {
            throw new InvalidOperationException("Calendar service not initialized. Call InitializeAsync first.");
        }

        _logger.LogInformation("Getting upcoming Google Calendar events, limit: {Limit}", limit);

        var request = _googleCalendarService.Events.List(_calendarId);
        request.TimeMinDateTimeOffset = DateTime.UtcNow;
        request.ShowDeleted = false;
        request.SingleEvents = true;
        request.MaxResults = limit;
        request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

        var events = await request.ExecuteAsync().ConfigureAwait(false);

        return events.Items?.Select(e => new CalendarEvent
        {
            EventId = e.Id ?? string.Empty,
            CalendarId = _calendarId,
            Title = e.Summary ?? "No Title",
            Description = e.Description ?? "",
            StartTime = e.Start?.DateTimeDateTimeOffset?.DateTime ?? DateTime.UtcNow,
            EndTime = e.End?.DateTimeDateTimeOffset?.DateTime ?? DateTime.UtcNow.AddHours(1),
            Location = e.Location ?? "",
            Attendees = e.Attendees?.Select(a => a.Email).ToList() ?? new List<string>()
        }) ?? Enumerable.Empty<CalendarEvent>();
    }

    public async Task<CalendarEvent> UpdateEventAsync(string eventId, CalendarEvent updatedEvent)
    {
        if (!_isConnected || _googleCalendarService == null)
        {
            throw new InvalidOperationException("Calendar service not initialized. Call InitializeAsync first.");
        }

        _logger.LogInformation("Updating Google Calendar event: {EventId}", eventId);

        var googleEvent = new Event
        {
            Summary = updatedEvent.Title,
            Description = updatedEvent.Description,
            Location = updatedEvent.Location,
            Start = new EventDateTime
            {
                DateTimeDateTimeOffset = updatedEvent.StartTime,
                TimeZone = TimeZoneInfo.Local.Id
            },
            End = new EventDateTime
            {
                DateTimeDateTimeOffset = updatedEvent.EndTime,
                TimeZone = TimeZoneInfo.Local.Id
            }
        };

        var request = _googleCalendarService.Events.Update(googleEvent, _calendarId, eventId);
        var updated = await request.ExecuteAsync().ConfigureAwait(false);

        return new CalendarEvent
        {
            EventId = updated.Id ?? eventId,
            CalendarId = _calendarId,
            Title = updated.Summary ?? updatedEvent.Title,
            Description = updated.Description ?? updatedEvent.Description,
            StartTime = updated.Start?.DateTimeDateTimeOffset?.DateTime ?? updatedEvent.StartTime,
            EndTime = updated.End?.DateTimeDateTimeOffset?.DateTime ?? updatedEvent.EndTime,
            Location = updated.Location ?? updatedEvent.Location,
            Attendees = updated.Attendees?.Select(a => a.Email).ToList() ?? updatedEvent.Attendees
        };
    }

    public async Task<bool> DeleteEventAsync(string eventId)
    {
        if (!_isConnected || _googleCalendarService == null)
        {
            throw new InvalidOperationException("Calendar service not initialized. Call InitializeAsync first.");
        }

        _logger.LogInformation("Deleting Google Calendar event: {EventId}", eventId);

        try
        {
            var request = _googleCalendarService.Events.Delete(_calendarId, eventId);
            await request.ExecuteAsync().ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete Google Calendar event: {EventId}", eventId);
            return false;
        }
    }

    public async Task<bool> IsConnectedAsync()
    {
        if (!_isConnected || _googleCalendarService == null || _credential == null)
        {
            return false;
        }

        try
        {
            // Test connection by making a simple API call
            var request = _googleCalendarService.CalendarList.Get("primary");
            await request.ExecuteAsync().ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
