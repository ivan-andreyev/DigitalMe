using System.Text.Json;

namespace DigitalMe.Common;

/// <summary>
/// Константы для JSON сериализации - устраняет дублирование JsonSerializerOptions
/// Заменяет 4+ дублированных создания JsonSerializerOptions в SlackApiClient
/// </summary>
public static class JsonConstants
{
    /// <summary>
    /// Стандартные опции JSON сериализации с camelCase именованием
    /// Используется для большинства внешних API (Slack, GitHub, etc.)
    /// </summary>
    public static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Опции JSON для отладки с форматированием
    /// Используется в development режиме для читаемости
    /// </summary>
    public static readonly JsonSerializerOptions PrettyPrintOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Опции JSON для snake_case API (некоторые внешние сервисы)
    /// </summary>
    public static readonly JsonSerializerOptions SnakeCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Строгие опции JSON без игнорирования null значений
    /// Используется для внутренних API где важна полнота данных
    /// </summary>
    public static readonly JsonSerializerOptions StrictOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
    };
}