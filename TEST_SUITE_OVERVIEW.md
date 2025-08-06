# 🧪 KGV Test Suite - Umfassende Test-Architektur

Moderne, production-ready Test-Suite für das KGV (Kleingartenverein) Backend-System mit .NET 9 und deutschen Lokalisierungs-Features.

## 📋 Überblick

Diese Test-Suite implementiert eine vollständige Test-Pyramide mit modernen .NET 9 Testing-Frameworks und spezieller Unterstützung für deutsche Anwendungsfälle:

```
                    🔺 E2E Tests
                   🔺🔺 Integration Tests  
                 🔺🔺🔺 Unit Tests
               🔺🔺🔺🔺 Architecture Tests
```

## 🏗️ Projekt-Struktur

```
src/
├── KGV.Tests.Unit/              # Unit Tests (Viele, Schnell)
│   ├── Domain/                  # Domain Model Tests
│   ├── Application/             # CQRS Handler Tests  
│   ├── Infrastructure/          # Repository Tests (Mocked)
│   └── Shared/                  # Test Utilities & Builders
├── KGV.Tests.Integration/       # Integration Tests (Wenige, Vollständig)
│   ├── Api/Controllers/         # HTTP Pipeline Tests
│   ├── Database/               # EF Core Integration Tests
│   ├── Performance/            # Load & Performance Tests
│   └── Shared/                 # TestServer & Seeding
├── KGV.Tests.Architecture/      # Architecture Tests (Regeln)
│   └── CleanArchitectureTests.cs
└── KGV.sln                     # Solution mit allen Test-Projekten
```

## 🛠️ Verwendete Frameworks

### Core Testing Frameworks
- **xUnit 2.9.2** - Modernes Test-Framework für .NET 9
- **FluentAssertions 6.12.1** - Ausdrucksstarke, lesbare Assertions
- **AutoFixture 4.18.1** - Automatische Test-Daten-Generierung
- **Moq 4.20.72** - Mocking-Framework der neuesten Generation

### Integration Testing  
- **Microsoft.AspNetCore.Mvc.Testing 9.0.0** - TestServer für HTTP Tests
- **Microsoft.EntityFrameworkCore.InMemory 9.0.0** - In-Memory Database
- **Testcontainers.PostgreSql 3.11.0** - Containerized Database Tests

### Architecture Testing
- **NetArchTest.Rules 1.3.2** - Architecture Rule Enforcement

### Code Coverage
- **coverlet.collector 6.0.2** - Code Coverage Collection für .NET 9

## 🇩🇪 Deutsche Lokalisierung

### Realistische Deutsche Test-Daten
```csharp
// Deutsche Städte und Bezirke
var bezirke = new[]
{
    ("München-Mitte", "Zentraler Bezirk der Landeshauptstadt München"),
    ("Berlin-Charlottenburg", "Westlicher Bezirk der Hauptstadt Berlin"),
    ("Hamburg-Altona", "Traditioneller Bezirk der Hansestadt Hamburg")
};

// Deutsche Namen und Adressen
var antraege = new[]
{
    ("Max", "Mustermann", "max.mustermann@beispiel.de", "Musterstraße 1", "80331", "München"),
    ("Anna", "Schmidt", "anna.schmidt@test.de", "Testweg 5", "10115", "Berlin")
};
```

### Deutsche Sonderzeichen Support
- **Umlaute**: äöüÄÖÜ vollständig unterstützt
- **Eszett**: ß in Namen und Beschreibungen
- **Encoding**: UTF-8 für alle Datenbank- und HTTP-Tests
- **Validation**: Spezielle Validierung für deutsche PLZ, Telefonnummern

### Deutsche Fehlermeldungen
```csharp
result.Should().BeTrue("weil deutsche Umlaute unterstützt werden sollten");
bezirk.Should().BeActive("weil neue Bezirke standardmäßig aktiv sind");
```

## 🚀 Ausführung

### Alle Tests ausführen
```bash
# Alle Test-Projekte
dotnet test

# Mit Code Coverage
dotnet test --collect:"XPlat Code Coverage"

# Parallel ausführen (Performance)
dotnet test --parallel
```

### Test-Kategorien einzeln
```bash
# Unit Tests (Schnell)
dotnet test KGV.Tests.Unit

# Integration Tests (Vollständig)  
dotnet test KGV.Tests.Integration

# Architecture Tests (Regeln)
dotnet test KGV.Tests.Architecture

# Performance Tests
dotnet test KGV.Tests.Integration --filter "Category=Performance"
```

### Mit detailliertem Output
```bash
dotnet test --logger "console;verbosity=detailed"
```

## 📊 Test-Kategorien im Detail

### 1. Unit Tests (KGV.Tests.Unit)
**Zweck**: Isolierte Tests einzelner Komponenten mit Mocking

**Abdeckung**:
- ✅ **Domain Models**: Bezirk, Antrag, Parzelle Entitäten
- ✅ **Value Objects**: Email, Address, PhoneNumber
- ✅ **CQRS Handlers**: Commands und Queries (gemockt)
- ✅ **Validators**: FluentValidation Rules
- ✅ **Repositories**: Interface-Tests mit Moq

**Beispiel**:
```csharp
[Fact]
public void Bezirk_Should_Support_German_Special_Characters()
{
    // Arrange
    var bezirk = BezirkTestDataBuilder.Create()
        .WithName("Gößweinstein-Süd")
        .WithBeschreibung("Bezirk für Gärtner in der Nähe")
        .Build();

    // Act & Assert
    bezirk.Should().ContainGermanCharacters("weil deutsche Umlaute unterstützt werden");
}
```

### 2. Integration Tests (KGV.Tests.Integration)
**Zweck**: End-to-End Tests der HTTP-Pipeline und Datenbank

**Abdeckung**:
- ✅ **HTTP API**: Vollständige Controller-Tests mit TestServer
- ✅ **Database**: EF Core mit In-Memory und TestContainers
- ✅ **Performance**: Response-Zeit und Load-Tests
- ✅ **Authentication**: Security und Authorization (wenn implementiert)

**Beispiel**:
```csharp
[Fact]
public async Task CreateBezirk_Should_Handle_German_Characters()
{
    // Arrange
    var command = new CreateBezirkCommand
    {
        Name = "Müller-Güntherstraße",
        Beschreibung = "Bereich für Gärtner"
    };

    // Act
    var response = await client.PostAsJsonAsync("/api/bezirke", command);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
}
```

### 3. Architecture Tests (KGV.Tests.Architecture)
**Zweck**: Durchsetzung der Clean Architecture Regeln

**Regeln**:
- ✅ **Dependency Rules**: Domain → Application → Infrastructure → API
- ✅ **Naming Conventions**: Controller, Handler, Repository Suffixe
- ✅ **Layer Isolation**: Keine Kreuz-Abhängigkeiten
- ✅ **German Domain Terms**: Verwendung deutscher Fachbegriffe

**Beispiel**:
```csharp
[Fact]
public void Domain_Should_Not_Have_Dependency_On_Infrastructure()
{
    var result = Types.InAssembly(DomainAssembly)
        .Should()
        .NotHaveDependencyOn("KGV.Infrastructure")
        .GetResult();

    result.IsSuccessful.Should().BeTrue("weil Domain unabhängig bleiben muss");
}
```

## 🧪 Test-Utilities und -Patterns

### TestDataBuilders (Builder Pattern)
```csharp
var bezirk = BezirkTestDataBuilder.Create()
    .WithName("München-Mitte")
    .WithBeschreibung("Zentraler Bezirk")
    .ForMunich()                    // Vorkonfigurierte deutsche Stadt
    .AsActive()                     // Status setzen
    .Build();
```

### Custom Assertions
```csharp
bezirk.Should().BeActive("weil neue Bezirke aktiv sein sollten");
bezirk.Should().ContainGermanCharacters("weil deutsche Zeichen unterstützt werden");
antrag.Should().HaveCompleteContactData("weil vollständige Daten erforderlich sind");
email.Should().HaveGermanDomain("weil deutsche E-Mail-Adressen bevorzugt werden");
```

### AutoFixture mit deutscher Lokalisierung
```csharp
[Theory]
[GermanAutoData]
public void Test_With_German_Generated_Data(string name, string beschreibung)
{
    // Test mit automatisch generierten deutschen Test-Daten
    name.Should().NotBeNullOrEmpty();
    // name wird automatisch mit deutschen Begriffen generiert
}
```

### TestServer Factory
```csharp
public class KgvWebApplicationFactory : WebApplicationFactory<Program>
{
    // In-Memory Database Setup
    // Deutsche Accept-Language Header
    // Test-spezifische Services
    // Automatische Datenbank-Bereinigung
}
```

## 📈 Performance Benchmarks

### Response Time Targets
- **GET Endpunkte**: < 1000ms
- **POST/PUT Endpunkte**: < 2000ms  
- **Concurrent Requests**: 20+ parallel
- **Large Datasets**: 100+ Bezirke < 2000ms

### Memory Usage
- **Stable Under Load**: < 50MB Increase nach 50 Requests
- **No Memory Leaks**: GC nach Test-Durchläufen

### Beispiel Performance Test
```csharp
[Fact]
public async Task GetAllBezirke_Should_Respond_Within_Time_Limit()
{
    const int maxResponseTimeMs = 1000;
    var stopwatch = Stopwatch.StartNew();

    var response = await client.GetAsync("/api/bezirke");
    stopwatch.Stop();

    stopwatch.ElapsedMilliseconds.Should().BeLessThan(maxResponseTimeMs);
}
```

## 🔧 Konfiguration

### xUnit Configuration
```xml
<!-- xunit.runner.json -->
{
  "methodDisplay": "method",
  "diagnosticMessages": true,
  "parallelizeAssembly": true,
  "parallelizeTestCollections": true,
  "maxParallelThreads": -1
}
```

### Test Konfiguration
```json
// appsettings.Testing.json
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

## 🚀 CI/CD Integration

### GitHub Actions Workflow
```yaml
name: Test Suite

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET 9
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'
    
    - name: Restore dependencies
      run: dotnet restore src/KGV.sln
    
    - name: Build
      run: dotnet build src/KGV.sln --no-restore
    
    - name: Unit Tests
      run: dotnet test src/KGV.Tests.Unit --no-build --verbosity normal
    
    - name: Integration Tests
      run: dotnet test src/KGV.Tests.Integration --no-build --verbosity normal
    
    - name: Architecture Tests
      run: dotnet test src/KGV.Tests.Architecture --no-build --verbosity normal
    
    - name: Code Coverage
      run: |
        dotnet test src/KGV.sln \
          --no-build \
          --collect:"XPlat Code Coverage" \
          --results-directory ./coverage
    
    - name: Upload Coverage
      uses: codecov/codecov-action@v3
      with:
        directory: ./coverage
```

### Docker Test Environment
```yaml
# docker-compose.test.yml
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

  redis-test:
    image: redis:7-alpine
    ports:
      - "6380:6379"
```

## 📊 Code Coverage Ziele

### Mindest-Abdeckung
- **Domain Layer**: 90%+ (Geschäftslogik kritisch)
- **Application Layer**: 85%+ (CQRS Handlers)
- **Infrastructure Layer**: 70%+ (Data Access)
- **API Layer**: 80%+ (Controller Logic)

### Ausschlüsse
- Generated Code (Migrations, etc.)
- Configuration Classes
- Program.cs und Startup.cs

## 🐛 Debugging und Troubleshooting

### Häufige Probleme

#### 1. Deutsche Zeichen in Tests
```csharp
// Problem: Encoding-Fehler bei deutschen Umlauten
// Lösung: UTF-8 explizit setzen
services.AddDbContext<KgvDbContext>(options =>
{
    options.UseInMemoryDatabase("TestDb");
    options.EnableSensitiveDataLogging(); // Für Test-Debugging
});
```

#### 2. Flaky Integration Tests
```csharp
// Problem: Tests schlagen sporadisch fehl
// Lösung: Proper cleanup und isolation
[Fact]
public async Task Test_With_Clean_Database()
{
    await _factory.CleanDatabaseAsync(); // Immer vor Test
    // Test logic...
}
```

#### 3. Performance Test Instabilität
```csharp
// Problem: Performance-Tests schwanken
// Lösung: Warm-up und mehrfache Messungen
[Fact]
public async Task Performance_Test_With_Warmup()
{
    // Warm-up request
    await client.GetAsync("/api/bezirke");
    
    // Actual measurement
    var times = new List<long>();
    for (int i = 0; i < 5; i++)
    {
        var stopwatch = Stopwatch.StartNew();
        await client.GetAsync("/api/bezirke");
        stopwatch.Stop();
        times.Add(stopwatch.ElapsedMilliseconds);
    }
    
    times.Average().Should().BeLessThan(1000);
}
```

## 📋 Checkliste für neue Tests

### Vor dem Schreiben
- [ ] **Test-Kategorie** bestimmen (Unit/Integration/Architecture)
- [ ] **Deutsche Test-Daten** vorbereiten
- [ ] **Abhängigkeiten** identifizieren (Mocking erforderlich?)
- [ ] **Edge Cases** definieren

### Beim Schreiben
- [ ] **Arrange-Act-Assert** Pattern
- [ ] **Descriptive Naming** mit Should/Wenn/Dann
- [ ] **Deutsche because-Texte** in Assertions
- [ ] **Single Responsibility** pro Test
- [ ] **Deterministic Results** (keine Zufallswerte)

### Nach dem Schreiben  
- [ ] **Test läuft grün** (lokal)
- [ ] **Test läuft in CI/CD**
- [ ] **Code Coverage** prüfen
- [ ] **Performance Impact** bewerten
- [ ] **Dokumentation** aktualisieren

## 🔗 Verwandte Dokumentation

- [KGV.Tests.Unit README](/src/KGV.Tests.Unit/README.md) - Unit Test Details
- [KGV.Tests.Integration README](/src/KGV.Tests.Integration/README.md) - Integration Test Details  
- [Clean Architecture Guide](/docs/architecture.md) - Architecture Principles
- [German Localization Guide](/docs/localization.md) - Deutsche Lokalisierung

## 🎯 Nächste Schritte

### Geplante Erweiterungen
- [ ] **E2E Tests** mit Playwright für Frontend
- [ ] **Contract Tests** mit Pact.NET für API Contracts
- [ ] **Chaos Engineering** mit Polly für Resilience Testing
- [ ] **Load Tests** mit NBomber für Skalierbarkeit
- [ ] **Security Tests** mit OWASP ZAP Integration

### Continuous Improvement
- [ ] **Test-Metriken** Dashboard (Test-Ausführungszeiten, Flakiness)
- [ ] **Mutation Testing** mit Stryker.NET
- [ ] **Property-based Testing** mit FsCheck
- [ ] **Visual Regression Testing** für UI-Komponenten

---

**Diese Test-Suite stellt sicher, dass das KGV Backend-System production-ready ist mit hoher Qualität, Performance und vollständiger Unterstützung für deutsche Anwendungsfälle. 🇩🇪**