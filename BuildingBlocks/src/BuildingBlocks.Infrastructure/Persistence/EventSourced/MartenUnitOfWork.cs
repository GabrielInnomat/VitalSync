using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.Messaging.DomainEvents;
using BuildingBlocks.Infrastructure.Messaging;
using BuildingBlocks.Infrastructure.Persistence;
using Marten;
using Wolverine.Marten;

namespace BuildingBlocks.Infrastructure.Persistence.EventSourced;

internal sealed class MartenUnitOfWork(
    IDocumentSession session,
    MartenAggregateTracker tracker,
    IMartenOutbox outbox,
    DomainEventEnvelopeSerializer serializer,
    IClock clock) : IUnitOfWork
{
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

            var expectedVersion = entry.Version();
            var streamKey = EntityKeyFormatter.GetStreamKey(entry.AggregateName, entry.AggregateId);

            session.Events.Append(streamKey, expectedVersion, uncommittedEvents);

            var version = expectedVersion - uncommittedEvents.Count;

            foreach (var domainEvent in uncommittedEvents)
            {
                var envelope = serializer.Wrap(
                    domainEvent,
                    Guid.NewGuid(),
                    entry.AggregateName,
                    entry.AggregateId,
                    ++version,
                    occurredAt);

                await outbox.PublishAsync(envelope).ConfigureAwait(false);
            }
        }

        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        tracker.ClearDomainEvents();
    }
}
