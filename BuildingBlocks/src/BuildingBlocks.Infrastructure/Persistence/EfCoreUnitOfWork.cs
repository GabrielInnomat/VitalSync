using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Wolverine.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// EF Core-backed unit of work for state-stored bounded contexts.
/// </summary>
/// <remarks>
/// On commit, every tracked aggregate's uncommitted domain events are wrapped in a <see cref="DomainEventEnvelope"/>
/// and enrolled in Wolverine's transactional outbox via <see cref="IDbContextOutbox{TContext}"/>; calling
/// <see cref="IDbContextOutbox{TContext}.SaveChangesAndFlushMessagesAsync(CancellationToken)"/> then persists the
/// aggregate changes and the outbox entries atomically in a single write-database transaction (ADR-0022, ADR-0023) —
/// the host must therefore register <typeparamref name="TContext"/> via <c>AddDbContextWithWolverineIntegration</c>
/// and apply <see cref="WolverineOptionsExtensions.ApplyBuildingBlockEfCoreOutbox"/>. After a successful save the
/// aggregates' event collections are cleared. It is owned by the unit-of-work pipeline behavior — command handlers
/// never commit themselves.
/// </remarks>
/// <typeparam name="TContext">The write-database context type of the bounded context.</typeparam>
/// <param name="outbox">The Wolverine outbox bound to the write-database context whose tracked changes are committed.</param>
public sealed class EfCoreUnitOfWork<TContext>(IDbContextOutbox<TContext> outbox) : IUnitOfWork
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

        foreach (var aggregate in aggregates)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                await outbox.PublishAsync(DomainEventEnvelopeSerializer.Wrap(domainEvent)).ConfigureAwait(false);
            }
        }

        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }
    }
}
