using KGV.Application.Common.Interfaces;
using KGV.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace KGV.Infrastructure.Data.Repositories;

/// <summary>
/// Generic repository implementation using Entity Framework
/// </summary>
/// <typeparam name="TEntity">Domain entity type</typeparam>
public class Repository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
{
    protected readonly KgvDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;
    protected readonly ILogger<Repository<TEntity>> _logger;

    public Repository(KgvDbContext context, ILogger<Repository<TEntity>> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dbSet = _context.Set<TEntity>();
    }

    public virtual async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbSet.FindAsync([id], cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting {EntityType} by ID {Id}", typeof(TEntity).Name, id);
            throw;
        }
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        string includeProperties = "",
        CancellationToken cancellationToken = default)
    {
        try
        {
            IQueryable<TEntity> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            foreach (var includeProperty in includeProperties.Split([','], StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty.Trim());
            }

            if (orderBy != null)
            {
                return await orderBy(query).ToListAsync(cancellationToken);
            }

            return await query.ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all {EntityType} entities", typeof(TEntity).Name);
            throw;
        }
    }

    public virtual async Task<(IEnumerable<TEntity> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        string includeProperties = "",
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // Limit max page size

            IQueryable<TEntity> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            foreach (var includeProperty in includeProperties.Split([','], StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty.Trim());
            }

            if (orderBy != null)
            {
                query = orderBy(query);
            }

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged {EntityType} entities", typeof(TEntity).Name);
            throw;
        }
    }

    public virtual async Task<TEntity?> GetFirstOrDefaultAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        string includeProperties = "",
        CancellationToken cancellationToken = default)
    {
        try
        {
            IQueryable<TEntity> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            foreach (var includeProperty in includeProperties.Split([','], StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty.Trim());
            }

            return await query.FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting first {EntityType} entity", typeof(TEntity).Name);
            throw;
        }
    }

    public virtual async Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IQueryable<TEntity> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.CountAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting {EntityType} entities", typeof(TEntity).Name);
            throw;
        }
    }

    public virtual async Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> filter,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbSet.AnyAsync(filter, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of {EntityType} entity", typeof(TEntity).Name);
            throw;
        }
    }

    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var entry = await _dbSet.AddAsync(entity, cancellationToken);
            return entry.Entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding {EntityType} entity", typeof(TEntity).Name);
            throw;
        }
    }

    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        try
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            await _dbSet.AddRangeAsync(entities, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding range of {EntityType} entities", typeof(TEntity).Name);
            throw;
        }
    }

    public virtual Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _dbSet.Update(entity);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating {EntityType} entity with ID {Id}", typeof(TEntity).Name, entity.Id);
            throw;
        }
    }

    public virtual Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        try
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            _dbSet.UpdateRange(entities);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating range of {EntityType} entities", typeof(TEntity).Name);
            throw;
        }
    }

    public virtual Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _dbSet.Remove(entity);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting {EntityType} entity with ID {Id}", typeof(TEntity).Name, entity.Id);
            throw;
        }
    }

    public virtual async Task DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await GetByIdAsync(id, cancellationToken);
            if (entity != null)
            {
                await DeleteAsync(entity, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting {EntityType} entity by ID {Id}", typeof(TEntity).Name, id);
            throw;
        }
    }

    public virtual Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        try
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            _dbSet.RemoveRange(entities);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting range of {EntityType} entities", typeof(TEntity).Name);
            throw;
        }
    }

    public virtual async Task SoftDeleteAsync(Guid id, string? deletedBy = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await GetByIdAsync(id, cancellationToken);
            if (entity != null)
            {
                entity.IsDeleted = true;
                entity.DeletedAt = DateTime.UtcNow;
                entity.DeletedBy = deletedBy;
                await UpdateAsync(entity, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error soft deleting {EntityType} entity with ID {Id}", typeof(TEntity).Name, id);
            throw;
        }
    }

    public virtual async Task RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            // Need to temporarily disable the global query filter to find soft-deleted entities
            var entity = await _dbSet.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
            if (entity != null && entity.IsDeleted)
            {
                entity.IsDeleted = false;
                entity.DeletedAt = null;
                entity.DeletedBy = null;
                await UpdateAsync(entity, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring {EntityType} entity with ID {Id}", typeof(TEntity).Name, id);
            throw;
        }
    }
}