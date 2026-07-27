using BuildingBlocks.Application;

namespace BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// The single Wolverine message handler that unwraps a <see cref="DomainEventEnvelope"/> and forwards it to the
/// domain-event publisher.
/// </summary>
/// <remarks>
/// This is the only Wolverine handler this package registers (ADR-0023): every domain event, regardless of its
/// concrete type or which bounded context raised it, is delivered here once Wolverine's transactional outbox
/// dispatches the enrolled <see cref="DomainEventEnvelope"/> after the write transaction commits. From here it flows
/// through the unchanged <see cref="IDomainEventPublisher"/> to in-context projection handlers and the
/// integration-event path. Discovered via <see cref="WolverineOptionsExtensions.ApplyBuildingBlockMessagingDefaults"/>,
/// which adds this package's assembly to Wolverine's handler discovery regardless of which service hosts it.
/// </remarks>
/// <param name="publisher">The publisher that fans the unwrapped domain event out to its handlers.</param>
public sealed class DomainEventEnvelopeHandler(IDomainEventPublisher publisher)
{
    /// <summary>
    /// Unwraps the envelope and publishes the domain event it carries.
    /// </summary>
    /// <param name="envelope">The envelope delivered by Wolverine's transactional outbox.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
    /// <returns>A task that represents the asynchronous handling operation.</returns>
    public Task Handle(DomainEventEnvelope envelope, CancellationToken cancellationToken)
    {
        var domainEvent = DomainEventEnvelopeSerializer.Unwrap(envelope);
        return publisher.PublishAsync(domainEvent, cancellationToken);
    }
}
