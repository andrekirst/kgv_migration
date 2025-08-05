using KGV.Domain.Enums;

namespace KGV.Infrastructure.Repositories.DTOs;

/// <summary>
/// Statistics for Parzelle (Plot) entities
/// </summary>
public class ParzellenStatistics
{
    /// <summary>
    /// Total number of plots
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Number of available plots
    /// </summary>
    public int AvailableCount { get; init; }

    /// <summary>
    /// Number of reserved plots
    /// </summary>
    public int ReservedCount { get; init; }

    /// <summary>
    /// Number of assigned plots
    /// </summary>
    public int AssignedCount { get; init; }

    /// <summary>
    /// Number of unavailable plots
    /// </summary>
    public int UnavailableCount { get; init; }

    /// <summary>
    /// Number of plots under development
    /// </summary>
    public int UnderDevelopmentCount { get; init; }

    /// <summary>
    /// Number of decommissioned plots
    /// </summary>
    public int DecommissionedCount { get; init; }

    /// <summary>
    /// Number of plots pending approval
    /// </summary>
    public int PendingApprovalCount { get; init; }

    /// <summary>
    /// Total area of all plots (in square meters)
    /// </summary>
    public decimal TotalArea { get; init; }

    /// <summary>
    /// Average plot size
    /// </summary>
    public decimal AveragePlotSize { get; init; }

    /// <summary>
    /// Minimum plot size
    /// </summary>
    public decimal MinPlotSize { get; init; }

    /// <summary>
    /// Maximum plot size
    /// </summary>
    public decimal MaxPlotSize { get; init; }

    /// <summary>
    /// Number of plots with water access
    /// </summary>
    public int PlotsWithWater { get; init; }

    /// <summary>
    /// Number of plots with electricity access
    /// </summary>
    public int PlotsWithElectricity { get; init; }

    /// <summary>
    /// Number of plots with both water and electricity
    /// </summary>
    public int PlotsWithBothUtilities { get; init; }

    /// <summary>
    /// Average price of plots (if available)
    /// </summary>
    public decimal? AveragePrice { get; init; }

    /// <summary>
    /// Minimum price of plots (if available)
    /// </summary>
    public decimal? MinPrice { get; init; }

    /// <summary>
    /// Maximum price of plots (if available)
    /// </summary>
    public decimal? MaxPrice { get; init; }

    /// <summary>
    /// Status distribution dictionary
    /// </summary>
    public IReadOnlyDictionary<ParzellenStatus, int> StatusDistribution { get; init; } = new Dictionary<ParzellenStatus, int>();

    /// <summary>
    /// District-wise plot distribution
    /// </summary>
    public IReadOnlyList<BezirkPlotDistribution> DistrictDistribution { get; init; } = new List<BezirkPlotDistribution>();

    /// <summary>
    /// Date when statistics were calculated
    /// </summary>
    public DateTime CalculatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the percentage of available plots
    /// </summary>
    public decimal AvailablePercentage => TotalCount > 0 ? (decimal)AvailableCount / TotalCount * 100 : 0;

    /// <summary>
    /// Gets the percentage of assigned plots
    /// </summary>
    public decimal AssignedPercentage => TotalCount > 0 ? (decimal)AssignedCount / TotalCount * 100 : 0;

    /// <summary>
    /// Gets the occupancy rate (assigned + reserved plots)
    /// </summary>
    public decimal OccupancyRate => TotalCount > 0 ? (decimal)(AssignedCount + ReservedCount) / TotalCount * 100 : 0;

    /// <summary>
    /// Gets the percentage of plots with water access
    /// </summary>
    public decimal WaterAccessPercentage => TotalCount > 0 ? (decimal)PlotsWithWater / TotalCount * 100 : 0;

    /// <summary>
    /// Gets the percentage of plots with electricity access
    /// </summary>
    public decimal ElectricityAccessPercentage => TotalCount > 0 ? (decimal)PlotsWithElectricity / TotalCount * 100 : 0;

    /// <summary>
    /// Gets the percentage of plots with both utilities
    /// </summary>
    public decimal BothUtilitiesPercentage => TotalCount > 0 ? (decimal)PlotsWithBothUtilities / TotalCount * 100 : 0;
}

/// <summary>
/// Helper class for district-wise plot distribution
/// </summary>
public class BezirkPlotDistribution
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
    /// Total plots in the district
    /// </summary>
    public int TotalPlots { get; init; }

    /// <summary>
    /// Available plots in the district
    /// </summary>
    public int AvailablePlots { get; init; }

    /// <summary>
    /// Assigned plots in the district
    /// </summary>
    public int AssignedPlots { get; init; }

    /// <summary>
    /// Reserved plots in the district
    /// </summary>
    public int ReservedPlots { get; init; }

    /// <summary>
    /// District occupancy rate
    /// </summary>
    public decimal OccupancyRate => TotalPlots > 0 ? (decimal)(AssignedPlots + ReservedPlots) / TotalPlots * 100 : 0;

    /// <summary>
    /// Gets the display name or falls back to name
    /// </summary>
    public string GetDisplayName() => !string.IsNullOrWhiteSpace(DisplayName) ? DisplayName : BezirkName;
}