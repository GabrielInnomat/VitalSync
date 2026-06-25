using BuildingBlocks.Domain.Events;

namespace BuildingBlocks.EventProcessing;

/// <summary>
/// Dispatches domain events to their registered handlers. Implementations
/// resolve the appropriate <see cref="IDomainEventHandler{TDomainEvent}"/>(s).
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>Dispatches a batch of domain events.</summary>
    /// <param name="domainEvents">The events to dispatch.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task DispatchAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default);
}