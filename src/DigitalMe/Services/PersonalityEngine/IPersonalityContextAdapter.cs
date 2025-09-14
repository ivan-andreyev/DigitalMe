using DigitalMe.Data.Entities;
using DigitalMe.Services.Strategies;

namespace DigitalMe.Services.PersonalityEngine;

/// <summary>
/// Интерфейс для адаптации профиля личности под ситуационный контекст.
/// Отвечает только за контекстную адаптацию черт личности.
/// </summary>
public interface IPersonalityContextAdapter
{
    /// <summary>
    /// Адаптирует профиль личности под текущий ситуационный контекст.
    /// </summary>
    /// <param name="basePersonality">Базовый профиль личности</param>
    /// <param name="context">Ситуационный контекст</param>
    /// <returns>Адаптированный профиль личности</returns>
    Task<PersonalityProfile> AdaptToContextAsync(PersonalityProfile basePersonality, SituationalContext context);

    /// <summary>
    /// Клонирует профиль личности для безопасной модификации.
    /// </summary>
    /// <param name="original">Оригинальный профиль</param>
    /// <returns>Клон профиля для модификации</returns>
    PersonalityProfile ClonePersonalityProfile(PersonalityProfile original);
}