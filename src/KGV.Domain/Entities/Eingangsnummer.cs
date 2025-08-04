using KGV.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace KGV.Domain.Entities;

/// <summary>
/// Entry number entity (Eingangsnummer) for tracking incoming documents
/// </summary>
public class Eingangsnummer : BaseEntity
{
    /// <summary>
    /// District identifier
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string Bezirk { get; private set; } = string.Empty;

    /// <summary>
    /// Sequential entry number within the district and year
    /// </summary>
    public int Nummer { get; private set; }

    /// <summary>
    /// Year the entry number was created
    /// </summary>
    public int Jahr { get; private set; }

    /// <summary>
    /// Date when the document was received
    /// </summary>
    public DateTime Eingangsdatum { get; private set; }

    /// <summary>
    /// Optional description of the document/entry
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; private set; }

    /// <summary>
    /// Whether this entry number is currently active
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Creates a new Eingangsnummer
    /// </summary>
    /// <param name="bezirk">District identifier</param>
    /// <param name="nummer">Sequential number</param>
    /// <param name="jahr">Year</param>
    /// <param name="eingangsdatum">Entry date</param>
    /// <param name="description">Optional description</param>
    public static Eingangsnummer Create(
        string bezirk,
        int nummer,
        int jahr,
        DateTime? eingangsdatum = null,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(bezirk))
            throw new ArgumentException("Bezirk is required", nameof(bezirk));

        if (nummer <= 0)
            throw new ArgumentException("Nummer must be positive", nameof(nummer));

        if (jahr < 1900 || jahr > DateTime.Now.Year + 10)
            throw new ArgumentException("Jahr must be a valid year", nameof(jahr));

        if (bezirk.Length > 10)
            throw new ArgumentException("Bezirk cannot be longer than 10 characters", nameof(bezirk));

        var eingangsnummer = new Eingangsnummer
        {
            Bezirk = bezirk.Trim().ToUpperInvariant(),
            Nummer = nummer,
            Jahr = jahr,
            Eingangsdatum = eingangsdatum ?? DateTime.UtcNow,
            Description = description?.Trim(),
            IsActive = true
        };

        return eingangsnummer;
    }

    /// <summary>
    /// Gets the formatted entry number
    /// </summary>
    public string GetFormattedValue()
    {
        return $"E-{Bezirk}-{Nummer:D4}/{Jahr}";
    }

    /// <summary>
    /// Updates the description
    /// </summary>
    public void UpdateDescription(string? description)
    {
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the entry date
    /// </summary>
    public void UpdateEingangsdatum(DateTime eingangsdatum)
    {
        Eingangsdatum = eingangsdatum;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the entry number
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
    /// Deactivates the entry number
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
    /// Gets the next available number for a given district and year
    /// </summary>
    public static int GetNextNumber(IEnumerable<Eingangsnummer> existingNumbers, string bezirk, int jahr)
    {
        var maxNumber = existingNumbers
            .Where(e => e.Bezirk == bezirk && e.Jahr == jahr)
            .Select(e => e.Nummer)
            .DefaultIfEmpty(0)
            .Max();

        return maxNumber + 1;
    }

    private Eingangsnummer()
    {
        // Required for EF Core
    }
}