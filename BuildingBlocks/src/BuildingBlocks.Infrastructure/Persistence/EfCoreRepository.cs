using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// EF Core-backed generic repository for state-stored aggregates.
/// </summary>
/// <remarks>
/// The repository works against the bounded context's write database only (ADR-0021) and deliberately offers no query
/// surface beyond <see cref="GetByIdAsync"/> — queries read the context's read database directly. There is no
/// <c>Update</c> method: retrieved aggregates are change-tracked and their modifications flow through the
/// <see cref="IUnitOfWork"/> when the unit-of-work behavior commits. Register it open-generically via
/// <c>UseEfCorePersistence</c> so each aggregate type resolves the same implementation.
/// </remarks>
/// <typeparam name="TAggregate">The type of the aggregate root.</typeparam>
/// <typeparam name="TKey">The type of the aggregate root's identity key.</typeparam>
/// <param name="context">The write-database context the repository operates on.</param>
public sealed class EfCoreRepository<TAggregate, TKey>(DbContext context) : IRepository<TAggregate, TKey>
    where TAggregate : AggregateRoot<TKey>
    where TKey : struct, IEntityKey
{
    /// <inheritdoc/>
    public async Task<TAggregate?> GetByIdAsync(TKey id, CancellationToken cancellationToken) =>
        await context.Set<TAggregate>().FindAsync([id], cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken) =>
        await context.Set<TAggregate>().AddAsync(aggregate, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public void Remove(TAggregate aggregate) =>
        context.Set<TAggregate>().Remove(aggregate);
}
