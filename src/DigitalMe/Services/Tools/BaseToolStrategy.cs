using DigitalMe.Models;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace DigitalMe.Services.Tools;

/// <summary>
/// Базовый класс для Tool Strategy с общими утилитами.
/// Предоставляет удобные методы для анализа сообщений и определения триггеров.
/// Implements both IToolStrategy and IParameterizedTool for backward compatibility.
/// </summary>
public abstract class BaseToolStrategy : IToolStrategy, IParameterizedTool
{
    protected readonly ILogger Logger;

    protected BaseToolStrategy(ILogger logger)
    {
        Logger = logger;
    }

    public abstract string ToolName { get; }
    public abstract string Description { get; }
    public virtual int Priority => 1; // По умолчанию средний приоритет

    public abstract Task<bool> ShouldTriggerAsync(string message, PersonalityContext context);
    public abstract Task<object> ExecuteAsync(Dictionary<string, object> parameters, PersonalityContext context);
    public abstract object GetParameterSchema();

    /// <summary>
    /// Проверяет содержит ли сообщение любое из указанных слов/фраз (без учета регистра)
    /// </summary>
    protected static bool ContainsWords(string message, params string[] words)
    {
        if (string.IsNullOrWhiteSpace(message) || words == null || words.Length == 0)
            return false;

        var messageLower = message.ToLower();
        return words.Any(word => messageLower.Contains(word.ToLower()));
    }

    /// <summary>
    /// Проверяет соответствие сообщения регулярному выражению
    /// </summary>
    protected static bool MatchesPattern(string message, string pattern, RegexOptions options = RegexOptions.IgnoreCase)
    {
        if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(pattern))
            return false;

        try
        {
            return Regex.IsMatch(message, pattern, options);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Проверяет платформу из которой пришло сообщение
    /// </summary>
    protected static bool IsPlatform(PersonalityContext context, params string[] platforms)
    {
        if (context?.CurrentState == null || platforms == null || platforms.Length == 0)
            return false;

        if (!context.CurrentState.TryGetValue("platform", out var platformObj))
            return false;

        var platform = platformObj?.ToString()?.ToLower();
        return platforms.Any(p => string.Equals(p.ToLower(), platform, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Получает значение параметра с проверкой типа и значением по умолчанию
    /// </summary>
    protected static T GetParameter<T>(Dictionary<string, object> parameters, string key, T defaultValue = default!)
    {
        if (parameters == null || !parameters.TryGetValue(key, out var value))
            return defaultValue;

        try
        {
            if (value is T directValue)
                return directValue;

            // Попытка конвертации для примитивных типов
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Проверяет наличие обязательных параметров
    /// </summary>
    protected static void ValidateRequiredParameters(Dictionary<string, object> parameters, params string[] requiredKeys)
    {
        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters));

        var missingKeys = requiredKeys.Where(key => !parameters.ContainsKey(key) || parameters[key] == null).ToList();

        if (missingKeys.Any())
            throw new ArgumentException($"Missing required parameters: {string.Join(", ", missingKeys)}");
    }
}
