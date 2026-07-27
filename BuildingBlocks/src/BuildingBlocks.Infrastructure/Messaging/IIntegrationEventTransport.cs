using BuildingBlocks.Application;

namespace BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// Transport that carries integration events from the publisher to the message broker.
/// </summary>
/// <remarks>
/// This is Infrastructure-internal plumbing, not a use-case contract (ADR-0024): services publish nothing directly —
/// the outbox-backed publisher hands mapped integration events to this transport. The Wolverine/RabbitMQ
/// implementation is the production default (ADR-0023); a no-op implementation backs hosts that have not enabled
/// messaging.
/// </remarks>
public interface IIntegrationEventTransport
{
    /// <summary>
    /// Publishes an integration event to the message broker.
    /// </summary>
    /// <param name="integrationEvent">The integration event to publish.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
    /// <returns>A task that represents the asynchronous publish operation.</returns>
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}
