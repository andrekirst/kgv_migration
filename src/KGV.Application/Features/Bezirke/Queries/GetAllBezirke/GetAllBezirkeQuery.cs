using MediatR;
using KGV.Application.Common.Models;
using KGV.Application.DTOs;
using KGV.Domain.Enums;

namespace KGV.Application.Features.Bezirke.Queries.GetAllBezirke;

/// <summary>
/// Query to get all Bezirke with pagination, filtering, and sorting
/// </summary>
public record GetAllBezirkeQuery : IRequest<Result<PagedResult<BezirkListDto>>>
{
    /// <summary>
    /// Page number (1-based, default: 1)
    /// </summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// Number of items per page (default: 20, max: 100)
    /// </summary>
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Search term for name or display name (optional)
    /// </summary>
    public string? SearchTerm { get; init; }

    /// <summary>
    /// Filter by status (optional)
    /// </summary>
    public BezirkStatus? Status { get; init; }

    /// <summary>
    /// Filter by active status (optional)
    /// </summary>
    public bool? IsActive { get; init; }

    /// <summary>
    /// Minimum area filter (in m²)
    /// </summary>
    public decimal? MinFlaeche { get; init; }

    /// <summary>
    /// Maximum area filter (in m²)
    /// </summary>
    public decimal? MaxFlaeche { get; init; }

    /// <summary>
    /// Sort field (default: SortOrder)
    /// </summary>
    public string SortBy { get; init; } = "SortOrder";

    /// <summary>
    /// Sort direction (default: ascending)
    /// </summary>
    public bool SortDescending { get; init; } = false;

    /// <summary>
    /// Include statistics in the result (optional)
    /// </summary>
    public bool IncludeStatistics { get; init; } = false;
}