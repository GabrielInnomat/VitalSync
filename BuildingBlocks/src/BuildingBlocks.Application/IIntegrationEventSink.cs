namespace BuildingBlocks.Application;

/// <summary>
/// Publishes integration events within the transactional context of the message currently being processed.
/// </summary>
/// <remarks>
/// This is the outbound half of the integration-event path (ADR-0022/0023): the outbox-backed publisher hands every
/// mapped integration event to the sink it received from its caller, and the caller binds the sink to the transaction
/// of the message being handled — making the binding visible in the type system instead of relying on a
/// container-resolved bus. Publishing through the sink is therefore held back until the surrounding message handling
/// succeeds, so a failed handler never leaks integration events across the context boundary, and correlation flows
/// onto the published events. The contract lives here because <see cref="IDomainEventPublisher"/> consumes it
/// (ADR-0024); all implementations reside in <c>BuildingBlocks.Infrastructure</c>.
/// </remarks>
public interface IIntegrationEventSink
{
    /// <summary>
    /// Publishes an integration event bound to the current message-handling transaction.
    /// </summary>
    /// <param name="integrationEvent">The integration event to publish.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
    /// <returns>A task that represents the asynchronous publish operation.</returns>
    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}
