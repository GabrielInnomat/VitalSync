using BuildingBlocks.Application;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Drains one batch of pending outbox messages and dispatches each to the domain-event publisher.
/// </summary>
/// <remarks>
/// The drainer is the per-scope work unit of the <see cref="OutboxProcessor"/>: it reads pending messages in dispatch
/// order, rehydrates each domain event, and hands it to the <see cref="IDomainEventPublisher"/>; a message is marked
/// processed only after the publisher succeeds, so delivery is at-least-once. Per-aggregate ordering is preserved by
/// processing in sequence order and, after a failure, skipping the remaining messages of the failed stream in the
/// same pass — other streams keep flowing while the failed one waits for its retry backoff.
/// </remarks>
/// <param name="store">The outbox store of the context's write database.</param>
/// <param name="publisher">The publisher that dispatches each event to projections and the integration-event path.</param>
/// <param name="logger">The logger for dispatch failures.</param>
public sealed class OutboxDrainer(IOutboxStore store, IDomainEventPublisher publisher, ILogger<OutboxDrainer> logger)
{
    /// <summary>
    /// The maximum number of messages read per drain pass.
    /// </summary>
    public const int BatchSize = 100;

    private static readonly Action<ILogger, long, string, Exception> DispatchFailedMessage =
        LoggerMessage.Define<long, string>(
            LogLevel.Error,
            new EventId(1, "OutboxDispatchFailed"),
            "Dispatching outbox message {OutboxMessageId} of stream {StreamId} failed; it will be retried");

    /// <summary>
    /// Processes one batch of pending outbox messages.
    /// </summary>
    /// <remarks>
    /// Call repeatedly until it returns <c>0</c> to fully drain the outbox.
    /// </remarks>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
    /// <returns>A task whose result is the number of messages successfully dispatched in this pass.</returns>
    public async Task<int> DrainAsync(CancellationToken cancellationToken)
    {
        var batch = await store.GetPendingAsync(BatchSize, cancellationToken).ConfigureAwait(false);
        if (batch.Count == 0)
        {
            return 0;
        }

        var processed = 0;
        var failedStreams = new HashSet<string>(StringComparer.Ordinal);

        foreach (var message in batch)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (failedStreams.Contains(message.StreamId))
            {
                continue;
            }

            try
            {
                var domainEvent = OutboxMessageSerializer.Deserialize(message);
                var streamPosition = message.StreamPosition == 0 ? message.Id : message.StreamPosition;
                await publisher.PublishAsync(domainEvent, streamPosition, cancellationToken).ConfigureAwait(false);
                await store.MarkProcessedAsync(message, cancellationToken).ConfigureAwait(false);
                processed++;
            }
#pragma warning disable CA1031 // At-least-once delivery: any handler failure must be caught so the message is retried.
            catch (Exception exception) when (exception is not OperationCanceledException)
#pragma warning restore CA1031
            {
                failedStreams.Add(message.StreamId);
                DispatchFailedMessage(logger, message.Id, message.StreamId, exception);
                await store.MarkFailedAsync(message, cancellationToken).ConfigureAwait(false);
            }
        }

        return processed;
    }
}
