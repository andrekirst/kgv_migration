using MediatR;
using KGV.Application.Common.Models;

namespace KGV.Application.Features.Parzellen.Commands.DeleteParzelle;

/// <summary>
/// Command to delete a Parzelle (Plot)
/// </summary>
public record DeleteParzelleCommand : IRequest<Result>
{
    /// <summary>
    /// ID of the Parzelle to delete
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// User ID who is deleting the plot
    /// </summary>
    public string? DeletedBy { get; init; }

    /// <summary>
    /// Whether to force delete even if there are associated applications
    /// (This will decommission the plot instead of actual deletion)
    /// </summary>
    public bool ForceDelete { get; init; } = false;

    /// <summary>
    /// Reason for deletion (required for audit trail)
    /// </summary>
    public string? DeletionReason { get; init; }

    /// <summary>
    /// Whether to transfer any existing assignments to other available plots
    /// </summary>
    public bool TransferExistingAssignments { get; init; } = false;
}