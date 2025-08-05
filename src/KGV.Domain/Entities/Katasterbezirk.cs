using KGV.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace KGV.Domain.Entities;

/// <summary>
/// Cadastral district entity (Katasterbezirk) representing land registry districts
/// </summary>
public class Katasterbezirk : BaseEntity
{
    /// <summary>
    /// Reference to the parent district
    /// </summary>
    public Guid BezirkId { get; private set; }

    /// <summary>
    /// Cadastral district identifier
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string KatasterbezirkCode { get; private set; } = string.Empty;

    /// <summary>
    /// Full name of the cadastral district
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string KatasterbezirkName { get; private set; } = string.Empty;

    /// <summary>
    /// Additional description or notes
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; private set; }

    /// <summary>
    /// Whether this cadastral district is currently active
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Sort order for displaying cadastral districts within a district
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// Navigation property to the parent district
    /// </summary>
    public virtual Bezirk Bezirk { get; private set; } = null!;

    /// <summary>
    /// Navigation property to junction table mappings
    /// </summary>
    public virtual ICollection<BezirkeKatasterbezirke> BezirkeKatasterbezirke { get; private set; } = new List<BezirkeKatasterbezirke>();

    /// <summary>
    /// Creates a new Katasterbezirk
    /// </summary>
    /// <param name="bezirkId">Parent district ID</param>
    /// <param name="katasterbezirkCode">Cadastral district code</param>
    /// <param name="katasterbezirkName">Cadastral district name</param>
    /// <param name="description">Description</param>
    /// <param name="sortOrder">Sort order</param>
    public static Katasterbezirk Create(
        Guid bezirkId,
        string katasterbezirkCode,
        string katasterbezirkName,
        string? description = null,
        int sortOrder = 0)
    {
        if (bezirkId == Guid.Empty)
            throw new ArgumentException("BezirkId cannot be empty", nameof(bezirkId));

        if (string.IsNullOrWhiteSpace(katasterbezirkCode))
            throw new ArgumentException("KatasterbezirkCode is required", nameof(katasterbezirkCode));

        if (string.IsNullOrWhiteSpace(katasterbezirkName))
            throw new ArgumentException("KatasterbezirkName is required", nameof(katasterbezirkName));

        if (katasterbezirkCode.Length > 10)
            throw new ArgumentException("KatasterbezirkCode cannot be longer than 10 characters", nameof(katasterbezirkCode));

        if (katasterbezirkName.Length > 50)
            throw new ArgumentException("KatasterbezirkName cannot be longer than 50 characters", nameof(katasterbezirkName));

        var katasterbezirk = new Katasterbezirk
        {
            BezirkId = bezirkId,
            KatasterbezirkCode = katasterbezirkCode.Trim().ToUpperInvariant(),
            KatasterbezirkName = katasterbezirkName.Trim(),
            Description = description?.Trim(),
            SortOrder = sortOrder,
            IsActive = true
        };

        return katasterbezirk;
    }

    /// <summary>
    /// Updates the cadastral district information
    /// </summary>
    public void Update(string? katasterbezirkName = null, string? description = null, int? sortOrder = null)
    {
        if (!string.IsNullOrWhiteSpace(katasterbezirkName))
        {
            if (katasterbezirkName.Length > 50)
                throw new ArgumentException("KatasterbezirkName cannot be longer than 50 characters", nameof(katasterbezirkName));
            
            KatasterbezirkName = katasterbezirkName.Trim();
        }

        if (description != null)
            Description = description.Trim();

        if (sortOrder.HasValue)
            SortOrder = sortOrder.Value;

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the cadastral district
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
    /// Deactivates the cadastral district
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
    /// Gets the full display name including code and name
    /// </summary>
    public string GetFullDisplayName()
    {
        return $"{KatasterbezirkCode} - {KatasterbezirkName}";
    }

    /// <summary>
    /// Checks if this cadastral district is mapped to any districts via junction table
    /// </summary>
    public bool HasJunctionMappings()
    {
        return BezirkeKatasterbezirke.Any(m => m.IsActive);
    }

    /// <summary>
    /// Gets all active junction mappings for this cadastral district
    /// </summary>
    public IEnumerable<BezirkeKatasterbezirke> GetActiveJunctionMappings()
    {
        return BezirkeKatasterbezirke.Where(m => m.IsActive);
    }

    private Katasterbezirk()
    {
        // Required for EF Core
    }
}