using FluentAssertions;
using KGV.Application.Features.Bezirke.Queries.GetAllBezirke;
using KGV.Domain.Entities;
using KGV.Infrastructure.Repositories.Interfaces;
using KGV.Tests.Unit.Shared;
using KGV.Tests.Unit.Shared.TestDataBuilders;
using Moq;
using Xunit;

namespace KGV.Tests.Unit.Application.Features.Bezirke.Queries;

/// <summary>
/// Unit Tests für den GetAllBezirkeQueryHandler.
/// Testet die Abfrage-Logik und Daten-Transformation für die Bezirks-Auflistung.
/// </summary>
public class GetAllBezirkeQueryHandlerTests : TestBase
{
    private readonly Mock<IBezirkRepository> _mockBezirkRepository;
    private readonly GetAllBezirkeQueryHandler _handler;

    public GetAllBezirkeQueryHandlerTests()
    {
        _mockBezirkRepository = new Mock<IBezirkRepository>();
        _handler = new GetAllBezirkeQueryHandler(_mockBezirkRepository.Object);
    }

    [Fact]
    public async Task Handle_Should_Return_All_Bezirke_When_No_Filter_Applied()
    {
        // Arrange
        var query = new GetAllBezirkeQuery();
        var expectedBezirke = BezirkTestDataBuilder.CreateGermanCityDistricts(3);

        _mockBezirkRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedBezirke);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull("weil ein Ergebnis zurückgegeben werden sollte");
        result.IsSuccess.Should().BeTrue("weil die Abfrage erfolgreich sein sollte");
        result.Value.Should().NotBeNull("weil eine Liste zurückgegeben werden sollte");
        result.Value.Should().HaveCount(3, "weil 3 Bezirke zurückgegeben werden sollten");

        // Verify Repository-Aufruf
        _mockBezirkRepository.Verify(
            r => r.GetAllAsync(It.IsAny<CancellationToken>()), 
            Times.Once, 
            "weil GetAllAsync einmal aufgerufen werden sollte");
    }

    [Fact]
    public async Task Handle_Should_Return_Empty_List_When_No_Bezirke_Exist()
    {
        // Arrange
        var query = new GetAllBezirkeQuery();

        _mockBezirkRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Bezirk>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull("weil ein Ergebnis zurückgegeben werden sollte");
        result.IsSuccess.Should().BeTrue("weil eine leere Abfrage erfolgreich sein sollte");
        result.Value.Should().NotBeNull("weil eine Liste zurückgegeben werden sollte");
        result.Value.Should().BeEmpty("weil keine Bezirke vorhanden sind");
    }

    [Fact]
    public async Task Handle_Should_Map_Bezirke_To_DTOs_Correctly()
    {
        // Arrange
        var query = new GetAllBezirkeQuery();
        var bezirk = BezirkTestDataBuilder.Create()
            .WithId(1)
            .WithName("München-Mitte")
            .WithBeschreibung("Zentraler Bezirk")
            .ForMunich()
            .Build();

        _mockBezirkRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Bezirk> { bezirk });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull("weil ein Ergebnis zurückgegeben werden sollte");
        result.IsSuccess.Should().BeTrue("weil die Abfrage erfolgreich sein sollte");
        result.Value.Should().HaveCount(1, "weil ein Bezirk zurückgegeben werden sollte");

        var dto = result.Value.First();
        dto.Id.Should().Be(bezirk.Id, "weil die ID korrekt gemappt werden sollte");
        dto.Name.Should().Be(bezirk.Name, "weil der Name korrekt gemappt werden sollte");
        dto.Beschreibung.Should().Be(bezirk.Beschreibung, "weil die Beschreibung korrekt gemappt werden sollte");
        dto.Status.Should().Be(bezirk.Status, "weil der Status korrekt gemappt werden sollte");
        dto.ErstelltAm.Should().Be(bezirk.ErstelltAm, "weil das Erstellungsdatum korrekt gemappt werden sollte");
        dto.GeaendertAm.Should().Be(bezirk.GeaendertAm, "weil das Änderungsdatum korrekt gemappt werden sollte");
    }

    [Fact]
    public async Task Handle_Should_Handle_German_Special_Characters_In_Names()
    {
        // Arrange
        var query = new GetAllBezirkeQuery();
        var bezirke = new List<Bezirk>
        {
            BezirkTestDataBuilder.Create()
                .WithId(1)
                .WithName("Müller-Güntherstraße")
                .WithBeschreibung("Bereich für Gärtner in der Nähe des Fußballplatzes")
                .Build(),
            BezirkTestDataBuilder.Create()
                .WithId(2)
                .WithName("Gößweinstein-Süd")
                .WithBeschreibung("Südlicher Bereich von Gößweinstein")
                .Build()
        };

        _mockBezirkRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(bezirke);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull("weil ein Ergebnis zurückgegeben werden sollte");
        result.IsSuccess.Should().BeTrue("weil die Abfrage erfolgreich sein sollte");
        result.Value.Should().HaveCount(2, "weil 2 Bezirke mit deutschen Sonderzeichen zurückgegeben werden sollten");

        result.Value.Should().Contain(dto => dto.Name.Contains("ü") && dto.Name.Contains("ß"), 
            "weil deutsche Umlaute und Sonderzeichen korrekt verarbeitet werden sollten");
    }

    [Fact]
    public async Task Handle_Should_Handle_Repository_Exception_Gracefully()
    {
        // Arrange
        var query = new GetAllBezirkeQuery();

        _mockBezirkRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Datenbankverbindung fehlgeschlagen"));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull("weil ein Ergebnis zurückgegeben werden sollte");
        result.IsSuccess.Should().BeFalse("weil bei Datenbankfehlern ein Fehlerresultat zurückgegeben werden sollte");
        result.Error.Should().Contain("Datenbankverbindung fehlgeschlagen", 
            "weil die ursprüngliche Exception-Message enthalten sein sollte");
    }

    [Fact]
    public async Task Handle_Should_Respect_Cancellation_Token()
    {
        // Arrange
        var query = new GetAllBezirkeQuery();
        var cancellationToken = new CancellationToken(canceled: true);

        _mockBezirkRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Bezirk>());

        // Act & Assert
        var act = async () => await _handler.Handle(query, cancellationToken);

        await act.Should().ThrowAsync<OperationCanceledException>(
            "weil ein abgebrochener Token respektiert werden sollte");
    }

    [Fact]
    public async Task Handle_Should_Return_Bezirke_Ordered_By_Name()
    {
        // Arrange
        var query = new GetAllBezirkeQuery();
        var bezirke = new List<Bezirk>
        {
            BezirkTestDataBuilder.Create().WithId(1).WithName("Z-Bezirk").Build(),
            BezirkTestDataBuilder.Create().WithId(2).WithName("A-Bezirk").Build(),
            BezirkTestDataBuilder.Create().WithId(3).WithName("M-Bezirk").Build()
        };

        _mockBezirkRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(bezirke);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull("weil ein Ergebnis zurückgegeben werden sollte");
        result.IsSuccess.Should().BeTrue("weil die Abfrage erfolgreich sein sollte");
        result.Value.Should().HaveCount(3, "weil 3 Bezirke zurückgegeben werden sollten");

        var orderedNames = result.Value.Select(dto => dto.Name).ToList();
        orderedNames.Should().BeInAscendingOrder("weil die Bezirke alphabetisch sortiert sein sollten");
        orderedNames.First().Should().Be("A-Bezirk", "weil A-Bezirk zuerst kommen sollte");
        orderedNames.Last().Should().Be("Z-Bezirk", "weil Z-Bezirk zuletzt kommen sollte");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(100)]
    public async Task Handle_Should_Handle_Different_Amounts_Of_Bezirke(int bezirkeCount)
    {
        // Arrange
        var query = new GetAllBezirkeQuery();
        var bezirke = new List<Bezirk>();

        for (int i = 1; i <= bezirkeCount; i++)
        {
            bezirke.Add(BezirkTestDataBuilder.Create()
                .WithId(i)
                .WithName($"Bezirk-{i:D3}")
                .Build());
        }

        _mockBezirkRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(bezirke);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull("weil ein Ergebnis zurückgegeben werden sollte");
        result.IsSuccess.Should().BeTrue("weil die Abfrage erfolgreich sein sollte");
        result.Value.Should().HaveCount(bezirkeCount, $"weil {bezirkeCount} Bezirke zurückgegeben werden sollten");
        
        if (bezirkeCount > 0)
        {
            result.Value.All(dto => dto.Id > 0).Should().BeTrue("weil alle DTOs gültige IDs haben sollten");
            result.Value.All(dto => !string.IsNullOrEmpty(dto.Name)).Should().BeTrue("weil alle DTOs Namen haben sollten");
        }
    }

    [Fact]
    public async Task Handle_Should_Include_All_Required_Properties_In_DTOs()
    {
        // Arrange
        var query = new GetAllBezirkeQuery();
        var bezirk = BezirkTestDataBuilder.Create()
            .WithId(42)
            .WithName("Vollständiger-Bezirk")
            .WithBeschreibung("Bezirk mit allen Eigenschaften")
            .WithErstelltAm(DateTime.Now.AddDays(-30))
            .WithGeaendertAm(DateTime.Now.AddDays(-1))
            .Build();

        _mockBezirkRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Bezirk> { bezirk });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull("weil ein Ergebnis zurückgegeben werden sollte");
        result.IsSuccess.Should().BeTrue("weil die Abfrage erfolgreich sein sollte");
        result.Value.Should().HaveCount(1, "weil ein Bezirk zurückgegeben werden sollte");

        var dto = result.Value.First();
        dto.Id.Should().Be(42, "weil die ID vollständig gemappt werden sollte");
        dto.Name.Should().NotBeNullOrEmpty("weil der Name gemappt werden sollte");
        dto.Beschreibung.Should().NotBeNullOrEmpty("weil die Beschreibung gemappt werden sollte");
        dto.Status.Should().BeDefined("weil der Status gültig sein sollte");
        dto.ErstelltAm.Should().NotBe(default(DateTime), "weil das Erstellungsdatum gemappt werden sollte");
        dto.GeaendertAm.Should().NotBeNull("weil das Änderungsdatum gemappt werden sollte");
    }
}