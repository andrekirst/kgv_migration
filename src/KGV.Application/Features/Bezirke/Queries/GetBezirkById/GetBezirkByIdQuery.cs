using MediatR;
using KGV.Application.Common.Models;
using KGV.Application.DTOs;

namespace KGV.Application.Features.Bezirke.Queries.GetBezirkById;

/// <summary>
/// Query to get a specific Bezirk by ID
/// </summary>
public record GetBezirkByIdQuery : IRequest<Result<BezirkDto>>
{
    /// <summary>
    /// ID of the Bezirk to retrieve
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Whether to include related Katasterbezirke (default: false)
    /// </summary>
    public bool IncludeKatasterbezirke { get; init; } = false;

    /// <summary>
    /// Whether to include related Parzellen (default: false)
    /// </summary>
    public bool IncludeParzellen { get; init; } = false;

    /// <summary>
    /// Whether to include audit information (default: true)
    /// </summary>
    public bool IncludeAuditInfo { get; init; } = true;
}