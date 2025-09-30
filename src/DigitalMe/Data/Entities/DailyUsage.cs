using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigitalMe.Data.Entities;

/// <summary>
/// Агрегированное использование API за день для пользователя и провайдера.
/// Используется для отслеживания квот и лимитов.
/// </summary>
[Table("DailyUsages")]
public class DailyUsage : BaseEntity
{
    /// <summary>
    /// Идентификатор пользователя.
    /// </summary>
    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Название провайдера API (например, "Anthropic", "OpenAI").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Дата использования (без времени).
    /// </summary>
    [Required]
    [Column(TypeName = "date")]
    public DateTime Date { get; set; } = DateTime.Today;

    /// <summary>
    /// Общее количество использованных токенов за день.
    /// </summary>
    public int TokensUsed { get; set; } = 0;

    /// <summary>
    /// Общее количество запросов за день.
    /// </summary>
    public int RequestCount { get; set; } = 0;

    /// <summary>
    /// Общая расчетная стоимость за день в долларах США.
    /// </summary>
    [Column(TypeName = "decimal(10, 6)")]
    public decimal TotalCost { get; set; } = 0m;

    /// <summary>
    /// Default constructor for Entity Framework.
    /// </summary>
    public DailyUsage() : base()
    {
    }
}