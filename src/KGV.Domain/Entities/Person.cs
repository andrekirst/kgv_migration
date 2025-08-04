using KGV.Domain.Common;
using KGV.Domain.Enums;
using KGV.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;

namespace KGV.Domain.Entities;

/// <summary>
/// Person entity representing system users and staff
/// </summary>
public class Person : BaseEntity
{
    /// <summary>
    /// Salutation
    /// </summary>
    public Anrede? Anrede { get; private set; }

    /// <summary>
    /// First name
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Vorname { get; private set; } = string.Empty;

    /// <summary>
    /// Last name
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Nachname { get; private set; } = string.Empty;

    /// <summary>
    /// Employee number
    /// </summary>
    [MaxLength(7)]
    public string? Nummer { get; private set; }

    /// <summary>
    /// Organizational unit
    /// </summary>
    [MaxLength(10)]
    public string? Organisationseinheit { get; private set; }

    /// <summary>
    /// Room number
    /// </summary>
    [MaxLength(10)]
    public string? Zimmer { get; private set; }

    /// <summary>
    /// Phone number
    /// </summary>
    public PhoneNumber? Telefon { get; private set; }

    /// <summary>
    /// Fax number
    /// </summary>
    public PhoneNumber? FAX { get; private set; }

    /// <summary>
    /// Email address
    /// </summary>
    public Email? Email { get; private set; }

    /// <summary>
    /// Dictation code
    /// </summary>
    [MaxLength(5)]
    public string? Diktatzeichen { get; private set; }

    /// <summary>
    /// Signature
    /// </summary>
    [MaxLength(50)]
    public string? Unterschrift { get; private set; }

    /// <summary>
    /// Job title/designation
    /// </summary>
    [MaxLength(30)]
    public string? Dienstbezeichnung { get; private set; }

    /// <summary>
    /// Group ID reference
    /// </summary>
    public Guid? GruppeId { get; private set; }

    /// <summary>
    /// Whether the person is an administrator
    /// </summary>
    public bool IstAdmin { get; private set; } = false;

    /// <summary>
    /// Permission: Administration access
    /// </summary>
    public bool DarfAdministration { get; private set; } = false;

    /// <summary>
    /// Permission: Performance groups access
    /// </summary>
    public bool DarfLeistungsgruppen { get; private set; } = false;

    /// <summary>
    /// Permission: Priority and SLA access
    /// </summary>
    public bool DarfPrioUndSLA { get; private set; } = false;

    /// <summary>
    /// Permission: Customer access
    /// </summary>
    public bool DarfKunden { get; private set; } = false;

    /// <summary>
    /// Whether the person is currently active
    /// </summary>
    public bool Aktiv { get; private set; } = true;

    /// <summary>
    /// Login username (derived from email or manually set)
    /// </summary>
    [MaxLength(100)]
    public string? Username { get; private set; }

    /// <summary>
    /// Last login timestamp
    /// </summary>
    public DateTime? LastLogin { get; private set; }

    /// <summary>
    /// Creates a new Person
    /// </summary>
    /// <param name="vorname">First name</param>
    /// <param name="nachname">Last name</param>
    /// <param name="email">Email address</param>
    /// <param name="anrede">Salutation</param>
    /// <param name="dienstbezeichnung">Job title</param>
    public static Person Create(
        string vorname,
        string nachname,
        Email? email = null,
        Anrede? anrede = null,
        string? dienstbezeichnung = null)
    {
        if (string.IsNullOrWhiteSpace(vorname))
            throw new ArgumentException("Vorname is required", nameof(vorname));

        if (string.IsNullOrWhiteSpace(nachname))
            throw new ArgumentException("Nachname is required", nameof(nachname));

        var person = new Person
        {
            Vorname = vorname.Trim(),
            Nachname = nachname.Trim(),
            Email = email,
            Anrede = anrede,
            Dienstbezeichnung = dienstbezeichnung?.Trim(),
            Username = email?.GetLocalPart(),
            Aktiv = true
        };

        return person;
    }

    /// <summary>
    /// Updates personal information
    /// </summary>
    public void UpdatePersonalInfo(
        string? vorname = null,
        string? nachname = null,
        Anrede? anrede = null,
        Email? email = null,
        PhoneNumber? telefon = null)
    {
        if (!string.IsNullOrWhiteSpace(vorname))
            Vorname = vorname.Trim();

        if (!string.IsNullOrWhiteSpace(nachname))
            Nachname = nachname.Trim();

        if (anrede.HasValue)
            Anrede = anrede.Value;

        if (email != null)
        {
            Email = email;
            if (string.IsNullOrWhiteSpace(Username))
                Username = email.GetLocalPart();
        }

        if (telefon != null)
            Telefon = telefon;

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates work-related information
    /// </summary>
    public void UpdateWorkInfo(
        string? nummer = null,
        string? organisationseinheit = null,
        string? zimmer = null,
        string? dienstbezeichnung = null,
        string? diktatzeichen = null,
        string? unterschrift = null)
    {
        if (nummer != null)
            Nummer = nummer.Trim();

        if (organisationseinheit != null)
            Organisationseinheit = organisationseinheit.Trim();

        if (zimmer != null)
            Zimmer = zimmer.Trim();

        if (dienstbezeichnung != null)
            Dienstbezeichnung = dienstbezeichnung.Trim();

        if (diktatzeichen != null)
            Diktatzeichen = diktatzeichen.Trim();

        if (unterschrift != null)
            Unterschrift = unterschrift.Trim();

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates permissions
    /// </summary>
    public void UpdatePermissions(
        bool? istAdmin = null,
        bool? darfAdministration = null,
        bool? darfLeistungsgruppen = null,
        bool? darfPrioUndSLA = null,
        bool? darfKunden = null)
    {
        if (istAdmin.HasValue)
            IstAdmin = istAdmin.Value;

        if (darfAdministration.HasValue)
            DarfAdministration = darfAdministration.Value;

        if (darfLeistungsgruppen.HasValue)
            DarfLeistungsgruppen = darfLeistungsgruppen.Value;

        if (darfPrioUndSLA.HasValue)
            DarfPrioUndSLA = darfPrioUndSLA.Value;

        if (darfKunden.HasValue)
            DarfKunden = darfKunden.Value;

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the person
    /// </summary>
    public void Activate()
    {
        if (!Aktiv)
        {
            Aktiv = true;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Deactivates the person
    /// </summary>
    public void Deactivate()
    {
        if (Aktiv)
        {
            Aktiv = false;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Records a login
    /// </summary>
    public void RecordLogin()
    {
        LastLogin = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the full name
    /// </summary>
    public string GetFullName()
    {
        return $"{Vorname} {Nachname}";
    }

    /// <summary>
    /// Gets the display name with title if available
    /// </summary>
    public string GetDisplayName()
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(Dienstbezeichnung))
            parts.Add(Dienstbezeichnung);

        parts.Add(GetFullName());

        return string.Join(" ", parts);
    }

    /// <summary>
    /// Checks if the person has administrative permissions
    /// </summary>
    public bool HasAdminPermissions()
    {
        return IstAdmin || DarfAdministration;
    }

    private Person()
    {
        // Required for EF Core
    }
}