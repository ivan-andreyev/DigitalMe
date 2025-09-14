using DigitalMe.Integrations.MCP.Models;

namespace DigitalMe.Integrations.MCP.Tools;

public class ToolRegistry
{
    public static IEnumerable<McpTool> GetAvailableTools()
    {
        return new[]
        {
            new McpTool
            {
                Name = "send_telegram_message",
                Description = "Отправить сообщение в Telegram",
                Parameters = new
                {
                    chat_id = new { type = "number", description = "ID чата" },
                    message = new { type = "string", description = "Текст сообщения" }
                }
            },
            new McpTool
            {
                Name = "create_calendar_event",
                Description = "Создать событие в Google Calendar",
                Parameters = new
                {
                    title = new { type = "string", description = "Название события" },
                    start_time = new { type = "string", description = "Время начала (ISO 8601)" },
                    end_time = new { type = "string", description = "Время окончания (ISO 8601)" }
                }
            },
            new McpTool
            {
                Name = "search_github_repositories",
                Description = "Поиск репозиториев на GitHub",
                Parameters = new
                {
                    query = new { type = "string", description = "Поисковый запрос" }
                }
            },
            new McpTool
            {
                Name = "get_personality_traits",
                Description = "Получить черты личности Ивана",
                Parameters = new
                {
                    category = new { type = "string", description = "Категория черт (опционально)" }
                }
            },
            new McpTool
            {
                Name = "store_memory",
                Description = "Сохранить важную информацию в долгосрочную память",
                Parameters = new
                {
                    key = new { type = "string", description = "Ключ для хранения" },
                    value = new { type = "string", description = "Значение для сохранения" },
                    importance = new { type = "number", description = "Важность (1-10)" }
                }
            }
        };
    }
}
