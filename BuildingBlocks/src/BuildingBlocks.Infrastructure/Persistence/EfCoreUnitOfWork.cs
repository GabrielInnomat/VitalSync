using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.Events;
using BuildingBlocks.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Wolverine.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// EF Core-backed unit of work for state-stored bounded contexts.
/// </summary>
/// <remarks>
/// On commit, every tracked aggregate's uncommitted domain events are stamped with the commit time (a single
/// <see cref="IClock.Now"/> value shared by the whole transaction; see <see cref="DomainEventStamper"/>), wrapped in a
/// <see cref="DomainEventEnvelope"/> and enrolled in Wolverine's transactional outbox via <see cref="IDbContextOutbox{TContext}"/>; calling
/// <see cref="IDbContextOutbox{TContext}.SaveChangesAndFlushMessagesAsync(CancellationToken)"/> then persists the
/// aggregate changes and the outbox entries atomically in a single write-database transaction (ADR-0022, ADR-0023) —
/// <c>UseEfCorePersistence</c> registers <typeparamref name="TContext"/> via
/// <c>AddDbContextWithWolverineIntegration</c>, and the host supplies the message store and transactional
/// middleware through <see cref="DependencyInjection.WolverineHostExtensions.UseBuildingBlocksEfCorePersistence"/> —
/// the one piece of Wolverine wiring ADR-0027 cannot hide, because Wolverine 3.0 forbids a container-registered
/// extension from touching the service collection. After a successful save the
/// aggregates' event collections are cleared. It is owned by the unit-of-work pipeline behavior — command handlers
/// never commit themselves.
/// </remarks>
/// <typeparam name="TContext">The write-database context type of the bounded context.</typeparam>
/// <param name="outbox">The Wolverine outbox bound to the write-database context whose tracked changes are committed.</param>
/// <param name="tracker">The tracker holding the aggregates that took part in the current command.</param>
/// <param name="clock">The clock supplying the commit time stamped onto each domain event.</param>
public sealed class EfCoreUnitOfWork<TContext>(
    IDbContextOutbox<TContext> outbox,
    EfCoreAggregateTracker tracker,
    IClock clock) : IUnitOfWork
    where TContext : DbContext
{
    /// <inheritdoc/>
    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        var entries = tracker.Entries;

        // State objects are immutable, so every applied event replaced the aggregate's state and left the instance
        // EF Core tracks behind. Copying the current values over is what turns the fold into an UPDATE; without it
        // the change tracker would see nothing to save.
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
