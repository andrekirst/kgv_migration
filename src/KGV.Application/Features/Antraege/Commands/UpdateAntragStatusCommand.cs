using KGV.Application.Common.Models;
using KGV.Application.DTOs;
using KGV.Domain.Enums;
using MediatR;

namespace KGV.Application.Features.Antraege.Commands;

/// <summary>
/// Command to update the status of an Antrag
/// </summary>
public class UpdateAntragStatusCommand : IRequest<Result<AntragDto>>
{
    /// <summary>
    /// Antrag ID to update
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// New status for the application
    /// </summary>
    public AntragStatus Status { get; set; }

    /// <summary>
    /// Optional note about the status change
    /// </summary>
    public string? Vermerk { get; set; }

    /// <summary>
    /// Case worker making the status change
    /// </summary>
    public string? Sachbearbeiter { get; set; }

    /// <summary>
    /// Whether to create a history entry for this status change
    /// </summary>
    public bool CreateHistoryEntry { get; set; } = true;
}