using DigitalMe.Models;

namespace DigitalMe.Services.Tools;

/// <summary>
/// Интерфейс стратегии инструмента для реализации Strategy Pattern.
/// Каждый инструмент реализует этот интерфейс для определения логики триггеров и выполнения.
/// </summary>
public interface IToolStrategy
{
    /// <summary>
    /// Уникальное имя инструмента
    /// </summary>
    string ToolName { get; }

    /// <summary>
    /// Описание инструмента для отображения пользователю
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Приоритет инструмента (выше = важнее при конфликтах)
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Определяет, должен ли данный инструмент сработать на основе сообщения и контекста
    /// </summary>
    /// <param name="message">Сообщение пользователя</param>
    /// <param name="context">Контекст личности с историей сообщений</param>
    /// <returns>True если инструмент должен сработать</returns>
    Task<bool> ShouldTriggerAsync(string message, PersonalityContext context);

    /// <summary>
    /// Выполняет инструмент с указанными параметрами
    /// </summary>
    /// <param name="parameters">Параметры для выполнения</param>
    /// <param name="context">Контекст личности для персонализации</param>
    /// <returns>Результат выполнения инструмента</returns>
    Task<object> ExecuteAsync(Dictionary<string, object> parameters, PersonalityContext context);
}
