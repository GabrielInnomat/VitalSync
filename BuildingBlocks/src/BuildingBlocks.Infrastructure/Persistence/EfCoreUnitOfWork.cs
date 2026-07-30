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
/// the host must therefore register <typeparamref name="TContext"/> via <c>AddDbContextWithWolverineIntegration</c>
/// and apply <see cref="WolverineOptionsExtensions.ApplyBuildingBlockEfCoreOutbox"/>. After a successful save the
/// aggregates' event collections are cleared. It is owned by the unit-of-work pipeline behavior — command handlers
/// never commit themselves.
/// </remarks>
/// <typeparam name="TContext">The write-database context type of the bounded context.</typeparam>
/// <param name="outbox">The Wolverine outbox bound to the write-database context whose tracked changes are committed.</param>
/// <param name="clock">The clock supplying the commit time stamped onto each domain event.</param>
public sealed class EfCoreUnitOfWork<TContext>(IDbContextOutbox<TContext> outbox, IClock clock) : IUnitOfWork
    where TContext : DbContext
{
    /// <inheritdoc/>
    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        var aggregates = outbox.DbContext.ChangeTracker.Entries()
            .Select(entry => entry.Entity)
            .OfType<IDomainEventsManager>()
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        var occurredAt = clock.Now;

        foreach (var aggregate in aggregates)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                var stamped = DomainEventStamper.Stamp(domainEvent, occurredAt);
                await outbox.PublishAsync(DomainEventEnvelopeSerializer.Wrap(stamped)).ConfigureAwait(false);
            }
        }

        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }
    }
}
