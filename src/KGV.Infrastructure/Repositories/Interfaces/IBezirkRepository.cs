using KGV.Application.Common.Interfaces;
using KGV.Application.Common.Models;
using KGV.Domain.Entities;
using KGV.Domain.Enums;
using KGV.Infrastructure.Repositories.DTOs;

namespace KGV.Infrastructure.Repositories.Interfaces;

/// <summary>
/// Repository interface for Bezirk (District) entities with specific operations
/// </summary>
public interface IBezirkRepository : IRepository<Bezirk>
{
    /// <summary>
    /// Gets districts by status
    /// </summary>
    /// <param name="status">District status to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of districts with the specified status</returns>
    Task<IEnumerable<Bezirk>> GetByStatusAsync(BezirkStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches districts by name or display name
    /// </summary>
    /// <param name="searchTerm">Search term to match against name or display name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of matching districts</returns>
    Task<IEnumerable<Bezirk>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets comprehensive statistics for all districts
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>District statistics</returns>
    Task<BezirkStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets districts with their related plots (Parzellen)
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive districts</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Districts with their plots loaded</returns>
    Task<IEnumerable<Bezirk>> GetWithParzellenAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active districts that have available plots
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Active districts with free plots</returns>
    Task<IEnumerable<Bezirk>> GetActiveWithFreePlotsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets districts ordered by name with pagination
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <param name="statusFilter">Optional status filter</param>
    /// <param name="searchTerm">Optional search term</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged result of districts</returns>
    Task<PagedResult<Bezirk>> GetPagedAsync(
        PaginationParameters pagination,
        BezirkStatus? statusFilter = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets district by name (case-insensitive)
    /// </summary>
    /// <param name="name">District name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>District if found, null otherwise</returns>
    Task<Bezirk?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a district name already exists
    /// </summary>
    /// <param name="name">District name to check</param>
    /// <param name="excludeId">Optional ID to exclude from the check (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if name exists, false otherwise</returns>
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets districts that need attention (e.g., under restructuring, suspended)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Districts requiring attention</returns>
    Task<IEnumerable<Bezirk>> GetDistrictsNeedingAttentionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the plot count for multiple districts efficiently
    /// </summary>
    /// <param name="plotCounts">Dictionary of district ID to plot count</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdatePlotCountsAsync(Dictionary<Guid, int> plotCounts, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets districts with their cadastral districts (Katasterbezirke)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Districts with cadastral districts loaded</returns>
    Task<IEnumerable<Bezirk>> GetWithKatasterbezirkeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the top districts by plot count
    /// </summary>
    /// <param name="topCount">Number of top districts to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Top districts by plot count</returns>
    Task<IEnumerable<BezirkPlotCount>> GetTopDistrictsByPlotCountAsync(int topCount = 10, CancellationToken cancellationToken = default);
}