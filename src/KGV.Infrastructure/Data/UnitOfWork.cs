using KGV.Application.Common.Interfaces;
using KGV.Domain.Entities;
using KGV.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace KGV.Infrastructure.Data;

/// <summary>
/// Unit of Work implementation using Entity Framework
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly KgvDbContext _context;
    private readonly ILogger<UnitOfWork> _logger;
    private IDbContextTransaction? _transaction;
    private bool _disposed = false;

    // Repository properties
    private IRepository<Antrag>? _antraege;
    private IRepository<Bezirk>? _bezirke;
    private IRepository<Katasterbezirk>? _katasterbezirke;
    private IRepository<AktenzeichenEntity>? _aktenzeichen;
    private IRepository<Eingangsnummer>? _eingangsnummern;
    private IRepository<Person>? _personen;
    private IRepository<Verlauf>? _verlaufe;

    public UnitOfWork(KgvDbContext context, ILogger<UnitOfWork> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IRepository<Antrag> Antraege
    {
        get
        {
            _antraege ??= new Repository<Antrag>(_context, 
                _logger.CreateLogger<Repository<Antrag>>());
            return _antraege;
        }
    }

    public IRepository<Bezirk> Bezirke
    {
        get
        {
            _bezirke ??= new Repository<Bezirk>(_context, 
                _logger.CreateLogger<Repository<Bezirk>>());
            return _bezirke;
        }
    }

    public IRepository<Katasterbezirk> Katasterbezirke
    {
        get
        {
            _katasterbezirke ??= new Repository<Katasterbezirk>(_context, 
                _logger.CreateLogger<Repository<Katasterbezirk>>());
            return _katasterbezirke;
        }
    }

    public IRepository<AktenzeichenEntity> Aktenzeichen
    {
        get
        {
            _aktenzeichen ??= new Repository<AktenzeichenEntity>(_context, 
                _logger.CreateLogger<Repository<AktenzeichenEntity>>());
            return _aktenzeichen;
        }
    }

    public IRepository<Eingangsnummer> Eingangsnummern
    {
        get
        {
            _eingangsnummern ??= new Repository<Eingangsnummer>(_context, 
                _logger.CreateLogger<Repository<Eingangsnummer>>());
            return _eingangsnummern;
        }
    }

    public IRepository<Person> Personen
    {
        get
        {
            _personen ??= new Repository<Person>(_context, 
                _logger.CreateLogger<Repository<Person>>());
            return _personen;
        }
    }

    public IRepository<Verlauf> Verlaufe
    {
        get
        {
            _verlaufe ??= new Repository<Verlauf>(_context, 
                _logger.CreateLogger<Repository<Verlauf>>());
            return _verlaufe;
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Saved {ChangeCount} changes to database", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving changes to database");
            throw;
        }
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("A transaction is already in progress");
            }

            _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            _logger.LogDebug("Database transaction started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error beginning database transaction");
            throw;
        }
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No transaction in progress");
            }

            await _transaction.CommitAsync(cancellationToken);
            _logger.LogDebug("Database transaction committed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error committing database transaction");
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync(cancellationToken);
                _logger.LogDebug("Database transaction rolled back");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back database transaction");
            throw;
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _transaction?.Dispose();
                _context.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}