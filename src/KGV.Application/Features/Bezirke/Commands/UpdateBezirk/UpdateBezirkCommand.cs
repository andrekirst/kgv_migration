using MediatR;
using KGV.Application.Common.Models;
using KGV.Application.DTOs;
using KGV.Domain.Enums;

namespace KGV.Application.Features.Bezirke.Commands.UpdateBezirk;

/// <summary>
/// Command to update an existing Bezirk (District)
/// </summary>
public record UpdateBezirkCommand : IRequest<Result<BezirkDto>>
{
    /// <summary>
    /// ID of the Bezirk to update
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Full display name of the district (optional, max 100 chars)
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Description of the district (optional, max 500 chars)
    /// </summary>
    public string? Beschreibung { get; init; }

    /// <summary>
    /// Sort order for displaying districts (optional)
    /// </summary>
    public int? SortOrder { get; init; }

    /// <summary>
    /// Total area of the district in square meters (optional)
    /// </summary>
    public decimal? Flaeche { get; init; }

    /// <summary>
    /// Status of the district (optional)
    /// </summary>
    public BezirkStatus? Status { get; init; }

    /// <summary>
    /// User ID who is updating the district
    /// </summary>
    public string? UpdatedBy { get; init; }
}