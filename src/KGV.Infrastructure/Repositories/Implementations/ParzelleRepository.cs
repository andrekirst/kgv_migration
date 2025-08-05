using KGV.Application.Common.Models;
using KGV.Domain.Entities;
using KGV.Domain.Enums;
using KGV.Infrastructure.Data;
using KGV.Infrastructure.Data.Repositories;
using KGV.Infrastructure.Repositories.DTOs;
using KGV.Infrastructure.Repositories.Interfaces;
using KGV.Infrastructure.Repositories.Specifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KGV.Infrastructure.Repositories.Implementations;

/// <summary>
/// Repository implementation for Parzelle (Plot) entities
/// </summary>
public class ParzelleRepository : Repository<Parzelle>, IParzelleRepository
{
    public ParzelleRepository(KgvDbContext context, ILogger<ParzelleRepository> logger)
        : base(context, logger)
    {
    }

    public async Task<IEnumerable<Parzelle>> GetByBezirkIdAsync(Guid bezirkId, CancellationToken cancellationToken = default)
    {
        try
        {
            var specification = new ParzelleSpecifications.ByBezirk(bezirkId);
            return await GetAsync(specification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Parzellen by BezirkId {BezirkId}", bezirkId);
            throw;
        }
    }

    public async Task<IEnumerable<Parzelle>> GetByStatusAsync(ParzellenStatus status, CancellationToken cancellationToken = default)
    {
        try
        {
            var specification = new ParzelleSpecifications.ByStatus(status);
            return await GetAsync(specification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Parzellen by status {Status}", status);
            throw;
        }
    }

    public async Task<ParzellenStatistics> GetStatisticsAsync(Guid? bezirkId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var specification = new ParzelleSpecifications.ForStatistics(bezirkId);
            var plots = await GetAsync(specification, cancellationToken);
            var plotsList = plots.ToList();

            var totalCount = plotsList.Count;
            var statusCounts = plotsList.GroupBy(p => p.Status).ToDictionary(g => g.Key, g => g.Count());

            // Calculate area statistics
            var areas = plotsList.Select(p => p.Flaeche).ToList();
            var prices = plotsList.Where(p => p.Preis.HasValue).Select(p => p.Preis!.Value).ToList();

            // Calculate district distribution
            var districtDistribution = plotsList
                .GroupBy(p => new { p.BezirkId, p.Bezirk.Name, p.Bezirk.DisplayName })
                .Select(g => new BezirkPlotDistribution
                {
                    BezirkId = g.Key.BezirkId,
                    BezirkName = g.Key.Name,
                    DisplayName = g.Key.DisplayName,
                    TotalPlots = g.Count(),
                    AvailablePlots = g.Count(p => p.Status == ParzellenStatus.Available),
                    AssignedPlots = g.Count(p => p.Status == ParzellenStatus.Assigned),
                    ReservedPlots = g.Count(p => p.Status == ParzellenStatus.Reserved)
                })
                .OrderByDescending(d => d.TotalPlots)
                .ToList();

            var statistics = new ParzellenStatistics
            {
                TotalCount = totalCount,
                AvailableCount = statusCounts.GetValueOrDefault(ParzellenStatus.Available, 0),
                ReservedCount = statusCounts.GetValueOrDefault(ParzellenStatus.Reserved, 0),
                AssignedCount = statusCounts.GetValueOrDefault(ParzellenStatus.Assigned, 0),
                UnavailableCount = statusCounts.GetValueOrDefault(ParzellenStatus.Unavailable, 0),
                UnderDevelopmentCount = statusCounts.GetValueOrDefault(ParzellenStatus.UnderDevelopment, 0),
                DecommissionedCount = statusCounts.GetValueOrDefault(ParzellenStatus.Decommissioned, 0),
                PendingApprovalCount = statusCounts.GetValueOrDefault(ParzellenStatus.PendingApproval, 0),
                TotalArea = areas.Sum(),
                AveragePlotSize = areas.Any() ? areas.Average() : 0,
                MinPlotSize = areas.Any() ? areas.Min() : 0,
                MaxPlotSize = areas.Any() ? areas.Max() : 0,
                PlotsWithWater = plotsList.Count(p => p.HasWasser),
                PlotsWithElectricity = plotsList.Count(p => p.HasStrom),
                PlotsWithBothUtilities = plotsList.Count(p => p.HasWasser && p.HasStrom),
                AveragePrice = prices.Any() ? prices.Average() : null,
                MinPrice = prices.Any() ? prices.Min() : null,
                MaxPrice = prices.Any() ? prices.Max() : null,
                StatusDistribution = statusCounts,
                DistrictDistribution = districtDistribution,
                CalculatedAt = DateTime.UtcNow
            };

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating Parzelle statistics for BezirkId {BezirkId}", bezirkId);
            throw;
        }
    }

    public async Task<IEnumerable<Parzelle>> GetFreieParzellenAsync(bool includeReserved = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var specification = new ParzelleSpecifications.Available(includeReserved);
            return await GetAsync(specification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting free Parzellen, includeReserved: {IncludeReserved}", includeReserved);
            throw;
        }
    }

    public async Task<Parzelle?> GetByBezirkAndNummerAsync(Guid bezirkId, string nummer, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(nummer))
                return null;

            var specification = new ParzelleSpecifications.ByBezirkAndNummer(bezirkId, nummer);
            return await GetSingleAsync(specification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Parzelle by BezirkId {BezirkId} and Nummer {Nummer}", bezirkId, nummer);
            throw;
        }
    }

    public async Task<Parzelle?> GetByFullNumberAsync(string fullNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fullNumber))
                return null;

            var specification = new ParzelleSpecifications.ByFullNumber(fullNumber);
            return await GetSingleAsync(specification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Parzelle by full number {FullNumber}", fullNumber);
            throw;
        }
    }

    public async Task<PagedResult<Parzelle>> GetPagedAsync(
        PaginationParameters pagination,
        Guid? bezirkId = null,
        ParzellenStatus? status = null,
        bool? hasWasser = null,
        bool? hasStrom = null,
        decimal? minFlaeche = null,
        decimal? maxFlaeche = null,
        decimal? minPreis = null,
        decimal? maxPreis = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var specification = new ParzelleSpecifications.ComplexFilter(
                bezirkId, status, hasWasser, hasStrom, minFlaeche, maxFlaeche, minPreis, maxPreis, searchTerm);
            
            return await base.GetPagedAsync(specification, pagination, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged Parzellen with filters");
            throw;
        }
    }

    public async Task<bool> NumberExistsInDistrictAsync(Guid bezirkId, string nummer, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(nummer))
                return false;

            var specification = new ParzelleSpecifications.NumberExistsInDistrict(bezirkId, nummer, excludeId);
            return await AnyAsync(specification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if Parzelle number exists in district {BezirkId}, nummer {Nummer}", bezirkId, nummer);
            throw;
        }
    }

    public async Task<IEnumerable<Parzelle>> GetPlotsNeedingAttentionAsync(int daysSinceLastUpdate = 365, CancellationToken cancellationToken = default)
    {
        try
        {
            var specification = new ParzelleSpecifications.NeedingAttention(daysSinceLastUpdate);
            return await GetAsync(specification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Parzellen needing attention");
            throw;
        }
    }

    public async Task<IEnumerable<Parzelle>> GetByUtilitiesAsync(bool hasWasser, bool hasStrom, CancellationToken cancellationToken = default)
    {
        try
        {
            var specification = new ParzelleSpecifications.ByUtilities(hasWasser, hasStrom);
            return await GetAsync(specification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Parzellen by utilities - Water: {HasWasser}, Electricity: {HasStrom}", hasWasser, hasStrom);
            throw;
        }
    }

    public async Task<IEnumerable<Parzelle>> GetByAreaRangeAsync(decimal minArea, decimal maxArea, CancellationToken cancellationToken = default)
    {
        try
        {
            var specification = new ParzelleSpecifications.ByAreaRange(minArea, maxArea);
            return await GetAsync(specification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Parzellen by area range {MinArea}-{MaxArea}", minArea, maxArea);
            throw;
        }
    }

    public async Task<IEnumerable<Parzelle>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice, CancellationToken cancellationToken = default)
    {
        try
        {
            var specification = new ParzelleSpecifications.ByPriceRange(minPrice, maxPrice);
            return await GetAsync(specification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Parzellen by price range {MinPrice}-{MaxPrice}", minPrice, maxPrice);
            throw;
        }
    }

    public async Task<IEnumerable<Parzelle>> GetRecentlyAssignedAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        try
        {
            var specification = new ParzelleSpecifications.RecentlyAssigned(days);
            return await GetAsync(specification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recently assigned Parzellen for {Days} days", days);
            throw;
        }
    }

    public async Task UpdatePlotStatusesAsync(Dictionary<Guid, ParzellenStatus> plotStatusUpdates, CancellationToken cancellationToken = default)
    {
        try
        {
            if (plotStatusUpdates?.Any() != true)
                return;

            var plotIds = plotStatusUpdates.Keys.ToList();
            var plots = await _dbSet
                .Where(p => plotIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            foreach (var plot in plots)
            {
                if (plotStatusUpdates.TryGetValue(plot.Id, out var newStatus))
                {
                    plot.ChangeStatus(newStatus);
                }
            }

            _context.UpdateRange(plots);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated status for {PlotCount} plots", plots.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating plot statuses for multiple plots");
            throw;
        }
    }

    public async Task<string> GetNextAvailableNumberAsync(Guid bezirkId, CancellationToken cancellationToken = default)
    {
        try
        {
            var specification = new ParzelleSpecifications.HighestNumberInDistrict(bezirkId);
            var highestPlot = await GetSingleAsync(specification, cancellationToken);

            if (highestPlot == null)
            {
                return "1";
            }

            // Try to parse the highest number and increment
            if (int.TryParse(highestPlot.Nummer, out var highestNumber))
            {
                return (highestNumber + 1).ToString();
            }

            // If the highest number is not a simple integer, try to extract number and increment
            var numericPart = new string(highestPlot.Nummer.Where(char.IsDigit).ToArray());
            if (int.TryParse(numericPart, out var parsedNumber))
            {
                return (parsedNumber + 1).ToString();
            }

            // Fallback: count all plots and add 1
            var plotCount = await _dbSet
                .AsNoTracking()
                .Where(p => p.BezirkId == bezirkId)
                .CountAsync(cancellationToken);

            return (plotCount + 1).ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting next available number for BezirkId {BezirkId}", bezirkId);
            throw;
        }
    }
}