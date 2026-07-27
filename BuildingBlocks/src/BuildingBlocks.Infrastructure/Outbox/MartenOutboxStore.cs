using Marten;

namespace BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Marten-backed <see cref="IOutboxStore"/> for event-sourced bounded contexts.
/// </summary>
/// <remarks>
/// Outbox messages are stored as Marten documents in the same write database (and the same session/transaction) as the
/// event streams, keeping the outbox write atomic with the stream append. Marking a message processed or failed saves
/// immediately, because the drain loop runs outside any command's unit of work.
/// </remarks>
/// <param name="session">The Marten session bound to the context's write database.</param>
public sealed class MartenOutboxStore(IDocumentSession session) : IOutboxStore
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return await session.Query<OutboxMessage>()
            .Where(message => message.ProcessedAt == null
                && (message.NextAttemptAt == null || message.NextAttemptAt <= now))
            .OrderBy(message => message.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task MarkProcessedAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.ProcessedAt = DateTimeOffset.UtcNow;
        session.Store(message);
        return session.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task MarkFailedAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.Attempts++;
        message.NextAttemptAt = DateTimeOffset.UtcNow + OutboxRetryPolicy.GetBackoff(message.Attempts);
        session.Store(message);
        return session.SaveChangesAsync(cancellationToken);
    }
}
