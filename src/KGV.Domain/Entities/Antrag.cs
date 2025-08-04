using KGV.Domain.Common;
using KGV.Domain.Enums;
using KGV.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace KGV.Domain.Entities;

/// <summary>
/// Application entity (Antrag) representing garden plot applications
/// </summary>
public class Antrag : BaseEntity
{
    /// <summary>
    /// File reference number
    /// </summary>
    public string? AktenzeichenValue { get; private set; }

    /// <summary>
    /// Waiting list number for district 32
    /// </summary>
    [MaxLength(20)]
    public string? WartelistenNr32 { get; private set; }

    /// <summary>
    /// Waiting list number for district 33
    /// </summary>
    [MaxLength(20)]
    public string? WartelistenNr33 { get; private set; }

    /// <summary>
    /// Primary applicant salutation
    /// </summary>
    public Anrede? Anrede { get; private set; }

    /// <summary>
    /// Primary applicant title
    /// </summary>
    [MaxLength(50)]
    public string? Titel { get; private set; }

    /// <summary>
    /// Primary applicant first name
    /// </summary>
    [MaxLength(50)]
    public string? Vorname { get; private set; }

    /// <summary>
    /// Primary applicant last name
    /// </summary>
    [MaxLength(50)]
    public string? Nachname { get; private set; }

    /// <summary>
    /// Secondary applicant salutation
    /// </summary>
    public Anrede? Anrede2 { get; private set; }

    /// <summary>
    /// Secondary applicant title
    /// </summary>
    [MaxLength(50)]
    public string? Titel2 { get; private set; }

    /// <summary>
    /// Secondary applicant first name
    /// </summary>
    [MaxLength(50)]
    public string? Vorname2 { get; private set; }

    /// <summary>
    /// Secondary applicant last name
    /// </summary>
    [MaxLength(50)]
    public string? Nachname2 { get; private set; }

    /// <summary>
    /// Letter salutation
    /// </summary>
    [MaxLength(150)]
    public string? Briefanrede { get; private set; }

    /// <summary>
    /// Address of the applicant
    /// </summary>
    public Address? Adresse { get; private set; }

    /// <summary>
    /// Primary phone number
    /// </summary>
    public PhoneNumber? Telefon { get; private set; }

    /// <summary>
    /// Mobile phone number
    /// </summary>
    public PhoneNumber? MobilTelefon { get; private set; }

    /// <summary>
    /// Business phone number
    /// </summary>
    public PhoneNumber? GeschTelefon { get; private set; }

    /// <summary>
    /// Secondary mobile phone number
    /// </summary>
    public PhoneNumber? MobilTelefon2 { get; private set; }

    /// <summary>
    /// Email address
    /// </summary>
    public Email? EMail { get; private set; }

    /// <summary>
    /// Application date
    /// </summary>
    public DateTime? Bewerbungsdatum { get; private set; }

    /// <summary>
    /// Confirmation date
    /// </summary>
    public DateTime? Bestaetigungsdatum { get; private set; }

    /// <summary>
    /// Current offer date
    /// </summary>
    public DateTime? AktuellesAngebot { get; private set; }

    /// <summary>
    /// Deletion date
    /// </summary>
    public DateTime? Loeschdatum { get; private set; }

    /// <summary>
    /// Applicant's wishes/preferences
    /// </summary>
    [MaxLength(600)]
    public string? Wunsch { get; private set; }

    /// <summary>
    /// Internal notes
    /// </summary>
    [MaxLength(2000)]
    public string? Vermerk { get; private set; }

    /// <summary>
    /// Whether the application is active
    /// </summary>
    public bool Aktiv { get; private set; } = true;

    /// <summary>
    /// When the application was deactivated
    /// </summary>
    public DateTime? DeaktiviertAm { get; private set; }

    /// <summary>
    /// Primary applicant birthday
    /// </summary>
    [MaxLength(100)]
    public string? Geburtstag { get; private set; }

    /// <summary>
    /// Secondary applicant birthday
    /// </summary>
    [MaxLength(100)]
    public string? Geburtstag2 { get; private set; }

    /// <summary>
    /// Current status of the application
    /// </summary>
    public AntragStatus Status { get; private set; } = AntragStatus.NeuEingegangen;

    /// <summary>
    /// Navigation property to related history entries
    /// </summary>
    public virtual ICollection<Verlauf> Verlauf { get; private set; } = new List<Verlauf>();

    /// <summary>
    /// Creates a new Antrag
    /// </summary>
    /// <param name="vorname">Primary applicant first name</param>
    /// <param name="nachname">Primary applicant last name</param>
    /// <param name="adresse">Applicant address</param>
    /// <param name="email">Email address</param>
    /// <param name="telefon">Phone number</param>
    /// <param name="bewerbungsdatum">Application date</param>
    public static Antrag Create(
        string vorname,
        string nachname,
        Address adresse,
        Email? email = null,
        PhoneNumber? telefon = null,
        DateTime? bewerbungsdatum = null)
    {
        if (string.IsNullOrWhiteSpace(vorname))
            throw new ArgumentException("Vorname is required", nameof(vorname));

        if (string.IsNullOrWhiteSpace(nachname))
            throw new ArgumentException("Nachname is required", nameof(nachname));

        var antrag = new Antrag
        {
            Vorname = vorname.Trim(),
            Nachname = nachname.Trim(),
            Adresse = adresse ?? throw new ArgumentNullException(nameof(adresse)),
            EMail = email,
            Telefon = telefon,
            Bewerbungsdatum = bewerbungsdatum ?? DateTime.UtcNow,
            Status = AntragStatus.NeuEingegangen,
            Aktiv = true
        };

        return antrag;
    }

    /// <summary>
    /// Updates the applicant's contact information
    /// </summary>
    public void UpdateContactInfo(Address? adresse = null, Email? email = null, 
        PhoneNumber? telefon = null, PhoneNumber? mobilTelefon = null)
    {
        if (adresse != null)
            Adresse = adresse;
        
        if (email != null)
            EMail = email;
        
        if (telefon != null)
            Telefon = telefon;
        
        if (mobilTelefon != null)
            MobilTelefon = mobilTelefon;

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the application status
    /// </summary>
    public void UpdateStatus(AntragStatus newStatus, string? vermerk = null)
    {
        var oldStatus = Status;
        Status = newStatus;
        
        if (!string.IsNullOrWhiteSpace(vermerk))
        {
            Vermerk = string.IsNullOrWhiteSpace(Vermerk) 
                ? vermerk 
                : $"{Vermerk}\n{DateTime.UtcNow:yyyy-MM-dd}: {vermerk}";
        }

        if (newStatus == AntragStatus.Deaktiviert && oldStatus != AntragStatus.Deaktiviert)
        {
            Aktiv = false;
            DeaktiviertAm = DateTime.UtcNow;
        }
        else if (newStatus != AntragStatus.Deaktiviert && oldStatus == AntragStatus.Deaktiviert)
        {
            Aktiv = true;
            DeaktiviertAm = null;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the confirmation date
    /// </summary>
    public void SetBestaetigungsdatum(DateTime bestaetigungsdatum)
    {
        Bestaetigungsdatum = bestaetigungsdatum;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the current offer date
    /// </summary>
    public void SetAktuellesAngebot(DateTime angebotsdatum)
    {
        AktuellesAngebot = angebotsdatum;
        if (Status == AntragStatus.NeuEingegangen || Status == AntragStatus.InBearbeitung)
        {
            Status = AntragStatus.AngebotGemacht;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the full name of the primary applicant
    /// </summary>
    public string GetFullName()
    {
        var parts = new List<string>();
        
        if (!string.IsNullOrWhiteSpace(Titel))
            parts.Add(Titel);
        
        if (!string.IsNullOrWhiteSpace(Vorname))
            parts.Add(Vorname);
        
        if (!string.IsNullOrWhiteSpace(Nachname))
            parts.Add(Nachname);

        return string.Join(" ", parts);
    }

    /// <summary>
    /// Gets the full name of the secondary applicant
    /// </summary>
    public string? GetSecondaryFullName()
    {
        if (string.IsNullOrWhiteSpace(Vorname2) && string.IsNullOrWhiteSpace(Nachname2))
            return null;

        var parts = new List<string>();
        
        if (!string.IsNullOrWhiteSpace(Titel2))
            parts.Add(Titel2);
        
        if (!string.IsNullOrWhiteSpace(Vorname2))
            parts.Add(Vorname2);
        
        if (!string.IsNullOrWhiteSpace(Nachname2))
            parts.Add(Nachname2);

        return string.Join(" ", parts);
    }

    private Antrag()
    {
        // Required for EF Core
    }
}