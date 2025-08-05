using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using KGV.Application.Common.Interfaces;
using KGV.Application.Common.Models;
using KGV.Application.DTOs;
using KGV.Domain.Entities;
using System.Linq.Expressions;

namespace KGV.Application.Features.Bezirke.Queries.SearchBezirke;

/// <summary>
/// Handler for SearchBezirkeQuery
/// Performs full-text search with relevance scoring and fuzzy matching
/// </summary>
public class SearchBezirkeQueryHandler : IRequestHandler<SearchBezirkeQuery, Result<IEnumerable<BezirkListDto>>>
{
    private readonly IRepository<Bezirk> _bezirkRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<SearchBezirkeQueryHandler> _logger;

    public SearchBezirkeQueryHandler(
        IRepository<Bezirk> bezirkRepository,
        IMapper mapper,
        ILogger<SearchBezirkeQueryHandler> logger)
    {
        _bezirkRepository = bezirkRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<BezirkListDto>>> Handle(SearchBezirkeQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Searching Bezirke with term: '{SearchTerm}', MaxResults: {MaxResults}, FuzzyMatch: {FuzzyMatch}", 
            request.SearchTerm, request.MaxResults, request.IncludeFuzzyMatch);

        try
        {
            // Build the search filter
            var filter = BuildSearchFilter(request);

            // Get all matching bezirke (we'll do relevance scoring in memory for simplicity)
            var bezirke = await _bezirkRepository.GetAllAsync(filter, cancellationToken);

            // Calculate relevance scores and sort
            var rankedResults = CalculateRelevanceScores(bezirke, request)
                .Where(r => r.Score >= request.MinRelevanceScore)
                .OrderByDescending(r => r.Score)
                .ThenBy(r => r.Bezirk.SortOrder)
                .ThenBy(r => r.Bezirk.Name)
                .Take(request.MaxResults)
                .Select(r => r.Bezirk)
                .ToList();

            // Map to DTOs
            var bezirkDtos = _mapper.Map<List<BezirkListDto>>(rankedResults);

            _logger.LogInformation("Search for '{SearchTerm}' returned {Count} results", 
                request.SearchTerm, bezirkDtos.Count);

            return Result<IEnumerable<BezirkListDto>>.Success(bezirkDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Bezirke with term: '{SearchTerm}'", request.SearchTerm);
            return Result<IEnumerable<BezirkListDto>>.Failure("Ein Fehler ist bei der Suche aufgetreten.");
        }
    }

    private Expression<Func<Bezirk, bool>> BuildSearchFilter(SearchBezirkeQuery request)
    {
        var searchTerm = request.SearchTerm.Trim().ToLower();
        
        Expression<Func<Bezirk, bool>> filter = b => 
            b.Name.ToLower().Contains(searchTerm) ||
            (b.DisplayName != null && b.DisplayName.ToLower().Contains(searchTerm));

        // Include descriptions if requested
        if (request.SearchInDescriptions)
        {
            filter = filter.Or(b => b.Description != null && b.Description.ToLower().Contains(searchTerm));
        }

        // Add status filters
        if (request.Status.HasValue)
        {
            var status = request.Status.Value;
            filter = filter.And(b => b.Status == status);
        }

        if (request.IsActive.HasValue)
        {
            var isActive = request.IsActive.Value;
            filter = filter.And(b => b.IsActive == isActive);
        }

        return filter;
    }

    private IEnumerable<(Bezirk Bezirk, double Score)> CalculateRelevanceScores(
        IEnumerable<Bezirk> bezirke, 
        SearchBezirkeQuery request)
    {
        var searchTerm = request.SearchTerm.Trim().ToLower();
        var searchWords = searchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var bezirk in bezirke)
        {
            var score = 0.0;

            // Exact name match gets highest score
            if (bezirk.Name.ToLower() == searchTerm)
            {
                score += 1.0;
            }
            else if (bezirk.Name.ToLower().Contains(searchTerm))
            {
                score += 0.8;
            }

            // Display name match
            if (!string.IsNullOrEmpty(bezirk.DisplayName))
            {
                if (bezirk.DisplayName.ToLower() == searchTerm)
                {
                    score += 0.9;
                }
                else if (bezirk.DisplayName.ToLower().Contains(searchTerm))
                {
                    score += 0.7;
                }
            }

            // Description match (if enabled)
            if (request.SearchInDescriptions && !string.IsNullOrEmpty(bezirk.Description))
            {
                if (bezirk.Description.ToLower().Contains(searchTerm))
                {
                    score += 0.3;
                }
            }

            // Word-by-word matching
            foreach (var word in searchWords)
            {
                if (bezirk.Name.ToLower().Contains(word))
                    score += 0.2;

                if (!string.IsNullOrEmpty(bezirk.DisplayName) && bezirk.DisplayName.ToLower().Contains(word))
                    score += 0.15;

                if (request.SearchInDescriptions && !string.IsNullOrEmpty(bezirk.Description) && 
                    bezirk.Description.ToLower().Contains(word))
                    score += 0.1;
            }

            // Fuzzy matching (simple Levenshtein-based approach)
            if (request.IncludeFuzzyMatch && score < 0.5)
            {
                var fuzzyScore = CalculateFuzzyScore(bezirk.Name.ToLower(), searchTerm);
                if (fuzzyScore > 0.7)
                {
                    score += fuzzyScore * 0.4;
                }

                if (!string.IsNullOrEmpty(bezirk.DisplayName))
                {
                    var displayFuzzyScore = CalculateFuzzyScore(bezirk.DisplayName.ToLower(), searchTerm);
                    if (displayFuzzyScore > 0.7)
                    {
                        score += displayFuzzyScore * 0.3;
                    }
                }
            }

            yield return (bezirk, Math.Min(score, 1.0));
        }
    }

    private double CalculateFuzzyScore(string source, string target)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            return 0.0;

        var distance = CalculateLevenshteinDistance(source, target);
        var maxLength = Math.Max(source.Length, target.Length);
        
        return 1.0 - (double)distance / maxLength;
    }

    private int CalculateLevenshteinDistance(string source, string target)
    {
        if (source.Length == 0) return target.Length;
        if (target.Length == 0) return source.Length;

        var matrix = new int[source.Length + 1, target.Length + 1];

        for (int i = 0; i <= source.Length; i++)
            matrix[i, 0] = i;

        for (int j = 0; j <= target.Length; j++)
            matrix[0, j] = j;

        for (int i = 1; i <= source.Length; i++)
        {
            for (int j = 1; j <= target.Length; j++)
            {
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[source.Length, target.Length];
    }
}

/// <summary>
/// Extension methods for combining expressions
/// </summary>
public static class SearchExpressionExtensions
{
    public static Expression<Func<T, bool>> Or<T>(
        this Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(T));
        var leftBody = new ReplaceParameterVisitor(left.Parameters[0], parameter).Visit(left.Body);
        var rightBody = new ReplaceParameterVisitor(right.Parameters[0], parameter).Visit(right.Body);
        var body = Expression.OrElse(leftBody, rightBody);
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}