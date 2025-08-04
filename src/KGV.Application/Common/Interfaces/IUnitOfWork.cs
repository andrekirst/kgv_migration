using KGV.Domain.Entities;

namespace KGV.Application.Common.Interfaces;

/// <summary>
/// Unit of Work pattern interface for coordinating repository operations
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Antrag repository
    /// </summary>
    IRepository<Antrag> Antraege { get; }

    /// <summary>
    /// Bezirk repository
    /// </summary>
    IRepository<Bezirk> Bezirke { get; }

    /// <summary>
    /// Katasterbezirk repository
    /// </summary>
    IRepository<Katasterbezirk> Katasterbezirke { get; }

    /// <summary>
    /// AktenzeichenEntity repository
    /// </summary>
    IRepository<AktenzeichenEntity> Aktenzeichen { get; }

    /// <summary>
    /// Eingangsnummer repository
    /// </summary>
    IRepository<Eingangsnummer> Eingangsnummern { get; }

    /// <summary>
    /// Person repository
    /// </summary>
    IRepository<Person> Personen { get; }

    /// <summary>
    /// Verlauf repository
    /// </summary>
    IRepository<Verlauf> Verlaufe { get; }

    /// <summary>
    /// Saves all pending changes
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a database transaction
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}