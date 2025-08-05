using MediatR;
using KGV.Application.Common.Models;

namespace KGV.Application.Features.Bezirke.Commands.DeleteBezirk;

/// <summary>
/// Command to delete a Bezirk (District)
/// </summary>
public record DeleteBezirkCommand : IRequest<Result>
{
    /// <summary>
    /// ID of the Bezirk to delete
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// User ID who is deleting the district
    /// </summary>
    public string? DeletedBy { get; init; }

    /// <summary>
    /// Whether to force delete even if there are associated Parzellen
    /// (This will archive the Bezirk instead of actual deletion)
    /// </summary>
    public bool ForceDelete { get; init; } = false;
}