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
/// Repository implementation for Bezirk (District) entities
/// </summary>
public class BezirkRepository : Repository<Bezirk>, IBezirkRepository
{
    public BezirkRepository(KgvDbContext context, ILogger<BezirkRepository> logger)
        : base(context, logger)
    {
    }

    public async Task<IEnumerable<Bezirk>> GetByStatusAsync(BezirkStatus status, CancellationToken cancellationToken = default)
    {
        try
        {
            var specification = new BezirkSpecifications.ByStatus(status);
            return await GetAsync(specification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Bezirke by status {Status}", status);
            throw;
        }
    }

    public async Task<IEnumerable<Bezirk>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Enumerable.Empty<Bezirk>();

            var specification = new BezirkSpecifications.SearchByName(searchTerm);
            return await GetAsync(specification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Bezirke with term {SearchTerm}", searchTerm);
            throw;
        }
    }

    public async Task<BezirkStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var specification = new BezirkSpecifications.ForStatistics();
            var districts = await GetAsync(specification, cancellationToken);
            var districtsList = districts.ToList();

            var totalCount = districtsList.Count;
            var statusCounts = districtsList.GroupBy(b => b.Status).ToDictionary(g => g.Key, g => g.Count());

            // Calculate statistics
            var statistics = new BezirkStatistics
            {
                TotalCount = totalCount,
                ActiveCount = statusCounts.GetValueOrDefault(BezirkStatus.Active, 0),
                InactiveCount = statusCounts.GetValueOrDefault(BezirkStatus.Inactive, 0),
                SuspendedCount = statusCounts.GetValueOrDefault(BezirkStatus.Suspended, 0),
                ArchivedCount = statusCounts.GetValueOrDefault(BezirkStatus.Archived, 0),
                UnderRestructuringCount = statusCounts.GetValueOrDefault(BezirkStatus.UnderRestructuring, 0),
                TotalPlotsCount = districtsList.Sum(b => b.AnzahlParzellen),
                TotalArea = districtsList.Where(b => b.Flaeche.HasValue).Sum(b => b.Flaeche),
                AverageAreaPerDistrict = districtsList.Where(b => b.Flaeche.HasValue).Any() 
                    ? districtsList.Where(b => b.Flaeche.HasValue).Average(b => b.Flaeche!.Value) 
                    : null,
                AveragePlotsPerDistrict = totalCount > 0 ? (decimal)districtsList.Sum(b => b.AnzahlParzellen) / totalCount : 0,
                TopDistrictsByPlotCount = districtsList
                    .OrderByDescending(b => b.AnzahlParzellen)
                    .Take(10)
                    .Select(b => new BezirkPlotCount
                    {
                        BezirkId = b.Id,
                        BezirkName = b.Name,
                        DisplayName = b.DisplayName,
                        PlotCount = b.AnzahlParzellen,
                        Area = b.Flaeche,
                        Status = b.Status
                    })
                    .ToList(),
                DistrictsWithFreePlots = await CountDistrictsWithFreePlotsAsync(cancellationToken),
                StatusDistribution = statusCounts,
                CalculatedAt = DateTime.UtcNow
            };

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating Bezirk statistics");
            throw;
        }
    }

    public async Task<IEnumerable<Bezirk>> GetWithParzellenAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var specification = new BezirkSpecifications.WithParzellen(includeInactive);
            return await GetAsync(specification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Bezirke with Parzellen, includeInactive: {IncludeInactive}", includeInactive);
            throw;
        }
    }

    public async Task<IEnumerable<Bezirk>> GetActiveWithFreePlotsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var specification = new BezirkSpecifications.WithAvailablePlots();
            return await GetAsync(specification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active Bezirke with free plots");
            throw;
        }
    }

    public async Task<PagedResult<Bezirk>> GetPagedAsync(
        PaginationParameters pagination,
        BezirkStatus? statusFilter = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var specification = new BezirkSpecifications.Paged(pagination.PageNumber, pagination.PageSize, statusFilter, searchTerm);
            return await base.GetPagedAsync(specification, pagination, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged Bezirke");
            throw;
        }
    }

    public async Task<Bezirk?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var specification = new BezirkSpecifications.ByName(name);
            return await GetSingleAsync(specification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Bezirk by name {Name}", name);
            throw;
        }
    }

    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            var specification = new BezirkSpecifications.NameExists(name, excludeId);
            return await AnyAsync(specification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if Bezirk name exists {Name}", name);
            throw;
        }
    }

    public async Task<IEnumerable<Bezirk>> GetDistrictsNeedingAttentionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var specification = new BezirkSpecifications.NeedingAttention();
            return await GetAsync(specification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Bezirke needing attention");
            throw;
        }
    }

    public async Task UpdatePlotCountsAsync(Dictionary<Guid, int> plotCounts, CancellationToken cancellationToken = default)
    {
        try
        {
            if (plotCounts?.Any() != true)
                return;

            var districtIds = plotCounts.Keys.ToList();
            var districts = await _dbSet
                .Where(b => districtIds.Contains(b.Id))
                .ToListAsync(cancellationToken);

            foreach (var district in districts)
            {
                if (plotCounts.TryGetValue(district.Id, out var newCount))
                {
                    district.UpdatePlotCount(newCount);
                }
            }

            _context.UpdateRange(districts);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated plot counts for {DistrictCount} districts", districts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating plot counts for multiple districts");
            throw;
        }
    }

    public async Task<IEnumerable<Bezirk>> GetWithKatasterbezirkeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var specification = new BezirkSpecifications.WithKatasterbezirke();
            return await GetAsync(specification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Bezirke with Katasterbezirke");
            throw;
        }
    }

    public async Task<IEnumerable<BezirkPlotCount>> GetTopDistrictsByPlotCountAsync(int topCount = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var specification = new BezirkSpecifications.OrderedByPlotCount(topCount);
            var districts = await GetAsync(specification, cancellationToken);

            return districts.Select(b => new BezirkPlotCount
            {
                BezirkId = b.Id,
                BezirkName = b.Name,
                DisplayName = b.DisplayName,
                PlotCount = b.AnzahlParzellen,
                Area = b.Flaeche,
                Status = b.Status
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top districts by plot count");
            throw;
        }
    }

    /// <summary>
    /// Helper method to count districts with free plots
    /// </summary>
    private async Task<int> CountDistrictsWithFreePlotsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbSet
                .AsNoTracking()
                .Where(b => b.Status == BezirkStatus.Active && 
                           b.Parzellen.Any(p => p.Status == ParzellenStatus.Available || p.Status == ParzellenStatus.Reserved))
                .CountAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting districts with free plots");
            throw;
        }
    }
}