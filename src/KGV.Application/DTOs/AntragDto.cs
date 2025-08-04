using KGV.Domain.Enums;

namespace KGV.Application.DTOs;

/// <summary>
/// Data Transfer Object for Antrag entity
/// </summary>
public class AntragDto
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// File reference number
    /// </summary>
    public string? Aktenzeichen { get; set; }

    /// <summary>
    /// Waiting list number for district 32
    /// </summary>
    public string? WartelistenNr32 { get; set; }

    /// <summary>
    /// Waiting list number for district 33
    /// </summary>
    public string? WartelistenNr33 { get; set; }

    /// <summary>
    /// Primary applicant salutation
    /// </summary>
    public string? Anrede { get; set; }

    /// <summary>
    /// Primary applicant title
    /// </summary>
    public string? Titel { get; set; }

    /// <summary>
    /// Primary applicant first name
    /// </summary>
    public string? Vorname { get; set; }

    /// <summary>
    /// Primary applicant last name
    /// </summary>
    public string? Nachname { get; set; }

    /// <summary>
    /// Secondary applicant salutation
    /// </summary>
    public string? Anrede2 { get; set; }

    /// <summary>
    /// Secondary applicant title
    /// </summary>
    public string? Titel2 { get; set; }

    /// <summary>
    /// Secondary applicant first name
    /// </summary>
    public string? Vorname2 { get; set; }

    /// <summary>
    /// Secondary applicant last name
    /// </summary>
    public string? Nachname2 { get; set; }

    /// <summary>
    /// Letter salutation
    /// </summary>
    public string? Briefanrede { get; set; }

    /// <summary>
    /// Street address
    /// </summary>
    public string? Strasse { get; set; }

    /// <summary>
    /// Postal code
    /// </summary>
    public string? PLZ { get; set; }

    /// <summary>
    /// City
    /// </summary>
    public string? Ort { get; set; }

    /// <summary>
    /// Primary phone number
    /// </summary>
    public string? Telefon { get; set; }

    /// <summary>
    /// Mobile phone number
    /// </summary>
    public string? MobilTelefon { get; set; }

    /// <summary>
    /// Business phone number
    /// </summary>
    public string? GeschTelefon { get; set; }

    /// <summary>
    /// Secondary mobile phone number
    /// </summary>
    public string? MobilTelefon2 { get; set; }

    /// <summary>
    /// Email address
    /// </summary>
    public string? EMail { get; set; }

    /// <summary>
    /// Application date
    /// </summary>
    public DateTime? Bewerbungsdatum { get; set; }

    /// <summary>
    /// Confirmation date
    /// </summary>
    public DateTime? Bestaetigungsdatum { get; set; }

    /// <summary>
    /// Current offer date
    /// </summary>
    public DateTime? AktuellesAngebot { get; set; }

    /// <summary>
    /// Deletion date
    /// </summary>
    public DateTime? Loeschdatum { get; set; }

    /// <summary>
    /// Applicant's wishes/preferences
    /// </summary>
    public string? Wunsch { get; set; }

    /// <summary>
    /// Internal notes
    /// </summary>
    public string? Vermerk { get; set; }

    /// <summary>
    /// Whether the application is active
    /// </summary>
    public bool Aktiv { get; set; } = true;

    /// <summary>
    /// When the application was deactivated
    /// </summary>
    public DateTime? DeaktiviertAm { get; set; }

    /// <summary>
    /// Primary applicant birthday
    /// </summary>
    public string? Geburtstag { get; set; }

    /// <summary>
    /// Secondary applicant birthday
    /// </summary>
    public string? Geburtstag2 { get; set; }

    /// <summary>
    /// Current status of the application
    /// </summary>
    public AntragStatus Status { get; set; }

    /// <summary>
    /// Status description in German
    /// </summary>
    public string StatusBeschreibung { get; set; } = string.Empty;

    /// <summary>
    /// Full name of primary applicant
    /// </summary>
    public string VollName { get; set; } = string.Empty;

    /// <summary>
    /// Full name of secondary applicant
    /// </summary>
    public string? VollName2 { get; set; }

    /// <summary>
    /// Full address
    /// </summary>
    public string? VollAdresse { get; set; }

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

    /// <summary>
    /// Related history entries
    /// </summary>
    public List<VerlaufDto> Verlauf { get; set; } = [];
}

/// <summary>
/// Simplified DTO for Antrag lists
/// </summary>
public class AntragListDto
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// File reference number
    /// </summary>
    public string? Aktenzeichen { get; set; }

    /// <summary>
    /// Full name of primary applicant
    /// </summary>
    public string VollName { get; set; } = string.Empty;

    /// <summary>
    /// Application date
    /// </summary>
    public DateTime? Bewerbungsdatum { get; set; }

    /// <summary>
    /// Current status
    /// </summary>
    public AntragStatus Status { get; set; }

    /// <summary>
    /// Status description in German
    /// </summary>
    public string StatusBeschreibung { get; set; } = string.Empty;

    /// <summary>
    /// City
    /// </summary>
    public string? Ort { get; set; }

    /// <summary>
    /// Whether the application is active
    /// </summary>
    public bool Aktiv { get; set; }

    /// <summary>
    /// Last update date
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}