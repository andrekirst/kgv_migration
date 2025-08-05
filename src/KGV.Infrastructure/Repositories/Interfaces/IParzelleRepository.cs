using KGV.Application.Common.Interfaces;
using KGV.Application.Common.Models;
using KGV.Domain.Entities;
using KGV.Domain.Enums;
using KGV.Infrastructure.Repositories.DTOs;

namespace KGV.Infrastructure.Repositories.Interfaces;

/// <summary>
/// Repository interface for Parzelle (Plot) entities with specific operations
/// </summary>
public interface IParzelleRepository : IRepository<Parzelle>
{
    /// <summary>
    /// Gets plots by district ID
    /// </summary>
    /// <param name="bezirkId">District ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of plots in the specified district</returns>
    Task<IEnumerable<Parzelle>> GetByBezirkIdAsync(Guid bezirkId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets plots by status
    /// </summary>
    /// <param name="status">Plot status to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of plots with the specified status</returns>
    Task<IEnumerable<Parzelle>> GetByStatusAsync(ParzellenStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets comprehensive statistics for all plots
    /// </summary>
    /// <param name="bezirkId">Optional district ID to filter statistics</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Plot statistics</returns>
    Task<ParzellenStatistics> GetStatisticsAsync(Guid? bezirkId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available plots (Available or Reserved status)
    /// </summary>
    /// <param name="includeReserved">Whether to include reserved plots</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Available plots</returns>
    Task<IEnumerable<Parzelle>> GetFreieParzellenAsync(bool includeReserved = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets plot by district and plot number
    /// </summary>
    /// <param name="bezirkId">District ID</param>
    /// <param name="nummer">Plot number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Plot if found, null otherwise</returns>
    Task<Parzelle?> GetByBezirkAndNummerAsync(Guid bezirkId, string nummer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets plot by full number format (BezirkName-PlotNumber)
    /// </summary>
    /// <param name="fullNumber">Full plot number (e.g., "A-123")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Plot if found, null otherwise</returns>
    Task<Parzelle?> GetByFullNumberAsync(string fullNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets plots with pagination and filtering
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="bezirkId">Optional district filter</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="hasWasser">Optional water access filter</param>
    /// <param name="hasStrom">Optional electricity access filter</param>
    /// <param name="minFlaeche">Optional minimum area filter</param>
    /// <param name="maxFlaeche">Optional maximum area filter</param>
    /// <param name="minPreis">Optional minimum price filter</param>
    /// <param name="maxPreis">Optional maximum price filter</param>
    /// <param name="searchTerm">Optional search term for plot number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of plots</returns>
    Task<PagedResult<Parzelle>> GetPagedAsync(
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
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a plot number already exists in a district
    /// </summary>
    /// <param name="bezirkId">District ID</param>
    /// <param name="nummer">Plot number</param>
    /// <param name="excludeId">Optional ID to exclude from the check (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if number exists in district, false otherwise</returns>
    Task<bool> NumberExistsInDistrictAsync(Guid bezirkId, string nummer, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets plots that are overdue for review or maintenance
    /// </summary>
    /// <param name="daysSinceLastUpdate">Number of days since last update to consider overdue</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Plots needing attention</returns>
    Task<IEnumerable<Parzelle>> GetPlotsNeedingAttentionAsync(int daysSinceLastUpdate = 365, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets plots with specific utility combinations
    /// </summary>
    /// <param name="hasWasser">Water access requirement</param>
    /// <param name="hasStrom">Electricity access requirement</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Plots matching utility requirements</returns>
    Task<IEnumerable<Parzelle>> GetByUtilitiesAsync(bool hasWasser, bool hasStrom, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets plots within a specific area range
    /// </summary>
    /// <param name="minArea">Minimum area in square meters</param>
    /// <param name="maxArea">Maximum area in square meters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Plots within the specified area range</returns>
    Task<IEnumerable<Parzelle>> GetByAreaRangeAsync(decimal minArea, decimal maxArea, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets plots within a specific price range
    /// </summary>
    /// <param name="minPrice">Minimum price</param>
    /// <param name="maxPrice">Maximum price</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Plots within the specified price range</returns>
    Task<IEnumerable<Parzelle>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recently assigned plots
    /// </summary>
    /// <param name="days">Number of days to look back</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Recently assigned plots</returns>
    Task<IEnumerable<Parzelle>> GetRecentlyAssignedAsync(int days = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple plot statuses efficiently
    /// </summary>
    /// <param name="plotStatusUpdates">Dictionary of plot ID to new status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdatePlotStatusesAsync(Dictionary<Guid, ParzellenStatus> plotStatusUpdates, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the next available plot number for a district
    /// </summary>
    /// <param name="bezirkId">District ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Next available plot number</returns>
    Task<string> GetNextAvailableNumberAsync(Guid bezirkId, CancellationToken cancellationToken = default);
}