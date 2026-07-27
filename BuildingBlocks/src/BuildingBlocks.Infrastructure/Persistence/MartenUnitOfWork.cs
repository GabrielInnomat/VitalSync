using BuildingBlocks.Application;
using BuildingBlocks.Domain;
using BuildingBlocks.Infrastructure.Outbox;
using Marten;

namespace BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Marten-backed unit of work for event-sourced bounded contexts.
/// </summary>
/// <remarks>
/// Committing saves the session in a single write-database transaction, which persists both the stream appends staged
/// by the event-sourced repository and the outbox documents written alongside them (ADR-0019/0022). After the commit
/// the tracked aggregates' event collections are cleared and the drain loop is signalled for an immediate,
/// low-latency dispatch. A wrong expected version surfaces Marten's concurrency exception from the save, which the
/// unit-of-work pipeline behavior translates into a <see cref="FailureCategory.Conflict"/> failure. It is owned by
/// the unit-of-work pipeline behavior — command handlers never commit themselves.
/// </remarks>
/// <param name="session">The Marten session whose staged changes are committed.</param>
/// <param name="tracker">The tracker holding the aggregates saved in this unit of work.</param>
/// <param name="signal">The outbox signal notified after a successful commit.</param>
public sealed class MartenUnitOfWork(IDocumentSession session, MartenAggregateTracker tracker, OutboxSignal signal) : IUnitOfWork
{
    /// <inheritdoc/>
    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        tracker.ClearDomainEvents();
        signal.Notify();
    }
}
