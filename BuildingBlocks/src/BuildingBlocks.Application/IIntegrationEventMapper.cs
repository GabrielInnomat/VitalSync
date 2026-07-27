using BuildingBlocks.Domain;

namespace BuildingBlocks.Application;

/// <summary>
/// Translates a domain event into the integration events that must be published across bounded-context boundaries.
/// </summary>
/// <remarks>
/// Each service owns its translation maps: implement this contract per service to select which domain events leave the
/// context and what shape they take on the broker, keeping domain events strictly internal. The outbox-backed
/// publisher in <c>BuildingBlocks.Infrastructure</c> invokes every registered mapper after the write transaction has
/// committed and publishes the returned integration events via the messaging transport. Return an empty collection
/// for domain events that carry no cross-context significance.
/// </remarks>
public interface IIntegrationEventMapper
{
    /// <summary>
    /// Maps the specified domain event to the integration events to publish across context boundaries.
    /// </summary>
    /// <param name="domainEvent">The domain event to translate.</param>
    /// <returns>The integration events to publish, or an empty collection when the domain event stays internal.</returns>
    IReadOnlyCollection<IIntegrationEvent> Map(IDomainEvent domainEvent);
}
