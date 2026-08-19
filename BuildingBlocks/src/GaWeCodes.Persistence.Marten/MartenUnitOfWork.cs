using GaWeCodes.Application.Persistence;
using GaWeCodes.Core.Persistence;
using GaWeCodes.Domain.Naming;
using Marten;
using Wolverine.Marten;

namespace GaWeCodes.Persistence.Marten;

internal sealed class MartenUnitOfWork(
    IDocumentSession session,
    MartenAggregateTracker tracker,
    IMartenOutbox outbox,
    DomainEventEnvelopeFactory envelopeFactory) : IUnitOfWork
{
    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        outbox.Enroll(session);

        foreach (var entry in tracker.Entries)
        {
            var uncommittedEvents = entry.Aggregate.DomainEvents;

            if (uncommittedEvents.Count == 0)
            {
                continue;
            }

            var streamKey = EntityKeyFormatter.GetStreamKey(entry.AggregateName, entry.AggregateId);

            session.Events.Append(streamKey, entry.CurrentVersion, uncommittedEvents);
        }

        foreach (var envelope in envelopeFactory.WrapUncommitted(tracker.Entries))
        {
            await outbox.PublishAsync(envelope).ConfigureAwait(false);
        }

        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        tracker.ClearDomainEvents();
    }
}
