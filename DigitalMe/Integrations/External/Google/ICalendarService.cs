namespace DigitalMe.Integrations.External.Google;

public interface ICalendarService
{
    Task<bool> InitializeAsync(string credentials);
    Task<CalendarEvent> CreateEventAsync(string title, DateTime startTime, DateTime endTime, string? description = null);
    Task<IEnumerable<CalendarEvent>> GetUpcomingEventsAsync(int limit = 10);
    Task<CalendarEvent> UpdateEventAsync(string eventId, CalendarEvent updatedEvent);
    Task<bool> DeleteEventAsync(string eventId);
    Task<bool> IsConnectedAsync();
}

public class CalendarEvent
{
    public string EventId { get; set; } = string.Empty;
    public string CalendarId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Location { get; set; } = string.Empty;
    public List<string> Attendees { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
