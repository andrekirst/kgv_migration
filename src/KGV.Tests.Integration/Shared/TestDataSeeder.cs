using Microsoft.EntityFrameworkCore;
using KGV.Domain.Entities;
using KGV.Domain.Enums;
using KGV.Domain.ValueObjects;
using KGV.Infrastructure.Data;

namespace KGV.Tests.Integration.Shared;

/// <summary>
/// Implementierung des Test-Daten-Seeders für realistische deutsche Test-Daten.
/// Erstellt konsistente Test-Daten für Integration Tests.
/// </summary>
public class TestDataSeeder : ITestDataSeeder
{
    private readonly KgvDbContext _context;

    public TestDataSeeder(KgvDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        await SeedCompleteHierarchyAsync();
    }

    public async Task<List<Bezirk>> SeedBezirkeAsync(int count = 5)
    {
        var bezirke = new List<Bezirk>();
        var germanCities = new[]
        {
            ("München-Mitte", "Zentraler Bezirk der Landeshauptstadt München"),
            ("Berlin-Charlottenburg", "Westlicher Bezirk der Hauptstadt Berlin"),
            ("Hamburg-Altona", "Traditioneller Bezirk der Hansestadt Hamburg"),
            ("Stuttgart-Mitte", "Innenstadtbezirk der Landeshauptstadt Stuttgart"),
            ("Düsseldorf-Altstadt", "Historischer Kern der Landeshauptstadt"),
            ("Köln-Innenstadt", "Zentraler Bereich der Domstadt Köln"),
            ("Frankfurt-Mitte", "Geschäftszentrum der Mainmetropole"),
            ("Nürnberg-Altstadt", "Historischer Kern der Frankenmetropole")
        };

        for (int i = 0; i < Math.Min(count, germanCities.Length); i++)
        {
            var (name, beschreibung) = germanCities[i];
            var bezirk = new Bezirk
            {
                Name = name,
                Beschreibung = beschreibung,
                Status = i % 4 == 3 ? BezirkStatus.Inaktiv : BezirkStatus.Aktiv, // Jeder 4. Bezirk inaktiv
                ErstelltAm = DateTime.Now.AddDays(-60 + i * 5),
                GeaendertAm = i % 2 == 0 ? DateTime.Now.AddDays(-10 + i) : null
            };

            _context.Bezirke.Add(bezirk);
            bezirke.Add(bezirk);
        }

        await _context.SaveChangesAsync();
        return bezirke;
    }

    public async Task<List<Antrag>> SeedAntraegeAsync(int count = 10)
    {
        var antraege = new List<Antrag>();
        var germanNames = new[]
        {
            ("Max", "Mustermann", Anrede.Herr, "max.mustermann@beispiel.de", "+49 89 12345678", "Musterstraße 1", "80331", "München"),
            ("Anna", "Schmidt", Anrede.Frau, "anna.schmidt@test.de", "+49 30 98765432", "Testweg 5", "10115", "Berlin"),
            ("Thomas", "Weber", Anrede.Herr, "thomas.weber@mail.de", "+49 40 11223344", "Hauptstraße 10", "20095", "Hamburg"),
            ("Maria", "Fischer", Anrede.Frau, "maria.fischer@email.de", "+49 711 55667788", "Königstraße 15", "70173", "Stuttgart"),
            ("Klaus", "Wagner", Anrede.Herr, "klaus.wagner@post.de", "+49 211 99887766", "Rheinstraße 20", "40213", "Düsseldorf"),
            ("Petra", "Bauer", Anrede.Frau, "petra.bauer@web.de", "+49 221 44556677", "Domstraße 8", "50667", "Köln"),
            ("Hans", "Richter", Anrede.Herr, "hans.richter@online.de", "+49 69 33445566", "Zeil 12", "60313", "Frankfurt"),
            ("Sabine", "Klein", Anrede.Frau, "sabine.klein@internet.de", "+49 911 22334455", "Hauptmarkt 6", "90403", "Nürnberg"),
            ("Jürgen", "Wolf", Anrede.Herr, "juergen.wolf@digital.de", "+49 201 66778899", "Limbecker Platz 3", "45127", "Essen"),
            ("Ingrid", "Neumann", Anrede.Frau, "ingrid.neumann@cyber.de", "+49 421 77889900", "Böttcherstraße 7", "28195", "Bremen")
        };

        var statusVariationen = new[]
        {
            AntragStatus.Eingegangen,
            AntragStatus.InBearbeitung,
            AntragStatus.Genehmigt,
            AntragStatus.Abgelehnt
        };

        for (int i = 0; i < Math.Min(count, germanNames.Length); i++)
        {
            var (vorname, nachname, anrede, email, telefon, strasse, plz, ort) = germanNames[i];
            var status = statusVariationen[i % statusVariationen.Length];

            var antrag = new Antrag
            {
                Vorname = vorname,
                Nachname = nachname,
                Anrede = anrede,
                Email = new Email(email),
                Telefonnummer = new PhoneNumber(telefon),
                Adresse = new Address(strasse, plz, ort),
                Status = status,
                Eingangsdatum = DateTime.Now.AddDays(-30 + i * 2),
                Bearbeitungsdatum = status != AntragStatus.Eingegangen 
                    ? DateTime.Now.AddDays(-25 + i * 2) 
                    : null,
                Bemerkungen = status switch
                {
                    AntragStatus.Eingegangen => "Antrag eingegangen und wartet auf Bearbeitung",
                    AntragStatus.InBearbeitung => "Antrag wird derzeit geprüft",
                    AntragStatus.Genehmigt => "Antrag wurde genehmigt",
                    AntragStatus.Abgelehnt => "Antrag wurde abgelehnt - keine freien Parzellen",
                    _ => "Standard-Bemerkung"
                }
            };

            _context.Antraege.Add(antrag);
            antraege.Add(antrag);
        }

        await _context.SaveChangesAsync();
        return antraege;
    }

    public async Task SeedCompleteHierarchyAsync()
    {
        // Zuerst Bezirke erstellen
        var bezirke = await SeedBezirkeAsync(5);

        // Dann Katasterbezirke
        await SeedKatasterbezirkeAsync();

        // Parzellen für jeden Bezirk erstellen
        var alleParzellen = new List<Parzelle>();
        foreach (var bezirk in bezirke.Where(b => b.Status == BezirkStatus.Aktiv))
        {
            var parzellen = await SeedParzellenForBezirkAsync(bezirk.Id, 10);
            alleParzellen.AddRange(parzellen);
        }

        // Anträge erstellen
        var antraege = await SeedAntraegeAsync(15);

        // Einige Anträge mit Parzellen verknüpfen
        var freieParzellen = alleParzellen.Where(p => p.Status == ParzellenStatus.Frei).ToList();
        var genehmigteAntraege = antraege.Where(a => a.Status == AntragStatus.Genehmigt).ToList();

        for (int i = 0; i < Math.Min(freieParzellen.Count, genehmigteAntraege.Count); i++)
        {
            genehmigteAntraege[i].ZugewieseneParzelleId = freieParzellen[i].Id;
            freieParzellen[i].Status = ParzellenStatus.Belegt;
            freieParzellen[i].GeaendertAm = DateTime.Now.AddDays(-5 + i);
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedKatasterbezirkeAsync()
    {
        var katasterbezirke = new[]
        {
            new Katasterbezirk { Name = "Altstadt", Nummer = "001", Beschreibung = "Historisches Zentrum" },
            new Katasterbezirk { Name = "Neustadt", Nummer = "002", Beschreibung = "Erweitertes Stadtgebiet" },
            new Katasterbezirk { Name = "Industriegebiet", Nummer = "003", Beschreibung = "Gewerbliche Nutzung" },
            new Katasterbezirk { Name = "Wohngebiet-Nord", Nummer = "004", Beschreibung = "Nördliches Wohngebiet" },
            new Katasterbezirk { Name = "Wohngebiet-Süd", Nummer = "005", Beschreibung = "Südliches Wohngebiet" }
        };

        _context.Katasterbezirke.AddRange(katasterbezirke);
        await _context.SaveChangesAsync();
    }

    private async Task<List<Parzelle>> SeedParzellenForBezirkAsync(int bezirkId, int count)
    {
        var parzellen = new List<Parzelle>();
        var statusVariationen = new[] { ParzellenStatus.Frei, ParzellenStatus.Belegt, ParzellenStatus.Reserviert };

        for (int i = 1; i <= count; i++)
        {
            var parzelle = new Parzelle
            {
                Nummer = $"P{bezirkId:D2}-{i:D3}",
                BezirkId = bezirkId,
                Groesse = 200 + (i % 5) * 50, // Größen zwischen 200-400 qm
                Status = statusVariationen[i % statusVariationen.Length],
                Beschreibung = $"Parzelle {i} im Bezirk {bezirkId}",
                ErstelltAm = DateTime.Now.AddDays(-45 + i),
                GeaendertAm = i % 3 == 0 ? DateTime.Now.AddDays(-10 + i) : null
            };

            _context.Parzellen.Add(parzelle);
            parzellen.Add(parzelle);
        }

        await _context.SaveChangesAsync();
        return parzellen;
    }

    public async Task CleanAsync()
    {
        // Lösche in umgekehrter Abhängigkeitsreihenfolge
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM Antraege");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM Parzellen");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM BezirkeKatasterbezirke");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM Bezirke");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM Katasterbezirke");
        
        await _context.SaveChangesAsync();
    }
}