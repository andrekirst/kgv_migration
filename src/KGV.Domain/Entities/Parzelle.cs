using KGV.Domain.Common;
using KGV.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace KGV.Domain.Entities;

/// <summary>
/// Parzelle (Plot) entity representing individual garden plots within a district
/// </summary>
public class Parzelle : BaseEntity
{
    /// <summary>
    /// Plot number/identifier within the district
    /// </summary>
    [Required]
    [MaxLength(20)]
    public required string Nummer { get; init; }

    /// <summary>
    /// Reference to the parent district
    /// </summary>
    public required Guid BezirkId { get; init; }

    /// <summary>
    /// Area of the plot in square meters
    /// </summary>
    [Range(0.01, 10000.00)]
    public decimal Flaeche { get; private set; }

    /// <summary>
    /// Current status of the plot
    /// </summary>
    public ParzellenStatus Status { get; private set; }

    /// <summary>
    /// Price or rental cost for the plot (optional)
    /// </summary>
    [Range(0.00, 100000.00)]
    public decimal? Preis { get; private set; }

    /// <summary>
    /// Date when the plot was assigned (if applicable)
    /// </summary>
    public DateTime? VergebenAm { get; private set; }

    /// <summary>
    /// Additional notes or description for the plot
    /// </summary>
    [MaxLength(1000)]
    public string? Beschreibung { get; private set; }

    /// <summary>
    /// Special features or characteristics of the plot
    /// </summary>
    [MaxLength(500)]
    public string? Besonderheiten { get; private set; }

    /// <summary>
    /// Whether the plot has water access
    /// </summary>
    public bool HasWasser { get; private set; }

    /// <summary>
    /// Whether the plot has electricity access
    /// </summary>
    public bool HasStrom { get; private set; }

    /// <summary>
    /// Priority level for assignment (higher numbers = higher priority)
    /// </summary>
    public int Prioritaet { get; private set; } = 0;

    /// <summary>
    /// Navigation property to the parent district
    /// </summary>
    public virtual Bezirk Bezirk { get; private set; } = null!;

    /// <summary>
    /// Navigation property to related applications
    /// </summary>
    public virtual ICollection<Antrag> Antraege { get; private set; } = new List<Antrag>();

    /// <summary>
    /// Creates a new Parzelle
    /// </summary>
    /// <param name="nummer">Plot number</param>
    /// <param name="bezirkId">Parent district ID</param>
    /// <param name="flaeche">Plot area in square meters</param>
    /// <param name="status">Initial status</param>
    /// <param name="preis">Optional price</param>
    /// <param name="beschreibung">Optional description</param>
    /// <param name="hasWasser">Water access availability</param>
    /// <param name="hasStrom">Electricity access availability</param>
    /// <param name="prioritaet">Assignment priority</param>
    public static Parzelle Create(
        string nummer,
        Guid bezirkId,
        decimal flaeche,
        ParzellenStatus status = ParzellenStatus.Available,
        decimal? preis = null,
        string? beschreibung = null,
        bool hasWasser = false,
        bool hasStrom = false,
        int prioritaet = 0)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(nummer))
            throw new ArgumentException("Nummer is required", nameof(nummer));

        if (bezirkId == Guid.Empty)
            throw new ArgumentException("BezirkId cannot be empty", nameof(bezirkId));

        if (flaeche <= 0)
            throw new ArgumentException("Flaeche must be greater than 0", nameof(flaeche));

        if (nummer.Length > 20)
            throw new ArgumentException("Nummer cannot be longer than 20 characters", nameof(nummer));

        if (preis.HasValue && preis.Value < 0)
            throw new ArgumentException("Preis cannot be negative", nameof(preis));

        var parzelle = new Parzelle
        {
            Nummer = nummer.Trim().ToUpperInvariant(),
            BezirkId = bezirkId,
            Flaeche = flaeche,
            Status = status,
            Preis = preis,
            Beschreibung = beschreibung?.Trim(),
            HasWasser = hasWasser,
            HasStrom = hasStrom,
            Prioritaet = prioritaet
        };

        return parzelle;
    }

    /// <summary>
    /// Updates the plot information
    /// </summary>
    public void Update(
        decimal? flaeche = null,
        decimal? preis = null,
        string? beschreibung = null,
        string? besonderheiten = null,
        bool? hasWasser = null,
        bool? hasStrom = null,
        int? prioritaet = null)
    {
        if (flaeche.HasValue)
        {
            if (flaeche.Value <= 0)
                throw new ArgumentException("Flaeche must be greater than 0", nameof(flaeche));
            Flaeche = flaeche.Value;
        }

        if (preis.HasValue)
        {
            if (preis.Value < 0)
                throw new ArgumentException("Preis cannot be negative", nameof(preis));
            Preis = preis.Value;
        }

        if (beschreibung != null)
            Beschreibung = beschreibung.Trim();

        if (besonderheiten != null)
            Besonderheiten = besonderheiten.Trim();

        if (hasWasser.HasValue)
            HasWasser = hasWasser.Value;

        if (hasStrom.HasValue)
            HasStrom = hasStrom.Value;

        if (prioritaet.HasValue)
            Prioritaet = prioritaet.Value;

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Changes the status of the plot
    /// </summary>
    public void ChangeStatus(ParzellenStatus newStatus, DateTime? vergebenAm = null)
    {
        if (Status == newStatus)
            return;

        Status = newStatus;

        // Set assignment date for assigned plots
        if (newStatus == ParzellenStatus.Assigned && vergebenAm.HasValue)
            VergebenAm = vergebenAm.Value;
        else if (newStatus != ParzellenStatus.Assigned)
            VergebenAm = null;

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Assigns the plot to someone
    /// </summary>
    public void Assign(DateTime? assignmentDate = null)
    {
        ChangeStatus(ParzellenStatus.Assigned, assignmentDate ?? DateTime.UtcNow);
    }

    /// <summary>
    /// Reserves the plot for future assignment
    /// </summary>
    public void Reserve()
    {
        if (Status == ParzellenStatus.Available)
        {
            ChangeStatus(ParzellenStatus.Reserved);
        }
    }

    /// <summary>
    /// Makes the plot available again
    /// </summary>
    public void MakeAvailable()
    {
        ChangeStatus(ParzellenStatus.Available);
        VergebenAm = null;
    }

    /// <summary>
    /// Marks the plot as unavailable (maintenance, etc.)
    /// </summary>
    public void MarkUnavailable()
    {
        ChangeStatus(ParzellenStatus.Unavailable);
        VergebenAm = null;
    }

    /// <summary>
    /// Gets the full display name including district and plot number
    /// </summary>
    public string GetFullDisplayName()
    {
        return $"{Bezirk?.Name ?? "Unknown"}-{Nummer}";
    }

    /// <summary>
    /// Checks if the plot is available for assignment
    /// </summary>
    public bool IsAvailableForAssignment()
    {
        return Status == ParzellenStatus.Available || Status == ParzellenStatus.Reserved;
    }

    /// <summary>
    /// Gets the status description in German
    /// </summary>
    public string GetStatusDescription()
    {
        return Status switch
        {
            ParzellenStatus.Available => "Verfügbar",
            ParzellenStatus.Reserved => "Reserviert",
            ParzellenStatus.Assigned => "Vergeben",
            ParzellenStatus.Unavailable => "Nicht verfügbar",
            ParzellenStatus.UnderDevelopment => "In Entwicklung",
            ParzellenStatus.Decommissioned => "Stillgelegt",
            ParzellenStatus.PendingApproval => "Genehmigung ausstehend",
            _ => "Unbekannt"
        };
    }

    /// <summary>
    /// Calculates the annual cost if price is set
    /// </summary>
    public decimal? GetAnnualCost()
    {
        return Preis.HasValue ? Preis.Value * 12 : null;
    }

    private Parzelle()
    {
        // Required for EF Core
    }
}