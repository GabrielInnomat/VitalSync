using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.Outbox;
using Marten;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Marten-backed repository for event-sourced aggregates, using Marten as a raw stream store (ADR-0019).
/// </summary>
/// <remarks>
/// Loading fetches the raw event stream and folds it through the aggregate's own
/// <see cref="IEventSourcedAggregateRoot{TKey}.LoadFromHistory"/> — Marten's convention-based
/// <c>Apply</c>-on-aggregate aggregation is never used, so the domain (ADR-0010/0012) stays untouched. Saving stages
/// an append of the uncommitted domain events with expected-version optimistic concurrency asserted against the
/// aggregate's <see cref="IEventSourcedAggregateRoot{TKey}.Version"/>, writes the matching outbox documents into the
/// same session, and registers the aggregate with the <see cref="MartenAggregateTracker"/>; everything becomes
/// durable atomically when the <see cref="MartenUnitOfWork"/> commits. Snapshotting is deferred (ADR-0019) and can be
/// added later as a purely additive change.
/// </remarks>
/// <typeparam name="TAggregate">The type of the event-sourced aggregate root.</typeparam>
/// <typeparam name="TKey">The type of the aggregate root's identity key.</typeparam>
/// <param name="session">The Marten session bound to the context's write database.</param>
/// <param name="tracker">The tracker that defers clearing the aggregate's events until commit.</param>
public sealed class MartenEventSourcedRepository<TAggregate, TKey>(IDocumentSession session, MartenAggregateTracker tracker)
    : IEventSourcedRepository<TAggregate, TKey>
    where TAggregate : class, IEventSourcedAggregateRoot<TKey>, new()
    where TKey : struct, IEntityKey
{
    /// <inheritdoc/>
    public async Task<TAggregate?> GetByIdAsync(TKey id, CancellationToken cancellationToken)
    {
        var streamKey = EntityKeyFormatter.GetStreamKey(typeof(TAggregate), id);
        var stream = await session.Events.FetchStreamAsync(streamKey, token: cancellationToken).ConfigureAwait(false);

        if (stream is not { Count: > 0 })
        {
            return null;
        }

        var aggregate = new TAggregate();
        ((IEventSourcedAggregateRoot<TKey>)aggregate).LoadFromHistory(stream.Select(@event => (IDomainEvent)@event.Data));
        return aggregate;
    }

    /// <inheritdoc/>
    public Task SaveAsync(TAggregate aggregate, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        var eventSourced = (IEventSourcedAggregateRoot<TKey>)aggregate;
        var uncommittedEvents = aggregate.DomainEvents;

        if (uncommittedEvents.Count == 0)
        {
            return Task.CompletedTask;
        }

        var streamKey = EntityKeyFormatter.GetStreamKey(typeof(TAggregate), aggregate.Id);
        session.Events.Append(streamKey, eventSourced.Version, uncommittedEvents);

        var basePosition = eventSourced.Version - uncommittedEvents.Count;
        var messages = OutboxMessageFactory.CreateMessages(streamKey, uncommittedEvents, basePosition);
        foreach (var message in messages)
        {
            session.Store(message);
        }

        tracker.Track((IDomainEventsManager)aggregate);
        return Task.CompletedTask;
    }
}
