using BuildingBlocks.Domain;

namespace BuildingBlocks.Application;

/// <summary>
/// Represents the single repository contract for aggregates, regardless of how they are persisted.
/// </summary>
/// <remarks>
/// The repository operates against the bounded context's write database only; queries never go through repositories,
/// because the read side reads its own read database directly. The surface is deliberately minimal: aggregates are
/// never hard-deleted (removal is modeled in the domain as a state change — a soft delete — and therefore flows as an
/// ordinary update), so there is no <c>Remove</c> method; and there is no <c>Update</c>/<c>Save</c> method, because
/// aggregates retrieved via <see cref="GetByIdAsync"/> are tracked and their changes flow through the
/// <see cref="IUnitOfWork"/> at commit time. This holds for both persistence styles: the EF Core implementation
/// relies on change tracking, and the event-store implementation appends the tracked aggregates' uncommitted events
/// at commit. Only the contract lives here; the implementations reside in <c>BuildingBlocks.Infrastructure</c>.
/// </remarks>
/// <typeparam name="TAggregate">The type of the aggregate root.</typeparam>
/// <typeparam name="TKey">The type of the aggregate root's identity key.</typeparam>
public interface IRepository<TAggregate, in TKey>
    where TAggregate : class, IAggregateRoot<TKey>
    where TKey : struct, IEntityKey
{
    /// <summary>
    /// Retrieves the aggregate with the specified identifier from the write database.
    /// </summary>
    /// <remarks>
    /// The returned aggregate is tracked: subsequent changes made to it are persisted when the surrounding
    /// <see cref="IUnitOfWork"/> commits, without any further repository call.
    /// </remarks>
    /// <param name="id">The unique identifier of the aggregate to retrieve.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
    /// <returns>A task whose result is the aggregate, or <see langword="null"/> when no aggregate with the specified identifier exists.</returns>
    Task<TAggregate?> GetByIdAsync(TKey id, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a new aggregate to the repository.
    /// </summary>
    /// <remarks>
    /// Call this exactly once for a newly created aggregate; the aggregate (its state or its uncommitted events,
    /// depending on the persistence style) is persisted when the surrounding <see cref="IUnitOfWork"/> commits.
    /// </remarks>
    /// <param name="aggregate">The aggregate to add.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
    /// <returns>A task that represents the asynchronous add operation.</returns>
    Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken);
}
