using KGV.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace KGV.Domain.Entities;

/// <summary>
/// District entity (Bezirk) representing administrative districts
/// </summary>
public class Bezirk : BaseEntity
{
    /// <summary>
    /// District name/identifier
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Full display name of the district
    /// </summary>
    [MaxLength(100)]
    public string? DisplayName { get; private set; }

    /// <summary>
    /// Description of the district
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; private set; }

    /// <summary>
    /// Whether the district is currently active
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Sort order for displaying districts
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// Navigation property to related cadastral districts
    /// </summary>
    public virtual ICollection<Katasterbezirk> Katasterbezirke { get; private set; } = new List<Katasterbezirk>();

    /// <summary>
    /// Navigation property to related file references
    /// </summary>
    public virtual ICollection<AktenzeichenEntity> Aktenzeichen { get; private set; } = new List<AktenzeichenEntity>();

    /// <summary>
    /// Navigation property to related entry numbers
    /// </summary>
    public virtual ICollection<Eingangsnummer> Eingangsnummern { get; private set; } = new List<Eingangsnummer>();

    /// <summary>
    /// Creates a new Bezirk
    /// </summary>
    /// <param name="name">District name/identifier</param>
    /// <param name="displayName">Full display name</param>
    /// <param name="description">Description</param>
    /// <param name="sortOrder">Sort order</param>
    public static Bezirk Create(string name, string? displayName = null, string? description = null, int sortOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));

        if (name.Length > 10)
            throw new ArgumentException("Name cannot be longer than 10 characters", nameof(name));

        var bezirk = new Bezirk
        {
            Name = name.Trim().ToUpperInvariant(),
            DisplayName = displayName?.Trim(),
            Description = description?.Trim(),
            SortOrder = sortOrder,
            IsActive = true
        };

        return bezirk;
    }

    /// <summary>
    /// Updates the district information
    /// </summary>
    public void Update(string? displayName = null, string? description = null, int? sortOrder = null)
    {
        if (displayName != null)
            DisplayName = displayName.Trim();

        if (description != null)
            Description = description.Trim();

        if (sortOrder.HasValue)
            SortOrder = sortOrder.Value;

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the district
    /// </summary>
    public void Activate()
    {
        if (!IsActive)
        {
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Deactivates the district
    /// </summary>
    public void Deactivate()
    {
        if (IsActive)
        {
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Gets the display name or falls back to the name
    /// </summary>
    public string GetDisplayName()
    {
        return !string.IsNullOrWhiteSpace(DisplayName) ? DisplayName : Name;
    }

    private Bezirk()
    {
        // Required for EF Core
    }
}