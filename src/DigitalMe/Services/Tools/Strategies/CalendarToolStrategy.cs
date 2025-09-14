using DigitalMe.Integrations.External.Google;
using DigitalMe.Models;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Tools.Strategies;

/// <summary>
/// Tool Strategy для создания событий в Google Calendar.
/// Срабатывает на упоминания создания встреч, событий или календаря.
/// </summary>
public class CalendarToolStrategy : BaseToolStrategy
{
    private readonly ICalendarService _calendarService;

    public CalendarToolStrategy(ICalendarService calendarService, ILogger<CalendarToolStrategy> logger)
        : base(logger)
    {
        _calendarService = calendarService;
    }

    public override string ToolName => "create_calendar_event";
    public override string Description => "Создать событие в Google Calendar";
    public override int Priority => 3; // Высокий приоритет для планирования

    public override Task<bool> ShouldTriggerAsync(string message, PersonalityContext context)
    {
        var messageLower = message.ToLower();

        // Триггеры для создания событий календаря
        var shouldTrigger = ContainsWords(messageLower,
            "создай событие", "добавь в календарь", "meeting", "встреча",
            "запланируй", "calendar", "календарь", "событие",
            "встреча на", "мероприятие", "appointment", "schedule",
            "забронируй время", "создай встречу", "назначь встречу");

        // Дополнительная проверка на временные выражения
        if (!shouldTrigger)
        {
            shouldTrigger = MatchesPattern(messageLower,
                @"\b(в|на|завтра|послезавтра|\d{1,2}:\d{2}|\d{1,2}\.\d{1,2})\b.*\b(встреча|событие|собрание)\b") ||
                MatchesPattern(messageLower,
                @"\b(напомни|reminder)\b.*\b(в|на|завтра)\b");
        }

        Logger.LogDebug("Calendar trigger check for message '{Message}': {Result}",
            message.Length > 50 ? message[..50] + "..." : message, shouldTrigger);

        return Task.FromResult(shouldTrigger);
    }

    public override async Task<object> ExecuteAsync(Dictionary<string, object> parameters, PersonalityContext context)
    {
        Logger.LogInformation("Executing calendar event creation");

        try
        {
            ValidateRequiredParameters(parameters, "title", "start_time", "end_time");

            var title = GetParameter<string>(parameters, "title");
            var startTimeStr = GetParameter<string>(parameters, "start_time");
            var endTimeStr = GetParameter<string>(parameters, "end_time");

            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title cannot be empty");

            if (!DateTime.TryParse(startTimeStr, out var startTime))
                throw new ArgumentException("Invalid start_time format");

            if (!DateTime.TryParse(endTimeStr, out var endTime))
                throw new ArgumentException("Invalid end_time format");

            if (endTime <= startTime)
                throw new ArgumentException("End time must be after start time");

            // Проверяем подключение к Calendar
            if (!await _calendarService.IsConnectedAsync())
            {
                Logger.LogInformation("Calendar not connected, attempting initialization");
                // Try to initialize with empty credentials (development mode)
                await _calendarService.InitializeAsync("");
            }

            var calendarEvent = await _calendarService.CreateEventAsync(title, startTime, endTime);

            var result = new
            {
                success = true,
                event_id = calendarEvent.EventId,
                title = calendarEvent.Title,
                start_time = calendarEvent.StartTime,
                end_time = calendarEvent.EndTime,
                duration_minutes = (calendarEvent.EndTime - calendarEvent.StartTime).TotalMinutes,
                tool_name = ToolName
            };

            Logger.LogInformation("Successfully created calendar event '{Title}' from {StartTime} to {EndTime}",
                title, startTime, endTime);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create calendar event");
            return new
            {
                success = false,
                error = ex.Message,
                tool_name = ToolName
            };
        }
    }

    public override object GetParameterSchema()
    {
        return new
        {
            type = "object",
            properties = new
            {
                title = new
                {
                    type = "string",
                    description = "Название события"
                },
                start_time = new
                {
                    type = "string",
                    description = "Время начала события в формате ISO 8601 (например: 2025-09-02T14:00:00Z)"
                },
                end_time = new
                {
                    type = "string",
                    description = "Время окончания события в формате ISO 8601 (например: 2025-09-02T15:00:00Z)"
                },
                description = new
                {
                    type = "string",
                    description = "Описание события (опционально)"
                }
            },
            required = new[] { "title", "start_time", "end_time" }
        };
    }
}
