using BuildingBlocks.Domain.Events;

namespace BuildingBlocks.EventProcessing;

/// <summary>Handles a domain event of type <typeparamref name="TDomainEvent"/>.</summary>
/// <typeparam name="TDomainEvent">The domain event type.</typeparam>
public interface IDomainEventHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    /// <summary>Handles the supplied domain event.</summary>
    /// <param name="domainEvent">The event to handle.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken = default);
}