using BuildingBlocks.Application;
using Wolverine;

namespace BuildingBlocks.Infrastructure.Messaging;

/// <summary>
/// The single Wolverine message handler that unwraps a <see cref="DomainEventEnvelope"/> and forwards it to the
/// domain-event publisher.
/// </summary>
/// <remarks>
/// This is the only Wolverine handler this package registers (ADR-0023): every domain event, regardless of its
/// concrete type or which bounded context raised it, is delivered here once Wolverine's transactional outbox
/// dispatches the enrolled <see cref="DomainEventEnvelope"/> after the write transaction commits. From here it flows
/// through the <see cref="IDomainEventPublisher"/> to in-context projection handlers and the integration-event path.
/// Wolverine injects the envelope's own <see cref="IMessageContext"/> as a handler parameter — the documented way to
/// obtain the context of the current message — and the handler binds the integration-event sink to it, so mapped
/// integration events enroll in this message's outbox (released only on success) and inherit its correlation.
/// Discovered via <see cref="WolverineOptionsExtensions.ApplyBuildingBlockDomainEventRouting"/>
/// (applied automatically by <see cref="BuildingBlocksWolverineExtension"/>),
/// which adds this package's assembly to Wolverine's handler discovery regardless of which service hosts it.
/// </remarks>
/// <param name="publisher">The publisher that fans the unwrapped domain event out to its handlers.</param>
/// <param name="sinkFactory">The factory that binds the integration-event sink to the handled message's context.</param>
public sealed class DomainEventEnvelopeHandler(IDomainEventPublisher publisher, IIntegrationEventSinkFactory sinkFactory)
{
    /// <summary>
    /// Unwraps the envelope and publishes the domain event it carries.
    /// </summary>
    /// <param name="envelope">The envelope delivered by Wolverine's transactional outbox.</param>
    /// <param name="context">The Wolverine context of this message, injected by Wolverine as a handler parameter.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
    /// <returns>A task that represents the asynchronous handling operation.</returns>
    public Task Handle(DomainEventEnvelope envelope, IMessageContext context, CancellationToken cancellationToken)
    {
        var domainEvent = DomainEventEnvelopeSerializer.Unwrap(envelope);
        return publisher.PublishAsync(domainEvent, sinkFactory.Create(context), cancellationToken);
    }
}
