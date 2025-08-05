using KGV.Application.Common.Models;
using KGV.Domain.Common;
using System.Linq.Expressions;

namespace KGV.Application.Common.Interfaces;

/// <summary>
/// Generic repository interface for domain entities
/// </summary>
/// <typeparam name="TEntity">Domain entity type</typeparam>
public interface IRepository<TEntity> where TEntity : BaseEntity
{
    /// <summary>
    /// Gets an entity by ID
    /// </summary>
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities with optional filtering
    /// </summary>
    Task<IEnumerable<TEntity>> GetAllAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        string includeProperties = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities with pagination
    /// </summary>
    Task<(IEnumerable<TEntity> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        string includeProperties = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the first entity matching the filter
    /// </summary>
    Task<TEntity?> GetFirstOrDefaultAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        string includeProperties = "",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts entities matching the filter
    /// </summary>
    Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entity matches the filter
    /// </summary>
    Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new entity
    /// </summary>
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple entities
    /// </summary>
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an entity
    /// </summary>
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple entities
    /// </summary>
    Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity (hard delete)
    /// </summary>
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity by ID (hard delete)
    /// </summary>
    Task DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple entities
    /// </summary>
    Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes an entity
    /// </summary>
    Task SoftDeleteAsync(Guid id, string? deletedBy = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a soft-deleted entity
    /// </summary>
    Task RestoreAsync(Guid id, CancellationToken cancellationToken = default);

    // Specification Pattern Methods

    /// <summary>
    /// Gets entities using a specification
    /// </summary>
    Task<IEnumerable<TEntity>> GetAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single entity using a specification
    /// </summary>
    Task<TEntity?> GetSingleAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities with pagination using a specification
    /// </summary>
    Task<PagedResult<TEntity>> GetPagedAsync(ISpecification<TEntity> specification, PaginationParameters pagination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts entities using a specification
    /// </summary>
    Task<int> CountAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entity matches the specification
    /// </summary>
    Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default);
}