using System.ComponentModel.DataAnnotations;

namespace KGV.Domain.Common;

/// <summary>
/// Base class for all domain entities
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Unique identifier for the entity
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// When the entity was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the entity was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Who created the entity
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Who last updated the entity
    /// </summary>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Soft delete flag
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// When the entity was deleted (soft delete)
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Who deleted the entity
    /// </summary>
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Row version for optimistic concurrency
    /// </summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }
}