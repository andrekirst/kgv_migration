using KGV.Domain.Enums;

namespace KGV.Application.DTOs;

/// <summary>
/// Data Transfer Object for Verlauf entity
/// </summary>
public class VerlaufDto
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Reference to the related application
    /// </summary>
    public Guid AntragId { get; set; }

    /// <summary>
    /// Type of history entry
    /// </summary>
    public VerlaufArt Art { get; set; }

    /// <summary>
    /// Description of the history entry type in German
    /// </summary>
    public string ArtBeschreibung { get; set; } = string.Empty;

    /// <summary>
    /// Date of the history entry
    /// </summary>
    public DateTime Datum { get; set; }

    /// <summary>
    /// Gemarkung (land registry district)
    /// </summary>
    public string? Gemarkung { get; set; }

    /// <summary>
    /// Flur (plot area)
    /// </summary>
    public string? Flur { get; set; }

    /// <summary>
    /// Parzelle (parcel number)
    /// </summary>
    public string? Parzelle { get; set; }

    /// <summary>
    /// Groesse (size of the plot)
    /// </summary>
    public string? Groesse { get; set; }

    /// <summary>
    /// Sachbearbeiter (case worker)
    /// </summary>
    public string? Sachbearbeiter { get; set; }

    /// <summary>
    /// Hinweis (note/hint)
    /// </summary>
    public string? Hinweis { get; set; }

    /// <summary>
    /// Kommentar (detailed comment)
    /// </summary>
    public string? Kommentar { get; set; }

    /// <summary>
    /// Formatted plot information
    /// </summary>
    public string? ParzellInfo { get; set; }

    /// <summary>
    /// Summary text for display
    /// </summary>
    public string Zusammenfassung { get; set; } = string.Empty;

    /// <summary>
    /// When the entity was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

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
}