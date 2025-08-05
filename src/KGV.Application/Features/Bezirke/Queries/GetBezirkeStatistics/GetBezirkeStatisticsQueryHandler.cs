using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using KGV.Application.Common.Interfaces;
using KGV.Application.Common.Models;
using KGV.Application.Features.Bezirke.DTOs;
using KGV.Domain.Entities;
using KGV.Domain.Enums;
using System.Linq.Expressions;

namespace KGV.Application.Features.Bezirke.Queries.GetBezirkeStatistics;

/// <summary>
/// Handler for GetBezirkeStatisticsQuery
/// Calculates comprehensive statistics for all districts
/// </summary>
public class GetBezirkeStatisticsQueryHandler : IRequestHandler<GetBezirkeStatisticsQuery, Result<BezirkStatistics>>
{
    private readonly IRepository<Bezirk> _bezirkRepository;
    private readonly IRepository<Parzelle> _parzelleRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetBezirkeStatisticsQueryHandler> _logger;

    public GetBezirkeStatisticsQueryHandler(
        IRepository<Bezirk> bezirkRepository,
        IRepository<Parzelle> parzelleRepository,
        IMapper mapper,
        ILogger<GetBezirkeStatisticsQueryHandler> logger)
    {
        _bezirkRepository = bezirkRepository;
        _parzelleRepository = parzelleRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<BezirkStatistics>> Handle(GetBezirkeStatisticsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Calculating Bezirke statistics - IncludeInactive: {IncludeInactive}, IncludeAreaStats: {IncludeAreaStats}", 
            request.IncludeInactive, request.IncludeAreaStatistics);

        try
        {
            // Build the filter
            var filter = BuildFilter(request);

            // Get all relevant bezirke
            var bezirke = await _bezirkRepository.GetAllAsync(filter, cancellationToken);
            var bezirkeList = bezirke.ToList();

            if (!bezirkeList.Any())
            {
                return Result<BezirkStatistics>.Success(new BezirkStatistics
                {
                    CalculatedAt = DateTime.UtcNow
                });
            }

            // Calculate basic statistics
            var statistics = new BezirkStatistics
            {
                TotalBezirke = bezirkeList.Count,
                ActiveBezirke = bezirkeList.Count(b => b.Status == BezirkStatus.Active),
                InactiveBezirke = bezirkeList.Count(b => b.Status == BezirkStatus.Inactive),
                SuspendedBezirke = bezirkeList.Count(b => b.Status == BezirkStatus.Suspended),
                ArchivedBezirke = bezirkeList.Count(b => b.Status == BezirkStatus.Archived)
            };

            // Calculate area statistics if requested
            if (request.IncludeAreaStatistics)
            {
                await CalculateAreaStatistics(statistics, bezirkeList, cancellationToken);
            }

            // Calculate Parzellen statistics
            await CalculateParzellenStatistics(statistics, bezirkeList, cancellationToken);

            // Calculate rankings if requested
            if (request.IncludeRankings)
            {
                CalculateRankings(statistics, bezirkeList);
            }

            statistics.CalculatedAt = DateTime.UtcNow;

            _logger.LogInformation("Successfully calculated statistics for {Count} Bezirke", bezirkeList.Count);

            return Result<BezirkStatistics>.Success(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating Bezirke statistics");
            return Result<BezirkStatistics>.Failure("Ein Fehler ist bei der Berechnung der Statistiken aufgetreten.");
        }
    }

    private Expression<Func<Bezirk, bool>> BuildFilter(GetBezirkeStatisticsQuery request)
    {
        Expression<Func<Bezirk, bool>> filter = b => true;

        // Include/exclude inactive
        if (!request.IncludeInactive)
        {
            filter = filter.And(b => b.Status == BezirkStatus.Active || b.Status == BezirkStatus.UnderRestructuring);
        }

        // Date range filters
        if (request.FromDate.HasValue)
        {
            var fromDate = request.FromDate.Value;
            filter = filter.And(b => b.CreatedAt >= fromDate);
        }

        if (request.ToDate.HasValue)
        {
            var toDate = request.ToDate.Value;
            filter = filter.And(b => b.CreatedAt <= toDate);
        }

        return filter;
    }

    private async Task CalculateAreaStatistics(BezirkStatistics statistics, List<Bezirk> bezirke, CancellationToken cancellationToken)
    {
        var bezirkeWithArea = bezirke.Where(b => b.Flaeche.HasValue).ToList();
        
        if (bezirkeWithArea.Any())
        {
            statistics.TotalFlaeche = bezirkeWithArea.Sum(b => b.Flaeche!.Value);
            statistics.AverageFlaeche = bezirkeWithArea.Average(b => b.Flaeche!.Value);

            // Find largest area district
            var largestAreaBezirk = bezirkeWithArea.OrderByDescending(b => b.Flaeche).First();
            statistics.LargestAreaBezirk = new BezirkStatisticItem
            {
                Id = largestAreaBezirk.Id,
                Name = largestAreaBezirk.Name,
                DisplayName = largestAreaBezirk.GetDisplayName(),
                Flaeche = largestAreaBezirk.Flaeche,
                ParzellenCount = largestAreaBezirk.AnzahlParzellen
            };
        }
    }

    private async Task CalculateParzellenStatistics(BezirkStatistics statistics, List<Bezirk> bezirke, CancellationToken cancellationToken)
    {
        // Calculate total Parzellen from the Bezirke
        statistics.TotalParzellen = bezirke.Sum(b => b.AnzahlParzellen);
        
        if (bezirke.Any())
        {
            statistics.AverageParzellen = (decimal)bezirke.Average(b => b.AnzahlParzellen);
        }

        // Get additional Parzellen statistics directly from Parzelle repository for accuracy
        var bezirkIds = bezirke.Select(b => b.Id).ToList();
        var actualParzellenCount = await _parzelleRepository.CountAsync(
            p => bezirkIds.Contains(p.BezirkId), 
            cancellationToken);

        // Use the actual count if different
        if (actualParzellenCount != statistics.TotalParzellen)
        {
            statistics.TotalParzellen = actualParzellenCount;
            if (bezirke.Any())
            {
                statistics.AverageParzellen = (decimal)actualParzellenCount / bezirke.Count;
            }
        }
    }

    private void CalculateRankings(BezirkStatistics statistics, List<Bezirk> bezirke)
    {
        // Find district with most Parzellen
        var bezirkWithMostParzellen = bezirke.OrderByDescending(b => b.AnzahlParzellen).FirstOrDefault();
        if (bezirkWithMostParzellen != null)
        {
            statistics.LargestBezirk = new BezirkStatisticItem
            {
                Id = bezirkWithMostParzellen.Id,
                Name = bezirkWithMostParzellen.Name,
                DisplayName = bezirkWithMostParzellen.GetDisplayName(),
                Flaeche = bezirkWithMostParzellen.Flaeche,
                ParzellenCount = bezirkWithMostParzellen.AnzahlParzellen
            };
        }
    }
}