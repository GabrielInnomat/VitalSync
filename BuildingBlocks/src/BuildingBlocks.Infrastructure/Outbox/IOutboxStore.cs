namespace BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Read/mark access to the transactional outbox in a bounded context's write database, used by the drain loop.
/// </summary>
/// <remarks>
/// This is Infrastructure-internal plumbing, not a use-case contract (ADR-0024): services never consume it directly.
/// Writing outbox messages is deliberately <b>not</b> part of this contract — messages are written by the unit-of-work
/// implementations inside the write transaction so the outbox write stays atomic with the state change. Implementations
/// exist per persistence style (EF Core and Marten) and operate on the context's write database only.
/// </remarks>
public interface IOutboxStore
{
    /// <summary>
    /// Retrieves the oldest pending outbox messages that are due for dispatch, in dispatch order.
    /// </summary>
    /// <param name="batchSize">The maximum number of messages to retrieve.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
    /// <returns>A task whose result is the pending messages ordered by <see cref="OutboxMessage.Id"/>.</returns>
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken);

    /// <summary>
    /// Marks a message as successfully dispatched so it is never delivered again.
    /// </summary>
    /// <param name="message">The message to mark as processed.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
    /// <returns>A task that represents the asynchronous mark operation.</returns>
    Task MarkProcessedAsync(OutboxMessage message, CancellationToken cancellationToken);

    /// <summary>
    /// Records a failed dispatch attempt so the message is retried later with a backoff.
    /// </summary>
    /// <param name="message">The message whose dispatch attempt failed.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
    /// <returns>A task that represents the asynchronous mark operation.</returns>
    Task MarkFailedAsync(OutboxMessage message, CancellationToken cancellationToken);
}
