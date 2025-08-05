using MediatR;
using KGV.Application.Common.Models;
using KGV.Application.DTOs;

namespace KGV.Application.Features.Parzellen.Commands.AssignParzelle;

/// <summary>
/// Command to assign a Parzelle (Plot) to an applicant
/// This command implements special business logic for plot assignment
/// </summary>
public record AssignParzelleCommand : IRequest<Result<ParzelleDto>>
{
    /// <summary>
    /// ID of the Parzelle to assign
    /// </summary>
    public required Guid ParzelleId { get; init; }

    /// <summary>
    /// ID of the applicant/person getting the plot (optional if using application ID)
    /// </summary>
    public Guid? PersonId { get; init; }

    /// <summary>
    /// ID of the application being processed (optional if using person ID directly)
    /// </summary>
    public Guid? AntragId { get; init; }

    /// <summary>
    /// Date when the plot is assigned (default: now)
    /// </summary>
    public DateTime? AssignmentDate { get; init; }

    /// <summary>
    /// Notes about the assignment
    /// </summary>
    public string? AssignmentNotes { get; init; }

    /// <summary>
    /// Priority override for this assignment (higher numbers = higher priority)
    /// </summary>
    public int? PriorityOverride { get; init; }

    /// <summary>
    /// Whether to force assignment even if plot is not in ideal status
    /// </summary>
    public bool ForceAssignment { get; init; } = false;

    /// <summary>
    /// Whether to automatically reserve related plots (if applicable)
    /// </summary>
    public bool ReserveRelatedPlots { get; init; } = false;

    /// <summary>
    /// User ID who is performing the assignment
    /// </summary>
    public string? AssignedBy { get; init; }

    /// <summary>
    /// Reason for the assignment (for audit trail)
    /// </summary>
    public string? AssignmentReason { get; init; }
}