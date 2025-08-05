using KGV.Domain.Entities;

namespace KGV.Tests.Integration.Shared;

/// <summary>
/// Interface für Test-Daten-Seeding in Integration Tests.
/// Ermöglicht das Setup realistischer deutscher Test-Daten für verschiedene Test-Szenarien.
/// </summary>
public interface ITestDataSeeder
{
    /// <summary>
    /// Seeded Standard-Test-Daten für allgemeine Tests.
    /// </summary>
    Task SeedAsync();

    /// <summary>
    /// Seeded spezifische Bezirke für Tests.
    /// </summary>
    Task<List<Bezirk>> SeedBezirkeAsync(int count = 5);

    /// <summary>
    /// Seeded spezifische Anträge für Tests.
    /// </summary>
    Task<List<Antrag>> SeedAntraegeAsync(int count = 10);

    /// <summary>
    /// Seeded eine komplette Test-Hierarchie (Bezirke -> Parzellen -> Anträge).
    /// </summary>
    Task SeedCompleteHierarchyAsync();

    /// <summary>
    /// Leert alle Test-Daten.
    /// </summary>
    Task CleanAsync();
}