using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.Messaging;
using Marten;
using Wolverine.Marten;

namespace BuildingBlocks.Infrastructure.Persistence;

public sealed class MartenUnitOfWork(IDocumentSession session, MartenAggregateTracker tracker, IMartenOutbox outbox, IClock clock) : IUnitOfWork
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

            session.Events.Append(entry.StreamKey(), entry.ExpectedVersion(), uncommittedEvents);

            foreach (var domainEvent in uncommittedEvents)
            {
                await outbox.PublishAsync(DomainEventEnvelopeSerializer.Wrap(domainEvent, Guid.NewGuid(), occurredAt)).ConfigureAwait(false);
            }
        }

        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        tracker.ClearDomainEvents();
    }
}
