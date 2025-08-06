using KGV.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace KGV.Domain.Entities;

/// <summary>
/// Junction table entity representing the many-to-many relationship between Bezirk and Katasterbezirk
/// This table maps districts to their cadastral districts as per the legacy database structure
/// </summary>
public class BezirkeKatasterbezirke : BaseEntity
{
    /// <summary>
    /// District name reference (legacy field)
    /// </summary>
    [Required]
    [MaxLength(10)]
    public required string BezirkName { get; init; }

    /// <summary>
    /// Cadastral district code
    /// </summary>
    [Required]
    [MaxLength(10)]
    public required string KatasterbezirkCode { get; init; }

    /// <summary>
    /// Cadastral district name
    /// </summary>
    [Required]
    [MaxLength(50)]
    public required string KatasterbezirkName { get; init; }

    /// <summary>
    /// Reference to the modern Bezirk entity (for navigation)
    /// </summary>
    public Guid? BezirkId { get; private set; }

    /// <summary>
    /// Reference to the modern Katasterbezirk entity (for navigation)
    /// </summary>
    public Guid? KatasterbezirkId { get; private set; }

    /// <summary>
    /// Whether this mapping is currently active
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Additional notes or description for this mapping
    /// </summary>
    [MaxLength(500)]
    public string? Beschreibung { get; private set; }

    /// <summary>
    /// Sort order within the district
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// Navigation property to the Bezirk entity
    /// </summary>
    public virtual Bezirk? Bezirk { get; private set; }

    /// <summary>
    /// Navigation property to the Katasterbezirk entity
    /// </summary>
    public virtual Katasterbezirk? Katasterbezirk { get; private set; }

    /// <summary>
    /// Creates a new BezirkeKatasterbezirke mapping
    /// </summary>
    /// <param name="bezirkName">District name</param>
    /// <param name="katasterbezirkCode">Cadastral district code</param>
    /// <param name="katasterbezirkName">Cadastral district name</param>
    /// <param name="bezirkId">Optional modern Bezirk ID</param>
    /// <param name="katasterbezirkId">Optional modern Katasterbezirk ID</param>
    /// <param name="beschreibung">Optional description</param>
    /// <param name="sortOrder">Sort order</param>
    public static BezirkeKatasterbezirke Create(
        string bezirkName,
        string katasterbezirkCode,
        string katasterbezirkName,
        Guid? bezirkId = null,
        Guid? katasterbezirkId = null,
        string? beschreibung = null,
        int sortOrder = 0)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(bezirkName))
            throw new ArgumentException("BezirkName is required", nameof(bezirkName));

        if (string.IsNullOrWhiteSpace(katasterbezirkCode))
            throw new ArgumentException("KatasterbezirkCode is required", nameof(katasterbezirkCode));

        if (string.IsNullOrWhiteSpace(katasterbezirkName))
            throw new ArgumentException("KatasterbezirkName is required", nameof(katasterbezirkName));

        if (bezirkName.Length > 10)
            throw new ArgumentException("BezirkName cannot be longer than 10 characters", nameof(bezirkName));

        if (katasterbezirkCode.Length > 10)
            throw new ArgumentException("KatasterbezirkCode cannot be longer than 10 characters", nameof(katasterbezirkCode));

        if (katasterbezirkName.Length > 50)
            throw new ArgumentException("KatasterbezirkName cannot be longer than 50 characters", nameof(katasterbezirkName));

        var mapping = new BezirkeKatasterbezirke
        {
            BezirkName = bezirkName.Trim().ToUpperInvariant(),
            KatasterbezirkCode = katasterbezirkCode.Trim().ToUpperInvariant(),
            KatasterbezirkName = katasterbezirkName.Trim(),
            BezirkId = bezirkId,
            KatasterbezirkId = katasterbezirkId,
            Beschreibung = beschreibung?.Trim(),
            SortOrder = sortOrder,
            IsActive = true
        };

        return mapping;
    }

    /// <summary>
    /// Updates the mapping information
    /// </summary>
    public void Update(
        string? katasterbezirkName = null,
        string? beschreibung = null,
        int? sortOrder = null,
        Guid? bezirkId = null,
        Guid? katasterbezirkId = null)
    {
        // KatasterbezirkName is init-only and cannot be updated after construction
        // If you need to update the name, create a new instance
        if (!string.IsNullOrWhiteSpace(katasterbezirkName))
        {
            throw new InvalidOperationException("KatasterbezirkName cannot be updated after construction. Create a new instance instead.");
        }

        if (beschreibung != null)
            Beschreibung = beschreibung.Trim();

        if (sortOrder.HasValue)
            SortOrder = sortOrder.Value;

        if (bezirkId.HasValue)
            BezirkId = bezirkId.Value;

        if (katasterbezirkId.HasValue)
            KatasterbezirkId = katasterbezirkId.Value;

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Links this mapping to modern entities
    /// </summary>
    public void LinkToModernEntities(Guid bezirkId, Guid katasterbezirkId)
    {
        if (bezirkId == Guid.Empty)
            throw new ArgumentException("BezirkId cannot be empty", nameof(bezirkId));

        if (katasterbezirkId == Guid.Empty)
            throw new ArgumentException("KatasterbezirkId cannot be empty", nameof(katasterbezirkId));

        BezirkId = bezirkId;
        KatasterbezirkId = katasterbezirkId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the mapping
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
    /// Deactivates the mapping
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
    /// Gets the full display name of the mapping
    /// </summary>
    public string GetFullDisplayName()
    {
        return $"{BezirkName} → {KatasterbezirkCode} ({KatasterbezirkName})";
    }

    /// <summary>
    /// Checks if this mapping is linked to modern entities
    /// </summary>
    public bool IsLinkedToModernEntities()
    {
        return BezirkId.HasValue && KatasterbezirkId.HasValue;
    }

    /// <summary>
    /// Creates a composite key for uniqueness (District + Cadastral District)
    /// </summary>
    public string GetCompositeKey()
    {
        return $"{BezirkName}_{KatasterbezirkCode}";
    }

    private BezirkeKatasterbezirke()
    {
        // Required for EF Core
    }
}