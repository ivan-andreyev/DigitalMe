using DigitalMe.Models;

namespace DigitalMe.Services.Tools;

/// <summary>
/// Реестр инструментов для управления коллекцией доступных Tool Strategy.
/// Обеспечивает регистрацию, поиск и управление инструментами.
/// </summary>
public interface IToolRegistry
{
    /// <summary>
    /// Регистрирует новый инструмент в реестре
    /// </summary>
    /// <param name="toolStrategy">Стратегия инструмента для регистрации</param>
    void RegisterTool(IToolStrategy toolStrategy);
    
    /// <summary>
    /// Получает все зарегистрированные инструменты
    /// </summary>
    /// <returns>Коллекция всех доступных инструментов</returns>
    IEnumerable<IToolStrategy> GetAllTools();
    
    /// <summary>
    /// Найти инструмент по имени
    /// </summary>
    /// <param name="toolName">Имя инструмента</param>
    /// <returns>Инструмент или null если не найден</returns>
    IToolStrategy? GetTool(string toolName);
    
    /// <summary>
    /// Определяет какие инструменты должны сработать для данного сообщения
    /// </summary>
    /// <param name="message">Сообщение пользователя</param>
    /// <param name="context">Контекст личности</param>
    /// <returns>Список инструментов которые должны сработать, отсортированный по приоритету</returns>
    Task<List<IToolStrategy>> GetTriggeredToolsAsync(string message, PersonalityContext context);
}