using DigitalMe.Integrations.External.GitHub;
using DigitalMe.Integrations.External.Google;
using DigitalMe.Integrations.External.Telegram;
using DigitalMe.Services;

namespace DigitalMe.Integrations.MCP.Tools;

/// <summary>
/// Исполнитель инструментов для MCP протокола.
/// Обеспечивает выполнение различных инструментов интеграции (Telegram, Calendar, Memory).
/// </summary>
public class ToolExecutor
{
    private readonly IPersonalityService _personalityService;
    private readonly ITelegramService _telegramService;
    private readonly ICalendarService _calendarService;
    private readonly IGitHubService _githubService;
    private readonly ILogger<ToolExecutor> _logger;

    public ToolExecutor(
        IPersonalityService personalityService,
        ITelegramService telegramService,
        ICalendarService calendarService,
        IGitHubService githubService,
        ILogger<ToolExecutor> logger)
    {
        _personalityService = personalityService;
        _telegramService = telegramService;
        _calendarService = calendarService;
        _githubService = githubService;
        _logger = logger;
    }

    public async Task<object> ExecuteToolAsync(string toolName, Dictionary<string, object> parameters)
    {
        _logger.LogInformation("Executing tool {ToolName} with parameters {Parameters}",
            toolName, string.Join(", ", parameters.Keys));

        try
        {
            return toolName switch
            {
                "get_personality_traits" => await GetPersonalityTraits(parameters),
                "store_memory" => await StoreMemory(parameters),

                // External services integrations
                "send_telegram_message" => await SendTelegramMessage(parameters),
                "create_calendar_event" => await CreateCalendarEvent(parameters),
                "search_github_repositories" => await SearchGitHubRepositories(parameters),

                _ => throw new NotSupportedException($"Tool {toolName} not supported")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute tool {ToolName}", toolName);
            return new { error = ex.Message };
        }
    }

    private async Task<object> GetPersonalityTraits(Dictionary<string, object> parameters)
    {
        var personalityResult = await _personalityService.GetPersonalityAsync();
        if (!personalityResult.IsSuccess)
            return new { error = "Ivan's personality not found", details = personalityResult.Error };

        var personality = personalityResult.Value!;
        var traits = personality.Traits ?? new List<PersonalityTrait>();
        var filteredTraits = traits.AsEnumerable();

        if (parameters.ContainsKey("category"))
        {
            var category = parameters["category"].ToString();
            filteredTraits = filteredTraits.Where(t => t.Category.Contains(category!, StringComparison.OrdinalIgnoreCase));
        }

        return new
        {
            personality_id = personality.Id,
            name = personality.Name,
            traits = filteredTraits.Select(t => new
            {
                category = t.Category,
                name = t.Name,
                description = t.Description,
                weight = t.Weight
            })
        };
    }

    private Task<object> StoreMemory(Dictionary<string, object> parameters)
    {
        var key = parameters["key"].ToString();
        var value = parameters["value"].ToString();
        var importance = Convert.ToDouble(parameters.GetValueOrDefault("importance", 5));

        // TODO: Implement actual memory storage when memory service is ready
        _logger.LogInformation("Storing memory: {Key} = {Value} (importance: {Importance})",
            key, value, importance);

        return Task.FromResult<object>(new
        {
            success = true,
            key,
            stored_at = DateTime.UtcNow,
            importance
        });
    }

    private async Task<object> SendTelegramMessage(Dictionary<string, object> parameters)
    {
        try
        {
            var chatId = Convert.ToInt64(parameters["chat_id"]);
            var message = parameters["message"].ToString()!;

            if (!await _telegramService.IsConnectedAsync())
            {
                // Try to initialize with empty token (development mode)
                await _telegramService.InitializeAsync("");
            }

            var telegramMessage = await _telegramService.SendMessageAsync(chatId, message);

            return new
            {
                success = true,
                message_id = telegramMessage.MessageId,
                chat_id = telegramMessage.ChatId,
                sent_at = telegramMessage.MessageDate
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Telegram message");
            return new { success = false, error = ex.Message };
        }
    }

    private async Task<object> CreateCalendarEvent(Dictionary<string, object> parameters)
    {
        try
        {
            var title = parameters["title"].ToString()!;
            var startTime = DateTime.Parse(parameters["start_time"].ToString()!);
            var endTime = DateTime.Parse(parameters["end_time"].ToString()!);

            if (!await _calendarService.IsConnectedAsync())
            {
                // Try to initialize with empty credentials (development mode)
                await _calendarService.InitializeAsync("");
            }

            var calendarEvent = await _calendarService.CreateEventAsync(title, startTime, endTime);

            return new
            {
                success = true,
                event_id = calendarEvent.EventId,
                title = calendarEvent.Title,
                start_time = calendarEvent.StartTime,
                end_time = calendarEvent.EndTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create calendar event");
            return new { success = false, error = ex.Message };
        }
    }

    private async Task<object> SearchGitHubRepositories(Dictionary<string, object> parameters)
    {
        try
        {
            var query = parameters["query"].ToString()!;

            if (!await _githubService.IsConnectedAsync())
            {
                // Try to initialize with empty token (development mode)
                await _githubService.InitializeAsync("");
            }

            var repositories = await _githubService.SearchRepositoriesAsync(query);

            return new
            {
                success = true,
                repositories = repositories.Select(r => new
                {
                    name = r.Name,
                    full_name = r.FullName,
                    description = r.Description,
                    url = r.HtmlUrl,
                    language = r.Language,
                    stars = r.StargazersCount
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search GitHub repositories");
            return new { success = false, error = ex.Message };
        }
    }
}
