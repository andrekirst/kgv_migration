using KGV.Domain.Entities;
using KGV.Domain.Enums;
using KGV.Domain.ValueObjects;

namespace KGV.Tests.Unit.Shared.TestDataBuilders;

/// <summary>
/// Test Data Builder für Antrag-Entitäten mit deutschen Standardwerten.
/// Implementiert das Builder-Pattern für realistische Antrags-Test-Daten.
/// </summary>
public class AntragTestDataBuilder
{
    private int _id = 1;
    private string _vorname = "Max";
    private string _nachname = "Mustermann";
    private Anrede _anrede = Anrede.Herr;
    private Email _email = new("max.mustermann@beispiel.de");
    private PhoneNumber _telefonnummer = new("+49 89 12345678");
    private Address _adresse = new("Musterstraße 1", "80331", "München");
    private AntragStatus _status = AntragStatus.Eingegangen;
    private string _bemerkungen = "Regulärer Antrag auf Parzellenzuteilung";
    private DateTime _eingangsdatum = DateTime.Now.AddDays(-7);
    private DateTime? _bearbeitungsdatum = null;
    private int? _zugewieseneParzelleId = null;

    /// <summary>
    /// Erstellt einen neuen Builder mit deutschen Standardwerten.
    /// </summary>
    public static AntragTestDataBuilder Create() => new();

    /// <summary>
    /// Setzt die Antrags-ID.
    /// </summary>
    public AntragTestDataBuilder WithId(int id)
    {
        _id = id;
        return this;
    }

    /// <summary>
    /// Setzt den Vornamen des Antragstellers.
    /// </summary>
    public AntragTestDataBuilder WithVorname(string vorname)
    {
        _vorname = vorname;
        return this;
    }

    /// <summary>
    /// Setzt den Nachnamen des Antragstellers.
    /// </summary>
    public AntragTestDataBuilder WithNachname(string nachname)
    {
        _nachname = nachname;
        return this;
    }

    /// <summary>
    /// Setzt die Anrede des Antragstellers.
    /// </summary>
    public AntragTestDataBuilder WithAnrede(Anrede anrede)
    {
        _anrede = anrede;
        return this;
    }

    /// <summary>
    /// Setzt die E-Mail-Adresse des Antragstellers.
    /// </summary>
    public AntragTestDataBuilder WithEmail(string email)
    {
        _email = new Email(email);
        return this;
    }

    /// <summary>
    /// Setzt die Telefonnummer des Antragstellers.
    /// </summary>
    public AntragTestDataBuilder WithTelefonnummer(string telefonnummer)
    {
        _telefonnummer = new PhoneNumber(telefonnummer);
        return this;
    }

    /// <summary>
    /// Setzt die Adresse des Antragstellers.
    /// </summary>
    public AntragTestDataBuilder WithAdresse(string strasse, string plz, string ort)
    {
        _adresse = new Address(strasse, plz, ort);
        return this;
    }

    /// <summary>
    /// Setzt den Antragsstatus.
    /// </summary>
    public AntragTestDataBuilder WithStatus(AntragStatus status)
    {
        _status = status;
        return this;
    }

    /// <summary>
    /// Setzt die Bemerkungen zum Antrag.
    /// </summary>
    public AntragTestDataBuilder WithBemerkungen(string bemerkungen)
    {
        _bemerkungen = bemerkungen;
        return this;
    }

    /// <summary>
    /// Setzt das Eingangsdatum des Antrags.
    /// </summary>
    public AntragTestDataBuilder WithEingangsdatum(DateTime eingangsdatum)
    {
        _eingangsdatum = eingangsdatum;
        return this;
    }

    /// <summary>
    /// Setzt das Bearbeitungsdatum des Antrags.
    /// </summary>
    public AntragTestDataBuilder WithBearbeitungsdatum(DateTime? bearbeitungsdatum)
    {
        _bearbeitungsdatum = bearbeitungsdatum;
        return this;
    }

    /// <summary>
    /// Setzt die zugewiesene Parzellen-ID.
    /// </summary>
    public AntragTestDataBuilder WithZugewieseneParzelleId(int? parzelleId)
    {
        _zugewieseneParzelleId = parzelleId;
        return this;
    }

    /// <summary>
    /// Erstellt einen weiblichen Antragsteller aus München.
    /// </summary>
    public AntragTestDataBuilder AsFrauFromMunich()
    {
        return WithAnrede(Anrede.Frau)
            .WithVorname("Anna")
            .WithNachname("Schmidt")
            .WithEmail("anna.schmidt@muenchen.de")
            .WithTelefonnummer("+49 89 98765432")
            .WithAdresse("Maximilianstraße 10", "80539", "München");
    }

    /// <summary>
    /// Erstellt einen männlichen Antragsteller aus Berlin.
    /// </summary>
    public AntragTestDataBuilder AsHerrFromBerlin()
    {
        return WithAnrede(Anrede.Herr)
            .WithVorname("Thomas")
            .WithNachname("Weber")
            .WithEmail("thomas.weber@berlin.de")
            .WithTelefonnummer("+49 30 12345678")
            .WithAdresse("Unter den Linden 1", "10117", "Berlin");
    }

    /// <summary>
    /// Erstellt einen Antrag im Status "Eingegangen".
    /// </summary>
    public AntragTestDataBuilder AsEingegangen()
    {
        return WithStatus(AntragStatus.Eingegangen)
            .WithEingangsdatum(DateTime.Now.AddDays(-3))
            .WithBearbeitungsdatum(null)
            .WithBemerkungen("Antrag wurde eingereicht und wartet auf Bearbeitung");
    }

    /// <summary>
    /// Erstellt einen Antrag im Status "In Bearbeitung".
    /// </summary>
    public AntragTestDataBuilder AsInBearbeitung()
    {
        return WithStatus(AntragStatus.InBearbeitung)
            .WithEingangsdatum(DateTime.Now.AddDays(-14))
            .WithBearbeitungsdatum(DateTime.Now.AddDays(-7))
            .WithBemerkungen("Antrag wird derzeit geprüft");
    }

    /// <summary>
    /// Erstellt einen genehmigten Antrag mit zugewiesener Parzelle.
    /// </summary>
    public AntragTestDataBuilder AsGenehmigt(int parzelleId)
    {
        return WithStatus(AntragStatus.Genehmigt)
            .WithEingangsdatum(DateTime.Now.AddDays(-30))
            .WithBearbeitungsdatum(DateTime.Now.AddDays(-1))
            .WithZugewieseneParzelleId(parzelleId)
            .WithBemerkungen("Antrag wurde genehmigt und Parzelle zugewiesen");
    }

    /// <summary>
    /// Erstellt einen abgelehnten Antrag.
    /// </summary>
    public AntragTestDataBuilder AsAbgelehnt(string ablehnungsgrund)
    {
        return WithStatus(AntragStatus.Abgelehnt)
            .WithEingangsdatum(DateTime.Now.AddDays(-21))
            .WithBearbeitungsdatum(DateTime.Now.AddDays(-3))
            .WithBemerkungen($"Antrag abgelehnt: {ablehnungsgrund}");
    }

    /// <summary>
    /// Erstellt die Antrags-Entität mit den konfigurierten Werten.
    /// </summary>
    public Antrag Build()
    {
        return new Antrag
        {
            Id = _id,
            Vorname = _vorname,
            Nachname = _nachname,
            Anrede = _anrede,
            Email = _email,
            Telefonnummer = _telefonnummer,
            Adresse = _adresse,
            Status = _status,
            Bemerkungen = _bemerkungen,
            Eingangsdatum = _eingangsdatum,
            Bearbeitungsdatum = _bearbeitungsdatum,
            ZugewieseneParzelleId = _zugewieseneParzelleId
        };
    }

    /// <summary>
    /// Erstellt eine Liste typischer deutscher Anträge.
    /// </summary>
    public static List<Antrag> CreateTypicalGermanApplications(int count = 5)
    {
        var builders = new[]
        {
            Create().AsFrauFromMunich().AsEingegangen(),
            Create().WithId(2).AsHerrFromBerlin().AsInBearbeitung(),
            Create().WithId(3).WithVorname("Maria").WithNachname("Fischer")
                .WithEmail("maria.fischer@hamburg.de").AsGenehmigt(101),
            Create().WithId(4).WithVorname("Klaus").WithNachname("Wagner")
                .WithEmail("klaus.wagner@stuttgart.de").AsAbgelehnt("Keine freien Parzellen verfügbar"),
            Create().WithId(5).WithVorname("Petra").WithNachname("Bauer")
                .WithEmail("petra.bauer@duesseldorf.de").AsEingegangen()
        };

        return builders.Take(count).Select(b => b.Build()).ToList();
    }
}