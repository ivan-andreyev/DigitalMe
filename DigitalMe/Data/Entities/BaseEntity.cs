using System.ComponentModel.DataAnnotations;

namespace DigitalMe.Data.Entities;

/// <summary>
/// Base entity class providing common properties for all domain entities.
/// Implements consistent Id generation, audit trail, and change tracking.
/// </summary>
public abstract class BaseEntity : IEntity
{
    /// <summary>
    /// Unique identifier for the entity, auto-generated on creation.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Timestamp when the entity was created (UTC).
    /// Set automatically on construction, immutable thereafter.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Timestamp when the entity was last updated (UTC).
    /// Updated automatically by EF Core SaveChanges override.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Protected constructor for Entity Framework and derived classes.
    /// Ensures consistent initialization of base properties.
    /// </summary>
    protected BaseEntity() { }
}

/// <summary>
/// Base entity with additional audit trail support.
/// Tracks who created/modified the entity and why.
/// </summary>
public abstract class AuditableBaseEntity : BaseEntity, IAuditableEntity
{
    /// <summary>
    /// Identifier of the user who created this entity.
    /// </summary>
    public string? CreatedBy { get; set; }
    
    /// <summary>
    /// Identifier of the user who last updated this entity.
    /// </summary>
    public string? UpdatedBy { get; set; }
    
    /// <summary>
    /// Optional reason for the last update (for audit purposes).
    /// </summary>
    public string? ChangeReason { get; set; }
    
    protected AuditableBaseEntity() : base() { }
}

/// <summary>
/// Interface for all entities with basic properties.
/// Enables generic repository patterns and consistent entity handling.
/// </summary>
public interface IEntity
{
    Guid Id { get; }
    DateTime CreatedAt { get; }
    DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Interface for entities that support audit trail functionality.
/// </summary>
public interface IAuditableEntity : IEntity
{
    string? CreatedBy { get; set; }
    string? UpdatedBy { get; set; }
    string? ChangeReason { get; set; }
}