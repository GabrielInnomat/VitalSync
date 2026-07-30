using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.Events;
using BuildingBlocks.Infrastructure.Messaging;
using Marten;
using Wolverine.Marten;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Marten-backed unit of work for event-sourced bounded contexts.
/// </summary>
/// <remarks>
/// Committing enrolls the session's <see cref="IMartenOutbox"/> for the current <see cref="IDocumentSession"/>, then,
/// for every aggregate tracked by the <see cref="MartenAggregateTracker"/>, stamps its uncommitted domain events with
/// the commit time (a single <see cref="IClock.Now"/> value shared by the whole transaction; see
/// <see cref="DomainEventStamper"/>), appends the stamped events to its stream with expected-version optimistic
/// concurrency — so replay rehydrates the real timestamps — wraps them in a <see cref="DomainEventEnvelope"/>, and
/// publishes them through the outbox; finally it saves the session — persisting the stream appends and the enrolled
/// outbox entries in the same write-database transaction (ADR-0019/0022/0023). After the commit the tracked
/// aggregates' event collections are cleared. A wrong expected version surfaces Marten's concurrency exception from
/// the save, which the unit-of-work pipeline behavior translates into a <see cref="FailureCategory.Conflict"/>
/// failure. It is owned by the unit-of-work pipeline behavior — command handlers never commit themselves.
/// </remarks>
/// <param name="session">The Marten session whose staged changes are committed.</param>
/// <param name="tracker">The tracker holding the aggregates loaded or added in this unit of work.</param>
/// <param name="outbox">The Wolverine outbox enrolled with the session to share its transaction.</param>
/// <param name="clock">The clock supplying the commit time stamped onto each domain event.</param>
public sealed class MartenUnitOfWork(IDocumentSession session, MartenAggregateTracker tracker, IMartenOutbox outbox, IClock clock) : IUnitOfWork
{
    /// <inheritdoc/>
    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        outbox.Enroll(session);

        var occurredAt = clock.Now;

        foreach (var entry in tracker.Entries)
        {
            var uncommittedEvents = entry.Aggregate.DomainEvents;

            if (uncommittedEvents.Count == 0)
            {
                continue;
            }

            var stampedEvents = uncommittedEvents
                .Select(domainEvent => DomainEventStamper.Stamp(domainEvent, occurredAt))
                .ToList();

            session.Events.Append(entry.StreamKey(), entry.ExpectedVersion(), stampedEvents);

            foreach (var stampedEvent in stampedEvents)
            {
                await outbox.PublishAsync(DomainEventEnvelopeSerializer.Wrap(stampedEvent)).ConfigureAwait(false);
            }
        }

        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        tracker.ClearDomainEvents();
    }
}
