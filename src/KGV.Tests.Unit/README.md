# KGV Unit Tests

Dieses Projekt enthält Unit Tests für das KGV (Kleingartenverein) Backend-System mit modernen .NET 9 Testing-Frameworks.

## 🏗️ Struktur

```
KGV.Tests.Unit/
├── Domain/                    # Tests für Domain-Schicht
│   ├── Entities/             # Entity-Tests (Bezirk, Antrag, etc.)
│   └── ValueObjects/         # Value Object-Tests (Email, Address, etc.)
├── Application/              # Tests für Application-Schicht
│   └── Features/            # CQRS Command/Query Handler Tests
├── Infrastructure/          # Tests für Infrastructure-Schicht
│   └── Repositories/        # Repository-Tests mit Mocking
└── Shared/                  # Gemeinsame Test-Utilities
    ├── TestDataBuilders/    # Builder Pattern für Test-Daten
    └── CustomAssertions.cs  # KGV-spezifische Assertions
```

## 🛠️ Verwendete Frameworks

- **xUnit 2.9.2** - Test-Framework für .NET 9
- **FluentAssertions 6.12.1** - Ausdrucksstarke Assertions
- **Moq 4.20.72** - Mocking-Framework
- **AutoFixture 4.18.1** - Automatische Test-Daten-Generierung
- **Microsoft.EntityFrameworkCore.InMemory 9.0.0** - In-Memory Database für Tests

## 🇩🇪 Deutsche Lokalisierung

Die Tests sind speziell für deutsche Anwendungsfälle entwickelt:

- **Deutsche Test-Daten**: Realistische deutsche Namen, Adressen und Städte
- **Umlaute & Sonderzeichen**: Tests für äöüß und andere deutsche Zeichen
- **Deutsche Fehlermeldungen**: Assertions mit deutschen "because"-Texten
- **Fachbegriffe**: Verwendung deutscher Domänen-Begriffe (Bezirk, Parzelle, Antrag)

## 🚀 Ausführung

### Alle Tests ausführen
```bash
dotnet test
```

### Spezifische Test-Kategorie
```bash
dotnet test --filter "Category=Domain"
dotnet test --filter "Category=Application"
```

### Mit Code Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Parallel ausführen
```bash
dotnet test --parallel
```

## 📊 Test-Kategorien

### Domain Tests
- **Entity Tests**: Geschäftslogik, Validierung, Verhalten
- **Value Object Tests**: Gleichheit, Unveränderlichkeit, Validierung
- **Enum Tests**: Gültige Werte, Konvertierungen

### Application Tests  
- **Command Handler Tests**: CQRS Commands mit Mocking
- **Query Handler Tests**: CQRS Queries mit Test-Daten
- **Validator Tests**: FluentValidation Rules
- **Mapping Tests**: AutoMapper Profile Validierung

### Infrastructure Tests
- **Repository Tests**: Datenbank-Interaktionen (gemockt)
- **Service Tests**: Externe Dependencies (gemockt)

## 🧪 Test-Utilities

### TestDataBuilders
Implementieren das Builder Pattern für realistische deutsche Test-Daten:

```csharp
var bezirk = BezirkTestDataBuilder.Create()
    .WithName("München-Mitte")
    .WithBeschreibung("Zentraler Bezirk der Landeshauptstadt")
    .ForMunich()
    .Build();
```

### Custom Assertions
KGV-spezifische Assertions für bessere Lesbarkeit:

```csharp
bezirk.Should().BeActive("weil neue Bezirke standardmäßig aktiv sind");
bezirk.Should().ContainGermanCharacters("weil deutsche Umlaute unterstützt werden");
antrag.Should().HaveCompleteContactData("weil vollständige Daten erforderlich sind");
```

### AutoFixture Integration
Automatische deutsche Test-Daten-Generierung:

```csharp
[Theory]
[GermanAutoData]
public void Test_With_German_Data(string name, string beschreibung)
{
    // Test mit automatisch generierten deutschen Daten
}
```

## 📈 Best Practices

### Arrange-Act-Assert Pattern
```csharp
[Fact]
public void Should_Create_Valid_Bezirk()
{
    // Arrange
    var name = "Test-Bezirk";
    var beschreibung = "Test-Beschreibung";

    // Act  
    var bezirk = new Bezirk(name, beschreibung);

    // Assert
    bezirk.Should().NotBeNull();
    bezirk.Name.Should().Be(name);
}
```

### Mocking mit Moq
```csharp
private readonly Mock<IBezirkRepository> _mockRepository;

// Setup
_mockRepository
    .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(expectedBezirk);

// Verify
_mockRepository.Verify(
    r => r.SaveAsync(It.IsAny<Bezirk>(), It.IsAny<CancellationToken>()),
    Times.Once);
```

### Test-Isolation
- Jeder Test ist unabhängig und isoliert
- Tests verwenden In-Memory Datenbank oder Mocks
- Keine Abhängigkeiten zwischen Tests
- Deterministische Ergebnisse

## 🐛 Debugging

### Test Explorer (Visual Studio/Rider)
- Tests einzeln ausführen und debuggen
- Breakpoints in Test-Code setzen
- Live Unit Testing für sofortiges Feedback

### Logging in Tests
```csharp
public class TestsWithLogging : ITestOutputHelper
{
    private readonly ITestOutputHelper _output;
    
    public TestsWithLogging(ITestOutputHelper output)
    {
        _output = output;
    }
    
    [Fact]
    public void Test_With_Logging()
    {
        _output.WriteLine("Debug-Information für Test");
        // Test-Code...
    }
}
```

## 📋 Checkliste für neue Tests

- [ ] **Arrange-Act-Assert** Pattern verwenden
- [ ] **Deutsche because**-Texte in Assertions
- [ ] **TestDataBuilder** für komplexe Objekte
- [ ] **Realistische deutsche Test-Daten**
- [ ] **Edge Cases** und Fehlerfälle testen
- [ ] **Mocking** für externe Dependencies
- [ ] **Descriptive Test-Namen** mit Soll/Should
- [ ] **Single Responsibility** - ein Test, ein Verhalten

## 🔗 Verwandte Projekte

- **KGV.Tests.Integration** - Integration Tests mit TestServer
- **KGV.Tests.Architecture** - Architecture Tests mit NetArchTest
- **KGV.API** - Web API Layer
- **KGV.Application** - Application Services (CQRS)
- **KGV.Domain** - Domain Models und Business Logic
- **KGV.Infrastructure** - Data Access und externe Services