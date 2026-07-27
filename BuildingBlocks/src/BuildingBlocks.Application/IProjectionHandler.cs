using BuildingBlocks.Domain;

namespace BuildingBlocks.Application;

/// <summary>
/// Handles a domain event to update a read model in the bounded context's read database.
/// </summary>
/// <remarks>
/// Projection handlers are invoked by the projection runner in <c>BuildingBlocks.Infrastructure</c> after the write
/// transaction that produced the event has committed; only the contract lives here. Delivery is at-least-once, so
/// idempotency is the handler's responsibility (typically an upsert by key): every domain event carries a stable
/// <see cref="IDomainEvent.EventId"/>, generated once when the event is raised, which a handler can record as a
/// last-processed marker to skip a redelivered event. Events of the same aggregate are delivered in order, but no
/// ordering is guaranteed across aggregates. Read models themselves are domain-shaped and belong to each service, not
/// to the Building Blocks.
/// </remarks>
/// <typeparam name="TDomainEvent">The type of the domain event the handler projects.</typeparam>
public interface IProjectionHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    /// <summary>
    /// Handles the domain event by updating the read model it projects.
    /// </summary>
    /// <param name="domainEvent">The domain event to project.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
    /// <returns>A task that represents the asynchronous projection operation.</returns>
    Task Handle(TDomainEvent domainEvent, CancellationToken cancellationToken);
}
