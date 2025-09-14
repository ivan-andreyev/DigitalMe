using DigitalMe.Models;
using DigitalMe.Integrations.External.GitHub;
using Microsoft.Extensions.Logging;

namespace DigitalMe.Services.Tools.Strategies;

/// <summary>
/// Tool Strategy для поиска репозиториев на GitHub.
/// Срабатывает на упоминания GitHub, поиска кода или репозиториев.
/// </summary>
public class GitHubToolStrategy : BaseToolStrategy
{
    private readonly IGitHubService _githubService;

    public GitHubToolStrategy(IGitHubService githubService, ILogger<GitHubToolStrategy> logger)
        : base(logger)
    {
        _githubService = githubService;
    }

    public override string ToolName => "search_github_repositories";
    public override string Description => "Поиск репозиториев на GitHub";
    public override int Priority => 1; // Средний приоритет для поиска

    public override Task<bool> ShouldTriggerAsync(string message, PersonalityContext context)
    {
        var messageLower = message.ToLower();

        // Триггеры для поиска в GitHub
        var shouldTrigger = ContainsWords(messageLower,
            "найди репозиторий", "github", "поищи код", "git",
            "репозиторий", "repo", "search github", "найти на гитхабе",
            "поиск кода", "open source", "библиотека", "library",
            "framework", "проект на гитхабе", "исходный код",
            "source code", "найди проект");

        // Дополнительная проверка на технические запросы
        if (!shouldTrigger && ContainsWords(messageLower, "код", "программа", "библиотека"))
        {
            shouldTrigger = ContainsWords(messageLower, "найди", "поищи", "search", "где найти");
        }

        Logger.LogDebug("GitHub trigger check for message '{Message}': {Result}",
            message.Length > 50 ? message[..50] + "..." : message, shouldTrigger);

        return Task.FromResult(shouldTrigger);
    }

    public override async Task<object> ExecuteAsync(Dictionary<string, object> parameters, PersonalityContext context)
    {
        Logger.LogInformation("Executing GitHub repository search");

        try
        {
            ValidateRequiredParameters(parameters, "query");

            var query = GetParameter<string>(parameters, "query");
            var limit = GetParameter(parameters, "limit", 10); // По умолчанию 10 результатов

            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Search query cannot be empty");

            // Проверяем подключение к GitHub
            if (!await _githubService.IsConnectedAsync())
            {
                Logger.LogInformation("GitHub not connected, attempting initialization");
                // Try to initialize with empty token (development mode)
                await _githubService.InitializeAsync("");
            }

            var repositories = await _githubService.SearchRepositoriesAsync(query);

            // Ограничиваем количество результатов
            var limitedRepositories = repositories.Take(Math.Min(limit, 20)).ToList();

            var result = new
            {
                success = true,
                query = query,
                total_found = repositories.Count(),
                returned_count = limitedRepositories.Count,
                repositories = limitedRepositories.Select(r => new
                {
                    name = r.Name,
                    full_name = r.FullName,
                    description = r.Description ?? "Нет описания",
                    url = r.HtmlUrl,
                    language = r.Language ?? "Неизвестно",
                    stars = r.StargazersCount,
                    forks = r.ForksCount,
                    last_updated = r.UpdatedAt,
                    is_private = r.IsPrivate,
                    owner = r.Owner
                }).ToList(),
                tool_name = ToolName
            };

            Logger.LogInformation("Successfully found {Count} GitHub repositories for query '{Query}'",
                limitedRepositories.Count, query);
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to search GitHub repositories");
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
                query = new
                {
                    type = "string",
                    description = "Поисковый запрос для поиска репозиториев"
                },
                limit = new
                {
                    type = "integer",
                    description = "Максимальное количество результатов (по умолчанию 10, максимум 20)",
                    minimum = 1,
                    maximum = 20,
                    @default = 10
                },
                language = new
                {
                    type = "string",
                    description = "Фильтр по языку программирования (опционально)"
                },
                sort = new
                {
                    type = "string",
                    description = "Сортировка результатов: stars, forks, updated (по умолчанию best-match)",
                    @enum = new[] { "stars", "forks", "updated" }
                }
            },
            required = new[] { "query" }
        };
    }
}
