using FluentAssertions;
using KGV.Application.Features.Bezirke.Commands.CreateBezirk;
using KGV.Domain.Entities;
using KGV.Domain.Enums;
using KGV.Infrastructure.Repositories.Interfaces;
using KGV.Tests.Unit.Shared;
using KGV.Tests.Unit.Shared.TestDataBuilders;
using Moq;
using Xunit;

namespace KGV.Tests.Unit.Application.Features.Bezirke.Commands;

/// <summary>
/// Unit Tests für den CreateBezirkCommandHandler.
/// Testet die Geschäftslogik der Bezirks-Erstellung einschließlich Validierung und Repository-Interaktionen.
/// </summary>
public class CreateBezirkCommandHandlerTests : TestBase
{
    private readonly Mock<IBezirkRepository> _mockBezirkRepository;
    private readonly CreateBezirkCommandHandler _handler;

    public CreateBezirkCommandHandlerTests()
    {
        _mockBezirkRepository = new Mock<IBezirkRepository>();
        _handler = new CreateBezirkCommandHandler(_mockBezirkRepository.Object);
    }

    [Fact]
    public async Task Handle_Should_Create_Bezirk_With_Valid_Command()
    {
        // Arrange
        var command = new CreateBezirkCommand
        {
            Name = "München-Mitte",
            Beschreibung = "Zentraler Bezirk der Landeshauptstadt München"
        };

        _mockBezirkRepository
            .Setup(r => r.ExistsByNameAsync(command.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockBezirkRepository
            .Setup(r => r.AddAsync(It.IsAny<Bezirk>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Bezirk bezirk, CancellationToken _) => 
            {
                bezirk.Id = 1; // Simuliere Datenbank-ID-Zuweisung
                return bezirk;
            });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull("weil ein Ergebnis zurückgegeben werden sollte");
        result.IsSuccess.Should().BeTrue("weil die Bezirks-Erstellung erfolgreich sein sollte");
        result.Value.Should().NotBeNull("weil ein Bezirk-DTO zurückgegeben werden sollte");
        result.Value.Name.Should().Be(command.Name, "weil der Name korrekt übernommen werden sollte");
        result.Value.Beschreibung.Should().Be(command.Beschreibung, "weil die Beschreibung korrekt übernommen werden sollte");
        result.Value.Status.Should().Be(BezirkStatus.Aktiv, "weil neue Bezirke standardmäßig aktiv sein sollten");

        // Verify Repository-Aufrufe
        _mockBezirkRepository.Verify(
            r => r.ExistsByNameAsync(command.Name, It.IsAny<CancellationToken>()), 
            Times.Once, 
            "weil auf Eindeutigkeit des Namens geprüft werden sollte");

        _mockBezirkRepository.Verify(
            r => r.AddAsync(It.Is<Bezirk>(b => 
                b.Name == command.Name && 
                b.Beschreibung == command.Beschreibung &&
                b.Status == BezirkStatus.Aktiv), 
                It.IsAny<CancellationToken>()), 
            Times.Once, 
            "weil der Bezirk mit korrekten Werten gespeichert werden sollte");
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_Bezirk_Name_Already_Exists()
    {
        // Arrange
        var command = new CreateBezirkCommand
        {
            Name = "München-Mitte",
            Beschreibung = "Zentraler Bezirk"
        };

        _mockBezirkRepository
            .Setup(r => r.ExistsByNameAsync(command.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull("weil ein Ergebnis zurückgegeben werden sollte");
        result.IsSuccess.Should().BeFalse("weil die Erstellung wegen doppeltem Namen fehlschlagen sollte");
        result.Error.Should().Contain("bereits vorhanden", "weil die Fehlermeldung auf den doppelten Namen hinweisen sollte");

        // Verify dass AddAsync nicht aufgerufen wurde
        _mockBezirkRepository.Verify(
            r => r.AddAsync(It.IsAny<Bezirk>(), It.IsAny<CancellationToken>()), 
            Times.Never, 
            "weil bei doppeltem Namen nichts gespeichert werden sollte");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Handle_Should_Return_Failure_When_Name_Is_Invalid(string invalidName)
    {
        // Arrange
        var command = new CreateBezirkCommand
        {
            Name = invalidName,
            Beschreibung = "Gültige Beschreibung"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull("weil ein Ergebnis zurückgegeben werden sollte");
        result.IsSuccess.Should().BeFalse("weil ungültige Namen abgelehnt werden sollten");
        result.Error.Should().Contain("Name", "weil die Fehlermeldung den Namen erwähnen sollte");

        // Verify dass keine Repository-Methoden aufgerufen wurden
        _mockBezirkRepository.Verify(
            r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), 
            Times.Never, 
            "weil bei ungültigem Namen keine Prüfung stattfinden sollte");

        _mockBezirkRepository.Verify(
            r => r.AddAsync(It.IsAny<Bezirk>(), It.IsAny<CancellationToken>()), 
            Times.Never, 
            "weil bei ungültigem Namen nichts gespeichert werden sollte");
    }

    [Fact]
    public async Task Handle_Should_Create_Bezirk_With_German_Special_Characters()
    {
        // Arrange
        var command = new CreateBezirkCommand
        {
            Name = "Müller-Güntherstraße",
            Beschreibung = "Bereich für Gärtner in der Nähe des Fußballplatzes"
        };

        _mockBezirkRepository
            .Setup(r => r.ExistsByNameAsync(command.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockBezirkRepository
            .Setup(r => r.AddAsync(It.IsAny<Bezirk>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Bezirk bezirk, CancellationToken _) => 
            {
                bezirk.Id = 1;
                return bezirk;
            });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull("weil ein Ergebnis zurückgegeben werden sollte");
        result.IsSuccess.Should().BeTrue("weil deutsche Sonderzeichen unterstützt werden sollten");
        result.Value.Name.Should().Be(command.Name, "weil deutsche Umlaute korrekt verarbeitet werden sollten");
        result.Value.Beschreibung.Should().Be(command.Beschreibung, "weil deutsche Sonderzeichen in der Beschreibung erhalten bleiben sollten");
    }

    [Fact]
    public async Task Handle_Should_Handle_Repository_Exception_Gracefully()
    {
        // Arrange
        var command = new CreateBezirkCommand
        {
            Name = "Test-Bezirk",
            Beschreibung = "Test-Beschreibung"
        };

        _mockBezirkRepository
            .Setup(r => r.ExistsByNameAsync(command.Name, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Datenbankfehler"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull("weil ein Ergebnis zurückgegeben werden sollte");
        result.IsSuccess.Should().BeFalse("weil bei Datenbankfehlern ein Fehlerresultat zurückgegeben werden sollte");
        result.Error.Should().Contain("Datenbankfehler", "weil die ursprüngliche Exception-Message enthalten sein sollte");
    }

    [Theory]
    [GermanAutoData]
    public async Task Handle_Should_Work_With_AutoFixture_Generated_Data(string name, string beschreibung)
    {
        // Arrange
        name = !string.IsNullOrWhiteSpace(name) ? name : "Fallback-Bezirk";
        beschreibung = !string.IsNullOrWhiteSpace(beschreibung) ? beschreibung : "Fallback-Beschreibung";

        var command = new CreateBezirkCommand
        {
            Name = name,
            Beschreibung = beschreibung
        };

        _mockBezirkRepository
            .Setup(r => r.ExistsByNameAsync(command.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockBezirkRepository
            .Setup(r => r.AddAsync(It.IsAny<Bezirk>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Bezirk bezirk, CancellationToken _) => 
            {
                bezirk.Id = 1;
                return bezirk;
            });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull("weil ein Ergebnis zurückgegeben werden sollte");
        result.IsSuccess.Should().BeTrue("weil die Erstellung mit generierten Daten erfolgreich sein sollte");
        result.Value.Name.Should().Be(command.Name, "weil der generierte Name verwendet werden sollte");
        result.Value.Beschreibung.Should().Be(command.Beschreibung, "weil die generierte Beschreibung verwendet werden sollte");
    }

    [Fact]
    public async Task Handle_Should_Set_Creation_Timestamp()
    {
        // Arrange
        var command = new CreateBezirkCommand
        {
            Name = "Zeitstempel-Test-Bezirk",
            Beschreibung = "Test für Zeitstempel-Funktionalität"
        };

        var beforeCreation = DateTime.Now;
        Bezirk? capturedBezirk = null;

        _mockBezirkRepository
            .Setup(r => r.ExistsByNameAsync(command.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockBezirkRepository
            .Setup(r => r.AddAsync(It.IsAny<Bezirk>(), It.IsAny<CancellationToken>()))
            .Callback<Bezirk, CancellationToken>((bezirk, _) => capturedBezirk = bezirk)
            .ReturnsAsync((Bezirk bezirk, CancellationToken _) => 
            {
                bezirk.Id = 1;
                return bezirk;
            });

        // Act
        await _handler.Handle(command, CancellationToken.None);
        var afterCreation = DateTime.Now;

        // Assert
        capturedBezirk.Should().NotBeNull("weil der Bezirk erfasst werden sollte");
        capturedBezirk!.ErstelltAm.Should().BeOnOrAfter(beforeCreation, "weil der Zeitstempel nach dem Test-Start gesetzt werden sollte");
        capturedBezirk.ErstelltAm.Should().BeOnOrBefore(afterCreation, "weil der Zeitstempel vor dem Test-Ende gesetzt werden sollte");
        capturedBezirk.GeaendertAm.Should().BeNull("weil bei der Erstellung noch kein Änderungsdatum gesetzt werden sollte");
    }

    [Fact]
    public async Task Handle_Should_Respect_Cancellation_Token()
    {
        // Arrange
        var command = new CreateBezirkCommand
        {
            Name = "Cancellation-Test-Bezirk",
            Beschreibung = "Test für Cancellation Token"
        };

        var cancellationToken = new CancellationToken(canceled: true);

        _mockBezirkRepository
            .Setup(r => r.ExistsByNameAsync(command.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        var act = async () => await _handler.Handle(command, cancellationToken);

        await act.Should().ThrowAsync<OperationCanceledException>("weil ein abgebrochener Token respektiert werden sollte");
    }
}