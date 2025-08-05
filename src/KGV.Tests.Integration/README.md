# KGV Integration Tests

Dieses Projekt enthält Integration Tests für das KGV (Kleingartenverein) Backend-System, die die vollständige HTTP-Pipeline und Datenbank-Interaktionen testen.

## 🏗️ Struktur

```
KGV.Tests.Integration/
├── Api/
│   └── Controllers/         # HTTP API Controller Tests
├── Database/               # Datenbank Integration Tests
├── Performance/           # Performance und Load Tests
└── Shared/               # Test-Infrastruktur
    ├── KgvWebApplicationFactory.cs  # Custom TestServer Factory
    ├── TestDataSeeder.cs           # Deutsche Test-Daten
    └── ITestDataSeeder.cs         # Seeding Interface
```

## 🛠️ Verwendete Frameworks

- **Microsoft.AspNetCore.Mvc.Testing 9.0.0** - TestServer für HTTP Tests
- **Microsoft.EntityFrameworkCore.InMemory 9.0.0** - In-Memory Database
- **Testcontainers.PostgreSql 3.11.0** - PostgreSQL Container für realistische Tests
- **xUnit 2.9.2** - Test-Framework
- **FluentAssertions 6.12.1** - Ausdrucksstarke Assertions
- **AutoFixture 4.18.1** - Test-Daten-Generierung

## 🌐 TestServer Konfiguration

### KgvWebApplicationFactory
Konfiguriert eine isolierte Test-Umgebung:

```csharp
public class KgvWebApplicationFactory : WebApplicationFactory<Program>
{
    // In-Memory Database für Tests
    // Deutsche Lokalisierungs-Header
    // Test-spezifische Services
    // Automatische Datenbank-Bereinigung
}
```

### Verwendung
```csharp
[Fact]
public async Task GetBezirke_Should_Return_All_Bezirke()
{
    // Arrange
    var client = _factory.CreateGermanClient();
    await _factory.SeedTestDataAsync();

    // Act
    var response = await client.GetAsync("/api/bezirke");

    // Assert
    response.Should().BeSuccessful();
}
```

## 🇩🇪 Deutsche Test-Daten

### TestDataSeeder
Erstellt realistische deutsche Test-Daten:

- **Bezirke**: München-Mitte, Berlin-Charlottenburg, Hamburg-Altona
- **Anträge**: Deutsche Namen, Adressen, Telefonnummern
- **Parzellen**: Realistische Größen und Nummerierung
- **Vollständige Hierarchie**: Bezirke → Parzellen → Anträge

```csharp
public async Task<List<Bezirk>> SeedBezirkeAsync(int count = 5)
{
    var germanCities = new[]
    {
        ("München-Mitte", "Zentraler Bezirk der Landeshauptstadt München"),
        ("Berlin-Charlottenburg", "Westlicher Bezirk der Hauptstadt Berlin"),
        // ...weitere deutsche Städte
    };
    // Seeding-Logic...
}
```

## 🚀 Test-Kategorien

### API Controller Tests
Testen die vollständige HTTP-Pipeline:

- **GET** Endpunkte mit Queryparametern
- **POST** Endpunkte mit JSON-Payload
- **PUT/PATCH** Updates mit Validierung
- **DELETE** Operationen mit Abhängigkeitsprüfung
- **Fehlerbehandlung** (400, 404, 500)
- **Deutsche Lokalisierung** (Accept-Language: de-DE)

### Database Integration Tests
Testen Entity Framework Core mit realer Datenbank:

- **CRUD-Operationen** mit deutschen Sonderzeichen
- **Beziehungen** zwischen Entitäten
- **Transaktionen** und Rollbacks
- **Constraints** und Validierungen
- **Concurrency** und Performance

### Performance Tests
Messen Response-Zeiten und Skalierbarkeit:

- **Response Time** für kritische Endpunkte (< 1000ms)
- **Concurrent Requests** (20+ parallele Anfragen)
- **Large Datasets** (100+ Bezirke)
- **Memory Usage** unter Last
- **Batch Operations** Skalierung

## 🧪 Ausführung

### Alle Integration Tests
```bash
dotnet test KGV.Tests.Integration
```

### Spezifische Test-Kategorien
```bash
dotnet test --filter "Category=Api"
dotnet test --filter "Category=Database"
dotnet test --filter "Category=Performance"
```

### Mit TestContainers (PostgreSQL)
```bash
# Benötigt Docker
dotnet test --filter "Category=Database" --settings test.runsettings
```

### Performance Tests mit Output
```bash
dotnet test KGV.Tests.Integration/Performance/ --logger "console;verbosity=detailed"
```

## 📊 Test-Szenarien

### HTTP API Tests
```csharp
[Fact]
public async Task CreateBezirk_Should_Handle_German_Special_Characters()
{
    // Arrange
    var command = new CreateBezirkCommand
    {
        Name = "Gößweinstein-Süd",
        Beschreibung = "Bezirk für Gärtner in der Nähe des Fußballplatzes"
    };

    // Act
    var response = await client.PostAsJsonAsync("/api/bezirke", command);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
    var created = await response.Content.ReadFromJsonAsync<BezirkDto>();
    created!.Name.Should().Be(command.Name);
}
```

### Datenbank Tests
```csharp
[Fact] 
public async Task Database_Should_Save_German_Characters_Correctly()
{
    // Arrange
    var bezirk = new Bezirk 
    { 
        Name = "Müller-Güntherstraße",
        Beschreibung = "Bereich für Gärtner in der Nähe"
    };

    // Act
    await factory.ExecuteWithDbContextAsync(async context =>
    {
        context.Bezirke.Add(bezirk);
        await context.SaveChangesAsync();
    });

    // Assert
    var retrieved = await factory.ExecuteWithDbContextAsync(async context =>
        await context.Bezirke.FindAsync(bezirk.Id));
    
    retrieved!.Name.Should().Be("Müller-Güntherstraße");
}
```

### Performance Tests
```csharp
[Fact]
public async Task GetAllBezirke_Should_Respond_Within_Time_Limit()
{
    // Arrange
    const int maxResponseTimeMs = 1000;
    var stopwatch = Stopwatch.StartNew();

    // Act
    var response = await client.GetAsync("/api/bezirke");
    stopwatch.Stop();

    // Assert
    response.IsSuccessStatusCode.Should().BeTrue();
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(maxResponseTimeMs);
}
```

## 🔧 Konfiguration

### appsettings.Testing.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=KgvTestDb;Trusted_Connection=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

### TestContainers Setup
```csharp
private static PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
    .WithDatabase("kgv_test")
    .WithUsername("test_user")
    .WithPassword("test_password")
    .Build();
```

## 🐛 Debugging

### Test Server Logs
```csharp
builder.ConfigureServices(services =>
{
    // Enable detailed logging for tests
    services.AddLogging(logging =>
    {
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Debug);
    });
});
```

### Database Debugging
```csharp
services.AddDbContext<KgvDbContext>(options =>
{
    options.UseInMemoryDatabase("TestDb");
    options.EnableSensitiveDataLogging(); // Für Test-Debugging
    options.EnableDetailedErrors();
});
```

## 📈 Best Practices

### Test-Isolation
- Jeder Test startet mit sauberer Datenbank
- `CleanDatabaseAsync()` vor jedem Test
- Unabhängige Test-Daten pro Test

### Realistische Test-Daten
- Deutsche Namen und Adressen
- Gültige PLZ und Telefonnummern
- Realistische Bezirks- und Parzellen-Daten

### HTTP Client Setup
```csharp
public HttpClient CreateGermanClient()
{
    var client = CreateClient();
    client.DefaultRequestHeaders.Add("Accept-Language", "de-DE");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    return client;
}
```

### Error Handling Tests
```csharp
[Fact]
public async Task CreateBezirk_Should_Return_BadRequest_For_Duplicate_Name()
{
    // Test für Fehlerbehandlung mit deutschen Fehlermeldungen
    var errorContent = await response.Content.ReadAsStringAsync();
    errorContent.Should().Contain("bereits vorhanden");
}
```

## 🔗 CI/CD Integration

### GitHub Actions
```yaml
- name: Run Integration Tests
  run: |
    dotnet test KGV.Tests.Integration \
      --configuration Release \
      --logger trx \
      --collect:"XPlat Code Coverage"
```

### Docker Compose für CI
```yaml
version: '3.8'
services:
  postgres-test:
    image: postgres:15
    environment:
      POSTGRES_DB: kgv_test
      POSTGRES_USER: test_user
      POSTGRES_PASSWORD: test_password
    ports:
      - "5433:5432"
```

## 📋 Checkliste für neue Integration Tests

- [ ] **TestServer Setup** mit KgvWebApplicationFactory
- [ ] **Datenbank-Bereinigung** vor/nach Tests
- [ ] **Deutsche Test-Daten** mit TestDataSeeder
- [ ] **HTTP Status Codes** prüfen
- [ ] **Response Content** validieren
- [ ] **Performance Limits** definieren
- [ ] **Fehlerszenarien** testen
- [ ] **Concurrent Access** berücksichtigen

## 🔗 Verwandte Projekte

- **KGV.Tests.Unit** - Unit Tests mit Mocking
- **KGV.Tests.Architecture** - Architecture Tests
- **KGV.API** - Web API zu testende Schicht
- **KGV.Application** - Application Services
- **KGV.Infrastructure** - Data Access Layer