using BuildingBlocks.Domain;

namespace BuildingBlocks.Application;

/// <summary>
/// Represents a repository for event-sourced aggregates whose state is derived from an append-only event stream.
/// </summary>
/// <remarks>
/// Loading fetches the aggregate's event stream and rehydrates the aggregate by replaying the history through
/// <see cref="IEventSourcedAggregateRoot{TKey}.LoadFromHistory"/>. Saving appends the aggregate's uncommitted domain
/// events to the stream using expected-version optimistic concurrency asserted against the aggregate's
/// <see cref="IEventSourcedAggregateRoot{TKey}.Version"/>; a concurrency conflict is translated by the
/// implementation into a <see cref="Failure"/> with category <see cref="FailureCategory.Conflict"/> rather than
/// surfacing a store-specific exception. Snapshotting is deliberately deferred and can be introduced later as a
/// purely additive change. Only the contract lives here; the event-store-backed implementation resides in
/// <c>BuildingBlocks.Infrastructure</c>.
/// </remarks>
/// <typeparam name="TAggregate">The type of the event-sourced aggregate root.</typeparam>
/// <typeparam name="TKey">The type of the aggregate root's identity key.</typeparam>
public interface IEventSourcedRepository<TAggregate, in TKey>
    where TAggregate : class, IEventSourcedAggregateRoot<TKey>
    where TKey : struct, IEntityKey
{
    /// <summary>
    /// Loads the aggregate with the specified identifier by replaying its event stream.
    /// </summary>
    /// <param name="id">The unique identifier of the aggregate to load.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
    /// <returns>A task whose result is the rehydrated aggregate, or <see langword="null"/> when no event stream exists for the specified identifier.</returns>
    Task<TAggregate?> GetByIdAsync(TKey id, CancellationToken cancellationToken);

    /// <summary>
    /// Appends the aggregate's uncommitted domain events to its event stream.
    /// </summary>
    /// <remarks>
    /// The append asserts expected-version optimistic concurrency against the aggregate's
    /// <see cref="IEventSourcedAggregateRoot{TKey}.Version"/>; the events become durable when the surrounding
    /// <see cref="IUnitOfWork"/> commits.
    /// </remarks>
    /// <param name="aggregate">The aggregate whose uncommitted domain events are appended.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    Task SaveAsync(TAggregate aggregate, CancellationToken cancellationToken);
}
