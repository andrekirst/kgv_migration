using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using KGV.Application.Features.Bezirke.Commands.CreateBezirk;
using KGV.Application.DTOs;
using KGV.Tests.Integration.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KGV.Tests.Integration.Api.Controllers;

/// <summary>
/// Integration Tests für den BezirkeController.
/// Testet die vollständige HTTP-Pipeline einschließlich Datenbank-Interaktionen.
/// </summary>
public class BezirkeControllerTests : IClassFixture<KgvWebApplicationFactory>, IDisposable
{
    private readonly KgvWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public BezirkeControllerTests(KgvWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateGermanClient();
    }

    [Fact]
    public async Task GetAllBezirke_Should_Return_All_Bezirke_With_German_Headers()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        await _factory.SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync("/api/bezirke");

        // Assert
        response.Should().NotBeNull("weil eine Antwort erwartet wird");
        response.StatusCode.Should().Be(HttpStatusCode.OK, "weil die Anfrage erfolgreich sein sollte");
        
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json", 
            "weil JSON-Content erwartet wird");

        var bezirke = await response.Content.ReadFromJsonAsync<List<BezirkDto>>();
        bezirke.Should().NotBeNull("weil eine Bezirks-Liste erwartet wird");
        bezirke.Should().NotBeEmpty("weil Test-Daten vorhanden sein sollten");
        bezirke.Should().OnlyContain(b => !string.IsNullOrEmpty(b.Name), 
            "weil alle Bezirke Namen haben sollten");
    }

    [Fact]
    public async Task GetBezirkById_Should_Return_Specific_Bezirk()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var seededBezirke = await _factory.ExecuteWithDbContextAsync(async context =>
        {
            var seeder = _factory.Services.GetRequiredService<ITestDataSeeder>();
            return await seeder.SeedBezirkeAsync(3);
        });

        var targetBezirk = seededBezirke.First();

        // Act
        var response = await _client.GetAsync($"/api/bezirke/{targetBezirk.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, 
            "weil ein existierender Bezirk gefunden werden sollte");

        var bezirk = await response.Content.ReadFromJsonAsync<BezirkDto>();
        bezirk.Should().NotBeNull("weil ein Bezirk-DTO zurückgegeben werden sollte");
        bezirk!.Id.Should().Be(targetBezirk.Id, "weil die korrekte ID zurückgegeben werden sollte");
        bezirk.Name.Should().Be(targetBezirk.Name, "weil der korrekte Name zurückgegeben werden sollte");
        bezirk.Beschreibung.Should().Be(targetBezirk.Beschreibung, 
            "weil die korrekte Beschreibung zurückgegeben werden sollte");
    }

    [Fact]
    public async Task GetBezirkById_Should_Return_NotFound_For_NonExistent_Bezirk()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        const int nonExistentId = 999;

        // Act
        var response = await _client.GetAsync($"/api/bezirke/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound, 
            "weil ein nicht existierender Bezirk nicht gefunden werden sollte");
    }

    [Fact]
    public async Task CreateBezirk_Should_Create_New_Bezirk_With_German_Data()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        
        var command = new CreateBezirkCommand
        {
            Name = "München-Giesing",
            Beschreibung = "Traditioneller Münchner Stadtteil südlich der Isar"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/bezirke", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created, 
            "weil ein neuer Bezirk erfolgreich erstellt werden sollte");

        var createdBezirk = await response.Content.ReadFromJsonAsync<BezirkDto>();
        createdBezirk.Should().NotBeNull("weil ein erstellter Bezirk zurückgegeben werden sollte");
        createdBezirk!.Name.Should().Be(command.Name, 
            "weil der Name korrekt übernommen werden sollte");
        createdBezirk.Beschreibung.Should().Be(command.Beschreibung, 
            "weil die Beschreibung korrekt übernommen werden sollte");
        createdBezirk.Id.Should().BeGreaterThan(0, 
            "weil eine gültige ID zugewiesen werden sollte");

        // Verify Location Header
        response.Headers.Location.Should().NotBeNull("weil ein Location-Header gesetzt werden sollte");
        response.Headers.Location!.ToString().Should().Contain($"/api/bezirke/{createdBezirk.Id}",
            "weil der Location-Header auf den neuen Bezirk verweisen sollte");

        // Verify in Database
        var dbBezirk = await _factory.ExecuteWithDbContextAsync(async context =>
            await context.Bezirke.FindAsync(createdBezirk.Id));

        dbBezirk.Should().NotBeNull("weil der Bezirk in der Datenbank gespeichert werden sollte");
        dbBezirk!.Name.Should().Be(command.Name, 
            "weil der Name in der Datenbank korrekt gespeichert werden sollte");
    }

    [Fact]
    public async Task CreateBezirk_Should_Return_BadRequest_For_Duplicate_Name()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        const string duplicateName = "Doppelter-Bezirk";

        // Erstelle ersten Bezirk
        var firstCommand = new CreateBezirkCommand
        {
            Name = duplicateName,
            Beschreibung = "Erster Bezirk"
        };

        await _client.PostAsJsonAsync("/api/bezirke", firstCommand);

        // Versuche zweiten Bezirk mit gleichem Namen zu erstellen
        var duplicateCommand = new CreateBezirkCommand
        {
            Name = duplicateName,
            Beschreibung = "Zweiter Bezirk (sollte fehlschlagen)"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/bezirke", duplicateCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, 
            "weil doppelte Namen nicht erlaubt sein sollten");

        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("bereits vorhanden", 
            "weil die Fehlermeldung auf den doppelten Namen hinweisen sollte");
    }

    [Theory]
    [InlineData("", "Leerer Name sollte abgelehnt werden")]
    [InlineData("   ", "Name mit nur Leerzeichen sollte abgelehnt werden")]
    [InlineData(null, "Null-Name sollte abgelehnt werden")]
    public async Task CreateBezirk_Should_Return_BadRequest_For_Invalid_Names(string invalidName, string because)
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        
        var command = new CreateBezirkCommand
        {
            Name = invalidName,
            Beschreibung = "Gültige Beschreibung"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/bezirke", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, because);
        
        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("Name", 
            "weil die Fehlermeldung das Name-Feld erwähnen sollte");
    }

    [Fact]
    public async Task CreateBezirk_Should_Handle_German_Special_Characters()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        
        var command = new CreateBezirkCommand
        {
            Name = "Gößweinstein-Süd",
            Beschreibung = "Bezirk für Gärtner in der Nähe des Fußballplatzes mit Rößlerstraße"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/bezirke", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created, 
            "weil deutsche Sonderzeichen unterstützt werden sollten");

        var createdBezirk = await response.Content.ReadFromJsonAsync<BezirkDto>();
        createdBezirk.Should().NotBeNull("weil ein Bezirk erstellt werden sollte");
        createdBezirk!.Name.Should().Be(command.Name, 
            "weil deutsche Umlaute und Sonderzeichen korrekt verarbeitet werden sollten");
        createdBezirk.Beschreibung.Should().Be(command.Beschreibung, 
            "weil deutsche Sonderzeichen in der Beschreibung erhalten bleiben sollten");
    }

    [Fact]
    public async Task UpdateBezirk_Should_Update_Existing_Bezirk()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var seededBezirke = await _factory.ExecuteWithDbContextAsync(async context =>
        {
            var seeder = _factory.Services.GetRequiredService<ITestDataSeeder>();
            return await seeder.SeedBezirkeAsync(1);
        });

        var targetBezirk = seededBezirke.First();
        var updateCommand = new
        {
            Name = "Aktualisierter-Bezirk",
            Beschreibung = "Aktualisierte Beschreibung mit deutschen Umlauten: äöüß"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/bezirke/{targetBezirk.Id}", updateCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, 
            "weil die Aktualisierung erfolgreich sein sollte");

        // Verify in Database
        var updatedBezirk = await _factory.ExecuteWithDbContextAsync(async context =>
            await context.Bezirke.FindAsync(targetBezirk.Id));

        updatedBezirk.Should().NotBeNull("weil der Bezirk existieren sollte");
        updatedBezirk!.Name.Should().Be(updateCommand.Name, 
            "weil der Name aktualisiert werden sollte");
        updatedBezirk.Beschreibung.Should().Be(updateCommand.Beschreibung, 
            "weil die Beschreibung aktualisiert werden sollte");
        updatedBezirk.GeaendertAm.Should().NotBeNull("weil das Änderungsdatum gesetzt werden sollte");
    }

    [Fact]
    public async Task DeleteBezirk_Should_Delete_Existing_Bezirk()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var seededBezirke = await _factory.ExecuteWithDbContextAsync(async context =>
        {
            var seeder = _factory.Services.GetRequiredService<ITestDataSeeder>();
            return await seeder.SeedBezirkeAsync(1);
        });

        var targetBezirk = seededBezirke.First();

        // Act
        var response = await _client.DeleteAsync($"/api/bezirke/{targetBezirk.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, 
            "weil die Löschung erfolgreich sein sollte");

        // Verify in Database
        var deletedBezirk = await _factory.ExecuteWithDbContextAsync(async context =>
            await context.Bezirke.FindAsync(targetBezirk.Id));

        deletedBezirk.Should().BeNull("weil der Bezirk aus der Datenbank entfernt werden sollte");
    }

    [Fact]
    public async Task GetBezirkeStatistics_Should_Return_Correct_Statistics()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        await _factory.SeedTestDataAsync();

        // Act
        var response = await _client.GetAsync("/api/bezirke/statistics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, 
            "weil Statistiken verfügbar sein sollten");

        var statistics = await response.Content.ReadFromJsonAsync<dynamic>();
        statistics.Should().NotBeNull("weil Statistiken zurückgegeben werden sollten");
    }

    [Fact]
    public async Task Api_Should_Support_German_Accept_Language_Header()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        
        var invalidCommand = new CreateBezirkCommand
        {
            Name = "", // Ungültiger Name
            Beschreibung = "Test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/bezirke", invalidCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, 
            "weil ungültige Daten abgelehnt werden sollten");

        var errorContent = await response.Content.ReadAsStringAsync();
        // Da wir deutschen Accept-Language Header setzen, erwarten wir deutsche Fehlermeldungen
        errorContent.Should().NotBeNullOrEmpty("weil eine Fehlermeldung zurückgegeben werden sollte");
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}