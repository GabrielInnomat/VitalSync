using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.Events;
using BuildingBlocks.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Wolverine.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Persistence;

public sealed class EfCoreUnitOfWork<TContext>(
    IDbContextOutbox<TContext> outbox,
    EfCoreAggregateTracker tracker,
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
            foreach (var domainEvent in entry.Aggregate.DomainEvents)
            {
                var stamped = DomainEventStamper.Stamp(domainEvent, occurredAt);
                await outbox.PublishAsync(DomainEventEnvelopeSerializer.Wrap(stamped)).ConfigureAwait(false);
            }
        }

        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken).ConfigureAwait(false);

        tracker.ClearDomainEvents();
    }
}
