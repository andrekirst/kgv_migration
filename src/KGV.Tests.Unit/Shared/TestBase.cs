using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;

namespace KGV.Tests.Unit.Shared;

/// <summary>
/// Basis-Klasse für alle Unit Tests mit AutoFixture und AutoMoq Integration.
/// Bietet einheitliche Test-Setup-Funktionalität für alle Tests.
/// </summary>
public abstract class TestBase
{
    protected readonly IFixture Fixture;

    protected TestBase()
    {
        Fixture = new Fixture()
            .Customize(new AutoMoqCustomization { ConfigureMembers = true });
        
        ConfigureFixture();
    }

    /// <summary>
    /// Konfiguriert das Fixture für spezielle Test-Anforderungen.
    /// Kann in abgeleiteten Klassen überschrieben werden.
    /// </summary>
    protected virtual void ConfigureFixture()
    {
        // Standard-Konfiguration für deutsche Daten
        Fixture.Customize<string>(composer => 
            composer.FromFactory(() => 
                Fixture.Create<Generator<string>>().First()));
    }

    /// <summary>
    /// Erstellt eine Instanz mit deutschen Test-Daten.
    /// </summary>
    protected T CreateWithGermanData<T>() where T : class
    {
        var item = Fixture.Create<T>();
        return CustomizeForGermanContext(item);
    }

    /// <summary>
    /// Anpassung von Objekten für deutschen Kontext.
    /// Überschreibbar für spezielle Entitäten.
    /// </summary>
    protected virtual T CustomizeForGermanContext<T>(T item) where T : class
    {
        return item;
    }
}

/// <summary>
/// AutoData-Attribut mit deutscher Lokalisierung für xUnit Tests.
/// </summary>
public class GermanAutoDataAttribute : AutoDataAttribute
{
    public GermanAutoDataAttribute() : base(() =>
    {
        var fixture = new Fixture()
            .Customize(new AutoMoqCustomization { ConfigureMembers = true });
        
        // Deutsche Test-Daten-Konfiguration
        fixture.Customize<string>(composer => 
            composer.FromFactory(() => 
                GenerateGermanString()));
        
        return fixture;
    })
    {
    }

    private static string GenerateGermanString()
    {
        var germanWords = new[]
        {
            "Kleingartenverein", "Parzelle", "Bezirk", "Antrag", "Verwaltung",
            "Garten", "Grundstück", "Mitglied", "Vorstand", "Satzung",
            "München", "Berlin", "Hamburg", "Stuttgart", "Düsseldorf"
        };
        
        var random = new Random();
        return germanWords[random.Next(germanWords.Length)] + "_" + random.Next(1000);
    }
}

/// <summary>
/// InlineAutoData-Attribut mit deutscher Lokalisierung für parametrisierte Tests.
/// </summary>
public class GermanInlineAutoDataAttribute : InlineAutoDataAttribute
{
    public GermanInlineAutoDataAttribute(params object[] values) 
        : base(new GermanAutoDataAttribute(), values)
    {
    }
}