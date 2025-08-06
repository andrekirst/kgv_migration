using MediatR;
using KGV.Application.Common.Models;
using KGV.Application.DTOs;
using KGV.Domain.Enums;

namespace KGV.Application.Features.Parzellen.Commands.CreateParzelle;

/// <summary>
/// Command to create a new Parzelle (Plot)
/// </summary>
public record CreateParzelleCommand : IRequest<Result<ParzelleDto>>
{
    /// <summary>
    /// Plot number/identifier within the district (required, max 20 chars)
    /// </summary>
    public required string Nummer { get; init; }

    /// <summary>
    /// Reference to the parent district (required)
    /// </summary>
    public required Guid BezirkId { get; init; }

    /// <summary>
    /// Area of the plot in square meters (required, 0.01 - 10,000)
    /// </summary>
    public required decimal Flaeche { get; init; }

    /// <summary>
    /// Initial status of the plot (default: Available)
    /// </summary>
    public ParzellenStatus Status { get; init; } = ParzellenStatus.Available;

    /// <summary>
    /// Price or rental cost for the plot (optional, 0 - 100,000)
    /// </summary>
    public decimal? Preis { get; init; }

    /// <summary>
    /// Additional notes or description for the plot (optional, max 1000 chars)
    /// </summary>
    public string? Beschreibung { get; init; }

    /// <summary>
    /// Special features or characteristics of the plot (optional, max 500 chars)
    /// </summary>
    public string? Besonderheiten { get; init; }

    /// <summary>
    /// Whether the plot has water access (default: false)
    /// </summary>
    public bool HasWasser { get; init; } = false;

    /// <summary>
    /// Whether the plot has electricity access (default: false)
    /// </summary>
    public bool HasStrom { get; init; } = false;

    /// <summary>
    /// Priority level for assignment (default: 0)
    /// </summary>
    public int Prioritaet { get; init; } = 0;

    /// <summary>
    /// User ID who is creating the plot
    /// </summary>
    public string? CreatedBy { get; init; }
}