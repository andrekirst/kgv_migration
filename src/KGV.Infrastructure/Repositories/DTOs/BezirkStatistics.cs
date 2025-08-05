using KGV.Domain.Enums;

namespace KGV.Infrastructure.Repositories.DTOs;

/// <summary>
/// Statistics for Bezirk (District) entities
/// </summary>
public class BezirkStatistics
{
    /// <summary>
    /// Total number of districts
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Number of active districts
    /// </summary>
    public int ActiveCount { get; init; }

    /// <summary>
    /// Number of inactive districts
    /// </summary>
    public int InactiveCount { get; init; }

    /// <summary>
    /// Number of suspended districts
    /// </summary>
    public int SuspendedCount { get; init; }

    /// <summary>
    /// Number of archived districts
    /// </summary>
    public int ArchivedCount { get; init; }

    /// <summary>
    /// Number of districts under restructuring
    /// </summary>
    public int UnderRestructuringCount { get; init; }

    /// <summary>
    /// Total number of plots across all districts
    /// </summary>
    public int TotalPlotsCount { get; init; }

    /// <summary>
    /// Total area across all districts (in square meters)
    /// </summary>
    public decimal? TotalArea { get; init; }

    /// <summary>
    /// Average area per district
    /// </summary>
    public decimal? AverageAreaPerDistrict { get; init; }

    /// <summary>
    /// Average number of plots per district
    /// </summary>
    public decimal AveragePlotsPerDistrict { get; init; }

    /// <summary>
    /// Districts with the most plots
    /// </summary>
    public IReadOnlyList<BezirkPlotCount> TopDistrictsByPlotCount { get; init; } = new List<BezirkPlotCount>();

    /// <summary>
    /// Districts with free plots available
    /// </summary>
    public int DistrictsWithFreePlots { get; init; }

    /// <summary>
    /// Status distribution dictionary
    /// </summary>
    public IReadOnlyDictionary<BezirkStatus, int> StatusDistribution { get; init; } = new Dictionary<BezirkStatus, int>();

    /// <summary>
    /// Date when statistics were calculated
    /// </summary>
    public DateTime CalculatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the percentage of active districts
    /// </summary>
    public decimal ActivePercentage => TotalCount > 0 ? (decimal)ActiveCount / TotalCount * 100 : 0;

    /// <summary>
    /// Gets the percentage of districts with free plots
    /// </summary>
    public decimal DistrictsWithFreePlotsPercentage => TotalCount > 0 ? (decimal)DistrictsWithFreePlots / TotalCount * 100 : 0;
}

/// <summary>
/// Helper class for district plot count information
/// </summary>
public class BezirkPlotCount
{
    /// <summary>
    /// District ID
    /// </summary>
    public Guid BezirkId { get; init; }

    /// <summary>
    /// District name
    /// </summary>
    public required string BezirkName { get; init; }

    /// <summary>
    /// Display name of the district
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Number of plots in the district
    /// </summary>
    public int PlotCount { get; init; }

    /// <summary>
    /// District area in square meters
    /// </summary>
    public decimal? Area { get; init; }

    /// <summary>
    /// District status
    /// </summary>
    public BezirkStatus Status { get; init; }

    /// <summary>
    /// Gets the display name or falls back to name
    /// </summary>
    public string GetDisplayName() => !string.IsNullOrWhiteSpace(DisplayName) ? DisplayName : BezirkName;
}