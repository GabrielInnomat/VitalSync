using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// EF Core-backed <see cref="IOutboxStore"/> for state-stored bounded contexts.
/// </summary>
/// <remarks>
/// Operates on the context's write database through the same <see cref="DbContext"/> the unit of work uses, so the
/// <see cref="OutboxMessage"/> entity must be mapped there via
/// <see cref="OutboxModelBuilderExtensions.AddOutboxMessages"/>. Marking a message processed or failed saves
/// immediately, because the drain loop runs outside any command's unit of work.
/// </remarks>
/// <param name="context">The write-database context that maps the outbox messages.</param>
public sealed class EfCoreOutboxStore(DbContext context) : IOutboxStore
{
    /// <inheritdoc/>
    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return await context.Set<OutboxMessage>()
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
        return context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task MarkFailedAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        message.Attempts++;
        message.NextAttemptAt = DateTimeOffset.UtcNow + OutboxRetryPolicy.GetBackoff(message.Attempts);
        return context.SaveChangesAsync(cancellationToken);
    }
}
