using FluentAssertions;
using KGV.Domain.Entities;
using KGV.Domain.Enums;
using KGV.Domain.ValueObjects;
using KGV.Tests.Integration.Shared;
using KGV.Tests.Unit.Shared.TestDataBuilders;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KGV.Tests.Integration.Database;

/// <summary>
/// Integration Tests für Datenbank-Operationen.
/// Testet Entity Framework Core-Konfigurationen, Beziehungen und Datenbank-Constraints.
/// </summary>
public class DatabaseIntegrationTests : IClassFixture<KgvWebApplicationFactory>
{
    private readonly KgvWebApplicationFactory _factory;

    public DatabaseIntegrationTests(KgvWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Database_Should_Save_And_Retrieve_Bezirk_With_German_Characters()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        
        var bezirk = BezirkTestDataBuilder.Create()
            .WithName("Gößweinstein-Süd")
            .WithBeschreibung("Bezirk für Gärtner in der Nähe des Fußballplatzes")
            .Build();

        // Act
        await _factory.ExecuteWithDbContextAsync(async context =>
        {
            context.Bezirke.Add(bezirk);
            await context.SaveChangesAsync();
        });

        var retrievedBezirk = await _factory.ExecuteWithDbContextAsync(async context =>
            context.Bezirke.FindAsync(bezirk.Id).AsTask());

        // Assert
        retrievedBezirk.Should().NotBeNull("weil der Bezirk in der Datenbank gespeichert werden sollte");
        retrievedBezirk!.Name.Should().Be("Gößweinstein-Süd", 
            "weil deutsche Sonderzeichen korrekt gespeichert werden sollten");
        retrievedBezirk.Beschreibung.Should().Contain("Gärtner", 
            "weil deutsche Umlaute in der Beschreibung erhalten bleiben sollten");
        retrievedBezirk.Beschreibung.Should().Contain("Nähe", 
            "weil deutsche Umlaute korrekt verarbeitet werden sollten");
        retrievedBezirk.Beschreibung.Should().Contain("Fußballplatzes", 
            "weil das ß korrekt gespeichert werden sollte");
    }

    [Fact]
    public async Task Database_Should_Enforce_Unique_Constraints_On_Bezirk_Names()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        
        var bezirk1 = BezirkTestDataBuilder.Create()
            .WithName("Eindeutiger-Bezirk")
            .Build();

        var bezirk2 = BezirkTestDataBuilder.Create()
            .WithId(2)
            .WithName("Eindeutiger-Bezirk") // Gleicher Name
            .Build();

        // Act & Assert
        await _factory.ExecuteWithDbContextAsync(async context =>
        {
            context.Bezirke.Add(bezirk1);
            await context.SaveChangesAsync();

            context.Bezirke.Add(bezirk2);
            
            var act = async () => await context.SaveChangesAsync();
            await act.Should().ThrowAsync<Exception>("weil doppelte Namen nicht erlaubt sein sollten");
        });
    }

    [Fact]
    public async Task Database_Should_Save_And_Retrieve_Value_Objects_Correctly()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        
        var antrag = AntragTestDataBuilder.Create()
            .WithEmail("max.müller@straße.de")
            .WithTelefonnummer("+49 89 12345678")
            .WithAdresse("Müllerstraße 10", "80331", "München")
            .Build();

        // Act
        await _factory.ExecuteWithDbContextAsync(async context =>
        {
            context.Antraege.Add(antrag);
            await context.SaveChangesAsync();
        });

        var retrievedAntrag = await _factory.ExecuteWithDbContextAsync(async context =>
            context.Antraege.FindAsync(antrag.Id).AsTask());

        // Assert
        retrievedAntrag.Should().NotBeNull("weil der Antrag gespeichert werden sollte");
        retrievedAntrag!.Email.Should().NotBeNull("weil das Email Value Object gespeichert werden sollte");
        retrievedAntrag.Email.Value.Should().Be("max.müller@straße.de", 
            "weil die E-Mail mit deutschen Zeichen korrekt gespeichert werden sollte");
        
        retrievedAntrag.Telefonnummer.Should().NotBeNull("weil das PhoneNumber Value Object gespeichert werden sollte");
        retrievedAntrag.Telefonnummer.Value.Should().Be("+49 89 12345678", 
            "weil die Telefonnummer korrekt gespeichert werden sollte");

        retrievedAntrag.Adresse.Should().NotBeNull("weil das Address Value Object gespeichert werden sollte");
        retrievedAntrag.Adresse.Strasse.Should().Be("Müllerstraße 10", 
            "weil die Straße mit Umlaut korrekt gespeichert werden sollte");
        retrievedAntrag.Adresse.Plz.Should().Be("80331");
        retrievedAntrag.Adresse.Ort.Should().Be("München", 
            "weil der Ort mit Umlaut korrekt gespeichert werden sollte");
    }

    [Fact]
    public async Task Database_Should_Handle_Entity_Relationships_Correctly()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        
        var bezirk = BezirkTestDataBuilder.Create()
            .WithName("Test-Bezirk-Beziehungen")
            .Build();

        // Act & Assert
        await _factory.ExecuteWithDbContextAsync(async context =>
        {
            context.Bezirke.Add(bezirk);
            await context.SaveChangesAsync();

            // Erstelle Parzelle für den Bezirk
            var parzelle = new Parzelle
            {
                Nummer = "P001",
                BezirkId = bezirk.Id,
                Groesse = 300,
                Status = ParzellenStatus.Frei,
                Beschreibung = "Test-Parzelle",
                ErstelltAm = DateTime.Now
            };

            context.Parzellen.Add(parzelle);
            await context.SaveChangesAsync();

            // Lade Bezirk mit Parzellen
            var bezirkMitParzellen = await context.Bezirke
                .Where(b => b.Id == bezirk.Id)
                .SingleOrDefaultAsync();

            bezirkMitParzellen.Should().NotBeNull("weil der Bezirk existieren sollte");
            
            // Prüfe separate Abfrage der Parzellen
            var parzellen = context.Parzellen.Where(p => p.BezirkId == bezirk.Id).ToList();
            parzellen.Should().HaveCount(1, "weil eine Parzelle für den Bezirk erstellt wurde");
            parzellen.First().Nummer.Should().Be("P001");
        });
    }

    [Fact]
    public async Task Database_Should_Support_Concurrent_Operations()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        
        const int numberOfBezirke = 10;
        var tasks = new List<Task>();

        // Act
        for (int i = 1; i <= numberOfBezirke; i++)
        {
            var index = i; // Capture for closure
            var task = _factory.ExecuteWithDbContextAsync(async context =>
            {
                var bezirk = BezirkTestDataBuilder.Create()
                    .WithId(index)
                    .WithName($"Concurrent-Bezirk-{index:D3}")
                    .WithBeschreibung($"Bezirk {index} für Concurrent-Test")
                    .Build();

                context.Bezirke.Add(bezirk);
                await context.SaveChangesAsync();
            });
            
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        // Assert
        var bezirkeCount = await _factory.ExecuteWithDbContextAsync(async context =>
            await context.Bezirke.CountAsync());

        bezirkeCount.Should().Be(numberOfBezirke, 
            "weil alle Bezirke concurrent erstellt werden sollten");
    }

    [Fact]
    public async Task Database_Should_Handle_Large_German_Text_Fields()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        
        var largeBeschreibung = string.Join(" ", Enumerable.Repeat(
            "Dies ist ein sehr langer deutscher Text mit Umlauten wie äöüß und anderen Sonderzeichen. " +
            "Er soll testen, ob die Datenbank große Textfelder korrekt verarbeiten kann. " +
            "Kleingartenvereine haben oft sehr detaillierte Beschreibungen ihrer Bezirke und Parzellen.",
            100)); // ~30KB Text

        var bezirk = BezirkTestDataBuilder.Create()
            .WithName("Großer-Text-Test")
            .WithBeschreibung(largeBeschreibung)
            .Build();

        // Act
        await _factory.ExecuteWithDbContextAsync(async context =>
        {
            context.Bezirke.Add(bezirk);
            await context.SaveChangesAsync();
        });

        var retrievedBezirk = await _factory.ExecuteWithDbContextAsync(async context =>
            context.Bezirke.FindAsync(bezirk.Id).AsTask());

        // Assert
        retrievedBezirk.Should().NotBeNull("weil der Bezirk gespeichert werden sollte");
        retrievedBezirk!.Beschreibung.Should().Be(largeBeschreibung, 
            "weil große Textfelder vollständig gespeichert werden sollten");
        retrievedBezirk.Beschreibung.Should().Contain("äöüß", 
            "weil deutsche Sonderzeichen auch in großen Texten erhalten bleiben sollten");
        retrievedBezirk.Beschreibung.Length.Should().BeGreaterThan(10000, 
            "weil der Text groß genug für den Test sein sollte");
    }

    [Fact]
    public async Task Database_Should_Handle_Enum_Values_Correctly()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        
        var antragStati = Enum.GetValues<AntragStatus>();
        var antraege = new List<Antrag>();

        foreach (var status in antragStati)
        {
            var antrag = AntragTestDataBuilder.Create()
                .WithId(antraege.Count + 1)
                .WithVorname($"Test{antraege.Count + 1}")
                .WithStatus(status)
                .Build();
            
            antraege.Add(antrag);
        }

        // Act
        await _factory.ExecuteWithDbContextAsync(async context =>
        {
            context.Antraege.AddRange(antraege);
            await context.SaveChangesAsync();
        });

        var retrievedAntraege = await _factory.ExecuteWithDbContextAsync(async context =>
            context.Antraege.ToListAsync());

        // Assert
        retrievedAntraege.Should().HaveCount(antragStati.Length, 
            "weil alle Enum-Werte gespeichert werden sollten");

        foreach (var expectedStatus in antragStati)
        {
            retrievedAntraege.Should().Contain(a => a.Status == expectedStatus, 
                $"weil der Status {expectedStatus} korrekt gespeichert werden sollte");
        }
    }

    [Fact]
    public async Task Database_Should_Handle_DateTime_Precision_Correctly()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        
        var preciseDatetime = new DateTime(2024, 12, 25, 14, 30, 45, 123, DateTimeKind.Local);
        
        var bezirk = BezirkTestDataBuilder.Create()
            .WithName("DateTime-Precision-Test")
            .WithErstelltAm(preciseDatetime)
            .WithGeaendertAm(preciseDatetime.AddMinutes(1))
            .Build();

        // Act
        await _factory.ExecuteWithDbContextAsync(async context =>
        {
            context.Bezirke.Add(bezirk);
            await context.SaveChangesAsync();
        });

        var retrievedBezirk = await _factory.ExecuteWithDbContextAsync(async context =>
            context.Bezirke.FindAsync(bezirk.Id).AsTask());

        // Assert
        retrievedBezirk.Should().NotBeNull("weil der Bezirk gespeichert werden sollte");
        retrievedBezirk!.ErstelltAm.Should().BeCloseTo(preciseDatetime, TimeSpan.FromSeconds(1),
            "weil DateTime-Werte mit angemessener Präzision gespeichert werden sollten");
        retrievedBezirk.GeaendertAm.Should().NotBeNull("weil das Änderungsdatum gesetzt war");
        retrievedBezirk.GeaendertAm!.Value.Should().BeCloseTo(preciseDatetime.AddMinutes(1), TimeSpan.FromSeconds(1),
            "weil auch nullable DateTime-Werte korrekt gespeichert werden sollten");
    }

    [Fact]
    public async Task Database_Should_Handle_Transaction_Rollback_Correctly()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        
        var bezirk = BezirkTestDataBuilder.Create()
            .WithName("Transaction-Test")
            .Build();

        // Act & Assert
        await _factory.ExecuteWithDbContextAsync(async context =>
        {
            using var transaction = await context.Database.BeginTransactionAsync();
            
            try
            {
                context.Bezirke.Add(bezirk);
                await context.SaveChangesAsync();

                // Simuliere einen Fehler
                throw new InvalidOperationException("Simulated error for rollback test");
            }
            catch (InvalidOperationException)
            {
                await transaction.RollbackAsync();
            }
        });

        // Prüfe, dass nichts gespeichert wurde
        var retrievedBezirk = await _factory.ExecuteWithDbContextAsync(async context =>
            context.Bezirke.FindAsync(bezirk.Id).AsTask());

        retrievedBezirk.Should().BeNull("weil die Transaktion zurückgerollt werden sollte");
    }
}