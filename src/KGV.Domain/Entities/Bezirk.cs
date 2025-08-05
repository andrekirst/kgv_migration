using KGV.Domain.Common;
using KGV.Domain.Enums;
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
    /// Total area of the district in square meters
    /// </summary>
    [Range(0.00, 1000000.00)]
    public decimal? Flaeche { get; private set; }

    /// <summary>
    /// Number of plots (Parzellen) in this district
    /// </summary>
    [Range(0, int.MaxValue)]
    public int AnzahlParzellen { get; private set; } = 0;

    /// <summary>
    /// Current status of the district
    /// </summary>
    public BezirkStatus Status { get; private set; } = BezirkStatus.Active;

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
    /// Navigation property to related plots (Parzellen)
    /// </summary>
    public virtual ICollection<Parzelle> Parzellen { get; private set; } = new List<Parzelle>();

    /// <summary>
    /// Navigation property to junction table mappings
    /// </summary>
    public virtual ICollection<BezirkeKatasterbezirke> BezirkeKatasterbezirke { get; private set; } = new List<BezirkeKatasterbezirke>();

    /// <summary>
    /// Creates a new Bezirk
    /// </summary>
    /// <param name="name">District name/identifier</param>
    /// <param name="displayName">Full display name</param>
    /// <param name="description">Description</param>
    /// <param name="sortOrder">Sort order</param>
    /// <param name="flaeche">Total area in square meters</param>
    /// <param name="status">Initial status</param>
    public static Bezirk Create(string name, string? displayName = null, string? description = null, int sortOrder = 0, decimal? flaeche = null, BezirkStatus status = BezirkStatus.Active)
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
            Flaeche = flaeche,
            Status = status,
            IsActive = status == BezirkStatus.Active
        };

        return bezirk;
    }

    /// <summary>
    /// Updates the district information
    /// </summary>
    public void Update(string? displayName = null, string? description = null, int? sortOrder = null, decimal? flaeche = null)
    {
        if (displayName != null)
            DisplayName = displayName.Trim();

        if (description != null)
            Description = description.Trim();

        if (sortOrder.HasValue)
            SortOrder = sortOrder.Value;

        if (flaeche.HasValue)
        {
            if (flaeche.Value < 0)
                throw new ArgumentException("Flaeche cannot be negative", nameof(flaeche));
            Flaeche = flaeche.Value;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Changes the status of the district
    /// </summary>
    public void ChangeStatus(BezirkStatus newStatus)
    {
        if (Status == newStatus)
            return;

        Status = newStatus;
        IsActive = newStatus == BezirkStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the district
    /// </summary>
    public void Activate()
    {
        ChangeStatus(BezirkStatus.Active);
    }

    /// <summary>
    /// Deactivates the district
    /// </summary>
    public void Deactivate()
    {
        ChangeStatus(BezirkStatus.Inactive);
    }

    /// <summary>
    /// Suspends the district temporarily
    /// </summary>
    public void Suspend()
    {
        ChangeStatus(BezirkStatus.Suspended);
    }

    /// <summary>
    /// Marks the district as under restructuring
    /// </summary>
    public void MarkUnderRestructuring()
    {
        ChangeStatus(BezirkStatus.UnderRestructuring);
    }

    /// <summary>
    /// Archives the district
    /// </summary>
    public void Archive()
    {
        ChangeStatus(BezirkStatus.Archived);
    }

    /// <summary>
    /// Updates the plot count for this district
    /// </summary>
    public void UpdatePlotCount(int count)
    {
        if (count < 0)
            throw new ArgumentException("Plot count cannot be negative", nameof(count));

        AnzahlParzellen = count;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Increments the plot count
    /// </summary>
    public void IncrementPlotCount()
    {
        AnzahlParzellen++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Decrements the plot count
    /// </summary>
    public void DecrementPlotCount()
    {
        if (AnzahlParzellen > 0)
        {
            AnzahlParzellen--;
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

    /// <summary>
    /// Gets the status description in German
    /// </summary>
    public string GetStatusDescription()
    {
        return Status switch
        {
            BezirkStatus.Inactive => "Inaktiv",
            BezirkStatus.Active => "Aktiv",
            BezirkStatus.Suspended => "Gesperrt",
            BezirkStatus.UnderRestructuring => "Umstrukturierung",
            BezirkStatus.Archived => "Archiviert",
            _ => "Unbekannt"
        };
    }

    /// <summary>
    /// Checks if the district can accept new plots
    /// </summary>
    public bool CanAcceptNewPlots()
    {
        return Status == BezirkStatus.Active || Status == BezirkStatus.UnderRestructuring;
    }

    /// <summary>
    /// Gets the average plot size if area and plot count are available
    /// </summary>
    public decimal? GetAveragePlotSize()
    {
        if (Flaeche.HasValue && AnzahlParzellen > 0)
            return Flaeche.Value / AnzahlParzellen;
        return null;
    }

    private Bezirk()
    {
        // Required for EF Core
    }
}