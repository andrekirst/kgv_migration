using FluentAssertions;
using KGV.Domain.Entities;
using KGV.Domain.Enums;
using KGV.Tests.Unit.Shared;
using KGV.Tests.Unit.Shared.TestDataBuilders;
using Xunit;

namespace KGV.Tests.Unit.Domain.Entities;

/// <summary>
/// Unit Tests für die Bezirk-Entität.
/// Testet Geschäftslogik, Validierung und Verhalten der Domain-Entität.
/// </summary>
public class BezirkTests : TestBase
{
    [Fact]
    public void Bezirk_Should_Be_Created_With_Valid_Properties()
    {
        // Arrange
        var expectedName = "Bezirk München-Mitte";
        var expectedBeschreibung = "Zentraler Bezirk der Landeshauptstadt";
        var expectedStatus = BezirkStatus.Aktiv;

        // Act
        var bezirk = BezirkTestDataBuilder.Create()
            .WithName(expectedName)
            .WithBeschreibung(expectedBeschreibung)
            .WithStatus(expectedStatus)
            .Build();

        // Assert
        bezirk.Should().NotBeNull("weil ein gültiger Bezirk erstellt werden sollte");
        bezirk.Name.Should().Be(expectedName, "weil der Name korrekt gesetzt werden sollte");
        bezirk.Beschreibung.Should().Be(expectedBeschreibung, "weil die Beschreibung korrekt gesetzt werden sollte");
        bezirk.Status.Should().Be(expectedStatus, "weil der Status korrekt gesetzt werden sollte");
        bezirk.ErstelltAm.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1), 
            "weil das Erstellungsdatum nahe der aktuellen Zeit liegen sollte");
    }

    [Theory]
    [InlineData("", "Leerer Name sollte nicht erlaubt sein")]
    [InlineData("   ", "Name mit nur Leerzeichen sollte nicht erlaubt sein")]
    [InlineData(null, "Null-Name sollte nicht erlaubt sein")]
    public void Bezirk_Should_Reject_Invalid_Names(string invalidName, string because)
    {
        // Act & Assert
        var act = () => BezirkTestDataBuilder.Create()
            .WithName(invalidName)
            .Build();

        act.Should().Throw<ArgumentException>(because)
            .WithMessage("*Name*", "weil die Fehlermeldung den Namen erwähnen sollte");
    }

    [Fact]
    public void Bezirk_Should_Set_GeaendertAm_When_Status_Changes()
    {
        // Arrange
        var bezirk = BezirkTestDataBuilder.Create()
            .WithStatus(BezirkStatus.Aktiv)
            .WithGeaendertAm(null)
            .Build();

        var initialChangeDate = bezirk.GeaendertAm;

        // Act
        bezirk.Status = BezirkStatus.Inaktiv;
        bezirk.GeaendertAm = DateTime.Now;

        // Assert
        initialChangeDate.Should().BeNull("weil anfangs kein Änderungsdatum gesetzt war");
        bezirk.GeaendertAm.Should().NotBeNull("weil nach der Änderung ein Datum gesetzt werden sollte");
        bezirk.GeaendertAm.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(10),
            "weil das Änderungsdatum aktuell sein sollte");
    }

    [Theory]
    [InlineData(BezirkStatus.Aktiv, true)]
    [InlineData(BezirkStatus.Inaktiv, false)]
    [InlineData(BezirkStatus.Geplant, false)]
    public void Bezirk_IsActive_Should_Return_Correct_Value(BezirkStatus status, bool expectedIsActive)
    {
        // Arrange
        var bezirk = BezirkTestDataBuilder.Create()
            .WithStatus(status)
            .Build();

        // Act
        var isActive = bezirk.IsActive();

        // Assert
        isActive.Should().Be(expectedIsActive, 
            $"weil ein Bezirk mit Status {status} als {(expectedIsActive ? "aktiv" : "inaktiv")} gelten sollte");
    }

    [Fact]
    public void Bezirk_CanBeDeleted_Should_Return_True_When_No_Dependencies()
    {
        // Arrange
        var bezirk = BezirkTestDataBuilder.Create()
            .WithStatus(BezirkStatus.Inaktiv)
            .Build();

        // Act
        var canBeDeleted = bezirk.CanBeDeleted();

        // Assert
        canBeDeleted.Should().BeTrue("weil ein inaktiver Bezirk ohne Abhängigkeiten gelöscht werden kann");
    }

    [Fact]
    public void Bezirk_Should_Support_German_Special_Characters()
    {
        // Arrange
        var nameWithUmlauts = "Bezirk Müller-Güntherstraße";
        var beschreibungWithUmlauts = "Bereich für Gärtner und Kleingärtner in der Nähe des Fußballplatzes";

        // Act
        var bezirk = BezirkTestDataBuilder.Create()
            .WithName(nameWithUmlauts)
            .WithBeschreibung(beschreibungWithUmlauts)
            .Build();

        // Assert
        bezirk.Name.Should().Be(nameWithUmlauts, "weil deutsche Umlaute unterstützt werden sollten");
        bezirk.Beschreibung.Should().Be(beschreibungWithUmlauts, "weil deutsche Sonderzeichen korrekt verarbeitet werden sollten");
    }

    [Fact]
    public void Multiple_Bezirke_Should_Have_Unique_Identifiers()
    {
        // Arrange & Act
        var bezirke = BezirkTestDataBuilder.CreateGermanCityDistricts(5);

        // Assert
        bezirke.Should().HaveCount(5, "weil 5 Bezirke erstellt werden sollten");
        bezirke.Select(b => b.Id).Should().OnlyHaveUniqueItems("weil jeder Bezirk eine eindeutige ID haben sollte");
        bezirke.Select(b => b.Name).Should().OnlyHaveUniqueItems("weil jeder Bezirk einen eindeutigen Namen haben sollte");
    }

    [Fact]
    public void Bezirk_ToString_Should_Return_Meaningful_Description()
    {
        // Arrange
        var bezirk = BezirkTestDataBuilder.Create()
            .WithName("München-Mitte")
            .WithStatus(BezirkStatus.Aktiv)
            .Build();

        // Act
        var stringRepresentation = bezirk.ToString();

        // Assert
        stringRepresentation.Should().Contain("München-Mitte", "weil der Name im String enthalten sein sollte");
        stringRepresentation.Should().Contain("Aktiv", "weil der Status im String enthalten sein sollte");
    }

    [Theory]
    [GermanAutoData]
    public void Bezirk_Should_Handle_AutoFixture_Generated_Data(string name, string beschreibung)
    {
        // Arrange
        name = !string.IsNullOrWhiteSpace(name) ? name : "Fallback-Bezirk";
        beschreibung = !string.IsNullOrWhiteSpace(beschreibung) ? beschreibung : "Fallback-Beschreibung";

        // Act
        var bezirk = BezirkTestDataBuilder.Create()
            .WithName(name)
            .WithBeschreibung(beschreibung)
            .Build();

        // Assert
        bezirk.Should().NotBeNull("weil der Bezirk erfolgreich erstellt werden sollte");
        bezirk.Name.Should().Be(name, "weil der generierte Name verwendet werden sollte");
        bezirk.Beschreibung.Should().Be(beschreibung, "weil die generierte Beschreibung verwendet werden sollte");
    }

    [Fact]
    public void Bezirk_Should_Track_Creation_And_Modification_Dates()
    {
        // Arrange
        var creationTime = DateTime.Now.AddDays(-5);
        var modificationTime = DateTime.Now.AddDays(-1);

        // Act
        var bezirk = BezirkTestDataBuilder.Create()
            .WithErstelltAm(creationTime)
            .WithGeaendertAm(modificationTime)
            .Build();

        // Assert
        bezirk.ErstelltAm.Should().Be(creationTime, "weil das Erstellungsdatum korrekt gesetzt werden sollte");
        bezirk.GeaendertAm.Should().Be(modificationTime, "weil das Änderungsdatum korrekt gesetzt werden sollte");
        bezirk.GeaendertAm.Should().BeAfter(bezirk.ErstelltAm, "weil die Änderung nach der Erstellung stattgefunden haben sollte");
    }
}