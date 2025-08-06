using KGV.Domain.Enums;

namespace KGV.Application.Features.Parzellen.DTOs;

/// <summary>
/// Statistics for Parzellen (Plots)
/// </summary>
public class ParzellenStatistics
{
    /// <summary>
    /// Total number of plots
    /// </summary>
    public int TotalParzellen { get; set; }

    /// <summary>
    /// Number of available plots
    /// </summary>
    public int AvailableParzellen { get; set; }

    /// <summary>
    /// Number of reserved plots
    /// </summary>
    public int ReservedParzellen { get; set; }

    /// <summary>
    /// Number of assigned plots
    /// </summary>
    public int AssignedParzellen { get; set; }

    /// <summary>
    /// Number of unavailable plots
    /// </summary>
    public int UnavailableParzellen { get; set; }

    /// <summary>
    /// Number of plots under development
    /// </summary>
    public int UnderDevelopmentParzellen { get; set; }

    /// <summary>
    /// Number of decommissioned plots
    /// </summary>
    public int DecommissionedParzellen { get; set; }

    /// <summary>
    /// Number of plots pending approval
    /// </summary>
    public int PendingApprovalParzellen { get; set; }

    /// <summary>
    /// Total area of all plots (in m²)
    /// </summary>
    public decimal TotalFlaeche { get; set; }

    /// <summary>
    /// Average area per plot (in m²)
    /// </summary>
    public decimal AverageFlaeche { get; set; }

    /// <summary>
    /// Minimum plot area (in m²)
    /// </summary>
    public decimal MinFlaeche { get; set; }

    /// <summary>
    /// Maximum plot area (in m²)
    /// </summary>
    public decimal MaxFlaeche { get; set; }

    /// <summary>
    /// Total value of all plots with prices
    /// </summary>
    public decimal? TotalValue { get; set; }

    /// <summary>
    /// Average price per plot
    /// </summary>
    public decimal? AveragePrice { get; set; }

    /// <summary>
    /// Number of plots with water access
    /// </summary>
    public int ParzellenWithWasser { get; set; }

    /// <summary>
    /// Number of plots with electricity access
    /// </summary>
    public int ParzellenWithStrom { get; set; }

    /// <summary>
    /// Number of plots with both water and electricity
    /// </summary>
    public int ParzellenWithBothUtilities { get; set; }

    /// <summary>
    /// Percentage of plots that are available for assignment
    /// </summary>
    public decimal AvailabilityPercentage { get; set; }

    /// <summary>
    /// Percentage of plots that are assigned
    /// </summary>
    public decimal AssignmentPercentage { get; set; }

    /// <summary>
    /// Statistics by district
    /// </summary>
    public List<ParzellenByBezirkStatistic> ByBezirk { get; set; } = new();

    /// <summary>
    /// Plot with the largest area
    /// </summary>
    public ParzellenStatisticItem? LargestParzelle { get; set; }

    /// <summary>
    /// Plot with the smallest area
    /// </summary>
    public ParzellenStatisticItem? SmallestParzelle { get; set; }

    /// <summary>
    /// Most expensive plot
    /// </summary>
    public ParzellenStatisticItem? MostExpensiveParzelle { get; set; }

    /// <summary>
    /// When the statistics were calculated
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Individual plot statistic item
/// </summary>
public class ParzellenStatisticItem
{
    /// <summary>
    /// Plot ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Plot number
    /// </summary>
    public string Nummer { get; set; } = string.Empty;

    /// <summary>
    /// District ID
    /// </summary>
    public Guid BezirkId { get; set; }

    /// <summary>
    /// District name
    /// </summary>
    public string BezirkName { get; set; } = string.Empty;

    /// <summary>
    /// Full display name
    /// </summary>
    public string FullDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Plot area (in m²)
    /// </summary>
    public decimal Flaeche { get; set; }

    /// <summary>
    /// Plot price (optional)
    /// </summary>
    public decimal? Preis { get; set; }

    /// <summary>
    /// Plot status
    /// </summary>
    public ParzellenStatus Status { get; set; }
}

/// <summary>
/// Statistics for plots by district
/// </summary>
public class ParzellenByBezirkStatistic
{
    /// <summary>
    /// District ID
    /// </summary>
    public Guid BezirkId { get; set; }

    /// <summary>
    /// District name
    /// </summary>
    public string BezirkName { get; set; } = string.Empty;

    /// <summary>
    /// District display name
    /// </summary>
    public string BezirkDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Total plots in this district
    /// </summary>
    public int TotalParzellen { get; set; }

    /// <summary>
    /// Available plots in this district
    /// </summary>
    public int AvailableParzellen { get; set; }

    /// <summary>
    /// Assigned plots in this district
    /// </summary>
    public int AssignedParzellen { get; set; }

    /// <summary>
    /// Total area of plots in this district (in m²)
    /// </summary>
    public decimal TotalFlaeche { get; set; }

    /// <summary>
    /// Average area per plot in this district (in m²)
    /// </summary>
    public decimal AverageFlaeche { get; set; }

    /// <summary>
    /// Availability percentage in this district
    /// </summary>
    public decimal AvailabilityPercentage { get; set; }
}