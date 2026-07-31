using BuildingBlocks.Domain;

namespace BuildingBlocks.Application;

/// <summary>
/// Dispatches a committed domain event to its in-context projection handlers and to the integration-event path.
/// </summary>
/// <remarks>
/// This is the contract of the outbox-backed publisher: it is invoked once per domain event delivered by the
/// messaging transport's transactional outbox after the write transaction that produced the event has committed, and
/// it fans the event out to the registered <see cref="IProjectionHandler{TDomainEvent}"/>s and, via the
/// <see cref="IIntegrationEventMapper"/>s, to the <see cref="IIntegrationEventSink"/> supplied by the caller — the
/// sink is bound to the transaction of the message being handled, so integration events are only released when the
/// handling succeeds. Delivery is at-least-once, so everything downstream
/// must be idempotent. Only the contract lives here; the implementation and its outbox wiring reside in
/// <c>BuildingBlocks.Infrastructure</c>.
/// </remarks>
public interface IDomainEventPublisher
{
    /// <summary>
    /// Publishes a committed domain event to the in-context projection handlers and the integration-event path.
    /// </summary>
    /// <param name="domainEvent">The committed domain event to publish.</param>
    /// <param name="integrationEventSink">The sink, bound to the current message-handling transaction, that carries mapped integration events to the broker.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
    /// <returns>A task that represents the asynchronous publish operation.</returns>
    Task PublishAsync(IDomainEvent domainEvent, IIntegrationEventSink integrationEventSink, CancellationToken cancellationToken);
}
