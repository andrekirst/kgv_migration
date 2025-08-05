using MediatR;
using KGV.Application.Common.Models;
using KGV.Application.DTOs;
using KGV.Domain.Enums;

namespace KGV.Application.Features.Bezirke.Commands.CreateBezirk;

/// <summary>
/// Command to create a new Bezirk (District)
/// </summary>
public record CreateBezirkCommand : IRequest<Result<BezirkDto>>
{
    /// <summary>
    /// District name/identifier (required, max 10 chars)
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Full display name of the district (optional, max 100 chars)
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Description of the district (optional, max 500 chars)
    /// </summary>
    public string? Beschreibung { get; init; }

    /// <summary>
    /// Sort order for displaying districts (default: 0)
    /// </summary>
    public int SortOrder { get; init; } = 0;

    /// <summary>
    /// Total area of the district in square meters (optional)
    /// </summary>
    public decimal? Flaeche { get; init; }

    /// <summary>
    /// Initial status of the district (default: Active)
    /// </summary>
    public BezirkStatus Status { get; init; } = BezirkStatus.Active;

    /// <summary>
    /// User ID who is creating the district
    /// </summary>
    public string? CreatedBy { get; init; }
}