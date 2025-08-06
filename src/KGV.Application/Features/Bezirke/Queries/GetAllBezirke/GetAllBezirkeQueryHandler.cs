using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using KGV.Application.Common.Interfaces;
using KGV.Application.Common.Models;
using KGV.Application.DTOs;
using KGV.Domain.Entities;
using KGV.Domain.Enums;
using System.Linq.Expressions;

namespace KGV.Application.Features.Bezirke.Queries.GetAllBezirke;

/// <summary>
/// Handler for GetAllBezirkeQuery
/// Retrieves paginated, filtered, and sorted list of districts
/// </summary>
public class GetAllBezirkeQueryHandler : IRequestHandler<GetAllBezirkeQuery, Result<PagedResult<BezirkListDto>>>
{
    private readonly IRepository<Bezirk> _bezirkRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllBezirkeQueryHandler> _logger;

    public GetAllBezirkeQueryHandler(
        IRepository<Bezirk> bezirkRepository,
        IMapper mapper,
        ILogger<GetAllBezirkeQueryHandler> logger)
    {
        _bezirkRepository = bezirkRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<PagedResult<BezirkListDto>>> Handle(GetAllBezirkeQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all Bezirke - Page: {PageNumber}, Size: {PageSize}, Search: {SearchTerm}", 
            request.PageNumber, request.PageSize, request.SearchTerm);

        try
        {
            // Build the filter expression
            var filter = BuildFilterExpression(request);

            // Build the sort expression
            var sortExpression = BuildSortExpression(request.SortBy, request.SortDescending);

            // Get total count for pagination
            var totalCount = await _bezirkRepository.CountAsync(filter, cancellationToken);

            if (totalCount == 0)
            {
                return Result<PagedResult<BezirkListDto>>.Success(
                    PagedResult<BezirkListDto>.Empty(request.PageNumber, request.PageSize));
            }

            // Get the data with pagination
            var bezirke = await _bezirkRepository.GetPagedAsync(
                filter,
                sortExpression,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            // Map to DTOs
            var bezirkDtos = _mapper.Map<List<BezirkListDto>>(bezirke);

            // Create paged result
            var pagedResult = new PagedResult<BezirkListDto>(
                bezirkDtos, 
                request.PageNumber, 
                request.PageSize, 
                totalCount);

            _logger.LogInformation("Successfully retrieved {Count} Bezirke out of {Total}", 
                bezirkDtos.Count, totalCount);

            return Result<PagedResult<BezirkListDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all Bezirke");
            return Result<PagedResult<BezirkListDto>>.Failure("Ein Fehler ist beim Abrufen der Bezirke aufgetreten.");
        }
    }

    private Expression<Func<Bezirk, bool>> BuildFilterExpression(GetAllBezirkeQuery request)
    {
        Expression<Func<Bezirk, bool>> filter = b => true;

        // Search term filter
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim().ToLower();
            filter = filter.And(b => 
                b.Name.ToLower().Contains(searchTerm) ||
                (b.DisplayName != null && b.DisplayName.ToLower().Contains(searchTerm)));
        }

        // Status filter
        if (request.Status.HasValue)
        {
            var status = request.Status.Value;
            filter = filter.And(b => b.Status == status);
        }

        // Active status filter
        if (request.IsActive.HasValue)
        {
            var isActive = request.IsActive.Value;
            filter = filter.And(b => b.IsActive == isActive);
        }

        // Area filters
        if (request.MinFlaeche.HasValue)
        {
            var minFlaeche = request.MinFlaeche.Value;
            filter = filter.And(b => b.Flaeche.HasValue && b.Flaeche >= minFlaeche);
        }

        if (request.MaxFlaeche.HasValue)
        {
            var maxFlaeche = request.MaxFlaeche.Value;
            filter = filter.And(b => b.Flaeche.HasValue && b.Flaeche <= maxFlaeche);
        }

        return filter;
    }

    private Expression<Func<Bezirk, object>> BuildSortExpression(string sortBy, bool descending)
    {
        return sortBy.ToLower() switch
        {
            "name" => b => b.Name,
            "displayname" => b => b.DisplayName ?? b.Name,
            "status" => b => b.Status,
            "isactive" => b => b.IsActive,
            "flaeche" => b => b.Flaeche ?? 0,
            "anzahlparzellen" => b => b.AnzahlParzellen,
            "createdat" => b => b.CreatedAt,
            "updatedat" => b => b.UpdatedAt ?? DateTime.MinValue,
            _ => b => b.SortOrder
        };
    }
}

/// <summary>
/// Extension methods for combining expressions
/// </summary>
public static class ExpressionExtensions
{
    public static Expression<Func<T, bool>> And<T>(
        this Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(T));
        var leftBody = new ReplaceParameterVisitor(left.Parameters[0], parameter).Visit(left.Body);
        var rightBody = new ReplaceParameterVisitor(right.Parameters[0], parameter).Visit(right.Body);
        var body = Expression.AndAlso(leftBody, rightBody);
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}

/// <summary>
/// Helper class for combining expressions
/// </summary>
public class ReplaceParameterVisitor : ExpressionVisitor
{
    private readonly ParameterExpression _oldParameter;
    private readonly ParameterExpression _newParameter;

    public ReplaceParameterVisitor(ParameterExpression oldParameter, ParameterExpression newParameter)
    {
        _oldParameter = oldParameter;
        _newParameter = newParameter;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return node == _oldParameter ? _newParameter : base.VisitParameter(node);
    }
}