using BuildingBlocks.Domain.Events;

namespace BuildingBlocks.EventProcessing.Outbox;

/// <summary>
/// Persists domain events to the outbox so they can be published reliably
/// within the same transaction as the originating state change.
/// </summary>
public interface IOutboxWriter
{
    /// <summary>Writes the supplied domain events to the outbox.</summary>
    /// <param name="domainEvents">The events to persist.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task WriteAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default);
}