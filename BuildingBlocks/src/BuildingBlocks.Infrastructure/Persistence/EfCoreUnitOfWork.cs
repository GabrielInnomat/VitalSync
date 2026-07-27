using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// EF Core-backed unit of work for state-stored bounded contexts.
/// </summary>
/// <remarks>
/// On commit, in a single write-database transaction, the unit of work persists the tracked aggregate changes,
/// collects the tracked aggregates' uncommitted domain events, and writes them to the transactional outbox atomically
/// with the state change (ADR-0022); the context's model must therefore map the outbox via
/// <see cref="OutboxModelBuilderExtensions.AddOutboxMessages"/>. After a successful save the aggregates' event
/// collections are cleared and the drain loop is signalled for an immediate, low-latency dispatch. It is owned by the
/// unit-of-work pipeline behavior — command handlers never commit themselves.
/// </remarks>
/// <param name="context">The write-database context whose tracked changes are committed.</param>
/// <param name="signal">The outbox signal notified after a successful commit.</param>
public sealed class EfCoreUnitOfWork(DbContext context, OutboxSignal signal) : IUnitOfWork
{
    /// <inheritdoc/>
    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        var aggregates = context.ChangeTracker.Entries()
            .Select(entry => entry.Entity)
            .OfType<IDomainEventsManager>()
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        foreach (var aggregate in aggregates)
        {
            var streamId = EntityKeyFormatter.GetStreamKeyForAggregate(aggregate);
            var messages = OutboxMessageFactory.CreateMessages(streamId, aggregate.DomainEvents, basePosition: null);
            context.Set<OutboxMessage>().AddRange(messages);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }

        signal.Notify();
    }
}
