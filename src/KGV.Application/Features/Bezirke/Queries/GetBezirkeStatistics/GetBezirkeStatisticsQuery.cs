using MediatR;
using KGV.Application.Common.Models;
using KGV.Application.Features.Bezirke.DTOs;

namespace KGV.Application.Features.Bezirke.Queries.GetBezirkeStatistics;

/// <summary>
/// Query to get aggregated statistics for Bezirke
/// </summary>
public record GetBezirkeStatisticsQuery : IRequest<Result<BezirkStatistics>>
{
    /// <summary>
    /// Whether to include inactive/archived districts in statistics (default: false)
    /// </summary>
    public bool IncludeInactive { get; init; } = false;

    /// <summary>
    /// Whether to calculate detailed area statistics (default: true)
    /// </summary>
    public bool IncludeAreaStatistics { get; init; } = true;

    /// <summary>
    /// Whether to include individual district rankings (default: true)
    /// </summary>
    public bool IncludeRankings { get; init; } = true;

    /// <summary>
    /// Minimum date for filtering (optional)
    /// </summary>
    public DateTime? FromDate { get; init; }

    /// <summary>
    /// Maximum date for filtering (optional)
    /// </summary>
    public DateTime? ToDate { get; init; }
}