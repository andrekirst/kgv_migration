using KGV.Application.Common.Models;
using KGV.Application.DTOs;
using KGV.Domain.Enums;
using MediatR;

namespace KGV.Application.Features.Antraege.Queries;

/// <summary>
/// Query to get a paginated list of Antraege with filtering
/// </summary>
public class GetAntraegeQuery : IRequest<Result<PaginatedResult<AntragListDto>>>
{
    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Search term for name, email, or address
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Filter by status
    /// </summary>
    public AntragStatus? Status { get; set; }

    /// <summary>
    /// Filter by active/inactive
    /// </summary>
    public bool? Aktiv { get; set; }

    /// <summary>
    /// Filter by application date from
    /// </summary>
    public DateTime? BewerbungsdatumFrom { get; set; }

    /// <summary>
    /// Filter by application date to
    /// </summary>
    public DateTime? BewerbungsdatumTo { get; set; }

    /// <summary>
    /// Filter by city
    /// </summary>
    public string? Ort { get; set; }

    /// <summary>
    /// Sort field
    /// </summary>
    public string SortBy { get; set; } = "Bewerbungsdatum";

    /// <summary>
    /// Sort direction (asc/desc)
    /// </summary>
    public string SortDirection { get; set; } = "desc";

    /// <summary>
    /// Whether to include only active applications
    /// </summary>
    public bool OnlyActive { get; set; } = false;
}