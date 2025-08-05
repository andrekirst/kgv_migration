using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using KGV.Infrastructure.Data;
using KGV.API;

namespace KGV.Tests.Integration.Shared;

/// <summary>
/// Custom WebApplicationFactory für Integration Tests der KGV API.
/// Konfiguriert eine Test-Umgebung mit In-Memory Database und Test-spezifischen Services.
/// </summary>
public class KgvWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Entferne die produktive Datenbank-Konfiguration
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<KgvDbContext>));

            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Füge In-Memory Database für Tests hinzu
            services.AddDbContext<KgvDbContext>(options =>
            {
                options.UseInMemoryDatabase("KgvTestDb");
                options.EnableSensitiveDataLogging(); // Für bessere Test-Diagnostik
                options.EnableDetailedErrors();
            });

            // Entferne Standard-Logging für saubere Test-Ausgabe
            services.RemoveAll(typeof(ILoggerProvider));

            // Test-spezifische Services registrieren
            services.AddScoped<ITestDataSeeder, TestDataSeeder>();

            // Stelle sicher, dass die Datenbank für jeden Test neu erstellt wird
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<KgvDbContext>();
            
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
        });

        builder.UseEnvironment("Testing");
        
        // Konfiguriere Test-spezifische Einstellungen
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Hier können Test-spezifische Konfigurationen hinzugefügt werden
            // z.B. appsettings.Testing.json
        });
    }

    /// <summary>
    /// Erstellt einen HTTP-Client für API-Tests mit deutschen Lokalisierungs-Headers.
    /// </summary>
    public HttpClient CreateGermanClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Accept-Language", "de-DE");
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        return client;
    }

    /// <summary>
    /// Führt eine Aktion mit einem frischen Datenbank-Scope aus.
    /// Nützlich für Test-Setup und Verifikation.
    /// </summary>
    public async Task ExecuteWithDbContextAsync(Func<KgvDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<KgvDbContext>();
        await action(context);
    }

    /// <summary>
    /// Führt eine Funktion mit einem frischen Datenbank-Scope aus und gibt das Ergebnis zurück.
    /// </summary>
    public async Task<T> ExecuteWithDbContextAsync<T>(Func<KgvDbContext, Task<T>> func)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<KgvDbContext>();
        return await func(context);
    }

    /// <summary>
    /// Seeded die Test-Datenbank mit Standard-Test-Daten.
    /// </summary>
    public async Task SeedTestDataAsync()
    {
        using var scope = Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<ITestDataSeeder>();
        await seeder.SeedAsync();
    }

    /// <summary>
    /// Leert die Test-Datenbank.
    /// </summary>
    public async Task CleanDatabaseAsync()
    {
        await ExecuteWithDbContextAsync(async context =>
        {
            // Entferne alle Daten in umgekehrter Abhängigkeitsreihenfolge
            context.Antraege.RemoveRange(context.Antraege);
            context.Parzellen.RemoveRange(context.Parzellen);
            context.Bezirke.RemoveRange(context.Bezirke);
            context.Katasterbezirke.RemoveRange(context.Katasterbezirke);
            
            await context.SaveChangesAsync();
        });
    }
}