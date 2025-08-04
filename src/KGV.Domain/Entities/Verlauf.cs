using KGV.Domain.Common;
using KGV.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace KGV.Domain.Entities;

/// <summary>
/// History entry entity (Verlauf) for tracking application progress
/// </summary>
public class Verlauf : BaseEntity
{
    /// <summary>
    /// Reference to the related application
    /// </summary>
    public Guid AntragId { get; private set; }

    /// <summary>
    /// Type of history entry
    /// </summary>
    public VerlaufArt Art { get; private set; }

    /// <summary>
    /// Date of the history entry
    /// </summary>
    public DateTime Datum { get; private set; }

    /// <summary>
    /// Gemarkung (land registry district)
    /// </summary>
    [MaxLength(50)]
    public string? Gemarkung { get; private set; }

    /// <summary>
    /// Flur (plot area)
    /// </summary>  
    [MaxLength(20)]
    public string? Flur { get; private set; }

    /// <summary>
    /// Parzelle (parcel number) 
    /// </summary>
    [MaxLength(20)]
    public string? Parzelle { get; private set; }

    /// <summary>
    /// Groesse (size of the plot)
    /// </summary>
    [MaxLength(20)]
    public string? Groesse { get; private set; }

    /// <summary>
    /// Sachbearbeiter (case worker)
    /// </summary>
    [MaxLength(100)]
    public string? Sachbearbeiter { get; private set; }

    /// <summary>
    /// Hinweis (note/hint)
    /// </summary>
    [MaxLength(100)]
    public string? Hinweis { get; private set; }

    /// <summary>
    /// Kommentar (detailed comment)
    /// </summary>
    [MaxLength(255)]
    public string? Kommentar { get; private set; }

    /// <summary>
    /// Navigation property to the related application
    /// </summary>
    public virtual Antrag Antrag { get; private set; } = null!;

    /// <summary>
    /// Creates a new Verlauf entry
    /// </summary>
    /// <param name="antragId">Application ID</param>
    /// <param name="art">Type of history entry</param>
    /// <param name="datum">Date of entry</param>
    /// <param name="sachbearbeiter">Case worker</param>
    /// <param name="kommentar">Comment</param>
    public static Verlauf Create(
        Guid antragId,
        VerlaufArt art,
        DateTime? datum = null,
        string? sachbearbeiter = null,
        string? kommentar = null)
    {
        if (antragId == Guid.Empty)
            throw new ArgumentException("AntragId cannot be empty", nameof(antragId));

        var verlauf = new Verlauf
        {
            AntragId = antragId,
            Art = art,
            Datum = datum ?? DateTime.UtcNow,
            Sachbearbeiter = sachbearbeiter?.Trim(),
            Kommentar = kommentar?.Trim()
        };

        return verlauf;
    }

    /// <summary>
    /// Creates a plot-related history entry
    /// </summary>
    /// <param name="antragId">Application ID</param>
    /// <param name="art">Type of history entry</param>
    /// <param name="gemarkung">Land registry district</param>
    /// <param name="flur">Plot area</param>
    /// <param name="parzelle">Parcel number</param>
    /// <param name="groesse">Plot size</param>
    /// <param name="sachbearbeiter">Case worker</param>
    /// <param name="kommentar">Comment</param>
    /// <param name="datum">Date of entry</param>
    public static Verlauf CreatePlotEntry(
        Guid antragId,
        VerlaufArt art,
        string? gemarkung = null,
        string? flur = null,
        string? parzelle = null,
        string? groesse = null,
        string? sachbearbeiter = null,
        string? kommentar = null,
        DateTime? datum = null)
    {
        if (antragId == Guid.Empty)
            throw new ArgumentException("AntragId cannot be empty", nameof(antragId));

        var verlauf = new Verlauf
        {
            AntragId = antragId,
            Art = art,
            Datum = datum ?? DateTime.UtcNow,
            Gemarkung = gemarkung?.Trim(),
            Flur = flur?.Trim(),
            Parzelle = parzelle?.Trim(),
            Groesse = groesse?.Trim(),
            Sachbearbeiter = sachbearbeiter?.Trim(),
            Kommentar = kommentar?.Trim()
        };

        return verlauf;
    }

    /// <summary>
    /// Updates the history entry
    /// </summary>
    public void Update(
        string? sachbearbeiter = null,
        string? kommentar = null,
        string? hinweis = null,
        DateTime? datum = null)
    {
        if (sachbearbeiter != null)
            Sachbearbeiter = sachbearbeiter.Trim();

        if (kommentar != null)
            Kommentar = kommentar.Trim();

        if (hinweis != null)
            Hinweis = hinweis.Trim();

        if (datum.HasValue)
            Datum = datum.Value;

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates plot information
    /// </summary>
    public void UpdatePlotInfo(
        string? gemarkung = null,
        string? flur = null,
        string? parzelle = null,
        string? groesse = null)
    {
        if (gemarkung != null)
            Gemarkung = gemarkung.Trim();

        if (flur != null)
            Flur = flur.Trim();

        if (parzelle != null)
            Parzelle = parzelle.Trim();

        if (groesse != null)
            Groesse = groesse.Trim();

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets a formatted summary of the history entry
    /// </summary>
    public string GetSummary()
    {
        var parts = new List<string>
        {
            $"{Datum:dd.MM.yyyy}: {GetArtDescription()}"
        };

        if (!string.IsNullOrWhiteSpace(Sachbearbeiter))
            parts.Add($"({Sachbearbeiter})");

        if (HasPlotInfo())
        {
            var plotInfo = GetPlotInfoString();
            if (!string.IsNullOrWhiteSpace(plotInfo))
                parts.Add($"- {plotInfo}");
        }

        if (!string.IsNullOrWhiteSpace(Kommentar))
            parts.Add($"- {Kommentar}");

        return string.Join(" ", parts);
    }

    /// <summary>
    /// Gets the description for the VerlaufArt
    /// </summary>
    public string GetArtDescription()
    {
        return Art switch
        {
            VerlaufArt.AntragEingegangen => "Antrag eingegangen",
            VerlaufArt.BestaetigungVersendet => "Bestätigung versendet",
            VerlaufArt.AngebotGemacht => "Angebot gemacht",
            VerlaufArt.AngebotAngenommen => "Angebot angenommen",
            VerlaufArt.AngebotAbgelehnt => "Angebot abgelehnt",
            VerlaufArt.Besichtigung => "Besichtigung",
            VerlaufArt.VertragErstellt => "Vertrag erstellt",
            VerlaufArt.Abgeschlossen => "Abgeschlossen",
            VerlaufArt.Notiz => "Notiz",
            _ => Art.ToString()
        };
    }

    /// <summary>
    /// Checks if the entry has plot information
    /// </summary>
    public bool HasPlotInfo()
    {
        return !string.IsNullOrWhiteSpace(Gemarkung) ||
               !string.IsNullOrWhiteSpace(Flur) ||
               !string.IsNullOrWhiteSpace(Parzelle) ||
               !string.IsNullOrWhiteSpace(Groesse);
    }

    /// <summary>
    /// Gets formatted plot information string
    /// </summary>
    public string GetPlotInfoString()
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(Gemarkung))
            parts.Add($"Gemarkung: {Gemarkung}");

        if (!string.IsNullOrWhiteSpace(Flur))
            parts.Add($"Flur: {Flur}");

        if (!string.IsNullOrWhiteSpace(Parzelle))
            parts.Add($"Parzelle: {Parzelle}");

        if (!string.IsNullOrWhiteSpace(Groesse))
            parts.Add($"Größe: {Groesse}");

        return string.Join(", ", parts);
    }

    private Verlauf()
    {
        // Required for EF Core
    }
}