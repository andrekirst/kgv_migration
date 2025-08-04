using KGV.Domain.Common;
using KGV.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace KGV.Domain.Entities;

/// <summary>
/// File reference entity (Aktenzeichen) for tracking document references
/// </summary>
public class AktenzeichenEntity : BaseEntity
{
    /// <summary>
    /// District identifier
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string Bezirk { get; private set; } = string.Empty;

    /// <summary>
    /// Sequential number within the district and year
    /// </summary>
    public int Nummer { get; private set; }

    /// <summary>
    /// Year the file reference was created
    /// </summary>
    public int Jahr { get; private set; }

    /// <summary>
    /// Whether this file reference is currently active
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Optional description or notes
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; private set; }

    /// <summary>
    /// Creates a new AktenzeichenEntity
    /// </summary>
    /// <param name="bezirk">District identifier</param>
    /// <param name="nummer">Sequential number</param>
    /// <param name="jahr">Year</param>
    /// <param name="description">Optional description</param>
    public static AktenzeichenEntity Create(string bezirk, int nummer, int jahr, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(bezirk))
            throw new ArgumentException("Bezirk is required", nameof(bezirk));

        if (nummer <= 0)
            throw new ArgumentException("Nummer must be positive", nameof(nummer));

        if (jahr < 1900 || jahr > DateTime.Now.Year + 10)
            throw new ArgumentException("Jahr must be a valid year", nameof(jahr));

        if (bezirk.Length > 10)
            throw new ArgumentException("Bezirk cannot be longer than 10 characters", nameof(bezirk));

        var aktenzeichen = new AktenzeichenEntity
        {
            Bezirk = bezirk.Trim().ToUpperInvariant(),
            Nummer = nummer,
            Jahr = jahr,
            Description = description?.Trim(),
            IsActive = true
        };

        return aktenzeichen;
    }

    /// <summary>
    /// Gets the Aktenzeichen value object
    /// </summary>
    public Aktenzeichen GetAktenzeichen()
    {
        return new Aktenzeichen(Bezirk, Nummer, Jahr);
    }

    /// <summary>
    /// Gets the formatted file reference number
    /// </summary>
    public string GetFormattedValue()
    {
        return GetAktenzeichen().GetFormattedValue();
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
    /// Activates the file reference
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
    /// Deactivates the file reference
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
    /// Creates from Aktenzeichen value object
    /// </summary>
    public static AktenzeichenEntity FromValueObject(Aktenzeichen aktenzeichen, string? description = null)
    {
        return Create(aktenzeichen.Bezirk, aktenzeichen.Nummer, aktenzeichen.Jahr, description);
    }

    private AktenzeichenEntity()
    {
        // Required for EF Core
    }
}