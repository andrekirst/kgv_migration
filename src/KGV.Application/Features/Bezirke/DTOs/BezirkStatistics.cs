namespace KGV.Application.Features.Bezirke.DTOs;

/// <summary>
/// Statistics for Bezirke (Districts)
/// </summary>
public class BezirkStatistics
{
    /// <summary>
    /// Total number of districts
    /// </summary>
    public int TotalBezirke { get; set; }

    /// <summary>
    /// Number of active districts
    /// </summary>
    public int ActiveBezirke { get; set; }

    /// <summary>
    /// Number of inactive districts
    /// </summary>
    public int InactiveBezirke { get; set; }

    /// <summary>
    /// Number of suspended districts
    /// </summary>
    public int SuspendedBezirke { get; set; }

    /// <summary>
    /// Number of archived districts
    /// </summary>
    public int ArchivedBezirke { get; set; }

    /// <summary>
    /// Total area of all districts (in m²)
    /// </summary>
    public decimal? TotalFlaeche { get; set; }

    /// <summary>
    /// Average area per district (in m²)
    /// </summary>
    public decimal? AverageFlaeche { get; set; }

    /// <summary>
    /// Total number of Parzellen across all districts
    /// </summary>
    public int TotalParzellen { get; set; }

    /// <summary>
    /// Average number of Parzellen per district
    /// </summary>
    public decimal AverageParzellen { get; set; }

    /// <summary>
    /// District with the most Parzellen
    /// </summary>
    public BezirkStatisticItem? LargestBezirk { get; set; }

    /// <summary>
    /// District with the largest area
    /// </summary>
    public BezirkStatisticItem? LargestAreaBezirk { get; set; }

    /// <summary>
    /// When the statistics were calculated
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Individual district statistic item
/// </summary>
public class BezirkStatisticItem
{
    /// <summary>
    /// District ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// District name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Number of Parzellen in this district
    /// </summary>
    public int ParzellenCount { get; set; }

    /// <summary>
    /// Total area (in m²)
    /// </summary>
    public decimal? Flaeche { get; set; }
}