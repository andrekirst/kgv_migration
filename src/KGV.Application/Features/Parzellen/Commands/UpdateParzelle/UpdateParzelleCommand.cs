using MediatR;
using KGV.Application.Common.Models;
using KGV.Application.DTOs;

namespace KGV.Application.Features.Parzellen.Commands.UpdateParzelle;

/// <summary>
/// Command to update an existing Parzelle (Plot)
/// </summary>
public record UpdateParzelleCommand : IRequest<Result<ParzelleDto>>
{
    /// <summary>
    /// ID of the Parzelle to update
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Area of the plot in square meters (optional)
    /// </summary>
    public decimal? Flaeche { get; init; }

    /// <summary>
    /// Price or rental cost for the plot (optional)
    /// </summary>
    public decimal? Preis { get; init; }

    /// <summary>
    /// Additional notes or description for the plot (optional)
    /// </summary>
    public string? Beschreibung { get; init; }

    /// <summary>
    /// Special features or characteristics of the plot (optional)
    /// </summary>
    public string? Besonderheiten { get; init; }

    /// <summary>
    /// Whether the plot has water access (optional)
    /// </summary>
    public bool? HasWasser { get; init; }

    /// <summary>
    /// Whether the plot has electricity access (optional)
    /// </summary>
    public bool? HasStrom { get; init; }

    /// <summary>
    /// Priority level for assignment (optional)
    /// </summary>
    public int? Prioritaet { get; init; }

    /// <summary>
    /// User ID who is updating the plot
    /// </summary>
    public string? UpdatedBy { get; init; }
}