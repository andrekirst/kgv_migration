using MediatR;
using KGV.Application.Common.Models;
using KGV.Application.DTOs;
using KGV.Domain.Enums;

namespace KGV.Application.Features.Bezirke.Queries.SearchBezirke;

/// <summary>
/// Query for full-text search in Bezirke
/// </summary>
public record SearchBezirkeQuery : IRequest<Result<IEnumerable<BezirkListDto>>>
{
    /// <summary>
    /// Search term (required, min 2 characters)
    /// </summary>
    public required string SearchTerm { get; init; }

    /// <summary>
    /// Maximum number of results to return (default: 50, max: 200)
    /// </summary>
    public int MaxResults { get; init; } = 50;

    /// <summary>
    /// Filter by status (optional)
    /// </summary>
    public BezirkStatus? Status { get; init; }

    /// <summary>
    /// Filter by active status (optional, default: active only)
    /// </summary>
    public bool? IsActive { get; init; } = true;

    /// <summary>
    /// Whether to include fuzzy matching (default: true)
    /// </summary>
    public bool IncludeFuzzyMatch { get; init; } = true;

    /// <summary>
    /// Whether to search in descriptions as well (default: false)
    /// </summary>
    public bool SearchInDescriptions { get; init; } = false;

    /// <summary>
    /// Minimum relevance score (0.0 - 1.0, default: 0.3)
    /// </summary>
    public double MinRelevanceScore { get; init; } = 0.3;
}