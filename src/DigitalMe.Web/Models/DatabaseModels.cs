using System.ComponentModel.DataAnnotations;

namespace DigitalMe.Web.Models;

public class UserProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string UserName { get; set; } = string.Empty;
    
    public string? DisplayName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ProfileData { get; set; } // JSON for demo purposes
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ProfileImageUrl { get; set; }
}

public class ChatSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string? Title { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public UserProfile UserProfile { get; set; } = null!;
    public List<ChatMessageEntity> Messages { get; set; } = new();
}

public class ChatMessageEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string MessageType { get; set; } = "user"; // user, assistant, system
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Metadata { get; set; } // JSON metadata
    
    // Navigation properties
    public ChatSession ChatSession { get; set; } = null!;
}

public class SystemConfiguration
{
    [Key]
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string ValueType { get; set; } = "string"; // string, int, bool, json
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}