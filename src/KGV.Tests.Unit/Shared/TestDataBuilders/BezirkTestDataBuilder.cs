using KGV.Domain.Entities;
using KGV.Domain.Enums;

namespace KGV.Tests.Unit.Shared.TestDataBuilders;

/// <summary>
/// Test Data Builder für Bezirk-Entitäten mit deutschen Standardwerten.
/// Implementiert das Builder-Pattern für flexible Test-Daten-Erstellung.
/// </summary>
public class BezirkTestDataBuilder
{
    private int _id = 1;
    private string _name = "Bezirk Mitte";
    private string _beschreibung = "Zentraler Bezirk der Stadt";
    private BezirkStatus _status = BezirkStatus.Aktiv;
    private DateTime _erstelltAm = DateTime.Now.AddDays(-30);
    private DateTime? _geaendertAm = DateTime.Now.AddDays(-1);

    /// <summary>
    /// Erstellt einen neuen Builder mit deutschen Standardwerten.
    /// </summary>
    public static BezirkTestDataBuilder Create() => new();

    /// <summary>
    /// Setzt die Bezirk-ID.
    /// </summary>
    public BezirkTestDataBuilder WithId(int id)
    {
        _id = id;
        return this;
    }

    /// <summary>
    /// Setzt den Bezirk-Namen.
    /// </summary>
    public BezirkTestDataBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// Setzt die Bezirk-Beschreibung.
    /// </summary>
    public BezirkTestDataBuilder WithBeschreibung(string beschreibung)
    {
        _beschreibung = beschreibung;
        return this;
    }

    /// <summary>
    /// Setzt den Bezirk-Status.
    /// </summary>
    public BezirkTestDataBuilder WithStatus(BezirkStatus status)
    {
        _status = status;
        return this;
    }

    /// <summary>
    /// Setzt das Erstellungsdatum.
    /// </summary>
    public BezirkTestDataBuilder WithErstelltAm(DateTime erstelltAm)
    {
        _erstelltAm = erstelltAm;
        return this;
    }

    /// <summary>
    /// Setzt das Änderungsdatum.
    /// </summary>
    public BezirkTestDataBuilder WithGeaendertAm(DateTime? geaendertAm)
    {
        _geaendertAm = geaendertAm;
        return this;
    }

    /// <summary>
    /// Erstellt einen Bezirk für München mit realistischen Daten.
    /// </summary>
    public BezirkTestDataBuilder ForMunich()
    {
        return WithName("Bezirk München-Mitte")
            .WithBeschreibung("Zentraler Bezirk der Landeshauptstadt München")
            .WithStatus(BezirkStatus.Aktiv);
    }

    /// <summary>
    /// Erstellt einen inaktiven Bezirk.
    /// </summary>
    public BezirkTestDataBuilder AsInactive()
    {
        return WithStatus(BezirkStatus.Inaktiv)
            .WithName("Ehemaliger Bezirk")
            .WithBeschreibung("Dieser Bezirk wurde aufgelöst");
    }

    /// <summary>
    /// Erstellt einen Bezirk im Planungsstatus.
    /// </summary>
    public BezirkTestDataBuilder AsPlanned()
    {
        return WithStatus(BezirkStatus.Geplant)
            .WithName("Geplanter Bezirk")
            .WithBeschreibung("Bezirk in der Planungsphase")
            .WithErstelltAm(DateTime.Now.AddDays(-7))
            .WithGeaendertAm(null);
    }

    /// <summary>
    /// Erstellt die Bezirk-Entität mit den konfigurierten Werten.
    /// </summary>
    public Bezirk Build()
    {
        return new Bezirk
        {
            Id = _id,
            Name = _name,
            Beschreibung = _beschreibung,
            Status = _status,
            ErstelltAm = _erstelltAm,
            GeaendertAm = _geaendertAm
        };
    }

    /// <summary>
    /// Erstellt eine Liste von Bezirken mit verschiedenen deutschen Städten.
    /// </summary>
    public static List<Bezirk> CreateGermanCityDistricts(int count = 5)
    {
        var germanCities = new[]
        {
            ("München-Mitte", "Zentraler Bezirk der Landeshauptstadt München"),
            ("Berlin-Charlottenburg", "Westlicher Bezirk der Hauptstadt Berlin"),
            ("Hamburg-Altona", "Traditioneller Bezirk der Hansestadt Hamburg"),
            ("Stuttgart-Mitte", "Innenstadtbezirk der Landeshauptstadt Stuttgart"),
            ("Düsseldorf-Altstadt", "Historischer Kern der Landeshauptstadt")
        };

        var bezirke = new List<Bezirk>();
        
        for (int i = 0; i < Math.Min(count, germanCities.Length); i++)
        {
            var (name, beschreibung) = germanCities[i];
            bezirke.Add(Create()
                .WithId(i + 1)
                .WithName(name)
                .WithBeschreibung(beschreibung)
                .WithStatus(BezirkStatus.Aktiv)
                .WithErstelltAm(DateTime.Now.AddDays(-30 - i))
                .WithGeaendertAm(DateTime.Now.AddDays(-i))
                .Build());
        }

        return bezirke;
    }
}