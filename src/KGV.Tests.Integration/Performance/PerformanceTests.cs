using System.Diagnostics;
using FluentAssertions;
using KGV.Tests.Integration.Shared;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace KGV.Tests.Integration.Performance;

/// <summary>
/// Performance Tests für kritische API-Endpunkte.
/// Testet Response-Zeiten, Throughput und Memory-Usage unter verschiedenen Lasten.
/// </summary>
public class PerformanceTests : IClassFixture<KgvWebApplicationFactory>
{
    private readonly KgvWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;
    private readonly HttpClient _client;

    public PerformanceTests(KgvWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
        _client = _factory.CreateGermanClient();
    }

    [Fact]
    public async Task GetAllBezirke_Should_Respond_Within_Acceptable_Time_Limit()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        await _factory.SeedTestDataAsync();

        const int maxResponseTimeMs = 1000; // 1 Sekunde Maximum
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/api/bezirke");
        stopwatch.Stop();

        // Assert
        response.Should().NotBeNull("weil eine Antwort erwartet wird");
        response.IsSuccessStatusCode.Should().BeTrue("weil die Anfrage erfolgreich sein sollte");
        
        var responseTime = stopwatch.ElapsedMilliseconds;
        _output.WriteLine($"GetAllBezirke Response Time: {responseTime}ms");
        
        responseTime.Should().BeLessThan(maxResponseTimeMs, 
            $"weil die Antwortzeit unter {maxResponseTimeMs}ms liegen sollte für gute User Experience");
    }

    [Fact]
    public async Task GetAllBezirke_Should_Handle_Large_Dataset_Efficiently()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        
        // Erstelle viele Bezirke für Performance-Test
        await _factory.ExecuteWithDbContextAsync(async context =>
        {
            var seeder = _factory.Services.GetRequiredService<ITestDataSeeder>();
            await seeder.SeedBezirkeAsync(100); // 100 Bezirke
        });

        const int maxResponseTimeMs = 2000; // 2 Sekunden für große Datenmenge
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/api/bezirke");
        stopwatch.Stop();

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue("weil die Anfrage erfolgreich sein sollte");
        
        var responseTime = stopwatch.ElapsedMilliseconds;
        _output.WriteLine($"GetAllBezirke (100 items) Response Time: {responseTime}ms");
        
        responseTime.Should().BeLessThan(maxResponseTimeMs, 
            "weil auch große Datenmengen effizient verarbeitet werden sollten");
    }

    [Fact]
    public async Task Concurrent_Requests_Should_Maintain_Performance()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        await _factory.SeedTestDataAsync();

        const int numberOfConcurrentRequests = 20;
        const int maxAverageResponseTimeMs = 1500; // 1.5 Sekunden Durchschnitt

        var tasks = new List<Task<(bool Success, long ResponseTime)>>();

        // Act
        for (int i = 0; i < numberOfConcurrentRequests; i++)
        {
            var task = MeasureRequestTime("/api/bezirke");
            tasks.Add(task);
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        var successfulRequests = results.Count(r => r.Success);
        var averageResponseTime = results.Where(r => r.Success).Average(r => r.ResponseTime);

        _output.WriteLine($"Concurrent Requests: {numberOfConcurrentRequests}");
        _output.WriteLine($"Successful Requests: {successfulRequests}");
        _output.WriteLine($"Average Response Time: {averageResponseTime:F1}ms");

        successfulRequests.Should().Be(numberOfConcurrentRequests, 
            "weil alle parallelen Anfragen erfolgreich sein sollten");
        
        averageResponseTime.Should().BeLessThan(maxAverageResponseTimeMs, 
            "weil die durchschnittliche Antwortzeit auch bei parallelen Anfragen akzeptabel sein sollte");
    }

    [Fact]
    public async Task CreateBezirk_Should_Complete_Within_Time_Limit()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();

        var createCommand = new
        {
            Name = "Performance-Test-Bezirk",
            Beschreibung = "Bezirk für Performance-Tests mit deutscher Beschreibung"
        };

        const int maxResponseTimeMs = 2000; // 2 Sekunden für Write-Operation
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _client.PostAsJsonAsync("/api/bezirke", createCommand);
        stopwatch.Stop();

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue("weil die Erstellung erfolgreich sein sollte");
        
        var responseTime = stopwatch.ElapsedMilliseconds;
        _output.WriteLine($"CreateBezirk Response Time: {responseTime}ms");
        
        responseTime.Should().BeLessThan(maxResponseTimeMs, 
            "weil Write-Operationen zeitnah abgeschlossen werden sollten");
    }

    [Fact]
    public async Task Batch_Creation_Should_Scale_Linearly()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();

        var batchSizes = new[] { 5, 10, 20 };
        var results = new List<(int BatchSize, long TotalTime, double AvgTimePerItem)>();

        // Act
        foreach (var batchSize in batchSizes)
        {
            await _factory.CleanDatabaseAsync(); // Clean zwischen Batches
            
            var stopwatch = Stopwatch.StartNew();
            var tasks = new List<Task>();

            for (int i = 1; i <= batchSize; i++)
            {
                var createCommand = new
                {
                    Name = $"Batch-Bezirk-{i:D3}",
                    Beschreibung = $"Bezirk {i} für Batch-Performance-Test"
                };

                var task = _client.PostAsJsonAsync("/api/bezirke", createCommand);
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
            stopwatch.Stop();

            var totalTime = stopwatch.ElapsedMilliseconds;
            var avgTimePerItem = (double)totalTime / batchSize;
            
            results.Add((batchSize, totalTime, avgTimePerItem));
            
            _output.WriteLine($"Batch Size: {batchSize}, Total Time: {totalTime}ms, Avg per Item: {avgTimePerItem:F1}ms");
        }

        // Assert
        // Prüfe, dass die Performance nicht drastisch verschlechtert
        var firstBatchAvg = results.First().AvgTimePerItem;
        var lastBatchAvg = results.Last().AvgTimePerItem;
        
        var performanceDegradation = lastBatchAvg / firstBatchAvg;
        
        performanceDegradation.Should().BeLessThan(3.0, 
            "weil die Performance pro Item nicht mehr als 3x schlechter werden sollte bei größeren Batches");
    }

    [Fact]
    public async Task Database_Query_Performance_Should_Be_Consistent()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        await _factory.ExecuteWithDbContextAsync(async context =>
        {
            var seeder = _factory.Services.GetRequiredService<ITestDataSeeder>();
            await seeder.SeedCompleteHierarchyAsync(); // Vollständige Hierarchie für realistische Tests
        });

        const int numberOfQueries = 10;
        const int maxResponseTimeVariance = 500; // 500ms Varianz Maximum

        var responseTimes = new List<long>();

        // Act
        for (int i = 0; i < numberOfQueries; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await _client.GetAsync("/api/bezirke");
            stopwatch.Stop();

            response.IsSuccessStatusCode.Should().BeTrue("weil alle Queries erfolgreich sein sollten");
            responseTimes.Add(stopwatch.ElapsedMilliseconds);
        }

        // Assert
        var averageTime = responseTimes.Average();
        var minTime = responseTimes.Min();
        var maxTime = responseTimes.Max();
        var variance = maxTime - minTime;

        _output.WriteLine($"Query Performance - Avg: {averageTime:F1}ms, Min: {minTime}ms, Max: {maxTime}ms, Variance: {variance}ms");

        variance.Should().BeLessThan(maxResponseTimeVariance, 
            "weil die Antwortzeiten konsistent sein sollten ohne große Schwankungen");
        
        averageTime.Should().BeLessThan(1000, 
            "weil die durchschnittliche Antwortzeit unter 1 Sekunde liegen sollte");
    }

    [Fact]
    public async Task Memory_Usage_Should_Remain_Stable_Under_Load()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        await _factory.SeedTestDataAsync();

        const int numberOfRequests = 50;
        
        // Measure initial memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var initialMemory = GC.GetTotalMemory(false);

        // Act
        for (int i = 0; i < numberOfRequests; i++)
        {
            var response = await _client.GetAsync("/api/bezirke");
            response.IsSuccessStatusCode.Should().BeTrue();
            
            // Dispose response to avoid accumulation
            response.Dispose();
        }

        // Force garbage collection before measuring
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        var finalMemory = GC.GetTotalMemory(false);
        var memoryIncrease = finalMemory - initialMemory;
        var memoryIncreaseMB = memoryIncrease / (1024.0 * 1024.0);

        // Assert
        _output.WriteLine($"Memory Usage - Initial: {initialMemory / (1024.0 * 1024.0):F1}MB, Final: {finalMemory / (1024.0 * 1024.0):F1}MB");
        _output.WriteLine($"Memory Increase: {memoryIncreaseMB:F1}MB after {numberOfRequests} requests");

        memoryIncreaseMB.Should().BeLessThan(50.0, 
            "weil der Speicherverbrauch nicht signifikant ansteigen sollte bei wiederholten Anfragen");
    }

    private async Task<(bool Success, long ResponseTime)> MeasureRequestTime(string endpoint)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await _client.GetAsync(endpoint);
            stopwatch.Stop();

            return (response.IsSuccessStatusCode, stopwatch.ElapsedMilliseconds);
        }
        catch
        {
            return (false, 0);
        }
    }
}