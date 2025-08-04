using KGV.Application.Common.Models;
using KGV.Application.DTOs;
using MediatR;

namespace KGV.Application.Features.Antraege.Queries;

/// <summary>
/// Query to get an Antrag by ID
/// </summary>
public class GetAntragByIdQuery : IRequest<Result<AntragDto>>
{
    /// <summary>
    /// Antrag ID to retrieve
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Whether to include history entries
    /// </summary>
    public bool IncludeHistory { get; set; } = true;

    public GetAntragByIdQuery(Guid id, bool includeHistory = true)
    {
        Id = id;
        IncludeHistory = includeHistory;
    }
}