using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigitalMe.Data.Entities;

/// <summary>
/// Basic user entity for MVP.
/// Minimal implementation for authentication/conversation tracking.
/// </summary>
[Table("Users")]
public class User : BaseEntity
{
    /// <summary>
    /// Username or display name.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Email address (optional for MVP).
    /// </summary>
    [MaxLength(200)]
    public string? Email { get; set; }

    public User() : base() { }
}
