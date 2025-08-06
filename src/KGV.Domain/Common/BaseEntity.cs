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
    /// PostgreSQL xmin for optimistic concurrency
    /// </summary>
    public uint xmin { get; set; }

    /// <summary>
    /// Sets the created by field and created at timestamp
    /// </summary>
    /// <param name="createdBy">User who created the entity</param>
    public virtual void SetCreatedBy(string createdBy)
    {
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the updated by field and updated at timestamp
    /// </summary>
    /// <param name="updatedBy">User who updated the entity</param>
    public virtual void SetUpdatedBy(string updatedBy)
    {
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the deleted by field and deleted at timestamp for soft delete
    /// </summary>
    /// <param name="deletedBy">User who deleted the entity</param>
    public virtual void SetDeletedBy(string deletedBy)
    {
        DeletedBy = deletedBy;
        DeletedAt = DateTime.UtcNow;
        IsDeleted = true;
    }

    /// <summary>
    /// Restores a soft-deleted entity
    /// </summary>
    public virtual void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }
}