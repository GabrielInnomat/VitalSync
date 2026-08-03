using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Wolverine.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Persistence;

public sealed class EfCoreUnitOfWork<TContext>(
    IDbContextOutbox<TContext> outbox,
    EfCoreAggregateTracker tracker,
    DomainEventEnvelopeSerializer serializer,
    IClock clock) : IUnitOfWork
    where TContext : DbContext
{
    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        var entries = tracker.Entries;

        foreach (var entry in entries)
        {
            outbox.DbContext.Entry(entry.PersistedState).CurrentValues.SetValues(entry.StateOwner.State);
        }

        var occurredAt = clock.Now;

        foreach (var entry in entries)
        {
            var domainEvents = entry.Aggregate.DomainEvents;
            var version = entry.StateOwner.Version - domainEvents.Count;

            foreach (var domainEvent in domainEvents)
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

        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken).ConfigureAwait(false);

        tracker.ClearDomainEvents();
    }
}
